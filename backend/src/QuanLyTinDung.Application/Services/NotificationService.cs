using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Application.Services;

public interface INotificationService
{
    /// <summary>
    /// Gửi thông báo cho 1 user
    /// </summary>
    Task SendAsync(long maNguoiNhan, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null);

    /// <summary>
    /// Gửi thông báo cho nhiều user
    /// </summary>
    Task SendToUsersAsync(IEnumerable<long> maNguoiNhan, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null);

    /// <summary>
    /// Gửi cho tất cả user thuộc 1 role (vd: tất cả ADMIN)
    /// </summary>
    Task SendToRoleAsync(string maVaiTroHienThi, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null);

    /// <summary>
    /// Gửi cho tất cả user thuộc 1 chi nhánh
    /// </summary>
    Task SendToBranchAsync(long maChiNhanh, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null);
}

public class NotificationService : INotificationService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IServiceScopeFactory scopeFactory, ILogger<NotificationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task SendAsync(long maNguoiNhan, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tb = new ThongBao
            {
                MaNguoiNhan = maNguoiNhan,
                LoaiNguoiNhan = "NGUOI_DUNG",
                TieuDe = tieuDe,
                NoiDung = noiDung,
                Loai = loai,
                DaDoc = false,
                MaHoSo = maHoSo,
                NgayGui = DateTime.Now
            };

            ctx.ThongBaos.Add(tb);
            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gửi thông báo cho user {MaNguoiNhan}: {TieuDe}", maNguoiNhan, tieuDe);
        }
    }

    public async Task SendToUsersAsync(IEnumerable<long> maNguoiNhan, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.Now;
            var thongBaos = maNguoiNhan
                .Where(id => id > 0)
                .Distinct()
                .Select(id => new ThongBao
                {
                    MaNguoiNhan = id,
                    LoaiNguoiNhan = "NGUOI_DUNG",
                    TieuDe = tieuDe,
                    NoiDung = noiDung,
                    Loai = loai,
                    DaDoc = false,
                    MaHoSo = maHoSo,
                    NgayGui = now
                })
                .ToList();

            if (thongBaos.Count == 0) return;
            ctx.ThongBaos.AddRange(thongBaos);
            await ctx.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gửi thông báo cho {Count} users: {TieuDe}", maNguoiNhan.Count(), tieuDe);
        }
    }

    public async Task SendToRoleAsync(string maVaiTroHienThi, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var userIds = await ctx.NguoiDungs
                .Include(u => u.VaiTro)
                .Where(u => u.VaiTro!.MaVaiTroHienThi == maVaiTroHienThi && u.TrangThai == "HOAT_DONG")
                .Select(u => u.MaNguoiDung)
                .ToListAsync();

            await SendToUsersAsync(userIds, tieuDe, noiDung, loai, maHoSo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gửi thông báo cho role {Role}: {TieuDe}", maVaiTroHienThi, tieuDe);
        }
    }

    public async Task SendToBranchAsync(long maChiNhanh, string tieuDe, string noiDung, string loai = "INAPP", long? maHoSo = null)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var userIds = await ctx.NguoiDungs
                .Where(u => u.MaChiNhanh == maChiNhanh && u.TrangThai == "HOAT_DONG")
                .Select(u => u.MaNguoiDung)
                .ToListAsync();

            await SendToUsersAsync(userIds, tieuDe, noiDung, loai, maHoSo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gửi thông báo cho chi nhánh {BranchId}: {TieuDe}", maChiNhanh, tieuDe);
        }
    }
}

/// <summary>
/// Các loại thông báo - dùng constant để tránh typo
/// </summary>
public static class NotificationTypes
{
    /// <summary>Thông báo trong app (mặc định)</summary>
    public const string INAPP = "INAPP";
    /// <summary>Email</summary>
    public const string EMAIL = "EMAIL";
    /// <summary>SMS</summary>
    public const string SMS = "SMS";
    /// <summary>Push notification (mobile)</summary>
    public const string PUSH = "PUSH";
    /// <summary>Trang riêng (deep link)</summary>
    public const string PAGE = "PAGE";
}
