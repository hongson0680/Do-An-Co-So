using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/nhat-ky")]
[Authorize(Roles = "ADMIN")]
public class NhatKyHeThongController : ControllerBase
{
    private readonly AppDbContext _context;

    public NhatKyHeThongController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách log có phân trang + filter (chỉ ADMIN)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<NhatKyHeThongDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? hanhDong = null,
        [FromQuery] string? tenThucThe = null,
        [FromQuery] long? maNguoiDung = null,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? tuNgay = null,
        [FromQuery] DateTime? denNgay = null)
    {
        var query = _context.NhatKyHeThongs.AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        if (!string.IsNullOrEmpty(hanhDong))
            query = query.Where(n => n.HanhDong == hanhDong);
        if (!string.IsNullOrEmpty(tenThucThe))
            query = query.Where(n => n.TenThucThe == tenThucThe);
        if (maNguoiDung.HasValue)
            query = query.Where(n => n.MaNguoiDung == maNguoiDung);
        if (!string.IsNullOrEmpty(search))
        {
            var kw = search.Trim().ToLower();
            query = query.Where(n =>
                n.MaThucThe.ToLower().Contains(kw) ||
                (n.GiaTriCu != null && n.GiaTriCu.ToLower().Contains(kw)) ||
                (n.GiaTriMoi != null && n.GiaTriMoi.ToLower().Contains(kw)) ||
                (n.TruongThayDoi != null && n.TruongThayDoi.ToLower().Contains(kw)));
        }
        if (tuNgay.HasValue)
            query = query.Where(n => n.NgayTao >= tuNgay.Value);
        if (denNgay.HasValue)
        {
            var end = denNgay.Value.Date.AddDays(1);
            query = query.Where(n => n.NgayTao < end);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(n => n.NgayTao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Lấy danh sách userId để join với NguoiDung
        var userIds = items.Where(n => n.MaNguoiDung.HasValue).Select(n => n.MaNguoiDung!.Value).Distinct().ToList();
        var users = await _context.NguoiDungs
            .Where(u => userIds.Contains(u.MaNguoiDung))
            .Select(u => new { u.MaNguoiDung, u.HoTen })
            .ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen);

        var dtos = items.Select(n => new NhatKyHeThongDto
        {
            MaNhatKy = n.MaNhatKy,
            MaNguoiDung = n.MaNguoiDung,
            TenNguoiDung = n.MaNguoiDung.HasValue && users.TryGetValue(n.MaNguoiDung.Value, out var ten) ? ten : null,
            HanhDong = n.HanhDong,
            TenThucThe = n.TenThucThe,
            MaThucThe = n.MaThucThe,
            GiaTriCu = n.GiaTriCu,
            GiaTriMoi = n.GiaTriMoi,
            TruongThayDoi = n.TruongThayDoi,
            IpNguon = n.IpNguon,
            TrinhDuyet = n.TrinhDuyet,
            NgayTao = n.NgayTao
        }).ToList();

        return Ok(new PagedResult<NhatKyHeThongDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Thống kê tổng quan + top user hoạt động
    /// </summary>
    [HttpGet("thong-ke")]
    public async Task<ActionResult<NhatKyThongKeDto>> GetThongKe()
    {
        var now = DateTime.Now;
        var dauNgay = new DateTime(now.Year, now.Month, now.Day);
        var dauTuan = dauNgay.AddDays(-(int)dauNgay.DayOfWeek);
        var dauThang = new DateTime(now.Year, now.Month, 1);

        var tongSoLog = await _context.NhatKyHeThongs.CountAsync();
        var soLuongHomNay = await _context.NhatKyHeThongs.CountAsync(n => n.NgayTao >= dauNgay);
        var soLuongTuanNay = await _context.NhatKyHeThongs.CountAsync(n => n.NgayTao >= dauTuan);
        var soLuongThangNay = await _context.NhatKyHeThongs.CountAsync(n => n.NgayTao >= dauThang);

        var theoHanhDong = await _context.NhatKyHeThongs
            .GroupBy(n => n.HanhDong)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var theoThucThe = await _context.NhatKyHeThongs
            .GroupBy(n => n.TenThucThe)
            .Select(g => new { Key = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var topUsers = await _context.NhatKyHeThongs
            .Where(n => n.MaNguoiDung.HasValue)
            .GroupBy(n => n.MaNguoiDung)
            .Select(g => new
            {
                MaNguoiDung = g.Key,
                SoLuot = g.Count(),
                LanCuoi = g.Max(x => x.NgayTao)
            })
            .OrderByDescending(x => x.SoLuot)
            .Take(10)
            .Join(_context.NguoiDungs,
                x => x.MaNguoiDung,
                u => u.MaNguoiDung,
                (x, u) => new NguoiDungHoatDongDto
                {
                    MaNguoiDung = u.MaNguoiDung,
                    TenDangNhap = u.TenDangNhap,
                    HoTen = u.HoTen,
                    SoLuotThaoTac = x.SoLuot,
                    LanCuoi = x.LanCuoi
                })
            .ToListAsync();

        return Ok(new NhatKyThongKeDto
        {
            TongSoLog = tongSoLog,
            SoLuongHomNay = soLuongHomNay,
            SoLuongTuanNay = soLuongTuanNay,
            SoLuongThangNay = soLuongThangNay,
            TheoHanhDong = theoHanhDong,
            TheoThucThe = theoThucThe,
            NguoiDungHoatDong = topUsers
        });
    }

    /// <summary>
    /// Lấy lịch sử thao tác của 1 bản ghi cụ thể (vd: maKhachHang=5 → xem ai đã sửa khách hàng 5)
    /// </summary>
    [HttpGet("thuc-the/{tenThucThe}/{maThucThe}")]
    public async Task<ActionResult<List<NhatKyHeThongDto>>> GetByEntity(string tenThucThe, string maThucThe)
    {
        var items = await _context.NhatKyHeThongs
            .Where(n => n.TenThucThe == tenThucThe && n.MaThucThe == maThucThe)
            .OrderByDescending(n => n.NgayTao)
            .Take(100)
            .ToListAsync();

        var userIds = items.Where(n => n.MaNguoiDung.HasValue).Select(n => n.MaNguoiDung!.Value).Distinct().ToList();
        var users = await _context.NguoiDungs
            .Where(u => userIds.Contains(u.MaNguoiDung))
            .Select(u => new { u.MaNguoiDung, u.HoTen })
            .ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen);

        var dtos = items.Select(n => new NhatKyHeThongDto
        {
            MaNhatKy = n.MaNhatKy,
            MaNguoiDung = n.MaNguoiDung,
            TenNguoiDung = n.MaNguoiDung.HasValue && users.TryGetValue(n.MaNguoiDung.Value, out var ten) ? ten : null,
            HanhDong = n.HanhDong,
            TenThucThe = n.TenThucThe,
            MaThucThe = n.MaThucThe,
            GiaTriCu = n.GiaTriCu,
            GiaTriMoi = n.GiaTriMoi,
            TruongThayDoi = n.TruongThayDoi,
            IpNguon = n.IpNguon,
            TrinhDuyet = n.TrinhDuyet,
            NgayTao = n.NgayTao
        }).ToList();

        return Ok(dtos);
    }
}
