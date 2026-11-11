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
public class HopDongController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notification;

    public HopDongController(AppDbContext context, IAuditLogService auditLog, INotificationService notification)
    {
        _context = context;
        _auditLog = auditLog;
        _notification = notification;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HopDongVayDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = _context.HopDongVays
            .Include(h => h.HoSoVay)
            .AsQueryable();

        // Guard: client gửi pageNumber/pageSize <= 0 sẽ làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.NgayTao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HopDongVayDto
            {
                MaHopDong = h.MaHopDong,
                SoHopDong = h.SoHopDong,
                MaHoSo = h.MaHoSo,
                MaHoSoHienThi = h.HoSoVay!.MaHoSoHienThi,
                TienGoc = h.TienGoc,
                LaiSuat = h.LaiSuat,
                NgayBatDau = h.NgayBatDau,
                NgayDaoHan = h.NgayDaoHan,
                NgayGiaiNgan = h.NgayGiaiNgan,
                TongPhaiTra = h.TongPhaiTra,
                TrangThai = h.TrangThai,
                GhiChu = h.GhiChu
            })
            .ToListAsync();

        return Ok(new PagedResult<HopDongVayDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HopDongVayDto>> GetById(long id)
    {
        var h = await _context.HopDongVays
            .Include(x => x.HoSoVay)
            .FirstOrDefaultAsync(x => x.MaHopDong == id);

        if (h == null) return NotFound();

        return Ok(new HopDongVayDto
        {
            MaHopDong = h.MaHopDong,
            SoHopDong = h.SoHopDong,
            MaHoSo = h.MaHoSo,
            MaHoSoHienThi = h.HoSoVay!.MaHoSoHienThi,
            TienGoc = h.TienGoc,
            LaiSuat = h.LaiSuat,
            NgayBatDau = h.NgayBatDau,
            NgayDaoHan = h.NgayDaoHan,
            NgayGiaiNgan = h.NgayGiaiNgan,
            TongPhaiTra = h.TongPhaiTra,
            TrangThai = h.TrangThai,
            GhiChu = h.GhiChu
        });
    }

    [HttpGet("ho-so/{hoSoId}")]
    public async Task<ActionResult<HopDongVayDto>> GetByHoSo(long hoSoId)
    {
        var h = await _context.HopDongVays
            .Include(x => x.HoSoVay)
            .FirstOrDefaultAsync(x => x.MaHoSo == hoSoId);

        if (h == null) return NotFound();

        return Ok(new HopDongVayDto
        {
            MaHopDong = h.MaHopDong,
            SoHopDong = h.SoHopDong,
            MaHoSo = h.MaHoSo,
            MaHoSoHienThi = h.HoSoVay!.MaHoSoHienThi,
            TienGoc = h.TienGoc,
            LaiSuat = h.LaiSuat,
            NgayBatDau = h.NgayBatDau,
            NgayDaoHan = h.NgayDaoHan,
            NgayGiaiNgan = h.NgayGiaiNgan,
            TongPhaiTra = h.TongPhaiTra,
            TrangThai = h.TrangThai,
            GhiChu = h.GhiChu
        });
    }

    [HttpPost]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Finance)]
    public async Task<ActionResult<HopDongVayDto>> Create([FromBody] CreateHopDongDto dto)
    {
        // Validate dates
        if (dto.NgayBatDau == default || dto.NgayDaoHan == default)
        {
            return BadRequest(new { message = "Ngày bắt đầu và ngày đáo hạn là bắt buộc" });
        }
        if (dto.NgayDaoHan <= dto.NgayBatDau)
        {
            return BadRequest(new { message = "Ngày đáo hạn phải sau ngày bắt đầu" });
        }

        var hoSo = await _context.HoSoVays
            .Include(h => h.SanPhamVay)
            .FirstOrDefaultAsync(h => h.MaHoSo == dto.MaHoSo);

        if (hoSo == null) return NotFound(new { message = "Hồ sơ không tồn tại" });

        // Lấy QUYẾT ĐỊNH MỚI NHẤT (rows append-only → phải xem bản gần nhất,
        // không thì 1 bản DA_PHE_DUYET cũ sẽ "luồn" qua sau khi hồ sơ bị TU_CHOI).
        var pheDuyetGanNhat = await _context.PheDuYets
            .Where(p => p.MaHoSo == dto.MaHoSo)
            .OrderByDescending(p => p.NgayPheDuyet)
            .FirstOrDefaultAsync();

        var duocPheDuyet = pheDuyetGanNhat?.QuyetDinh == "DA_PHE_DUYET"
                           || hoSo.TrangThai == "DA_PHE_DUYET";
        if (!duocPheDuyet)
        {
            return BadRequest(new { message = "Hồ sơ chưa được phê duyệt hoặc đã bị từ chối" });
        }

        // Check if hồ sơ đã có hợp đồng
        var existing = await _context.HopDongVays.AnyAsync(x => x.MaHoSo == dto.MaHoSo);
        if (existing)
        {
            return BadRequest(new { message = "Hồ sơ này đã có hợp đồng" });
        }

        var soTienVay = pheDuyetGanNhat != null && pheDuyetGanNhat.QuyetDinh == "DA_PHE_DUYET"
            ? pheDuyetGanNhat.SoTienDuocDuyet ?? hoSo.SoTienDeNghi
            : hoSo.SoTienDeNghi;
        var laiSuat = pheDuyetGanNhat != null && pheDuyetGanNhat.QuyetDinh == "DA_PHE_DUYET"
            ? pheDuyetGanNhat.LaiSuatDuocDuyet ?? hoSo.SanPhamVay!.LaiSuat
            : hoSo.SanPhamVay!.LaiSuat;

        // Validate lai suat range theo DB CHECK
        if (laiSuat <= 0 || laiSuat > 0.30m)
        {
            return BadRequest(new { message = $"Lãi suất không hợp lệ: {laiSuat}. Phải > 0 và <= 30%" });
        }

        if (soTienVay <= 0)
        {
            return BadRequest(new { message = "Số tiền vay phải > 0" });
        }

        var soHopDong = $"HD-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        var hopDong = new HopDongVay
        {
            SoHopDong = soHopDong,
            MaHoSo = dto.MaHoSo,
            TienGoc = soTienVay,
            LaiSuat = laiSuat,
            NgayBatDau = dto.NgayBatDau,
            NgayDaoHan = dto.NgayDaoHan,
            TrangThai = "HOAT_DONG",
            GhiChu = dto.GhiChu,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        using var transaction = await _context.Database.BeginTransactionAsync();

        // Update ho_so_vay bằng SQL thô để tránh lỗi trigger khi EF dùng OUTPUT clause.
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE ho_so_vay SET ngay_cap_nhat = {0}, trang_thai = {1} WHERE ma_ho_so = {2}",
            DateTime.Now,
            "DA_LAP_HOP_DONG",
            dto.MaHoSo);

        _context.HopDongVays.Add(hopDong);

        // Lịch trả nợ được tạo TRONG transaction (SaveChangesAsync bên trong
        // CreateLichTraNo tham gia transaction đang mở) → không có trường hợp
        // hợp đồng đã commit mà thiếu lịch trả nợ.
        await CreateLichTraNo(hopDong);

        await transaction.CommitAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.CREATE,
            tenThucThe: AuditEntities.HOP_DONG_VAY,
            maThucThe: hopDong.SoHopDong,
            giaTriMoi: $"Lập HĐ cho HS#{dto.MaHoSo}: {soTienVay:N0}đ - LS: {laiSuat * 100}% - từ {dto.NgayBatDau:dd/MM/yyyy} đến {dto.NgayDaoHan:dd/MM/yyyy}");

        // Thông báo cho kế toán (cần giải ngân)
        await _notification.SendToRoleAsync("ACCOUNTANT",
            $"Hợp đồng mới cần giải ngân: {hopDong.SoHopDong}",
            $"HĐ {hopDong.SoHopDong} - Số tiền: {soTienVay:N0}đ đã được lập, cần giải ngân.",
            maHoSo: hopDong.MaHoSo);

        return CreatedAtAction(nameof(GetById), new { id = hopDong.MaHopDong }, await GetById(hopDong.MaHopDong));
    }

    private async Task CreateLichTraNo(HopDongVay hopDong)
    {
        // Tính số kỳ thanh toán theo số tháng giữa 2 ngày
        var months = ((hopDong.NgayDaoHan.Year - hopDong.NgayBatDau.Year) * 12)
                   + (hopDong.NgayDaoHan.Month - hopDong.NgayBatDau.Month);

        if (months <= 0) months = 1; // Bảo vệ chia cho 0

        // ==== Công thức dư nợ giảm dần (Reducing Balance) ====
        // Lãi suất tháng = lãi suất năm / 12
        // Mỗi kỳ:
        //   Lãi kỳ n = Dư nợ đầu kỳ × lãi suất tháng
        //   Gốc kỳ n = (Tổng gốc / số kỳ)  ← chia đều
        //   Tổng trả kỳ n = Gốc + Lãi
        //   Dư nợ cuối kỳ n = Dư nợ đầu kỳ - Gốc trả
        //
        // Lưu ý: Tổng lãi phải trả = Σ(lãi kỳ), KHÔNG dùng công thức gốc/lãi phẳng

        var laiSuatThang = hopDong.LaiSuat / 12m;
        var gocMoiKy = Math.Round(hopDong.TienGoc / months, 2);
        var duNoHienTai = hopDong.TienGoc;
        decimal tongPhaiTra = 0;

        for (int i = 1; i <= months; i++)
        {
            // Lãi kỳ này = dư nợ hiện tại × lãi suất tháng
            var laiKy = Math.Round(duNoHienTai * laiSuatThang, 2);

            // Kỳ cuối: điều chỉnh gốc để dư nợ về đúng 0 (tránh sai số làm tròn)
            decimal gocKy;
            if (i == months)
            {
                gocKy = duNoHienTai;
            }
            else
            {
                gocKy = gocMoiKy;
            }

            duNoHienTai -= gocKy;
            if (duNoHienTai < 0) duNoHienTai = 0; // tránh âm do làm tròn

            var tongKy = gocKy + laiKy;
            tongPhaiTra += tongKy;

            var lichTraNo = new LichTraNo
            {
                MaHopDong = hopDong.MaHopDong,
                KyThanhToan = i,
                NgayDenHan = hopDong.NgayBatDau.AddMonths(i),
                GocPhaiTra = gocKy,
                LaiPhaiTra = laiKy,
                PhiPhat = 0,
                TrangThai = "CHUA_THANH_TOAN",
                NgayTao = DateTime.Now
            };

            _context.LichTraNos.Add(lichTraNo);
        }

        // Cập nhật tổng phải trả thực tế (không dùng công thức ước lượng cũ)
        hopDong.TongPhaiTra = tongPhaiTra;

        await _context.SaveChangesAsync();
    }

    [HttpPost("{id}/giai-ngan")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Finance)]
    public async Task<ActionResult> GiaiNgan(long id)
    {
        var hopDong = await _context.HopDongVays.FindAsync(id);
        if (hopDong == null) return NotFound();

        if (hopDong.NgayGiaiNgan.HasValue)
            return BadRequest("Hợp đồng đã được giải ngân");

        hopDong.NgayGiaiNgan = DateTime.Now;
        hopDong.TrangThai = "HOAT_DONG";
        hopDong.NgayCapNhat = DateTime.Now;

        using var transaction = await _context.Database.BeginTransactionAsync();

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE ho_so_vay SET ngay_cap_nhat = {0}, trang_thai = {1} WHERE ma_ho_so = {2}",
            DateTime.Now,
            "DA_GIAI_NGAN",
            hopDong.MaHoSo);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.DISBURSE,
            tenThucThe: AuditEntities.HOP_DONG_VAY,
            maThucThe: hopDong.SoHopDong,
            giaTriMoi: $"Giải ngân {hopDong.TienGoc:N0}đ");

        // Thông báo cho cán bộ tạo hợp đồng
        var hoSoForNotify = await _context.HoSoVays.FindAsync(hopDong.MaHoSo);
        if (hoSoForNotify != null)
        {
            await _notification.SendAsync(hoSoForNotify.MaCanBo,
                $"Hợp đồng {hopDong.SoHopDong} đã giải ngân",
                $"Đã giải ngân thành công {hopDong.TienGoc:N0}đ vào hợp đồng {hopDong.SoHopDong}.",
                maHoSo: hopDong.MaHoSo);
        }

        return Ok(new { message = "Giải ngân thành công" });
    }

    [HttpGet("{id}/lich-tra-no")]
    public async Task<ActionResult<List<LichTraNoDto>>> GetLichTraNo(long id)
    {
        var items = await _context.LichTraNos
            .Where(l => l.MaHopDong == id)
            .OrderBy(l => l.KyThanhToan)
            .Select(l => new LichTraNoDto
            {
                MaLichTraNo = l.MaLichTraNo,
                MaHopDong = l.MaHopDong,
                KyThanhToan = l.KyThanhToan,
                NgayDenHan = l.NgayDenHan,
                GocPhaiTra = l.GocPhaiTra,
                LaiPhaiTra = l.LaiPhaiTra,
                PhiPhat = l.PhiPhat,
                TrangThai = l.TrangThai
            })
            .ToListAsync();

        return Ok(items);
    }
}
