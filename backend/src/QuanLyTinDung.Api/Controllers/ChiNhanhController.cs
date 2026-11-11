using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;
using System.Text.RegularExpressions;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChiNhanhController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ChiNhanhController> _logger;

    public ChiNhanhController(AppDbContext context, ILogger<ChiNhanhController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Validate dữ liệu chi nhánh theo CHECK constraint của database
    /// Trả về chuỗi lỗi nếu không hợp lệ, null nếu OK
    /// </summary>
    private string? ValidateChiNhanh(CreateChiNhanhDto dto, long? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(dto.MaChiNhanhHienThi))
            return "Mã chi nhánh không được để trống";
        if (dto.MaChiNhanhHienThi.Length > 20)
            return "Mã chi nhánh không được vượt quá 20 ký tự";
        if (string.IsNullOrWhiteSpace(dto.TenChiNhanh))
            return "Tên chi nhánh không được để trống";
        if (dto.TenChiNhanh.Length > 255)
            return "Tên chi nhánh không được vượt quá 255 ký tự";

        // Validate số điện thoại: 10 chữ số (theo CHECK constraint CK_chi_nhanh_sdt)
        if (!string.IsNullOrEmpty(dto.SoDienThoai))
        {
            if (!Regex.IsMatch(dto.SoDienThoai, @"^\d{10}$"))
                return "Số điện thoại phải đúng 10 chữ số";
        }

        // Validate trạng thái hợp lệ
        var validTrangThai = new[] { "HOAT_DONG", "DA_DONG", "BAO_TRI" };
        if (!string.IsNullOrEmpty(dto.TrangThai) && !validTrangThai.Contains(dto.TrangThai))
            return $"Trạng thái không hợp lệ. Chỉ chấp nhận: {string.Join(", ", validTrangThai)}";

        return null;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChiNhanhDto>>> GetAll()
    {
        var items = await _context.ChiNhanhs
            .Where(c => c.TrangThai == "HOAT_DONG")
            .OrderBy(c => c.TenChiNhanh)
            .Select(c => new ChiNhanhDto
            {
                MaChiNhanh = c.MaChiNhanh,
                MaChiNhanhHienThi = c.MaChiNhanhHienThi,
                TenChiNhanh = c.TenChiNhanh,
                DiaChi = c.DiaChi,
                SoDienThoai = c.SoDienThoai,
                TruongChiNhanh = c.TruongChiNhanh,
                VungDiaLy = c.VungDiaLy,
                TrangThai = c.TrangThai,
                NgayTao = c.NgayTao
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChiNhanhDto>> GetById(long id)
    {
        var cn = await _context.ChiNhanhs.FindAsync(id);
        if (cn == null) return NotFound();

        return Ok(new ChiNhanhDto
        {
            MaChiNhanh = cn.MaChiNhanh,
            MaChiNhanhHienThi = cn.MaChiNhanhHienThi,
            TenChiNhanh = cn.TenChiNhanh,
            DiaChi = cn.DiaChi,
            SoDienThoai = cn.SoDienThoai,
            TruongChiNhanh = cn.TruongChiNhanh,
            VungDiaLy = cn.VungDiaLy,
            TrangThai = cn.TrangThai,
            NgayTao = cn.NgayTao
        });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ChiNhanhDto>> Create([FromBody] CreateChiNhanhDto dto)
    {
        // Validate trước khi save để trả lỗi rõ ràng
        var validationError = ValidateChiNhanh(dto);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        // Kiểm tra trùng mã chi nhánh
        var exists = await _context.ChiNhanhs
            .AnyAsync(c => c.MaChiNhanhHienThi == dto.MaChiNhanhHienThi);
        if (exists)
            return BadRequest(new { message = $"Mã chi nhánh '{dto.MaChiNhanhHienThi}' đã tồn tại" });

        var cn = new ChiNhanh
        {
            MaChiNhanhHienThi = dto.MaChiNhanhHienThi.Trim(),
            TenChiNhanh = dto.TenChiNhanh.Trim(),
            DiaChi = dto.DiaChi?.Trim(),
            SoDienThoai = dto.SoDienThoai?.Trim(),
            TruongChiNhanh = dto.TruongChiNhanh?.Trim(),
            VungDiaLy = dto.VungDiaLy?.Trim(),
            TrangThai = string.IsNullOrEmpty(dto.TrangThai) ? "HOAT_DONG" : dto.TrangThai,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        try
        {
            _context.ChiNhanhs.Add(cn);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = cn.MaChiNhanh }, new ChiNhanhDto
            {
                MaChiNhanh = cn.MaChiNhanh,
                MaChiNhanhHienThi = cn.MaChiNhanhHienThi,
                TenChiNhanh = cn.TenChiNhanh,
                DiaChi = cn.DiaChi,
                SoDienThoai = cn.SoDienThoai,
                TruongChiNhanh = cn.TruongChiNhanh,
                VungDiaLy = cn.VungDiaLy,
                TrangThai = cn.TrangThai,
                NgayTao = cn.NgayTao
            });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi tạo chi nhánh: {Message}", dbEx.InnerException?.Message);
            return BadRequest(new { message = "Không thể tạo chi nhánh: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult<ChiNhanhDto>> Update(long id, [FromBody] CreateChiNhanhDto dto)
    {
        var cn = await _context.ChiNhanhs.FindAsync(id);
        if (cn == null) return NotFound();

        // Validate trước khi save
        var validationError = ValidateChiNhanh(dto);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        cn.TenChiNhanh = dto.TenChiNhanh.Trim();
        cn.DiaChi = dto.DiaChi?.Trim();
        cn.SoDienThoai = dto.SoDienThoai?.Trim();
        cn.TruongChiNhanh = dto.TruongChiNhanh?.Trim();
        cn.VungDiaLy = dto.VungDiaLy?.Trim();
        if (!string.IsNullOrEmpty(dto.TrangThai))
            cn.TrangThai = dto.TrangThai;
        cn.NgayCapNhat = DateTime.Now;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi cập nhật chi nhánh: {Message}", dbEx.InnerException?.Message);
            return BadRequest(new { message = "Không thể cập nhật chi nhánh: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }

        return Ok(new ChiNhanhDto
        {
            MaChiNhanh = cn.MaChiNhanh,
            MaChiNhanhHienThi = cn.MaChiNhanhHienThi,
            TenChiNhanh = cn.TenChiNhanh,
            DiaChi = cn.DiaChi,
            SoDienThoai = cn.SoDienThoai,
            TruongChiNhanh = cn.TruongChiNhanh,
            VungDiaLy = cn.VungDiaLy,
            TrangThai = cn.TrangThai,
            NgayTao = cn.NgayTao
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult> Delete(long id)
    {
        var cn = await _context.ChiNhanhs.FindAsync(id);
        if (cn == null) return NotFound();

        // Kiểm tra ràng buộc: chi nhánh còn người dùng không?
        var hasUsers = await _context.NguoiDungs.AnyAsync(u => u.MaChiNhanh == id);
        if (hasUsers)
        {
            // Soft delete: chuyển trạng thái thay vì xóa cứng
            cn.TrangThai = "DA_DONG";
            cn.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Chi nhánh đang có nhân viên, đã chuyển sang trạng thái ngừng hoạt động" });
        }

        try
        {
            cn.TrangThai = "DA_DONG";
            cn.NgayCapNhat = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi xóa chi nhánh: {Message}", dbEx.InnerException?.Message);
            return BadRequest(new { message = "Không thể xóa chi nhánh: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }

        return NoContent();
    }
}
