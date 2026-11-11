using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Application.Services;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Extensions;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _config;
    private readonly IAuditLogService _auditLog;
    private readonly ILoginAttemptService _loginAttempts;
    private readonly ILogger<AuthController> _logger;

    public AuthController(AppDbContext context, IConfiguration config, IAuditLogService auditLog,
        ILoginAttemptService loginAttempts, ILogger<AuthController> logger)
    {
        _context = context;
        _config = config;
        _auditLog = auditLog;
        _loginAttempts = loginAttempts;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        // ---- Lockout: nếu tài khoản đang bị khóa sau quá nhiều lần sai, chặn sớm.
        // Trả 429 (không 401) để không bị interceptor "401 → về /login" xử lý nhầm.
        var lockKey = request.TenDangNhap ?? string.Empty;
        var remaining = _loginAttempts.GetLockoutRemaining(lockKey);
        if (remaining > TimeSpan.Zero)
        {
            var phút = Math.Ceiling(remaining.TotalMinutes);
            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.LOGIN_FAILED,
                tenThucThe: AuditEntities.NGUOI_DUNG,
                maThucThe: lockKey,
                giaTriMoi: $"Tài khoản đang bị khóa, thử lại sau ~{phút} phút");
            return StatusCode(429, new
            {
                message = $"Quá nhiều lần thử sai. Vui lòng thử lại sau {phút} phút."
            });
        }

        var user = await _context.NguoiDungs
            .Include(u => u.VaiTro)
            .Include(u => u.ChiNhanh)
            .FirstOrDefaultAsync(u => u.TenDangNhap == request.TenDangNhap);

        if (user == null)
        {
            _loginAttempts.RegisterFailure(lockKey);
            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.LOGIN_FAILED,
                tenThucThe: AuditEntities.NGUOI_DUNG,
                maThucThe: lockKey,
                giaTriMoi: "Tên đăng nhập không tồn tại");
            return Unauthorized(new { message = "Tên đăng nhập không tồn tại" });
        }

        if (string.IsNullOrWhiteSpace(user.MatKhauBam) || !BCrypt.Net.BCrypt.Verify(request.MatKhau, user.MatKhauBam))
        {
            _loginAttempts.RegisterFailure(lockKey);
            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.LOGIN_FAILED,
                tenThucThe: AuditEntities.NGUOI_DUNG,
                maThucThe: user.TenDangNhap,
                giaTriMoi: "Sai mật khẩu");
            return Unauthorized(new { message = "Mật khẩu không đúng" });
        }

        // Đăng nhập đúng → xoá bộ đếm lỗi để bắt đầu lại.
        _loginAttempts.Reset(lockKey);

        if (user.TrangThai != "HOAT_DONG")
        {
            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.LOGIN_FAILED,
                tenThucThe: AuditEntities.NGUOI_DUNG,
                maThucThe: user.TenDangNhap,
                giaTriMoi: "Tài khoản bị khóa");
            return Unauthorized(new { message = "Tài khoản đã bị khóa" });
        }

        // Update last login
        user.LanDangNhapCuoi = DateTime.Now;
        await _context.SaveChangesAsync();

        // Generate JWT
        var token = GenerateJwtToken(user.MaNguoiDung, user.TenDangNhap, user.VaiTro!.MaVaiTroHienThi, user.MaChiNhanh, "access");
        var refreshToken = GenerateJwtToken(user.MaNguoiDung, user.TenDangNhap, user.VaiTro!.MaVaiTroHienThi, user.MaChiNhanh, "refresh");

        var accessExpiry = _config.GetValue<int>("Jwt:AccessExpiryHours", 8);

        // Ghi log thành công
        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.LOGIN,
            tenThucThe: AuditEntities.NGUOI_DUNG,
            maThucThe: user.TenDangNhap);

        return Ok(new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = accessExpiry * 3600,
            User = new UserDto
            {
                MaNguoiDung = user.MaNguoiDung,
                TenDangNhap = user.TenDangNhap,
                HoTen = user.HoTen,
                Email = user.Email,
                MaVaiTro = user.VaiTro!.MaVaiTroHienThi,
                TenVaiTro = user.VaiTro!.TenVaiTro,
                MaChiNhanh = user.MaChiNhanh,
                TenChiNhanh = user.ChiNhanh!.TenChiNhanh
            }
        });
    }

    /// <summary>
    /// Đổi refresh token (dài hạn) lấy access token mới — không cần nhập mật khẩu
    /// lại. Refresh token phải có claim token_type=refresh.
    ///
    /// Trước đây access token 24h, không refresh → mỗi ngày user bị đá ra.
    /// Giờ access 8h, refresh 7 ngày: access hết hạn, app tự refresh nền.
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        // Xác thực refresh token: kiểm tra chữ ký, hạn, issuer/audience, và
        // claim token_type phải là "refresh" (access token không được dùng để refresh).
        try
        {
            var claims = ValidateRefreshToken(request.RefreshToken);

            var userIdClaim = claims.Claims
                .FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var uid))
                return Unauthorized(new { message = "Refresh token không hợp lệ" });

            var user = await _context.NguoiDungs
                .Include(u => u.VaiTro)
                .Include(u => u.ChiNhanh)
                .FirstOrDefaultAsync(u => u.MaNguoiDung == uid);

            if (user == null || user.TrangThai != "HOAT_DONG")
                return Unauthorized(new { message = "Tài khoản không hợp lệ hoặc đã bị khóa" });

            var accessExpiry = _config.GetValue<int>("Jwt:AccessExpiryHours", 8);
            var newToken = GenerateJwtToken(user.MaNguoiDung, user.TenDangNhap, user.VaiTro!.MaVaiTroHienThi, user.MaChiNhanh, "access");

            return Ok(new LoginResponse
            {
                Token = newToken,
                RefreshToken = GenerateJwtToken(user.MaNguoiDung, user.TenDangNhap, user.VaiTro!.MaVaiTroHienThi, user.MaChiNhanh, "refresh"),
                ExpiresIn = accessExpiry * 3600,
                User = new UserDto
                {
                    MaNguoiDung = user.MaNguoiDung,
                    TenDangNhap = user.TenDangNhap,
                    HoTen = user.HoTen,
                    Email = user.Email,
                    MaVaiTro = user.VaiTro!.MaVaiTroHienThi,
                    TenVaiTro = user.VaiTro!.TenVaiTro,
                    MaChiNhanh = user.MaChiNhanh,
                    TenChiNhanh = user.ChiNhanh!.TenChiNhanh
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh token không hợp lệ cho user");
            return Unauthorized(new { message = "Refresh token không hợp lệ, vui lòng đăng nhập lại" });
        }
    }

    /// <summary>
    /// Đọc & xác thực refresh token (JWT, không kiểm tra audience/issuer trùng
    /// access token — refresh token cũng dùng cùng secret). Trả về JsonWebToken,
    /// ném exception nếu token không hợp lệ / hết hạn.
    /// </summary>
    private ClaimsPrincipal ValidateRefreshToken(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new ArgumentException("Refresh token không được để trống");

        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));

        var principal = handler.ValidateToken(refreshToken, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _config["Jwt:Issuer"] ?? "QLTDBank",
            ValidAudience = _config["Jwt:Audience"] ?? "QLTDBankClient",
            IssuerSigningKey = key,
        }, out _);

        if (principal == null)
            throw new Exception("Token không hợp lệ");

        // Phải là refresh token, không phải access token
        var tokenType = principal.Claims.FirstOrDefault(c => c.Type == "token_type")?.Value;
        if (tokenType != "refresh")
            throw new Exception("Không phải refresh token");

        return principal;
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !long.TryParse(userId, out var uid))
            return Unauthorized();

        var user = await _context.NguoiDungs
            .Include(u => u.VaiTro)
            .Include(u => u.ChiNhanh)
            .FirstOrDefaultAsync(u => u.MaNguoiDung == uid);

        if (user == null)
            return NotFound();

        return Ok(new UserDto
        {
            MaNguoiDung = user.MaNguoiDung,
            TenDangNhap = user.TenDangNhap,
            HoTen = user.HoTen,
            Email = user.Email,
            MaVaiTro = user.VaiTro!.MaVaiTroHienThi,
            TenVaiTro = user.VaiTro!.TenVaiTro,
            MaChiNhanh = user.MaChiNhanh,
            TenChiNhanh = user.ChiNhanh!.TenChiNhanh
        });
    }

    private string GenerateJwtToken(long userId, string username, string role, long? maChiNhanh = null,
        string tokenType = "access")
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claim "branch" (chuẩn OIDC) để frontend/authorization cô lập dữ liệu
        // theo chi nhánh (trừ ADMIN).
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Name, username),
            new(ClaimTypes.Role, role),
            new("token_type", tokenType),
        };
        if (maChiNhanh.HasValue)
            claims.Add(new Claim("branch", maChiNhanh.Value.ToString()));

        // Access token: ngắn hạn (mặc định 8h). Refresh token: dài hạn (mặc định
        // 7 ngày) — user không phải đăng nhập lại, access hết hạn chỉ cần refresh.
        var expires = tokenType == "refresh"
            ? DateTime.Now.AddDays(_config.GetValue<int>("Jwt:RefreshExpiryDays", 7))
            : DateTime.Now.AddHours(_config.GetValue<int>("Jwt:AccessExpiryHours", 8));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Đổi mật khẩu cho user hiện tại (yêu cầu đã đăng nhập)
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Không xác định được người dùng" });
        }

        var user = await _context.NguoiDungs.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Không tìm thấy người dùng" });
        }

        // Verify mật khẩu hiện tại
        if (string.IsNullOrWhiteSpace(user.MatKhauBam) || !BCrypt.Net.BCrypt.Verify(dto.MatKhauCu, user.MatKhauBam))
        {
            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.PASSWORD_CHANGE,
                tenThucThe: AuditEntities.NGUOI_DUNG,
                maThucThe: user.TenDangNhap,
                giaTriMoi: "Đổi mật khẩu thất bại - sai mật khẩu hiện tại");
            return BadRequest(new { message = "Mật khẩu hiện tại không đúng" });
        }

        // Không cho đổi trùng mật khẩu cũ
        if (!string.IsNullOrWhiteSpace(user.MatKhauBam) && BCrypt.Net.BCrypt.Verify(dto.MatKhauMoi, user.MatKhauBam))
        {
            return BadRequest(new { message = "Mật khẩu mới phải khác mật khẩu hiện tại" });
        }

        // Cập nhật
        user.MatKhauBam = BCrypt.Net.BCrypt.HashPassword(dto.MatKhauMoi);
        user.NgayCapNhat = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.PASSWORD_CHANGE,
            tenThucThe: AuditEntities.NGUOI_DUNG,
            maThucThe: user.TenDangNhap,
            giaTriMoi: "Đổi mật khẩu thành công");

        return Ok(new { message = "Đổi mật khẩu thành công" });
    }
}
