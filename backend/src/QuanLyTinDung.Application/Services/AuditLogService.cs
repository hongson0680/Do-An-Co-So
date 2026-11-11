using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Application.Services;

/// <summary>
/// Service trung tâm ghi log audit.
/// HttpContext không khả dụng ở Application layer, nên controller sẽ truyền trực tiếp
/// userId, ip, userAgent vào LogAsync.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Ghi log với đầy đủ thông tin (gọi từ controller sau khi đã lấy HttpContext)
    /// </summary>
    Task LogAsync(
        long? maNguoiDung,
        string hanhDong,
        string tenThucThe,
        string maThucThe,
        string? giaTriCu = null,
        string? giaTriMoi = null,
        string? truongThayDoi = null,
        string? ipNguon = null,
        string? trinhDuyet = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IServiceScopeFactory scopeFactory, ILogger<AuditLogService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task LogAsync(
        long? maNguoiDung,
        string hanhDong,
        string tenThucThe,
        string maThucThe,
        string? giaTriCu = null,
        string? giaTriMoi = null,
        string? truongThayDoi = null,
        string? ipNguon = null,
        string? trinhDuyet = null)
    {
        // CK_nhatky_hanhdong của DB chỉ chấp nhận: CREATE, UPDATE, DELETE, APPROVE,
        // REJECT, LOGIN, LOGOUT, EXPORT, DISBURSE. Các hành động mở rộng trong
        // AuditActions (STATUS_CHANGE, APPRAISE, PAYMENT, LOGIN_FAILED,
        // PASSWORD_CHANGE) được chuẩn hóa về nhóm tương ứng trước khi ghi,
        // chi tiết được giữ trong giaTriMoi (ví dụ "Điểm rủi ro: ...").
        var actionCode = NormalizeHanhDong(hanhDong);

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var log = new NhatKyHeThong
            {
                MaNguoiDung = maNguoiDung,
                HanhDong = actionCode,
                TenThucThe = tenThucThe,
                MaThucThe = maThucThe,
                GiaTriCu = giaTriCu,
                GiaTriMoi = giaTriMoi,
                TruongThayDoi = truongThayDoi,
                IpNguon = ipNguon,
                TrinhDuyet = trinhDuyet,
                NgayTao = DateTime.Now
            };

            ctx.NhatKyHeThongs.Add(log);
            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            // KHÔNG được để lỗi audit làm hỏng request chính
            _logger.LogError(ex,
                "Không thể ghi audit log: HanhDong={HanhDong}, TenThucThe={TenThucThe}, MaThucThe={MaThucThe}",
                hanhDong, tenThucThe, maThucThe);
        }
    }

    /// <summary>
    /// Map hành động mở rộng về 9 giá trị mà CHECK constraint của DB chấp nhận.
    /// </summary>
    private static string NormalizeHanhDong(string hanhDong) => hanhDong switch
    {
        "STATUS_CHANGE" => AuditActions.UPDATE,
        "APPRAISE" => AuditActions.CREATE,
        "PAYMENT" => AuditActions.CREATE,
        "LOGIN_FAILED" => AuditActions.LOGIN,
        "PASSWORD_CHANGE" => AuditActions.UPDATE,
        "ROLE_ASSIGN" => AuditActions.UPDATE,
        _ => hanhDong
    };
}

/// <summary>
/// Các hành động audit chuẩn - dùng constant để tránh typo
/// </summary>
public static class AuditActions
{
    public const string LOGIN = "LOGIN";
    public const string LOGIN_FAILED = "LOGIN_FAILED";
    public const string LOGOUT = "LOGOUT";

    public const string CREATE = "CREATE";
    public const string UPDATE = "UPDATE";
    public const string DELETE = "DELETE";
    public const string VIEW = "VIEW";

    public const string APPROVE = "APPROVE";
    public const string REJECT = "REJECT";
    public const string DISBURSE = "DISBURSE";
    public const string PAYMENT = "PAYMENT";
    public const string APPRAISE = "APPRAISE";

    public const string STATUS_CHANGE = "STATUS_CHANGE";
    public const string PASSWORD_CHANGE = "PASSWORD_CHANGE";
    public const string ROLE_ASSIGN = "ROLE_ASSIGN";
}

public static class AuditEntities
{
    public const string KHACH_HANG = "KhachHang";
    public const string HO_SO_VAY = "HoSoVay";
    public const string HOP_DONG_VAY = "HopDongVay";
    public const string THANH_TOAN = "ThanhToan";
    public const string THAM_DINH = "ThamDinhTinDung";
    public const string PHE_DUYET = "PheDuyet";
    public const string NGUOI_DUNG = "NguoiDung";
    public const string SAN_PHAM_VAY = "SanPhamVay";
    public const string CHI_NHANH = "ChiNhanh";
    public const string VAI_TRO = "VaiTro";
    public const string LICH_TRA_NO = "LichTraNo";
    public const string TAI_SAN_DAM_BAO = "TaiSanDamBao";
    public const string TAI_LIEU_VAY = "TaiLieuVay";
}
