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
[Route("api/tai-san-dam-bao")]
[Authorize]
public class TaiSanDamBaoController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<TaiSanDamBaoController> _logger;

    public TaiSanDamBaoController(AppDbContext context, IAuditLogService auditLog, ILogger<TaiSanDamBaoController> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    /// <summary>
    /// Lấy danh sách TSBĐ (lọc theo hồ sơ)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<TaiSanDamBaoDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] long? maHoSo = null,
        [FromQuery] string? loaiTaiSan = null)
    {
        var query = _context.TaiSanDamBaos.AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        if (maHoSo.HasValue)
            query = query.Where(t => t.MaHoSo == maHoSo);
        if (!string.IsNullOrEmpty(loaiTaiSan))
            query = query.Where(t => t.LoaiTaiSan == loaiTaiSan);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.NgayTao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Join(_context.HoSoVays,
                t => t.MaHoSo,
                h => h.MaHoSo,
                (t, h) => new TaiSanDamBaoDto
                {
                    MaTaiSan = t.MaTaiSan,
                    MaHoSo = t.MaHoSo,
                    MaHoSoHienThi = h.MaHoSoHienThi,
                    LoaiTaiSan = t.LoaiTaiSan,
                    ChuSoHuu = t.ChuSoHuu,
                    SoGiayChungNhan = t.SoGiayChungNhan,
                    DiaChiTaiSan = t.DiaChiTaiSan,
                    GiaTriUocTinh = t.GiaTriUocTinh,
                    TyLeVayGiaTriTS = t.TyLeVayGiaTriTS,
                    TinhTrangPhapLy = t.TinhTrangPhapLy,
                    NgayDinhGia = t.NgayDinhGia,
                    DonViDinhGia = t.DonViDinhGia,
                    SoHopDongBaoHiem = t.SoHopDongBaoHiem,
                    NgayHetHanBaoHiem = t.NgayHetHanBaoHiem,
                    NgayTao = t.NgayTao,
                    NgayCapNhat = t.NgayCapNhat
                })
            .ToListAsync();

        return Ok(new PagedResult<TaiSanDamBaoDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TaiSanDamBaoDto>> GetById(long id)
    {
        var item = await _context.TaiSanDamBaos.FindAsync(id);
        if (item == null) return NotFound();

        var hoSo = await _context.HoSoVays.FindAsync(item.MaHoSo);
        return Ok(new TaiSanDamBaoDto
        {
            MaTaiSan = item.MaTaiSan,
            MaHoSo = item.MaHoSo,
            MaHoSoHienThi = hoSo?.MaHoSoHienThi ?? string.Empty,
            LoaiTaiSan = item.LoaiTaiSan,
            ChuSoHuu = item.ChuSoHuu,
            SoGiayChungNhan = item.SoGiayChungNhan,
            DiaChiTaiSan = item.DiaChiTaiSan,
            GiaTriUocTinh = item.GiaTriUocTinh,
            TyLeVayGiaTriTS = item.TyLeVayGiaTriTS,
            TinhTrangPhapLy = item.TinhTrangPhapLy,
            NgayDinhGia = item.NgayDinhGia,
            DonViDinhGia = item.DonViDinhGia,
            SoHopDongBaoHiem = item.SoHopDongBaoHiem,
            NgayHetHanBaoHiem = item.NgayHetHanBaoHiem,
            NgayTao = item.NgayTao,
            NgayCapNhat = item.NgayCapNhat
        });
    }

    [HttpPost]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.CollateralManagers)]
    public async Task<ActionResult<TaiSanDamBaoDto>> Create([FromBody] CreateTaiSanDamBaoDto dto)
    {
        try
        {
            _logger.LogInformation("Tạo TSBĐ mới cho hồ sơ: {MaHoSo}, loại: {LoaiTaiSan}, giá trị: {GiaTri}",
                dto.MaHoSo, dto.LoaiTaiSan, dto.GiaTriUocTinh);

            // Validate đầu vào
            if (string.IsNullOrWhiteSpace(dto.LoaiTaiSan))
                return BadRequest(new { message = "Loại tài sản không được để trống" });
            if (dto.GiaTriUocTinh <= 0)
                return BadRequest(new { message = "Giá trị ước tính phải lớn hơn 0" });
            if (dto.TyLeVayGiaTriTS < 0 || dto.TyLeVayGiaTriTS > 1)
                return BadRequest(new { message = "Tỷ lệ vay/giá trị TSBĐ phải nằm trong khoảng 0 đến 1" });
            if (string.IsNullOrWhiteSpace(dto.TinhTrangPhapLy))
                return BadRequest(new { message = "Tình trạng pháp lý không được để trống" });

            // Kiểm tra hồ sơ tồn tại
            var hoSo = await _context.HoSoVays.FindAsync(dto.MaHoSo);
            if (hoSo == null)
            {
                return BadRequest(new { message = $"Hồ sơ {dto.MaHoSo} không tồn tại" });
            }

            // RB38: Kiểm tra tổng giá trị TSBĐ cho vay tối đa >= số tiền vay đề nghị
            // Logic: có thể thêm 1 phần tài sản, tổng cuối cùng mới phải đủ
            var tongGiaTriChoVayToiDaMoi = dto.GiaTriUocTinh * dto.TyLeVayGiaTriTS;
            var tongTSTonTai = await _context.TaiSanDamBaos
                .Where(t => t.MaHoSo == dto.MaHoSo)
                .SumAsync(t => (decimal?)(t.GiaTriUocTinh * t.TyLeVayGiaTriTS)) ?? 0m;
            var tongSauKhiThem = tongTSTonTai + tongGiaTriChoVayToiDaMoi;
            if (tongSauKhiThem < hoSo.SoTienDeNghi)
            {
                return BadRequest(new
                {
                    message = $"RB38: Tổng giá trị TSBĐ cho vay tối đa ({tongSauKhiThem:N0}đ) < số tiền đề nghị ({hoSo.SoTienDeNghi:N0}đ). Cần bổ sung thêm TSBĐ trị giá tối thiểu {(hoSo.SoTienDeNghi - tongSauKhiThem):N0}đ (tính theo LTV {dto.TyLeVayGiaTriTS * 100}%).",
                    chiThieu = hoSo.SoTienDeNghi - tongSauKhiThem,
                    tongHienTai = tongSauKhiThem,
                    soTienDeNghi = hoSo.SoTienDeNghi
                });
            }

            var item = new TaiSanDamBao
            {
                MaHoSo = dto.MaHoSo,
                LoaiTaiSan = dto.LoaiTaiSan,
                ChuSoHuu = dto.ChuSoHuu,
                SoGiayChungNhan = dto.SoGiayChungNhan,
                DiaChiTaiSan = dto.DiaChiTaiSan,
                GiaTriUocTinh = dto.GiaTriUocTinh,
                TyLeVayGiaTriTS = dto.TyLeVayGiaTriTS,
                TinhTrangPhapLy = dto.TinhTrangPhapLy,
                NgayDinhGia = dto.NgayDinhGia,
                DonViDinhGia = dto.DonViDinhGia,
                SoHopDongBaoHiem = dto.SoHopDongBaoHiem,
                NgayHetHanBaoHiem = dto.NgayHetHanBaoHiem,
                NgayTao = DateTime.Now,
                NgayCapNhat = DateTime.Now
            };

            _context.TaiSanDamBaos.Add(item);
            await _context.SaveChangesAsync();

            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.CREATE,
                tenThucThe: AuditEntities.TAI_SAN_DAM_BAO,
                maThucThe: item.MaTaiSan.ToString(),
                giaTriMoi: $"Thêm TSBĐ #{item.MaTaiSan}: {item.LoaiTaiSan} - giá trị {item.GiaTriUocTinh:N0}đ cho HS#{item.MaHoSo}");

            return CreatedAtAction(nameof(GetById), new { id = item.MaTaiSan }, new TaiSanDamBaoDto
            {
                MaTaiSan = item.MaTaiSan,
                MaHoSo = item.MaHoSo,
                MaHoSoHienThi = hoSo.MaHoSoHienThi,
                LoaiTaiSan = item.LoaiTaiSan,
                ChuSoHuu = item.ChuSoHuu,
                SoGiayChungNhan = item.SoGiayChungNhan,
                DiaChiTaiSan = item.DiaChiTaiSan,
                GiaTriUocTinh = item.GiaTriUocTinh,
                TyLeVayGiaTriTS = item.TyLeVayGiaTriTS,
                TinhTrangPhapLy = item.TinhTrangPhapLy,
                NgayDinhGia = item.NgayDinhGia,
                DonViDinhGia = item.DonViDinhGia,
                SoHopDongBaoHiem = item.SoHopDongBaoHiem,
                NgayHetHanBaoHiem = item.NgayHetHanBaoHiem,
                NgayTao = item.NgayTao,
                NgayCapNhat = item.NgayCapNhat
            });
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi tạo TSBĐ: {Message}", dbEx.InnerException?.Message);
            return BadRequest(new { message = "Không thể lưu tài sản đảm bảo: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi không xác định khi tạo TSBĐ");
            throw; // Để middleware xử lý
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.CollateralManagers)]
    public async Task<ActionResult> Update(long id, [FromBody] UpdateTaiSanDamBaoDto dto)
    {
        try
        {
            var item = await _context.TaiSanDamBaos.FindAsync(id);
            if (item == null) return NotFound();

            var giaTriCu = $"{item.LoaiTaiSan} - {item.GiaTriUocTinh:N0}đ - LTV {item.TyLeVayGiaTriTS * 100}%";

            item.LoaiTaiSan = dto.LoaiTaiSan;
            item.ChuSoHuu = dto.ChuSoHuu;
            item.SoGiayChungNhan = dto.SoGiayChungNhan;
            item.DiaChiTaiSan = dto.DiaChiTaiSan;
            item.GiaTriUocTinh = dto.GiaTriUocTinh;
            item.TyLeVayGiaTriTS = dto.TyLeVayGiaTriTS;
            item.TinhTrangPhapLy = dto.TinhTrangPhapLy;
            item.NgayDinhGia = dto.NgayDinhGia;
            item.DonViDinhGia = dto.DonViDinhGia;
            item.SoHopDongBaoHiem = dto.SoHopDongBaoHiem;
            item.NgayHetHanBaoHiem = dto.NgayHetHanBaoHiem;
            item.NgayCapNhat = DateTime.Now;

            await _context.SaveChangesAsync();

            var giaTriMoi = $"{item.LoaiTaiSan} - {item.GiaTriUocTinh:N0}đ - LTV {item.TyLeVayGiaTriTS * 100}%";
            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.UPDATE,
                tenThucThe: AuditEntities.TAI_SAN_DAM_BAO,
                maThucThe: id.ToString(),
                giaTriCu: giaTriCu,
                giaTriMoi: giaTriMoi);

            return NoContent();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi cập nhật TSBĐ: {Message}", dbEx.InnerException?.Message);
            return BadRequest(new { message = "Không thể cập nhật tài sản: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult> Delete(long id)
    {
        try
        {
            var item = await _context.TaiSanDamBaos.FindAsync(id);
            if (item == null) return NotFound();

            var thongTin = $"{item.LoaiTaiSan} - {item.GiaTriUocTinh:N0}đ";

            _context.TaiSanDamBaos.Remove(item);
            await _context.SaveChangesAsync();

            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.DELETE,
                tenThucThe: AuditEntities.TAI_SAN_DAM_BAO,
                maThucThe: id.ToString(),
                giaTriCu: thongTin);

            return NoContent();
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Lỗi DB khi xóa TSBĐ: {Message}", dbEx.InnerException?.Message);
            return BadRequest(new { message = "Không thể xóa tài sản: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }
    }
}
