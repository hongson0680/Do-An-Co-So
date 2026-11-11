using System.Security.Claims;
using QuanLyTinDung.Application.Services;

namespace QuanLyTinDung.Api.Extensions;

/// <summary>
/// Extension cho IAuditLogService - tự lấy userId, IP, User-Agent từ HttpContext.
/// </summary>
public static class AuditLogExtensions
{
    public static async Task LogFromHttpAsync(
        this IAuditLogService svc,
        HttpContext http,
        string hanhDong,
        string tenThucThe,
        string maThucThe,
        string? giaTriCu = null,
        string? giaTriMoi = null,
        string? truongThayDoi = null)
    {
        long? maNguoiDung = null;
        if (http != null)
        {
            var userIdClaim = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (long.TryParse(userIdClaim, out var uid)) maNguoiDung = uid;
        }

        var ip = http?.Connection.RemoteIpAddress?.ToString();
        var trinhDuyet = http?.Request.Headers["User-Agent"].ToString();
        if (trinhDuyet?.Length > 500) trinhDuyet = trinhDuyet.Substring(0, 500);

        await svc.LogAsync(maNguoiDung, hanhDong, tenThucThe, maThucThe,
            giaTriCu, giaTriMoi, truongThayDoi, ip, trinhDuyet);
    }
}
