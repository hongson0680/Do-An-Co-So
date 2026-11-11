using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
[Route("api/phe-duyet")]
[Authorize]
public class PheDuyetController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly INotificationService _notification;
    private readonly IApprovalPolicyService _approvalPolicy;

    public PheDuyetController(AppDbContext context, IAuditLogService auditLog,
        INotificationService notification, IApprovalPolicyService approvalPolicy)
    {
        _context = context;
        _auditLog = auditLog;
        _notification = notification;
        _approvalPolicy = approvalPolicy;
    }

    [HttpGet("ho-so/{hoSoId}")]
    public async Task<ActionResult<PheDuyetDto>> GetByHoSo(long hoSoId)
    {
        var pd = await _context.PheDuYets
            .FirstOrDefaultAsync(p => p.MaHoSo == hoSoId);

        if (pd == null) return NotFound();

        return Ok(new PheDuyetDto
        {
            MaPheDuyet = pd.MaPheDuyet,
            MaHoSo = pd.MaHoSo,
            MaNguoiPheDuyet = pd.MaNguoiPheDuyet,
            CapPheDuyet = pd.CapPheDuyet,
            QuyetDinh = pd.QuyetDinh,
            SoTienDuocDuyet = pd.SoTienDuocDuyet,
            LaiSuatDuocDuyet = pd.LaiSuatDuocDuyet,
            DieuKienPheDuyet = pd.DieuKienPheDuyet,
            LyDoTuChoi = pd.LyDoTuChoi,
            NgayPheDuyet = pd.NgayPheDuyet
        });
    }

    [HttpPost]
    [Authorize(Roles = Roles.Approvers)]
    public async Task<ActionResult<PheDuyetDto>> Create([FromBody] CreatePheDuyetDto dto)
    {
        // Validate quyetDinh against DB CHECK constraint
        var validQuyetDinh = new[] { "YEU_CAU_BO_SUNG", "TU_CHOI", "DA_PHE_DUYET" };
        if (!validQuyetDinh.Contains(dto.QuyetDinh))
        {
            return BadRequest(new
            {
                message = $"Quyết định không hợp lệ: '{dto.QuyetDinh}'. Chỉ chấp nhận: YEU_CAU_BO_SUNG, TU_CHOI, DA_PHE_DUYET"
            });
        }

        // Validate soTienDuocDuyet when approved
        if (dto.QuyetDinh == "DA_PHE_DUYET" && dto.SoTienDuocDuyet <= 0)
        {
            return BadRequest(new { message = "Số tiền được duyệt phải > 0 khi phê duyệt" });
        }

        // Validate lãi suất (DB CHECK: > 0 AND <= 0.30)
        // - Âm là sai → báo lỗi (trước đây điều kiện "> 0" làm giá trị âm lọt
        //   qua rồi bị lưu thành NULL, mất dữ liệu âm thầm).
        // - Khi phê duyệt thì BẮT BUỘC có lãi suất hợp lệ (> 0 và <= 30%).
        // - TU_CHOI / YEU_CAU_BO_SUNG: không cần lãi suất (để 0 = NULL).
        if (dto.LaiSuatDuocDuyet < 0)
        {
            return BadRequest(new { message = "Lãi suất không được âm" });
        }
        if (dto.LaiSuatDuocDuyet > 0.30m)
        {
            return BadRequest(new { message = "Lãi suất phải <= 30%" });
        }
        if (dto.QuyetDinh == "DA_PHE_DUYET" && dto.LaiSuatDuocDuyet <= 0)
        {
            return BadRequest(new { message = "Lãi suất phê duyệt phải > 0 và <= 30%" });
        }

        // Hạn mức phê duyệt theo cấp (L1 ≤ 500tr, L2 ≤ 5 tỷ; ADMIN không giới hạn)
        // chỉ áp dụng khi DUYET và số tiền vượt hạn mức của vai phê duyệt.
        if (dto.QuyetDinh == "DA_PHE_DUYET" && _approvalPolicy.ExceedsLimit(User.CurrentRole(), dto.SoTienDuocDuyet))
        {
            var cap = _approvalPolicy.GetLimit(User.CurrentRole());
            return BadRequest(new
            {
                message = $"Hạn mức phê duyệt của {User.CurrentRole()} là {cap!.Value:N0}đ. Hồ sơ {dto.SoTienDuocDuyet:N0}đ vượt hạn mức → cần cấp phê duyệt cao hơn."
            });
        }

        var userId = long.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        var ngayPheDuyet = DateTime.Now;

        // Cấp phê duyệt ghi vào DB (cap_phe_duyet): 1 = L1, 2 = L2.
        // Trước đây hardcode 1 cho mọi vai → sai dữ liệu cấp phê duyệt.
        var capPheDuyet = _approvalPolicy.GetApprovalLevel(User.CurrentRole());

        // Dùng raw SQL để tránh lỗi EF Core OUTPUT clause với trigger TRG_pheduyet_check_thamdinh
        long pdId;
        try
        {
            // Đặt NOCOUNT ON trước insert để trigger không bị xung đột với EF Core OUTPUT clause
            // Lấy connection hiện tại từ DbContext
            var dbConn = _context.Database.GetDbConnection();
            var wasOpen = dbConn.State == System.Data.ConnectionState.Open;
            if (!wasOpen) await dbConn.OpenAsync();

            try
            {
                await using var command = dbConn.CreateCommand();
                command.CommandText = @"
SET NOCOUNT ON;
INSERT INTO dbo.phe_duyet (
    ma_ho_so, ma_nguoi_phe_duyet, cap_phe_duyet, quyet_dinh,
    so_tien_duoc_duyet, lai_suat_duoc_duyet, dieu_kien_phe_duyet, ly_do_tu_choi,
    ngay_phe_duyet, ngay_cap_nhat
) VALUES (
    @maHoSo, @maNguoiPheDuyet, @capPheDuyet, @quyetDinh,
    @soTienDuocDuyet, @laiSuatDuocDuyet, @dieuKienPheDuyet, @lyDoTuChoi,
    @ngayPheDuyet, @ngayCapNhat
);

-- Đồng bộ trạng thái hồ sơ (RB42): chỉ chuyển khi đang CHỜ PHÊ DUYET
-- (DU_TAI_LIEU/DA_PHE_DUYET/... thì giữ nguyên, tránh vi phạm trigger trạng thái).
-- YEU_CAU_BO_SUNG: không đổi trạng thái hồ sơ (hồ sơ tiếp tục quy trình bổ sung).
IF @quyetDinh IN (N'DA_PHE_DUYET', N'TU_CHOI')
BEGIN
    UPDATE dbo.ho_so_vay
    SET trang_thai = @quyetDinh,
        ngay_cap_nhat = @ngayCapNhat
    WHERE ma_ho_so = @maHoSo AND trang_thai = N'CHO_PHE_DUYET';
END

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
                AddParam("@maNguoiPheDuyet", userId, System.Data.DbType.Int64);
                AddParam("@capPheDuyet", capPheDuyet, System.Data.DbType.Int32);
                AddParam("@quyetDinh", dto.QuyetDinh, System.Data.DbType.String);
                AddParam("@soTienDuocDuyet", dto.QuyetDinh == "DA_PHE_DUYET" ? (object)dto.SoTienDuocDuyet : DBNull.Value, System.Data.DbType.Decimal);
                AddParam("@laiSuatDuocDuyet", dto.LaiSuatDuocDuyet > 0 ? (object)dto.LaiSuatDuocDuyet : DBNull.Value, System.Data.DbType.Decimal);
                AddParam("@dieuKienPheDuyet", string.IsNullOrEmpty(dto.DieuKienPheDuyet) ? (object)DBNull.Value : dto.DieuKienPheDuyet, System.Data.DbType.String);
                AddParam("@lyDoTuChoi", string.IsNullOrEmpty(dto.LyDoTuChoi) ? (object)DBNull.Value : dto.LyDoTuChoi, System.Data.DbType.String);
                AddParam("@ngayPheDuyet", ngayPheDuyet, System.Data.DbType.DateTime2);
                AddParam("@ngayCapNhat", ngayPheDuyet, System.Data.DbType.DateTime2);

                var resultId = await command.ExecuteScalarAsync();
                pdId = Convert.ToInt64(resultId);
            }
            finally
            {
                if (!wasOpen) await dbConn.CloseAsync();
            }
        }
        catch (DbException dbEx)
        {
            return BadRequest(new { message = "Không thể lưu phê duyệt: " + (dbEx.InnerException?.Message ?? dbEx.Message) });
        }

        // Lấy thông tin để log và trả về
        var hoSo = await _context.HoSoVays.FindAsync(dto.MaHoSo);
        var maHienThi = hoSo?.MaHoSoHienThi ?? dto.MaHoSo.ToString();
        var hanhDong = dto.QuyetDinh switch
        {
            "DA_PHE_DUYET" => AuditActions.APPROVE,
            "TU_CHOI" => AuditActions.REJECT,
            _ => AuditActions.UPDATE
        };

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: hanhDong,
            tenThucThe: AuditEntities.PHE_DUYET,
            maThucThe: maHienThi,
            giaTriMoi: $"Quyết định: {dto.QuyetDinh} - Số tiền: {dto.SoTienDuocDuyet:N0} - LS: {dto.LaiSuatDuocDuyet * 100}%{(string.IsNullOrEmpty(dto.LyDoTuChoi) ? "" : $" - Lý do: {dto.LyDoTuChoi}")}");

        // Thông báo cho cán bộ tạo hồ sơ
        if (hoSo != null)
        {
            string tieuDe = dto.QuyetDinh switch
            {
                "DA_PHE_DUYET" => $"Hồ sơ {maHienThi} đã được phê duyệt",
                "TU_CHOI" => $"Hồ sơ {maHienThi} bị từ chối",
                "YEU_CAU_BO_SUNG" => $"Hồ sơ {maHienThi} cần bổ sung",
                _ => $"Hồ sơ {maHienThi} - Cập nhật"
            };
            string noiDung = dto.QuyetDinh switch
            {
                "DA_PHE_DUYET" => $"Số tiền duyệt: {dto.SoTienDuocDuyet:N0}đ, lãi suất: {dto.LaiSuatDuocDuyet * 100}%",
                "TU_CHOI" => $"Lý do: {dto.LyDoTuChoi ?? "Không rõ"}",
                "YEU_CAU_BO_SUNG" => $"Điều kiện: {dto.DieuKienPheDuyet ?? "Không rõ"}",
                _ => ""
            };
            await _notification.SendAsync(hoSo.MaCanBo, tieuDe, noiDung, maHoSo: hoSo.MaHoSo);
        }

        return CreatedAtAction(nameof(GetByHoSo), new { hoSoId = dto.MaHoSo }, new PheDuyetDto
        {
            MaPheDuyet = pdId,
            MaHoSo = dto.MaHoSo,
            MaNguoiPheDuyet = userId,
            CapPheDuyet = capPheDuyet,
            QuyetDinh = dto.QuyetDinh,
            SoTienDuocDuyet = dto.QuyetDinh == "DA_PHE_DUYET" ? dto.SoTienDuocDuyet : null,
            LaiSuatDuocDuyet = dto.LaiSuatDuocDuyet > 0 ? dto.LaiSuatDuocDuyet : null,
            DieuKienPheDuyet = dto.DieuKienPheDuyet,
            LyDoTuChoi = dto.LyDoTuChoi,
            NgayPheDuyet = ngayPheDuyet
        });
    }
}

public class PheDuyetDto
{
    public long MaPheDuyet { get; set; }
    public long MaHoSo { get; set; }
    public long MaNguoiPheDuyet { get; set; }
    public int CapPheDuyet { get; set; }
    public string QuyetDinh { get; set; } = string.Empty;
    public decimal? SoTienDuocDuyet { get; set; }
    public decimal? LaiSuatDuocDuyet { get; set; }
    public string? DieuKienPheDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
    public DateTime NgayPheDuyet { get; set; }
}

public class CreatePheDuyetDto
{
    public long MaHoSo { get; set; }
    public string QuyetDinh { get; set; } = string.Empty;
    public decimal SoTienDuocDuyet { get; set; }
    public decimal LaiSuatDuocDuyet { get; set; }
    public string? DieuKienPheDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
}
