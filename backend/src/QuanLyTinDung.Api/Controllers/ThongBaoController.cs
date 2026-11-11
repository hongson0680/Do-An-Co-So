using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/thong-bao")]
[Authorize]
public class ThongBaoController : ControllerBase
{
    private readonly AppDbContext _context;

    public ThongBaoController(AppDbContext context)
    {
        _context = context;
    }

    private long GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return long.TryParse(claim, out var id) ? id : 0;
    }

    /// <summary>
    /// Lấy danh sách thông báo của user hiện tại (phân trang)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ThongBaoDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? chuaDoc = null)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var query = _context.ThongBaos
            .Where(t => t.MaNguoiNhan == userId)
            .AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 200);

        if (chuaDoc == true)
            query = query.Where(t => !t.DaDoc);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.NgayGui)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy mã hồ sơ hiển thị
        var maHoSoIds = items.Where(t => t.MaHoSo.HasValue).Select(t => t.MaHoSo!.Value).Distinct().ToList();
        var hoSoDict = await _context.HoSoVays
            .Where(h => maHoSoIds.Contains(h.MaHoSo))
            .Select(h => new { h.MaHoSo, h.MaHoSoHienThi })
            .ToDictionaryAsync(h => h.MaHoSo, h => h.MaHoSoHienThi);

        var dtos = items.Select(t => new ThongBaoDto
        {
            MaThongBao = t.MaThongBao,
            MaNguoiNhan = t.MaNguoiNhan,
            LoaiNguoiNhan = t.LoaiNguoiNhan,
            TieuDe = t.TieuDe,
            NoiDung = t.NoiDung,
            Loai = t.Loai,
            DaDoc = t.DaDoc,
            MaHoSo = t.MaHoSo,
            MaHoSoHienThi = t.MaHoSo.HasValue && hoSoDict.TryGetValue(t.MaHoSo.Value, out var ms) ? ms : null,
            NgayGui = t.NgayGui
        }).ToList();

        return Ok(new PagedResult<ThongBaoDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Lấy 10 thông báo mới nhất (cho dropdown chuông)
    /// </summary>
    [HttpGet("recent")]
    public async Task<ActionResult<List<ThongBaoDto>>> GetRecent([FromQuery] int take = 10)
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var items = await _context.ThongBaos
            .Where(t => t.MaNguoiNhan == userId)
            .OrderByDescending(t => t.NgayGui)
            .Take(take)
            .ToListAsync();

        var maHoSoIds = items.Where(t => t.MaHoSo.HasValue).Select(t => t.MaHoSo!.Value).Distinct().ToList();
        var hoSoDict = await _context.HoSoVays
            .Where(h => maHoSoIds.Contains(h.MaHoSo))
            .Select(h => new { h.MaHoSo, h.MaHoSoHienThi })
            .ToDictionaryAsync(h => h.MaHoSo, h => h.MaHoSoHienThi);

        var dtos = items.Select(t => new ThongBaoDto
        {
            MaThongBao = t.MaThongBao,
            MaNguoiNhan = t.MaNguoiNhan,
            LoaiNguoiNhan = t.LoaiNguoiNhan,
            TieuDe = t.TieuDe,
            NoiDung = t.NoiDung,
            Loai = t.Loai,
            DaDoc = t.DaDoc,
            MaHoSo = t.MaHoSo,
            MaHoSoHienThi = t.MaHoSo.HasValue && hoSoDict.TryGetValue(t.MaHoSo.Value, out var ms) ? ms : null,
            NgayGui = t.NgayGui
        }).ToList();

        return Ok(dtos);
    }

    /// <summary>
    /// Số thông báo chưa đọc (cho badge icon chuông)
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<ActionResult<UnreadCountDto>> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();

        var count = await _context.ThongBaos
            .Where(t => t.MaNguoiNhan == userId && !t.DaDoc)
            .CountAsync();

        return Ok(new UnreadCountDto { Unread = count });
    }

    /// <summary>
    /// Đánh dấu 1 thông báo đã đọc
    /// </summary>
    [HttpPut("{id}/read")]
    public async Task<ActionResult> MarkAsRead(long id)
    {
        var userId = GetCurrentUserId();
        var tb = await _context.ThongBaos.FindAsync(id);
        if (tb == null) return NotFound();
        if (tb.MaNguoiNhan != userId) return Forbid();

        if (!tb.DaDoc)
        {
            tb.DaDoc = true;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Đã đánh dấu đã đọc" });
    }

    /// <summary>
    /// Đánh dấu tất cả thông báo là đã đọc
    /// </summary>
    [HttpPut("read-all")]
    public async Task<ActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();
        var chuaDoc = await _context.ThongBaos
            .Where(t => t.MaNguoiNhan == userId && !t.DaDoc)
            .ToListAsync();

        foreach (var tb in chuaDoc)
        {
            tb.DaDoc = true;
        }
        await _context.SaveChangesAsync();

        return Ok(new { message = $"Đã đánh dấu {chuaDoc.Count} thông báo là đã đọc" });
    }

    /// <summary>
    /// Xóa 1 thông báo (của user hiện tại)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(long id)
    {
        var userId = GetCurrentUserId();
        var tb = await _context.ThongBaos.FindAsync(id);
        if (tb == null) return NotFound();
        if (tb.MaNguoiNhan != userId) return Forbid();

        _context.ThongBaos.Remove(tb);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Thống kê thông báo
    /// </summary>
    [HttpGet("thong-ke")]
    public async Task<ActionResult<ThongBaoThongKeDto>> GetThongKe()
    {
        var userId = GetCurrentUserId();
        if (userId == 0) return Unauthorized();
        var query = _context.ThongBaos.Where(t => t.MaNguoiNhan == userId);

        var tongSo = await query.CountAsync();
        var chuaDoc = await query.CountAsync(t => !t.DaDoc);
        var theoLoai = await query
            .GroupBy(t => t.Loai)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        return Ok(new ThongBaoThongKeDto
        {
            TongSoThongBao = tongSo,
            ChuaDoc = chuaDoc,
            TheoLoai = theoLoai
        });
    }
}

public class UnreadCountDto
{
    public int Unread { get; set; }
}
