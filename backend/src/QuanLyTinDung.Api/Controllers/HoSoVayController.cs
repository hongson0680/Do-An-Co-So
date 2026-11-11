using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Application.Services;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Extensions;
using QuanLyTinDung.Api.Authorization;
using QuanLyTinDung.Api.Helpers;
using System.Data.Common;
using System.Security.Claims;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HoSoVayController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notification;
    private readonly ILogger<HoSoVayController> _logger;

    public HoSoVayController(AppDbContext context, IAuditLogService auditLog,
        INotificationService notification, ILogger<HoSoVayController> logger)
    {
        _context = context;
        _auditLog = auditLog;
        _notification = notification;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<HoSoVayDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? trangThai = null)
    {
        var query = _context.HoSoVays
            .Include(h => h.KhachHang)
            .Include(h => h.SanPhamVay)
            .Include(h => h.CanBo)
            .AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(h => h.MaHoSoHienThi.Contains(search) || h.KhachHang!.HoTen.Contains(search));

        if (!string.IsNullOrEmpty(trangThai))
            query = query.Where(h => h.TrangThai == trangThai);

        // Cô lập dữ liệu theo chi nhánh: CREDIT_OFFICER chỉ thấy hồ sơ do
        // cán bộ thuộc chi nhánh của mình tạo. Vai trung tâm / ADMIN thấy tất cả.
        if (User.ShouldScopeByBranch())
        {
            var branchId = User.CurrentBranchId()!.Value;
            var branchUserIds = await _context.NguoiDungs
                .Where(u => u.MaChiNhanh == branchId)
                .Select(u => u.MaNguoiDung)
                .ToListAsync();
            if (branchUserIds.Count == 0)
                return Ok(new PagedResult<HoSoVayDto>
                {
                    Items = new List<HoSoVayDto>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                });
            query = query.Where(h => branchUserIds.Contains(h.MaCanBo));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(h => h.NgayTao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(h => new HoSoVayDto
            {
                MaHoSo = h.MaHoSo,
                MaHoSoHienThi = h.MaHoSoHienThi,
                MaKhachHang = h.MaKhachHang,
                TenKhachHang = h.KhachHang!.HoTen,
                MaSanPham = h.MaSanPham,
                TenSanPham = h.SanPhamVay!.TenSanPham,
                SoTienDeNghi = h.SoTienDeNghi,
                KyHanThang = h.KyHanThang,
                MucDich = h.MucDich,
                TrangThai = h.TrangThai,
                NgayTao = h.NgayTao,
                NgayNop = h.NgayNop,
                MaCanBo = h.MaCanBo,
                TenCanBo = h.CanBo!.HoTen
            })
            .ToListAsync();

        return Ok(new PagedResult<HoSoVayDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<HoSoVayDto>> GetById(long id)
    {
        var h = await _context.HoSoVays
            .Include(x => x.KhachHang)
            .Include(x => x.SanPhamVay)
            .Include(x => x.CanBo)
            .FirstOrDefaultAsync(x => x.MaHoSo == id);

        if (h == null) return NotFound();

        return Ok(new HoSoVayDto
        {
            MaHoSo = h.MaHoSo,
            MaHoSoHienThi = h.MaHoSoHienThi,
            MaKhachHang = h.MaKhachHang,
            TenKhachHang = h.KhachHang!.HoTen,
            MaSanPham = h.MaSanPham,
            TenSanPham = h.SanPhamVay!.TenSanPham,
            SoTienDeNghi = h.SoTienDeNghi,
            KyHanThang = h.KyHanThang,
            MucDich = h.MucDich,
            TrangThai = h.TrangThai,
            NgayTao = h.NgayTao,
            NgayNop = h.NgayNop,
            MaCanBo = h.MaCanBo,
            TenCanBo = h.CanBo!.HoTen
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Originators)]
    public async Task<ActionResult<HoSoVayDto>> Create([FromBody] CreateHoSoVayDto dto)
    {
        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

        var maHoSo = $"HSV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        var ngayTao = DateTime.Now;
        var ngayCapNhat = DateTime.Now;

        try
        {
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = @"
INSERT INTO dbo.ho_so_vay (
    ma_ho_so_hien_thi,
    ma_khach_hang,
    ma_san_pham,
    ma_can_bo,
    so_tien_de_nghi,
    ky_han_thang,
    muc_dich,
    trang_thai,
    ngay_tao,
    ngay_cap_nhat
)
VALUES (
    @maHoSoHienThi,
    @maKhachHang,
    @maSanPham,
    @maCanBo,
    @soTienDeNghi,
    @kyHanThang,
    @mucDich,
    @trangThai,
    @ngayTao,
    @ngayCapNhat
);
SELECT CAST(SCOPE_IDENTITY() AS bigint);";

            var p1 = command.CreateParameter();
            p1.ParameterName = "@maHoSoHienThi";
            p1.Value = maHoSo;
            p1.DbType = System.Data.DbType.String;
            command.Parameters.Add(p1);

            var p2 = command.CreateParameter();
            p2.ParameterName = "@maKhachHang";
            p2.Value = dto.MaKhachHang;
            p2.DbType = System.Data.DbType.Int64;
            command.Parameters.Add(p2);

            var p3 = command.CreateParameter();
            p3.ParameterName = "@maSanPham";
            p3.Value = dto.MaSanPham;
            p3.DbType = System.Data.DbType.Int64;
            command.Parameters.Add(p3);

            var p4 = command.CreateParameter();
            p4.ParameterName = "@maCanBo";
            p4.Value = userId;
            p4.DbType = System.Data.DbType.Int64;
            command.Parameters.Add(p4);

            var p5 = command.CreateParameter();
            p5.ParameterName = "@soTienDeNghi";
            p5.Value = dto.SoTienDeNghi;
            p5.DbType = System.Data.DbType.Decimal;
            command.Parameters.Add(p5);

            var p6 = command.CreateParameter();
            p6.ParameterName = "@kyHanThang";
            p6.Value = dto.KyHanThang;
            p6.DbType = System.Data.DbType.Int32;
            command.Parameters.Add(p6);

            var p7 = command.CreateParameter();
            p7.ParameterName = "@mucDich";
            p7.Value = dto.MucDich;
            p7.DbType = System.Data.DbType.String;
            command.Parameters.Add(p7);

            var p8 = command.CreateParameter();
            p8.ParameterName = "@trangThai";
            p8.Value = "MOI";
            p8.DbType = System.Data.DbType.String;
            command.Parameters.Add(p8);

            var p9 = command.CreateParameter();
            p9.ParameterName = "@ngayTao";
            p9.Value = ngayTao;
            p9.DbType = System.Data.DbType.DateTime2;
            command.Parameters.Add(p9);

            var p10 = command.CreateParameter();
            p10.ParameterName = "@ngayCapNhat";
            p10.Value = ngayCapNhat;
            p10.DbType = System.Data.DbType.DateTime2;
            command.Parameters.Add(p10);

            var resultId = await command.ExecuteScalarAsync();
            var maHoSoId = Convert.ToInt64(resultId);

            await _auditLog.LogFromHttpAsync(HttpContext,
                hanhDong: AuditActions.CREATE,
                tenThucThe: AuditEntities.HO_SO_VAY,
                maThucThe: maHoSo,
                giaTriMoi: $"Tạo HS: KH#{dto.MaKhachHang}, SP#{dto.MaSanPham}, Số tiền: {dto.SoTienDeNghi:N0}");

            await _notification.SendToRoleAsync("CREDIT_OFFICER",
                $"Hồ sơ vay mới: {maHoSo}",
                $"Hồ sơ vay mới được tạo với số tiền {dto.SoTienDeNghi:N0}đ - cần thẩm định.",
                maHoSo: maHoSoId);

            var result = await GetById(maHoSoId);
            return CreatedAtAction(nameof(GetById), new { id = maHoSoId }, result.Value);
        }
        catch (DbException ex)
        {
            // Không trả ex.Message cho client (lộ chi tiết SQL). Chi tiết lỗi
            // chỉ ghi vào log server.
            _logger.LogError(ex, "Lỗi DB khi tạo hồ sơ vay (KH {MaKhachHang})", dto.MaKhachHang);
            return BadRequest(new { message = "Không thể tạo hồ sơ vay. Dữ liệu không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ." });
        }
    }

    [HttpPut("{id}/trang-thai")]
    [Authorize(Roles = Roles.Originators)]
    public async Task<ActionResult> UpdateTrangThai(long id, [FromBody] UpdateTrangThaiDto dto)
    {
        var hoSo = await _context.HoSoVays.FindAsync(id);
        if (hoSo == null) return NotFound();

        var trangThaiCu = hoSo.TrangThai;
        var ngayCapNhat = DateTime.Now;
        DateTime? ngayNop = dto.TrangThai == "DA_NOP" ? DateTime.Now : null;

        try
        {
            await using var connection = _context.Database.GetDbConnection();
            if (connection.State != System.Data.ConnectionState.Open)
                await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            if (dto.TrangThai == "DA_NOP")
            {
                command.CommandText = @"
UPDATE dbo.ho_so_vay
SET trang_thai = @trangThai,
    ngay_cap_nhat = @ngayCapNhat,
    ngay_nop = @ngayNop
WHERE ma_ho_so = @id";

                var pNop = command.CreateParameter();
                pNop.ParameterName = "@ngayNop";
                pNop.Value = ngayNop;
                pNop.DbType = System.Data.DbType.DateTime2;
                command.Parameters.Add(pNop);
            }
            else
            {
                command.CommandText = @"
UPDATE dbo.ho_so_vay
SET trang_thai = @trangThai,
    ngay_cap_nhat = @ngayCapNhat
WHERE ma_ho_so = @id";
            }

            var pId = command.CreateParameter();
            pId.ParameterName = "@id";
            pId.Value = id;
            pId.DbType = System.Data.DbType.Int64;
            command.Parameters.Add(pId);

            var pTrangThai = command.CreateParameter();
            pTrangThai.ParameterName = "@trangThai";
            pTrangThai.Value = dto.TrangThai;
            pTrangThai.DbType = System.Data.DbType.String;
            command.Parameters.Add(pTrangThai);

            var pNgayCapNhat = command.CreateParameter();
            pNgayCapNhat.ParameterName = "@ngayCapNhat";
            pNgayCapNhat.Value = ngayCapNhat;
            pNgayCapNhat.DbType = System.Data.DbType.DateTime2;
            command.Parameters.Add(pNgayCapNhat);

            await command.ExecuteNonQueryAsync();
        }
        catch (DbException ex)
        {
            // Không trả ex.Message cho client (lộ chi tiết SQL).
            _logger.LogError(ex, "Lỗi DB khi đổi trạng thái hồ sơ {MaHoSo}", id);
            return BadRequest(new { message = "Không thể cập nhật trạng thái hồ sơ. Dữ liệu không hợp lệ hoặc vi phạm ràng buộc nghiệp vụ." });
        }

        hoSo.TrangThai = dto.TrangThai;
        hoSo.NgayCapNhat = ngayCapNhat;
        if (ngayNop.HasValue)
            hoSo.NgayNop = ngayNop;

        // Nếu người dùng có ghi chú, giữ lại trong audit (giaTriMoi) để không
        // mất thông tin lý do đổi trạng thái (trước đây ghiChu bị bỏ qua).
        var giaTriMoi = string.IsNullOrWhiteSpace(dto.GhiChu)
            ? dto.TrangThai
            : $"{dto.TrangThai} — {dto.GhiChu}";

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.STATUS_CHANGE,
            tenThucThe: AuditEntities.HO_SO_VAY,
            maThucThe: hoSo.MaHoSoHienThi,
            giaTriCu: trangThaiCu,
            giaTriMoi: giaTriMoi);

        // Thông báo cho cán bộ tạo hồ sơ
        if (dto.TrangThai == "DA_NOP" || dto.TrangThai == "TU_CHOI")
        {
            await _notification.SendAsync(hoSo.MaCanBo,
                $"Hồ sơ {hoSo.MaHoSoHienThi} - trạng thái mới",
                $"Hồ sơ {hoSo.MaHoSoHienThi} đã chuyển sang trạng thái: {dto.TrangThai}",
                maHoSo: hoSo.MaHoSo);
        }

        return Ok(new { message = "Cập nhật trạng thái thành công" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = Roles.AdminOnly)]
    public async Task<ActionResult> Delete(long id)
    {
        var hoSo = await _context.HoSoVays.FindAsync(id);
        if (hoSo == null) return NotFound();

        var maHienThi = hoSo.MaHoSoHienThi;
        _context.HoSoVays.Remove(hoSo);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.DELETE,
            tenThucThe: AuditEntities.HO_SO_VAY,
            maThucThe: maHienThi,
            giaTriCu: $"Xóa hồ sơ {maHienThi}");

        return NoContent();
    }
}
