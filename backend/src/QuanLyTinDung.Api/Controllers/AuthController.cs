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

    public AuthController(AppDbContext context, IConfiguration config, IAuditLogService auditLog,
        ILoginAttemptService loginAttempts)
    {
        _context = context;
        _config = config;
        _auditLog = auditLog;
        _loginAttempts = loginAttempts;
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
                maThucThe: request.TenDangNhap,
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
                maThucThe: request.TenDangNhap,
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
        var token = GenerateJwtToken(user.MaNguoiDung, user.TenDangNhap, user.VaiTro!.MaVaiTroHienThi, user.MaChiNhanh);

        // Ghi log thành công
        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.LOGIN,
            tenThucThe: AuditEntities.NGUOI_DUNG,
            maThucThe: user.TenDangNhap);

        return Ok(new LoginResponse
        {
            Token = token,
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

    private string GenerateJwtToken(long userId, string username, string role, long? maChiNhanh = null)
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
        };
        if (maChiNhanh.HasValue)
            claims.Add(new Claim("branch", maChiNhanh.Value.ToString()));

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.Now.AddHours(24),
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
