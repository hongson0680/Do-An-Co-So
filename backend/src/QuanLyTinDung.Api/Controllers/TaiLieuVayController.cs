using System.Security.Claims;
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
[Route("api/tai-lieu-vay")]
[Authorize]
public class TaiLieuVayController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public TaiLieuVayController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<TaiLieuVayDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] long? maHoSo = null,
        [FromQuery] string? loaiTaiLieu = null,
        [FromQuery] string? trangThai = null)
    {
        var query = _context.TaiLieuVays.AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        if (maHoSo.HasValue)
            query = query.Where(t => t.MaHoSo == maHoSo);
        if (!string.IsNullOrEmpty(loaiTaiLieu))
            query = query.Where(t => t.LoaiTaiLieu == loaiTaiLieu);
        if (!string.IsNullOrEmpty(trangThai))
            query = query.Where(t => t.TrangThaiXacMinh == trangThai);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.NgayTao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy thông tin hồ sơ + người upload
        var maHoSoIds = items.Select(t => t.MaHoSo).Distinct().ToList();
        var maNguoiIds = items.Where(t => t.MaNguoiUpload.HasValue).Select(t => t.MaNguoiUpload!.Value).Distinct().ToList();
        var hoSoDict = await _context.HoSoVays
            .Where(h => maHoSoIds.Contains(h.MaHoSo))
            .ToDictionaryAsync(h => h.MaHoSo, h => h.MaHoSoHienThi);
        var userDict = await _context.NguoiDungs
            .Where(u => maNguoiIds.Contains(u.MaNguoiDung))
            .ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen);

        var dtos = items.Select(t => new TaiLieuVayDto
        {
            MaTaiLieu = t.MaTaiLieu,
            MaHoSo = t.MaHoSo,
            MaHoSoHienThi = hoSoDict.TryGetValue(t.MaHoSo, out var ms) ? ms : string.Empty,
            LoaiTaiLieu = t.LoaiTaiLieu,
            TenTapTin = t.TenTapTin,
            DuongDanTapTin = t.DuongDanTapTin,
            PhienBan = t.PhienBan,
            TrangThaiXacMinh = t.TrangThaiXacMinh,
            MaNguoiUpload = t.MaNguoiUpload,
            TenNguoiUpload = t.MaNguoiUpload.HasValue && userDict.TryGetValue(t.MaNguoiUpload.Value, out var ten) ? ten : null,
            GhiChu = t.GhiChu,
            NgayTao = t.NgayTao,
            NgayCapNhat = t.NgayCapNhat
        }).ToList();

        return Ok(new PagedResult<TaiLieuVayDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaiLieuVayDto>> GetById(long id)
    {
        var item = await _context.TaiLieuVays.FindAsync(id);
        if (item == null) return NotFound();

        var hoSo = await _context.HoSoVays.FindAsync(item.MaHoSo);
        var nguoiUpload = item.MaNguoiUpload.HasValue
            ? await _context.NguoiDungs.FindAsync(item.MaNguoiUpload.Value)
            : null;

        return Ok(new TaiLieuVayDto
        {
            MaTaiLieu = item.MaTaiLieu,
            MaHoSo = item.MaHoSo,
            MaHoSoHienThi = hoSo?.MaHoSoHienThi ?? string.Empty,
            LoaiTaiLieu = item.LoaiTaiLieu,
            TenTapTin = item.TenTapTin,
            DuongDanTapTin = item.DuongDanTapTin,
            PhienBan = item.PhienBan,
            TrangThaiXacMinh = item.TrangThaiXacMinh,
            MaNguoiUpload = item.MaNguoiUpload,
            TenNguoiUpload = nguoiUpload?.HoTen,
            GhiChu = item.GhiChu,
            NgayTao = item.NgayTao,
            NgayCapNhat = item.NgayCapNhat
        });
    }

    [HttpPost]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.CollateralManagers)]
    public async Task<ActionResult<TaiLieuVayDto>> Create([FromBody] CreateTaiLieuVayDto dto)
    {
        var hoSo = await _context.HoSoVays.FindAsync(dto.MaHoSo);
        if (hoSo == null)
        {
            return BadRequest(new { message = $"Hồ sơ {dto.MaHoSo} không tồn tại" });
        }

        var userId = GetCurrentUserId();

        var item = new TaiLieuVay
        {
            MaHoSo = dto.MaHoSo,
            LoaiTaiLieu = dto.LoaiTaiLieu,
            TenTapTin = dto.TenTapTin,
            DuongDanTapTin = dto.DuongDanTapTin,
            PhienBan = dto.PhienBan,
            TrangThaiXacMinh = dto.TrangThaiXacMinh,
            MaNguoiUpload = userId,
            GhiChu = dto.GhiChu,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        _context.TaiLieuVays.Add(item);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.CREATE,
            tenThucThe: AuditEntities.TAI_LIEU_VAY,
            maThucThe: item.MaTaiLieu.ToString(),
            giaTriMoi: $"Upload {item.LoaiTaiLieu}: {item.TenTapTin} cho HS#{item.MaHoSo}");

        return CreatedAtAction(nameof(GetById), new { id = item.MaTaiLieu }, new TaiLieuVayDto
        {
            MaTaiLieu = item.MaTaiLieu,
            MaHoSo = item.MaHoSo,
            MaHoSoHienThi = hoSo.MaHoSoHienThi,
            LoaiTaiLieu = item.LoaiTaiLieu,
            TenTapTin = item.TenTapTin,
            DuongDanTapTin = item.DuongDanTapTin,
            PhienBan = item.PhienBan,
            TrangThaiXacMinh = item.TrangThaiXacMinh,
            MaNguoiUpload = item.MaNguoiUpload,
            GhiChu = item.GhiChu,
            NgayTao = item.NgayTao,
            NgayCapNhat = item.NgayCapNhat
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.CollateralManagers)]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateTaiLieuVayDto dto)
    {
        var item = await _context.TaiLieuVays.FindAsync(id);
        if (item == null) return NotFound();

        var cu = $"{item.LoaiTaiLieu} - {item.TenTapTin} - {item.TrangThaiXacMinh}";

        item.LoaiTaiLieu = dto.LoaiTaiLieu;
        item.TenTapTin = dto.TenTapTin;
        item.DuongDanTapTin = dto.DuongDanTapTin;
        item.PhienBan = dto.PhienBan;
        item.TrangThaiXacMinh = dto.TrangThaiXacMinh;
        item.GhiChu = dto.GhiChu;
        item.NgayCapNhat = DateTime.Now;

        await _context.SaveChangesAsync();

        var moi = $"{item.LoaiTaiLieu} - {item.TenTapTin} - {item.TrangThaiXacMinh}";
        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.UPDATE,
            tenThucThe: AuditEntities.TAI_LIEU_VAY,
            maThucThe: id.ToString(),
            giaTriCu: cu,
            giaTriMoi: moi);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        var item = await _context.TaiLieuVays.FindAsync(id);
        if (item == null) return NotFound();

        var thongTin = $"{item.LoaiTaiLieu} - {item.TenTapTin}";

        _context.TaiLieuVays.Remove(item);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.DELETE,
            tenThucThe: AuditEntities.TAI_LIEU_VAY,
            maThucThe: id.ToString(),
            giaTriCu: thongTin);

        return NoContent();
    }
}
