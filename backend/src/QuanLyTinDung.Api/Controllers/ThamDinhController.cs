using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Application.Services;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Extensions;
using System.Data.Common;
using System.Security.Claims;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThamDinhController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<ThamDinhController> _logger;

    public ThamDinhController(AppDbContext context, IAuditLogService auditLog,
        ILogger<ThamDinhController> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ThamDinhTinDungDto>>> GetAll([FromQuery] int pageSize = 100)
    {
        var items = await _context.ThamDinhTinDungs
            .Include(t => t.HoSoVay)
            .Include(t => t.CanBoThamDinh)
            .OrderByDescending(t => t.NgayThamDinh)
            .Take(pageSize)
            .Select(t => new ThamDinhTinDungDto
            {
                MaThamDinh = t.MaThamDinh,
                MaHoSo = t.MaHoSo,
                MaHoSoHienThi = t.HoSoVay!.MaHoSoHienThi,
                MaCanBoThamDinh = t.MaCanBoThamDinh,
                TenCanBoThamDinh = t.CanBoThamDinh!.HoTen,
                ThuNhapXacMinh = t.ThuNhapXacMinh,
                ChiPhiHangThang = t.ChiPhiHangThang,
                TyLeNoThuNhap = t.TyLeNoThuNhap,
                DiemThuNhap = t.DiemThuNhap,
                DiemTaiSanBaoDam = t.DiemTaiSanBaoDam,
                DiemCic = t.DiemCic,
                NgayTraCuuCic = t.NgayTraCuuCic,
                DiemRuiRo = t.DiemRuiRo,
                KhuyenNgh = t.KhuyenNgh,
                GhiChu = t.GhiChu,
                NgayThamDinh = t.NgayThamDinh
            })
            .ToListAsync();

        return Ok(new PagedResult<ThamDinhTinDungDto>
        {
            Items = items,
            TotalCount = items.Count,
            PageNumber = 1,
            PageSize = pageSize
        });
    }

    [HttpGet("ho-so/{hoSoId}")]
    public async Task<ActionResult<ThamDinhTinDungDto>> GetByHoSo(long hoSoId)
    {
        var t = await _context.ThamDinhTinDungs
            .Include(x => x.HoSoVay)
            .Include(x => x.CanBoThamDinh)
            .FirstOrDefaultAsync(x => x.MaHoSo == hoSoId);

        if (t == null) return NotFound();

        return Ok(new ThamDinhTinDungDto
        {
            MaThamDinh = t.MaThamDinh,
            MaHoSo = t.MaHoSo,
            MaHoSoHienThi = t.HoSoVay!.MaHoSoHienThi,
            MaCanBoThamDinh = t.MaCanBoThamDinh,
            TenCanBoThamDinh = t.CanBoThamDinh!.HoTen,
            ThuNhapXacMinh = t.ThuNhapXacMinh,
            ChiPhiHangThang = t.ChiPhiHangThang,
            TyLeNoThuNhap = t.TyLeNoThuNhap,
            DiemThuNhap = t.DiemThuNhap,
            DiemTaiSanBaoDam = t.DiemTaiSanBaoDam,
            DiemCic = t.DiemCic,
            NgayTraCuuCic = t.NgayTraCuuCic,
            DiemRuiRo = t.DiemRuiRo,
            KhuyenNgh = t.KhuyenNgh,
            GhiChu = t.GhiChu,
            NgayThamDinh = t.NgayThamDinh
        });
    }

    [HttpPost]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Appraisers)]
    public async Task<ActionResult<ThamDinhTinDungDto>> Create([FromBody] CreateThamDinhDto dto)
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        // Lấy thông tin hồ sơ để tính tỷ lệ nợ/thu nhập
        var hoSo = await _context.HoSoVays
            .Include(h => h.SanPhamVay)
            .FirstOrDefaultAsync(h => h.MaHoSo == dto.MaHoSo);
        if (hoSo == null) return NotFound(new { message = "Hồ sơ không tồn tại" });

        // Tính tỷ lệ nợ/thu nhập: số tiền đề nghị / thu nhập xác minh hàng năm
        decimal? tyLeNoThuNhap = null;
        if (dto.ThuNhapXacMinh.HasValue && dto.ThuNhapXacMinh.Value > 0)
        {
            // Chuẩn hóa: nếu thu nhập là hàng tháng thì nhân 12
            var thuNhapNam = dto.ThuNhapXacMinh.Value * 12;
            if (thuNhapNam > 0)
            {
                tyLeNoThuNhap = Math.Round(hoSo.SoTienDeNghi / thuNhapNam, 4);
            }
        }

        var ngayThamDinh = DateTime.Now;
        var ngayCapNhat = DateTime.Now;

        try
        {
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO dbo.tham_dinh_tin_dung (
    ma_ho_so,
    ma_can_bo_tham_dinh,
    thu_nhap_xac_minh,
    chi_phi_hang_thang,
    ty_le_no_thu_nhap,
    diem_thu_nhap,
    diem_tai_san_bao_dam,
    diem_cic,
    ngay_tra_cuu_cic,
    diem_rui_ro,
    khuyen_nghi,
    ghi_chu,
    ngay_tham_dinh,
    ngay_cap_nhat
)
VALUES (
    @maHoSo,
    @maCanBoThamDinh,
    @thuNhapXacMinh,
    @chiPhiHangThang,
    @tyLeNoThuNhap,
    @diemThuNhap,
    @diemTaiSanBaoDam,
    @diemCic,
    @ngayTraCuuCic,
    @diemRuiRo,
    @khuyenNgh,
    @ghiChu,
    @ngayThamDinh,
    @ngayCapNhat
);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            void AddParam(string name, object? value, System.Data.DbType dbType)
            {
                var p = command.CreateParameter();
                p.ParameterName = name;
                p.Value = value ?? DBNull.Value;
                p.DbType = dbType;
                command.Parameters.Add(p);
            }

            AddParam("@maHoSo", dto.MaHoSo, System.Data.DbType.Int64);
            AddParam("@maCanBoThamDinh", userId, System.Data.DbType.Int64);
            AddParam("@thuNhapXacMinh", dto.ThuNhapXacMinh, System.Data.DbType.Decimal);
            AddParam("@chiPhiHangThang", dto.ChiPhiHangThang, System.Data.DbType.Decimal);
            AddParam("@tyLeNoThuNhap", tyLeNoThuNhap, System.Data.DbType.Decimal);
            AddParam("@diemThuNhap", dto.DiemThuNhap, System.Data.DbType.Decimal);
            AddParam("@diemTaiSanBaoDam", dto.DiemTaiSanBaoDam, System.Data.DbType.Decimal);
            AddParam("@diemCic", dto.DiemCic, System.Data.DbType.Decimal);
            AddParam("@ngayTraCuuCic", dto.NgayTraCuuCic, System.Data.DbType.Date);
            AddParam("@diemRuiRo", dto.DiemRuiRo, System.Data.DbType.Decimal);
            AddParam("@khuyenNgh", dto.KhuyenNgh, System.Data.DbType.String);
            AddParam("@ghiChu", dto.GhiChu, System.Data.DbType.String);
            AddParam("@ngayThamDinh", ngayThamDinh, System.Data.DbType.DateTime2);
            AddParam("@ngayCapNhat", ngayCapNhat, System.Data.DbType.DateTime2);

            var resultId = await command.ExecuteScalarAsync();
            var maThamDinh = Convert.ToInt64(resultId);

            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.APPRAISE,
                tenThucThe: AuditEntities.THAM_DINH,
                maThucThe: hoSo?.MaHoSoHienThi ?? dto.MaHoSo.ToString(),
                giaTriMoi: $"Điểm rủi ro: {dto.DiemRuiRo} - Khuyến nghị: {dto.KhuyenNgh ?? "-"} - Thu nhập: {dto.ThuNhapXacMinh:N0}đ");

            return CreatedAtAction(nameof(GetByHoSo), new { hoSoId = dto.MaHoSo }, await GetByHoSo(dto.MaHoSo));
        }
        catch (DbException ex)
        {
            // Không trả ex.Message cho client (lộ chi tiết SQL).
            _logger.LogError(ex, "Lỗi DB khi tạo thẩm định cho hồ sơ {MaHoSo}", dto.MaHoSo);
            return BadRequest(new { message = "Không thể tạo thẩm định. Dữ liệu không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ." });
        }
    }
}
