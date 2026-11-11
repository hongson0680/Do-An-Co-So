using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Helpers;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Danh sách user thuộc chi nhánh hiện tại (dùng để lọc hồ sơ vay).
    /// Trả về null nghĩa là user không bị giới hạn theo chi nhánh → thấy toàn hệ thống.
    /// </summary>
    private async Task<List<long>?> GetBranchUserIdsAsync()
    {
        if (!User.ShouldScopeByBranch())
            return null;

        var branchId = User.CurrentBranchId()!.Value;
        return await _context.NguoiDungs
            .Where(u => u.MaChiNhanh == branchId)
            .Select(u => u.MaNguoiDung)
            .ToListAsync();
    }

    [HttpGet]
    public async Task<ActionResult<DashboardDto>> GetStats()
    {
        var now = DateTime.Now;
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

        // Cô lập theo chi nhánh: CREDIT_OFFICER chỉ thấy hồ sơ của cán bộ
        // cùng chi nhánh (cùng cơ chế với HoSoVayController). Vai trung tâm
        // (thẩm định, phê duyệt, ngân quỹ, báo cáo) và ADMIN thấy toàn hệ thống.
        var branchUserIds = await GetBranchUserIdsAsync();
        bool coLoc = branchUserIds != null;

        var hoSoQuery = _context.HoSoVays.AsQueryable();
        var hopDongQuery = _context.HopDongVays.AsQueryable();

        if (coLoc)
        {
            var ids = branchUserIds!;
            hoSoQuery = hoSoQuery.Where(h => ids.Contains(h.MaCanBo));
            hopDongQuery = hopDongQuery.Where(h =>
                _context.HoSoVays.Any(hs => hs.MaHoSo == h.MaHoSo && ids.Contains(hs.MaCanBo)));
        }

        // Khách hàng dùng chung toàn hệ thống (bảng khach_hang không có cột chi
        // nhánh — xem docs/rbac-matrix.md), nên số lượng KHÔNG lọc theo chi nhánh.
        var tongKhachHang = await _context.KhachHangs.CountAsync();
        var tongHoSoVay = await hoSoQuery.CountAsync();
        var tongHopDong = await hopDongQuery.CountAsync();
        var tongGiaiNgan = await hopDongQuery.Where(h => h.NgayGiaiNgan.HasValue).SumAsync(h => h.TienGoc);

        var hoSoMoi = await hoSoQuery.CountAsync(h => h.TrangThai == "MOI");
        // "Đang xử lý" = chưa kết thúc quy trình: loại MOI (chưa xử lý),
        // DA_DONG / TU_CHOI (đã kết thúc), DA_GIAI_NGAN (đã giải ngân),
        // DA_LAP_HOP_DONG (đã lập hợp đồng → sang bước ngân quỹ).
        var hoSoDangXyLy = await hoSoQuery.CountAsync(h =>
            h.TrangThai != "MOI" && h.TrangThai != "DA_DONG" && h.TrangThai != "TU_CHOI"
            && h.TrangThai != "DA_GIAI_NGAN" && h.TrangThai != "DA_LAP_HOP_DONG");
        // "Đã duyệt" = hồ sơ đã được phê duyệt (chưa tính giải ngân).
        // Trước đây đếm DA_GIAI_NGAN → sai nghĩa với tiêu đề.
        var hoSoDaDuyet = await hoSoQuery.CountAsync(h =>
            h.TrangThai == "DA_PHE_DUYET" || h.TrangThai == "DA_LAP_HOP_DONG" || h.TrangThai == "DA_GIAI_NGAN");
        var hoSoTuChoi = await hoSoQuery.CountAsync(h => h.TrangThai == "TU_CHOI");

        var tienGiaiNganThangNay = await hopDongQuery
            .Where(h => h.NgayGiaiNgan.HasValue && h.NgayGiaiNgan >= firstDayOfMonth)
            .SumAsync(h => h.TienGoc);

        var tongHopDongActive = await hopDongQuery.CountAsync(h => h.TrangThai == "HOAT_DONG");
        var hopDongQuaHan = await hopDongQuery.CountAsync(h =>
            h.TrangThai == "QUA_HAN" || h.TrangThai == "QUA_HAN_30" || h.TrangThai == "QUA_HAN_60");

        var tiLeQuaHan = tongHopDongActive > 0 ? (decimal)hopDongQuaHan / tongHopDongActive * 100 : 0;

        return Ok(new DashboardDto
        {
            TongKhachHang = tongKhachHang,
            TongHoSoVay = tongHoSoVay,
            TongHopDong = tongHopDong,
            TongGiaiNgan = tongGiaiNgan,
            HoSoMoi = hoSoMoi,
            HoSoDangXyLy = hoSoDangXyLy,
            HoSoDaDuyet = hoSoDaDuyet,
            HoSoTuChoi = hoSoTuChoi,
            TienGiaiNganThangNay = tienGiaiNganThangNay,
            TiLeQuaHan = tiLeQuaHan
        });
    }

    /// <summary>
    /// Thống kê số lượng hồ sơ theo từng trạng thái (kèm tổng tiền)
    /// </summary>
    [HttpGet("ho-so-theo-trang-thai")]
    public async Task<ActionResult<List<HoSoTheoTrangThaiDto>>> GetHoSoByStatus()
    {
        var branchUserIds = await GetBranchUserIdsAsync();
        var query = _context.HoSoVays.AsQueryable();
        if (branchUserIds != null)
            query = query.Where(h => branchUserIds.Contains(h.MaCanBo));

        var data = await query
            .GroupBy(h => h.TrangThai)
            .Select(g => new HoSoTheoTrangThaiDto
            {
                TrangThai = g.Key,
                SoLuong = g.Count(),
                TongTien = g.Sum(h => h.SoTienDeNghi)
            })
            .OrderByDescending(x => x.SoLuong)
            .ToListAsync();

        return Ok(data);
    }

    /// <summary>
    /// Thống kê giải ngân theo 12 tháng gần nhất
    /// </summary>
    [HttpGet("giai-ngan-theo-thang")]
    public async Task<ActionResult<List<ChartDataDto>>> GetGiaiNganTheoThang()
    {
        var startDate = DateTime.Now.AddMonths(-11);
        var firstOfMonth = new DateTime(startDate.Year, startDate.Month, 1);

        var hopDongQuery = _context.HopDongVays.AsQueryable();
        var branchUserIds = await GetBranchUserIdsAsync();
        if (branchUserIds != null)
            hopDongQuery = hopDongQuery.Where(h =>
                _context.HoSoVays.Any(hs => hs.MaHoSo == h.MaHoSo && branchUserIds.Contains(hs.MaCanBo)));

        var raw = await hopDongQuery
            .Where(h => h.NgayGiaiNgan.HasValue && h.NgayGiaiNgan >= firstOfMonth)
            .Select(h => new { h.NgayGiaiNgan, h.TienGoc })
            .ToListAsync();

        // Nhóm theo tháng trong bộ nhớ (DateTime không nhóm trực tiếp trong LINQ-to-Entities)
        // Sort theo (Năm, Tháng) chứ không sort theo chuỗi "MM/yyyy" — nếu sort
        // chuỗi thì khi đổi năm các tháng sẽ bị xáo trộn (vd "01/2025" > "12/2024").
        var data = raw
            .GroupBy(x => new { x.NgayGiaiNgan!.Value.Year, x.NgayGiaiNgan.Value.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                Value = g.Sum(x => x.TienGoc)
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .Select(x => new ChartDataDto { Label = x.Label, Value = x.Value })
            .ToList();

        return Ok(data);
    }

    /// <summary>
    /// Top 10 khách hàng vay nhiều nhất (đã giải ngân)
    /// </summary>
    [HttpGet("top-khach-hang")]
    public async Task<ActionResult<List<TopKhachHangDto>>> GetTopKhachHang([FromQuery] int top = 10)
    {
        // Guard: client gửi top <= 0 → Take âm → lỗi runtime
        top = Math.Clamp(top, 1, 100);

        var hopDongQuery = _context.HopDongVays.AsQueryable();
        var branchUserIds = await GetBranchUserIdsAsync();
        if (branchUserIds != null)
            hopDongQuery = hopDongQuery.Where(h =>
                _context.HoSoVays.Any(hs => hs.MaHoSo == h.MaHoSo && branchUserIds.Contains(hs.MaCanBo)));

        var data = await hopDongQuery
            .Where(h => h.NgayGiaiNgan.HasValue)
            .GroupBy(h => h.MaHoSo)
            .Select(g => new
            {
                MaHoSo = g.Key,
                TongVay = g.Sum(x => x.TienGoc),
                SoHopDong = g.Count()
            })
            .Join(_context.HoSoVays,
                grp => grp.MaHoSo,
                hs => hs.MaHoSo,
                (grp, hs) => new { hs.MaKhachHang, grp.TongVay, grp.SoHopDong })
            .GroupBy(x => x.MaKhachHang)
            .Select(g => new
            {
                MaKhachHang = g.Key,
                TongVay = g.Sum(x => x.TongVay),
                SoHopDong = g.Sum(x => x.SoHopDong)
            })
            .OrderByDescending(x => x.TongVay)
            .Take(top)
            .Join(_context.KhachHangs,
                x => x.MaKhachHang,
                kh => kh.MaKhachHang,
                (x, kh) => new TopKhachHangDto
                {
                    MaKhachHang = kh.MaKhachHang,
                    TenKh = kh.HoTen,
                    SoGiayTo = kh.SoGiayTo,
                    TongVay = x.TongVay,
                    SoHopDong = x.SoHopDong
                })
            .ToListAsync();

        return Ok(data);
    }
}
