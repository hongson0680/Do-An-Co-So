using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Api.Services;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/export")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IExportService _export;

    public ExportController(AppDbContext context, IExportService export)
    {
        _context = context;
        _export = export;
    }

    /// <summary>
    /// Xuất danh sách khách hàng
    /// </summary>
    [HttpGet("khach-hang")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Reporters)]
    public async Task<IActionResult> ExportKhachHang(
        [FromQuery] string format = "excel",
        [FromQuery] string? search = null,
        [FromQuery] string? loaiKhachHang = null)
    {
        var query = _context.KhachHangs.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(k => k.HoTen.Contains(search) || k.SoGiayTo.Contains(search));
        if (!string.IsNullOrEmpty(loaiKhachHang))
            query = query.Where(k => k.LoaiKhachHang.ToString() == loaiKhachHang);

        var data = await query.OrderByDescending(k => k.NgayTao).ToListAsync();

        var mappings = new Dictionary<string, Func<dynamic, object?>>
        {
            ["STT"] = k => k.MaKhachHang,
            ["Mã KH"] = k => k.MaKhachHang.ToString(),
            ["Họ tên"] = k => k.HoTen,
            ["Số giấy tờ"] = k => k.SoGiayTo,
            ["Loại KH"] = k => k.LoaiKhachHang.ToString(),
            ["Giới tính"] = k => k.GioiTinh ?? "",
            ["Ngày sinh"] = k => k.NgaySinh,
            ["SĐT"] = k => k.SoDienThoai ?? "",
            ["Email"] = k => k.Email ?? "",
            ["Địa chỉ"] = k => k.DiaChi ?? "",
            ["Nghề nghiệp"] = k => k.NgheNghiep ?? "",
            ["Mức độ rủi ro"] = k => k.MucDoRuiRo,
            ["Ngày tạo"] = k => k.NgayTao,
        };

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(_export.ExportToPdf(data, "DANH SÁCH KHÁCH HÀNG", mappings!), "danh-sach-khach-hang.pdf", "application/pdf"),
            _ => ReturnFile(_export.ExportToExcel(data, "KhachHang", "DANH SÁCH KHÁCH HÀNG", mappings!), "danh-sach-khach-hang.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    /// <summary>
    /// Xuất danh sách hồ sơ vay
    /// </summary>
    [HttpGet("ho-so-vay")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Reporters)]
    public async Task<IActionResult> ExportHoSoVay(
        [FromQuery] string format = "excel",
        [FromQuery] string? trangThai = null,
        [FromQuery] DateTime? tuNgay = null,
        [FromQuery] DateTime? denNgay = null)
    {
        var query = _context.HoSoVays
            .Include(h => h.KhachHang)
            .Include(h => h.SanPhamVay)
            .Include(h => h.CanBo)
            .AsQueryable();

        if (!string.IsNullOrEmpty(trangThai))
            query = query.Where(h => h.TrangThai == trangThai);
        if (tuNgay.HasValue)
            query = query.Where(h => h.NgayTao >= tuNgay.Value);
        if (denNgay.HasValue)
        {
            var end = denNgay.Value.Date.AddDays(1);
            query = query.Where(h => h.NgayTao < end);
        }

        var data = await query.OrderByDescending(h => h.NgayTao).ToListAsync();

        var mappings = new Dictionary<string, Func<dynamic, object?>>
        {
            ["Mã HS"] = h => h.MaHoSoHienThi,
            ["Khách hàng"] = h => h.KhachHang?.HoTen ?? "",
            ["Sản phẩm"] = h => h.SanPhamVay?.TenSanPham ?? "",
            ["Số tiền"] = h => h.SoTienDeNghi,
            ["Kỳ hạn (tháng)"] = h => h.KyHanThang,
            ["Mục đích"] = h => h.MucDich,
            ["Trạng thái"] = h => h.TrangThai,
            ["Cán bộ tạo"] = h => h.CanBo?.HoTen ?? "",
            ["Ngày tạo"] = h => h.NgayTao,
            ["Ngày nộp"] = h => h.NgayNop,
        };

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(_export.ExportToPdf(data, "DANH SÁCH HỒ SƠ VAY", mappings!), "danh-sach-ho-so-vay.pdf", "application/pdf"),
            _ => ReturnFile(_export.ExportToExcel(data, "HoSoVay", "DANH SÁCH HỒ SƠ VAY", mappings!), "danh-sach-ho-so-vay.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    /// <summary>
    /// Xuất danh sách hợp đồng vay
    /// </summary>
    [HttpGet("hop-dong")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Reporters)]
    public async Task<IActionResult> ExportHopDong([FromQuery] string format = "excel")
    {
        var data = await _context.HopDongVays
            .Include(h => h.HoSoVay).ThenInclude(hs => hs!.KhachHang)
            .OrderByDescending(h => h.NgayTao)
            .ToListAsync();

        var mappings = new Dictionary<string, Func<dynamic, object?>>
        {
            ["Số HĐ"] = h => h.SoHopDong,
            ["Mã HS"] = h => h.HoSoVay?.MaHoSoHienThi ?? "",
            ["Khách hàng"] = h => h.HoSoVay?.KhachHang?.HoTen ?? "",
            ["Tiền gốc"] = h => h.TienGoc,
            ["Lãi suất (%)"] = h => h.LaiSuat * 100,
            ["Ngày bắt đầu"] = h => h.NgayBatDau,
            ["Ngày đáo hạn"] = h => h.NgayDaoHan,
            ["Ngày giải ngân"] = h => h.NgayGiaiNgan,
            ["Tổng phải trả"] = h => h.TongPhaiTra,
            ["Trạng thái"] = h => h.TrangThai,
        };

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(_export.ExportToPdf(data, "DANH SÁCH HỢP ĐỒNG VAY", mappings!), "danh-sach-hop-dong.pdf", "application/pdf"),
            _ => ReturnFile(_export.ExportToExcel(data, "HopDong", "DANH SÁCH HỢP ĐỒNG VAY", mappings!), "danh-sach-hop-dong.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    /// <summary>
    /// Xuất lịch trả nợ của 1 hợp đồng
    /// </summary>
    [HttpGet("hop-dong/{id}/lich-tra-no")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Reporters)]
    public async Task<IActionResult> ExportLichTraNo(long id, [FromQuery] string format = "excel")
    {
        var hopDong = await _context.HopDongVays
            .Include(h => h.HoSoVay).ThenInclude(hs => hs!.KhachHang)
            .FirstOrDefaultAsync(h => h.MaHopDong == id);

        if (hopDong == null) return NotFound(new { message = "Không tìm thấy hợp đồng" });

        var data = await _context.LichTraNos
            .Where(l => l.MaHopDong == id)
            .OrderBy(l => l.KyThanhToan)
            .ToListAsync();

        var mappings = new Dictionary<string, Func<dynamic, object?>>
        {
            ["Kỳ"] = l => l.KyThanhToan,
            ["Ngày đến hạn"] = l => l.NgayDenHan,
            ["Gốc phải trả"] = l => l.GocPhaiTra,
            ["Lãi phải trả"] = l => l.LaiPhaiTra,
            ["Phí phạt"] = l => l.PhiPhat,
            ["Tổng"] = l => l.GocPhaiTra + l.LaiPhaiTra + l.PhiPhat,
            ["Trạng thái"] = l => l.TrangThai,
        };

        var title = $"LỊCH TRẢ NỢ - HĐ {hopDong.SoHopDong} - KH {hopDong.HoSoVay?.KhachHang?.HoTen ?? ""}";

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(_export.ExportToPdf(data, title, mappings!), $"lich-tra-no-{hopDong.SoHopDong}.pdf", "application/pdf"),
            _ => ReturnFile(_export.ExportToExcel(data, "LichTraNo", title, mappings!), $"lich-tra-no-{hopDong.SoHopDong}.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    /// <summary>
    /// Xuất danh sách thanh toán
    /// </summary>
    [HttpGet("thanh-toan")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Reporters)]
    public async Task<IActionResult> ExportThanhToan(
        [FromQuery] string format = "excel",
        [FromQuery] DateTime? tuNgay = null,
        [FromQuery] DateTime? denNgay = null)
    {
        var query = _context.ThanhToans
            .Include(t => t.HopDongVay)
            .Include(t => t.HoSoVay).ThenInclude(hs => hs!.KhachHang)
            .AsQueryable();

        if (tuNgay.HasValue) query = query.Where(t => t.NgayThanhToan >= tuNgay.Value);
        if (denNgay.HasValue)
        {
            var end = denNgay.Value.Date.AddDays(1);
            query = query.Where(t => t.NgayThanhToan < end);
        }

        var data = await query.OrderByDescending(t => t.NgayThanhToan).ToListAsync();

        var mappings = new Dictionary<string, Func<dynamic, object?>>
        {
            ["Mã TT"] = t => t.MaGiaoDich,
            ["Số HĐ"] = t => t.HopDongVay?.SoHopDong ?? "",
            ["Mã HS"] = t => t.HoSoVay?.MaHoSoHienThi ?? "",
            ["Khách hàng"] = t => t.HoSoVay?.KhachHang?.HoTen ?? "",
            ["Số tiền"] = t => t.SoTienDaTra,
            ["Gốc"] = t => t.SoTienApGoc,
            ["Lãi"] = t => t.SoTienApLai,
            ["Phí"] = t => t.SoTienApPhi,
            ["Phương thức"] = t => t.PhuongThucThanhToan ?? "",
            ["Kênh"] = t => t.KenhThanhToan ?? "",
            ["Ngày TT"] = t => t.NgayThanhToan,
        };

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(_export.ExportToPdf(data, "DANH SÁCH THANH TOÁN", mappings!), "danh-sach-thanh-toan.pdf", "application/pdf"),
            _ => ReturnFile(_export.ExportToExcel(data, "ThanhToan", "DANH SÁCH THANH TOÁN", mappings!), "danh-sach-thanh-toan.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    /// <summary>
    /// Xuất báo cáo thống kê dashboard
    /// </summary>
    [HttpGet("dashboard")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Reporters)]
    public async Task<IActionResult> ExportDashboard([FromQuery] string format = "excel")
    {
        var now = DateTime.Now;
        var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);

        // Build summary
        var data = new List<Dictionary<string, object?>>
        {
            new() { ["Chỉ tiêu"] = "Tổng khách hàng", ["Giá trị"] = await _context.KhachHangs.CountAsync() },
            new() { ["Chỉ tiêu"] = "Tổng hồ sơ vay", ["Giá trị"] = await _context.HoSoVays.CountAsync() },
            new() { ["Chỉ tiêu"] = "Tổng hợp đồng", ["Giá trị"] = await _context.HopDongVays.CountAsync() },
            new() { ["Chỉ tiêu"] = "Tổng giải ngân", ["Giá trị"] = await _context.HopDongVays.Where(h => h.NgayGiaiNgan.HasValue).SumAsync(h => h.TienGoc) },
            new() { ["Chỉ tiêu"] = "Hồ sơ mới", ["Giá trị"] = await _context.HoSoVays.CountAsync(h => h.TrangThai == "MOI") },
            new() { ["Chỉ tiêu"] = "Hồ sơ đang xử lý", ["Giá trị"] = await _context.HoSoVays.CountAsync(h => h.TrangThai == "DANG_THAM_DINH" || h.TrangThai == "CHO_PHE_DUYET") },
            new() { ["Chỉ tiêu"] = "Hồ sơ đã duyệt", ["Giá trị"] = await _context.HoSoVays.CountAsync(h => h.TrangThai == "DA_PHE_DUYET" || h.TrangThai == "DA_GIAI_NGAN") },
            new() { ["Chỉ tiêu"] = "Hồ sơ từ chối", ["Giá trị"] = await _context.HoSoVays.CountAsync(h => h.TrangThai == "TU_CHOI") },
            new() { ["Chỉ tiêu"] = "Giải ngân tháng này", ["Giá trị"] = await _context.HopDongVays.Where(h => h.NgayGiaiNgan >= firstDayOfMonth).SumAsync(h => h.TienGoc) },
        };

        // Top 10 khách hàng
        var topKH = await _context.HopDongVays
            .Where(h => h.NgayGiaiNgan.HasValue)
            .GroupBy(h => h.MaHoSo)
            .Select(g => new
            {
                g.First().HoSoVay!.MaKhachHang,
                TongVay = g.Sum(x => x.TienGoc),
                g.First().HoSoVay!.KhachHang!.HoTen
            })
            .GroupBy(x => x.MaKhachHang)
            .Select(g => new
            {
                MaKH = g.Key,
                HoTen = g.First().HoTen,
                TongVay = g.Sum(x => x.TongVay)
            })
            .OrderByDescending(x => x.TongVay)
            .Take(10)
            .ToListAsync();

        // Tạo file Excel với 2 sheet
        using var workbook = new ClosedXML.Excel.XLWorkbook();

        // Sheet 1: Thống kê tổng quan
        var sheet1 = workbook.Worksheets.Add("ThongKe");
        sheet1.Cell("A1").Value = "BÁO CÁO THỐNG KÊ TỔNG QUAN";
        sheet1.Cell("A1").Style.Font.Bold = true;
        sheet1.Cell("A1").Style.Font.FontSize = 16;
        sheet1.Cell("A1").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        sheet1.Range("A1:B1").Merge();

        sheet1.Cell("A3").Value = "Chỉ tiêu";
        sheet1.Cell("B3").Value = "Giá trị";
        sheet1.Range("A3:B3").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2563EB");
        sheet1.Range("A3:B3").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        sheet1.Range("A3:B3").Style.Font.Bold = true;

        int row = 4;
        foreach (var item in data)
        {
            sheet1.Cell(row, 1).Value = item["Chỉ tiêu"]?.ToString();
            sheet1.Cell(row, 2).Value = item["Giá trị"]?.ToString();
            row++;
        }
        sheet1.Columns().AdjustToContents();

        // Sheet 2: Top khách hàng
        var sheet2 = workbook.Worksheets.Add("TopKhachHang");
        sheet2.Cell("A1").Value = "TOP 10 KHÁCH HÀNG VAY NHIỀU NHẤT";
        sheet2.Cell("A1").Style.Font.Bold = true;
        sheet2.Cell("A1").Style.Font.FontSize = 16;
        sheet2.Cell("A1").Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
        sheet2.Range("A1:C1").Merge();

        sheet2.Cell("A3").Value = "Họ tên";
        sheet2.Cell("B3").Value = "Mã KH";
        sheet2.Cell("C3").Value = "Tổng vay";
        sheet2.Range("A3:C3").Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.FromHtml("#2563EB");
        sheet2.Range("A3:C3").Style.Font.FontColor = ClosedXML.Excel.XLColor.White;
        sheet2.Range("A3:C3").Style.Font.Bold = true;

        row = 4;
        int stt = 1;
        foreach (var kh in topKH)
        {
            sheet2.Cell(row, 1).Value = kh.HoTen;
            sheet2.Cell(row, 2).Value = kh.MaKH;
            sheet2.Cell(row, 3).Value = kh.TongVay;
            sheet2.Cell(row, 3).Style.NumberFormat.Format = "#,##0";
            row++;
            stt++;
        }
        sheet2.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(stream.ToArray(), "bao-cao-thong-ke.pdf", "application/pdf"),
            _ => ReturnFile(stream.ToArray(), "bao-cao-thong-ke.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    /// <summary>
    /// Xuất nhật ký hệ thống (chỉ ADMIN)
    /// </summary>
    [HttpGet("audit-log")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ExportAuditLog(
        [FromQuery] string format = "excel",
        [FromQuery] string? hanhDong = null,
        [FromQuery] string? tenThucThe = null,
        [FromQuery] DateTime? tuNgay = null,
        [FromQuery] DateTime? denNgay = null)
    {
        var query = _context.NhatKyHeThongs.AsQueryable();
        if (!string.IsNullOrEmpty(hanhDong)) query = query.Where(n => n.HanhDong == hanhDong);
        if (!string.IsNullOrEmpty(tenThucThe)) query = query.Where(n => n.TenThucThe == tenThucThe);
        if (tuNgay.HasValue) query = query.Where(n => n.NgayTao >= tuNgay.Value);
        if (denNgay.HasValue)
        {
            var end = denNgay.Value.Date.AddDays(1);
            query = query.Where(n => n.NgayTao < end);
        }

        var data = await query.OrderByDescending(n => n.NgayTao).Take(10000).ToListAsync();

        // Lấy tên user
        var userIds = data.Where(n => n.MaNguoiDung.HasValue).Select(n => n.MaNguoiDung!.Value).Distinct().ToList();
        var userDict = await _context.NguoiDungs
            .Where(u => userIds.Contains(u.MaNguoiDung))
            .ToDictionaryAsync(u => u.MaNguoiDung, u => u.HoTen);

        string ResolveUserName(long? maNguoiDung)
        {
            if (!maNguoiDung.HasValue) return "Hệ thống";
            return userDict.TryGetValue(maNguoiDung.Value, out var ten) ? ten : "Hệ thống";
        }

        var mappings = new Dictionary<string, Func<dynamic, object?>>
        {
            ["Thời gian"] = n => n.NgayTao,
            ["Người thực hiện"] = n => ResolveUserName(n.MaNguoiDung),
            ["Hành động"] = n => n.HanhDong,
            ["Đối tượng"] = n => n.TenThucThe,
            ["Mã"] = n => n.MaThucThe,
            ["Chi tiết"] = n => n.GiaTriMoi ?? "",
            ["Giá trị cũ"] = n => n.GiaTriCu ?? "",
            ["IP"] = n => n.IpNguon ?? "",
        };

        return format.ToLower() switch
        {
            "pdf" => ReturnFile(_export.ExportToPdf(data, "NHẬT KÝ HỆ THỐNG", mappings!), "nhat-ky-he-thong.pdf", "application/pdf"),
            _ => ReturnFile(_export.ExportToExcel(data, "NhatKy", "NHẬT KÝ HỆ THỐNG", mappings!), "nhat-ky-he-thong.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        };
    }

    private FileContentResult ReturnFile(byte[] bytes, string fileName, string contentType)
    {
        // Encode tên file theo chuẩn RFC 5987 để hỗ trợ tiếng Việt
        var encoded = System.Net.WebUtility.UrlEncode(fileName);
        Response.Headers.Append("Content-Disposition",
            $"attachment; filename=\"{fileName}\"; filename*=UTF-8''{encoded}");
        return File(bytes, contentType);
    }
}
