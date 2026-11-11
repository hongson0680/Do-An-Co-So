using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Application.Services;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Extensions;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NguoiDungController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public NguoiDungController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Danh sách người dùng — chỉ ADMIN (quản trị người dùng).
    /// Trước đây chỉ [Authorize] nên mọi vai đã đăng nhập đều đọc được
    /// danh sách tài khoản (tên đăng nhập, email, vai trò) của toàn hệ thống.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult<PagedResult<NguoiDungDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var query = _context.NguoiDungs.AsQueryable();

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.HoTen.Contains(search) || u.TenDangNhap.Contains(search));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(u => u.HoTen)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new NguoiDungDto
            {
                MaNguoiDung = u.MaNguoiDung,
                TenDangNhap = u.TenDangNhap,
                HoTen = u.HoTen,
                Email = u.Email,
                SoDienThoai = u.SoDienThoai,
                MaVaiTro = u.MaVaiTro,
                TenVaiTro = u.VaiTro!.TenVaiTro,
                MaChiNhanh = u.MaChiNhanh,
                TenChiNhanh = u.ChiNhanh!.TenChiNhanh,
                TrangThai = u.TrangThai,
                LanDangNhapCuoi = u.LanDangNhapCuoi,
                NgayTao = u.NgayTao
            })
            .ToListAsync();

        return Ok(new PagedResult<NguoiDungDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NguoiDungDto>> GetById(long id)
    {
        var u = await _context.NguoiDungs
            .Include(x => x.VaiTro)
            .Include(x => x.ChiNhanh)
            .FirstOrDefaultAsync(x => x.MaNguoiDung == id);

        if (u == null) return NotFound();

        return Ok(new NguoiDungDto
        {
            MaNguoiDung = u.MaNguoiDung,
            TenDangNhap = u.TenDangNhap,
            HoTen = u.HoTen,
            Email = u.Email,
            SoDienThoai = u.SoDienThoai,
            MaVaiTro = u.MaVaiTro,
            TenVaiTro = u.VaiTro!.TenVaiTro,
            MaChiNhanh = u.MaChiNhanh,
            TenChiNhanh = u.ChiNhanh!.TenChiNhanh,
            TrangThai = u.TrangThai,
            LanDangNhapCuoi = u.LanDangNhapCuoi,
            NgayTao = u.NgayTao
        });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<NguoiDungDto>> Create([FromBody] CreateNguoiDungDto dto)
    {
        var user = new NguoiDung
        {
            TenDangNhap = dto.TenDangNhap,
            MatKhauBam = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau),
            HoTen = dto.HoTen,
            Email = dto.Email,
            SoDienThoai = dto.SoDienThoai,
            MaVaiTro = dto.MaVaiTro,
            MaChiNhanh = dto.MaChiNhanh,
            TrangThai = "HOAT_DONG",
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        _context.NguoiDungs.Add(user);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.CREATE,
            tenThucThe: AuditEntities.NGUOI_DUNG,
            maThucThe: user.TenDangNhap,
            giaTriMoi: $"Tạo user: {user.HoTen} - Email: {user.Email} - Role: {user.MaVaiTro}");

        return CreatedAtAction(nameof(GetById), new { id = user.MaNguoiDung }, await GetById(user.MaNguoiDung));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult<NguoiDungDto>> Update(long id, [FromBody] CreateNguoiDungDto dto)
    {
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user == null) return NotFound();

        var roleCu = user.MaVaiTro;
        user.HoTen = dto.HoTen;
        user.Email = dto.Email;
        user.SoDienThoai = dto.SoDienThoai;
        user.MaVaiTro = dto.MaVaiTro;
        user.MaChiNhanh = dto.MaChiNhanh;
        user.NgayCapNhat = DateTime.Now;

        var coDoiMatKhau = !string.IsNullOrEmpty(dto.MatKhau);
        if (coDoiMatKhau)
            user.MatKhauBam = BCrypt.Net.BCrypt.HashPassword(dto.MatKhau);

        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: coDoiMatKhau ? AuditActions.PASSWORD_CHANGE : AuditActions.UPDATE,
            tenThucThe: AuditEntities.NGUOI_DUNG,
            maThucThe: user.TenDangNhap,
            giaTriCu: $"Role cũ: {roleCu}",
            giaTriMoi: $"Role mới: {user.MaVaiTro}{(coDoiMatKhau ? " + đổi mật khẩu" : "")}");

        return Ok(await GetById(user.MaNguoiDung));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult> Delete(long id)
    {
        var user = await _context.NguoiDungs.FindAsync(id);
        if (user == null) return NotFound();

        var tenDangNhap = user.TenDangNhap;
        user.TrangThai = "TAM_KHOA";
        user.NgayCapNhat = DateTime.Now;
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.DELETE,
            tenThucThe: AuditEntities.NGUOI_DUNG,
            maThucThe: tenDangNhap,
            giaTriCu: "Đang hoạt động",
            giaTriMoi: "Tạm khóa");

        return NoContent();
    }
}
