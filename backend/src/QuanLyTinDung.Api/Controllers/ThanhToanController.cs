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
public class ThanhToanController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<ThanhToanController> _logger;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notification;

    public ThanhToanController(AppDbContext context, ILogger<ThanhToanController> logger, IAuditLogService auditLog, INotificationService notification)
    {
        _context = context;
        _logger = logger;
        _auditLog = auditLog;
        _notification = notification;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ThanhToanDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.ThanhToans
            .Include(t => t.HopDongVay)
            .Include(t => t.HoSoVay)
            .AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.NgayThanhToan)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new ThanhToanDto
            {
                MaThanhToan = t.MaThanhToan,
                MaLichTraNo = t.MaLichTraNo,
                MaHopDong = t.MaHopDong,
                SoHopDong = t.HopDongVay!.SoHopDong,
                MaHoSo = t.MaHoSo,
                MaHoSoHienThi = t.HoSoVay!.MaHoSoHienThi,
                SoTienDaTra = t.SoTienDaTra,
                SoTienApGoc = t.SoTienApGoc,
                SoTienApLai = t.SoTienApLai,
                SoTienApPhi = t.SoTienApPhi,
                NgayThanhToan = t.NgayThanhToan,
                PhuongThucThanhToan = t.PhuongThucThanhToan,
                MaGiaoDich = t.MaGiaoDich,
                KenhThanhToan = t.KenhThanhToan,
                GhiChu = t.GhiChu
            })
            .ToListAsync();

        return Ok(new PagedResult<ThanhToanDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("hop-dong/{hopDongId}")]
    public async Task<ActionResult<List<ThanhToanDto>>> GetByHopDong(long hopDongId)
    {
        var items = await _context.ThanhToans
            .Include(t => t.HopDongVay)
            .Include(t => t.HoSoVay)
            .Where(t => t.MaHopDong == hopDongId)
            .OrderByDescending(t => t.NgayThanhToan)
            .Select(t => new ThanhToanDto
            {
                MaThanhToan = t.MaThanhToan,
                MaLichTraNo = t.MaLichTraNo,
                MaHopDong = t.MaHopDong,
                SoHopDong = t.HopDongVay!.SoHopDong,
                MaHoSo = t.MaHoSo,
                MaHoSoHienThi = t.HoSoVay!.MaHoSoHienThi,
                SoTienDaTra = t.SoTienDaTra,
                SoTienApGoc = t.SoTienApGoc,
                SoTienApLai = t.SoTienApLai,
                SoTienApPhi = t.SoTienApPhi,
                NgayThanhToan = t.NgayThanhToan,
                PhuongThucThanhToan = t.PhuongThucThanhToan,
                MaGiaoDich = t.MaGiaoDich,
                KenhThanhToan = t.KenhThanhToan,
                GhiChu = t.GhiChu
            })
            .ToListAsync();

        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Finance)]
    public async Task<ActionResult<ThanhToanDto>> Create([FromBody] CreateThanhToanDto dto)
    {
        _logger.LogInformation("Tạo thanh toán: LichTraNo={MaLichTraNo}, HopDong={MaHopDong}, HoSo={MaHoSo}, SoTien={SoTienDaTra}",
            dto.MaLichTraNo, dto.MaHopDong, dto.MaHoSo, dto.SoTienDaTra);

        // 1. Validate dữ liệu đầu vào
        if (dto.SoTienDaTra <= 0)
        {
            return BadRequest(new { message = "Số tiền thanh toán phải lớn hơn 0" });
        }
        if (dto.MaLichTraNo <= 0 || dto.MaHopDong <= 0 || dto.MaHoSo <= 0)
        {
            return BadRequest(new { message = "Mã lịch trả nợ / hợp đồng / hồ sơ không hợp lệ" });
        }

        // 2. Kiểm tra FK tồn tại
        var lichTraNo = await _context.LichTraNos
            .Include(l => l.HopDongVay)
            .FirstOrDefaultAsync(l => l.MaLichTraNo == dto.MaLichTraNo);
        if (lichTraNo == null)
        {
            return BadRequest(new { message = $"Không tìm thấy lịch trả nợ với mã {dto.MaLichTraNo}" });
        }
        if (lichTraNo.TrangThai == "DA_THANH_TOAN")
        {
            return BadRequest(new { message = "Kỳ trả nợ này đã được thanh toán" });
        }
        if (lichTraNo.MaHopDong != dto.MaHopDong)
        {
            return BadRequest(new { message = $"Mã lịch trả nợ {dto.MaLichTraNo} không thuộc hợp đồng {dto.MaHopDong}" });
        }

        var hopDong = await _context.HopDongVays
            .Include(h => h.HoSoVay)
            .FirstOrDefaultAsync(h => h.MaHopDong == dto.MaHopDong);
        if (hopDong == null)
        {
            return BadRequest(new { message = $"Không tìm thấy hợp đồng với mã {dto.MaHopDong}" });
        }
        if (hopDong.MaHoSo != dto.MaHoSo)
        {
            return BadRequest(new { message = $"Mã hợp đồng {dto.MaHopDong} không thuộc hồ sơ {dto.MaHoSo}" });
        }

        // 3. Chuẩn hóa phương thức thanh toán (tránh lỗi CHECK constraint nếu có)
        var phuongThuc = (dto.PhuongThucThanhToan ?? "TIEN_MAT").Trim().ToUpper();
        if (string.IsNullOrEmpty(phuongThuc)) phuongThuc = "TIEN_MAT";

        // 4. Tạo bản ghi thanh toán (kèm GUID để tránh trùng UNIQUE ma_giao_dich khi
        //    sinh theo giây — 2 giao dịch trong cùng 1 giây)
        var thanhToan = new ThanhToan
        {
            MaLichTraNo = dto.MaLichTraNo,
            MaHopDong = dto.MaHopDong,
            MaHoSo = dto.MaHoSo,
            SoTienDaTra = dto.SoTienDaTra,
            SoTienApGoc = dto.SoTienApGoc,
            SoTienApLai = dto.SoTienApLai,
            SoTienApPhi = dto.SoTienApPhi,
            NgayThanhToan = dto.NgayThanhToan,
            PhuongThucThanhToan = phuongThuc,
            MaGiaoDich = dto.MaGiaoDich ?? $"TT-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
            KenhThanhToan = dto.KenhThanhToan,
            GhiChu = dto.GhiChu,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        try
        {
            // 1 transaction: ghi thanh toán + cập nhật trạng thái lịch trả nợ.
            // Nếu lỗi giữa chừng → cả 2 bảng rollback (tránh "đã ghi nhận mà
            // lịch trả nợ vẫn CHUA_THANH_TOAN").
            using var transaction = await _context.Database.BeginTransactionAsync();

            // Tính trạng thái kỳ trả nợ SAU giao dịch này (chưa SaveChanges).
            var tongDaTraTruoc = await _context.ThanhToans
                .Where(t => t.MaLichTraNo == dto.MaLichTraNo)
                .SumAsync(t => t.SoTienDaTra);
            var tongPhaiTraKy = lichTraNo.GocPhaiTra + lichTraNo.LaiPhaiTra + lichTraNo.PhiPhat;
            var tongDaTraSau = tongDaTraTruoc + dto.SoTienDaTra;

            if (tongDaTraSau >= tongPhaiTraKy)
                lichTraNo.TrangThai = "DA_THANH_TOAN";
            else if (tongDaTraSau > 0)
                lichTraNo.TrangThai = "THANH_TOAN_MOT_PHAN";

            _context.ThanhToans.Add(thanhToan);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.PAYMENT,
                tenThucThe: AuditEntities.THANH_TOAN,
                maThucThe: thanhToan.MaGiaoDich ?? thanhToan.MaThanhToan.ToString(),
                giaTriMoi: $"TT kỳ #{dto.MaLichTraNo} của HĐ#{dto.MaHopDong}: {dto.SoTienDaTra:N0}đ - Kênh: {phuongThuc}");

            // Thông báo khi kỳ thanh toán ĐẠT đủ (dựa trên trạng thái MỚI đã tính
            // ở trên, không phải entity stale như trước).
            if (lichTraNo.TrangThai == "DA_THANH_TOAN")
            {
                var hd = await _context.HopDongVays.FindAsync(dto.MaHopDong);
                if (hd != null)
                {
                    var hs = await _context.HoSoVays.FindAsync(hd.MaHoSo);
                    if (hs != null)
                    {
                        await _notification.SendAsync(hs.MaCanBo,
                            $"Hợp đồng {hd.SoHopDong} - Thanh toán hoàn tất kỳ {lichTraNo.KyThanhToan}",
                            $"Kỳ {lichTraNo.KyThanhToan} của HĐ {hd.SoHopDong} đã được thanh toán đủ {dto.SoTienDaTra:N0}đ.",
                            maHoSo: hd.MaHoSo);
                    }
                }
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi tạo thanh toán: {Message}", dbEx.InnerException?.Message ?? dbEx.Message);
            return BadRequest(new { message = "Không thể lưu thanh toán: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }

        // 5. Trả về DTO (không dùng CreatedAtAction để tránh gọi lại action khác)
        var result = new ThanhToanDto
        {
            MaThanhToan = thanhToan.MaThanhToan,
            MaLichTraNo = thanhToan.MaLichTraNo,
            MaHopDong = thanhToan.MaHopDong,
            SoHopDong = hopDong.SoHopDong,
            MaHoSo = thanhToan.MaHoSo,
            MaHoSoHienThi = hopDong.HoSoVay?.MaHoSoHienThi ?? string.Empty,
            SoTienDaTra = thanhToan.SoTienDaTra,
            SoTienApGoc = thanhToan.SoTienApGoc,
            SoTienApLai = thanhToan.SoTienApLai,
            SoTienApPhi = thanhToan.SoTienApPhi,
            NgayThanhToan = thanhToan.NgayThanhToan,
            PhuongThucThanhToan = thanhToan.PhuongThucThanhToan,
            MaGiaoDich = thanhToan.MaGiaoDich,
            KenhThanhToan = thanhToan.KenhThanhToan,
            GhiChu = thanhToan.GhiChu
        };

        return Created($"/api/thanhtoan/{thanhToan.MaThanhToan}", result);
    }
}
