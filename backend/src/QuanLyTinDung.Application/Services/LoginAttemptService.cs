using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace QuanLyTinDung.Application.Services;

/// <summary>
/// Đếm số lần đăng nhập sai và khóa tạm thời tài khoản.
///
/// Dùng IMemoryCache nên không cần thêm bảng/cột vào DB (phù hợp ràng buộc
/// "không đụng cấu trúc database"). Đổi lại: số liệu mất khi restart API —
/// chấp nhận được vì đây là lớp phòng thủ bổ sung, không phải biện pháp
/// bảo mật chính (mật khẩu vẫn phải mạnh + hash BCrypt).
///
/// Rủi ro: khi chạy nhiều instance API, mỗi instance đếm riêng. Với hệ thống
/// đơn instance thì không sao. Nếu scale out, thay bằng Redis/distributed cache.
/// </summary>
public interface ILoginAttemptService
{
    /// <summary>Ghi nhận 1 lần thử sai. Trả về số lần thử sai liên tiếp hiện tại.</summary>
    int RegisterFailure(string key);

    /// <summary>Xóa bộ đếm sau khi đăng nhập thành công.</summary>
    void Reset(string key);

    /// <summary>Số lần thử sai liên tiếp hiện tại (0 nếu chưa có).</summary>
    int GetFailureCount(string key);

    /// <summary>
    /// Còn bao lâu nữa thì được thử lại (giây). 0 nếu không bị khóa.
    /// </summary>
    TimeSpan GetLockoutRemaining(string key);
}

public class LoginAttemptService : ILoginAttemptService
{
    private const string CacheKeyPrefix = "login-fail:";
    private const string CountKeySuffix = ":count";
    private const string LockUntilKeySuffix = ":lockuntil";

    private readonly IMemoryCache _cache;
    private readonly int _maxAttempts;
    private readonly int _lockoutSeconds;

    public LoginAttemptService(IMemoryCache cache, IConfiguration configuration)
    {
        _cache = cache;
        _maxAttempts = configuration.GetValue<int>("LoginSecurity:MaxFailedAttempts", 5);
        _lockoutSeconds = configuration.GetValue<int>("LoginSecurity:LockoutSeconds", 900);
    }

    public int RegisterFailure(string key)
    {
        var cacheKey = CacheKeyPrefix + key.ToLowerInvariant();

        var count = _cache.Get<int>(cacheKey + CountKeySuffix) + 1;
        // Giữ bộ đếm lâu hơn thời gian khóa để lần thử tiếp theo vẫn bị tính.
        _cache.Set(cacheKey + CountKeySuffix, count, TimeSpan.FromMinutes(30));

        if (count >= _maxAttempts)
        {
            var lockUntil = DateTimeOffset.UtcNow.AddSeconds(_lockoutSeconds);
            _cache.Set(cacheKey + LockUntilKeySuffix, lockUntil, TimeSpan.FromSeconds(_lockoutSeconds));
        }

        return count;
    }

    public void Reset(string key)
    {
        var cacheKey = CacheKeyPrefix + key.ToLowerInvariant();
        _cache.Remove(cacheKey + CountKeySuffix);
        _cache.Remove(cacheKey + LockUntilKeySuffix);
    }

    public int GetFailureCount(string key)
    {
        var cacheKey = CacheKeyPrefix + key.ToLowerInvariant();
        return _cache.Get<int>(cacheKey + CountKeySuffix);
    }

    public TimeSpan GetLockoutRemaining(string key)
    {
        var cacheKey = CacheKeyPrefix + key.ToLowerInvariant();
        var lockUntil = _cache.Get<DateTimeOffset?>(cacheKey + LockUntilKeySuffix);
        if (lockUntil == null) return TimeSpan.Zero;

        var remaining = lockUntil.Value - DateTimeOffset.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
