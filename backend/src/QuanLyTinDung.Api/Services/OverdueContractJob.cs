using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Api.Services;

/// <summary>
/// Job nền: đánh dấu hợp đồng quá hạn.
///
/// Trước đây không có gì set trạng thái QUA_HAN / QUA_HAN_30 / QUA_HAN_60 —
/// Dashboard chỉ ĐỌC các trạng thái đó nên tỉ lệ quá hạn luôn bằng 0.
/// Job này chạy định kỳ, quét hợp đồng có kỳ trả nợ quá hạn chưa thanh toán
/// và cập nhật trạng thái hợp đồng theo số ngày trễ hạn:
///   &lt;= 30 ngày  → QUA_HAN
///   &lt;= 60 ngày  → QUA_HAN_30
///   &gt; 60 ngày   → QUA_HAN_60
///
/// Hợp đồng đã trả hết (DA_DONG) hoặc bị hủy (DA_HUY) không bị đụng tới.
/// Không đụng cấu trúc DB — chỉ UPDATE dữ liệu trạng thái (giá trị nằm trong
/// CHECK constraint CK_hopdong_trangthai).
/// </summary>
public class OverdueContractJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<OverdueContractJob> _logger;
    private readonly int _intervalMinutes;

    public OverdueContractJob(IServiceProvider services, ILogger<OverdueContractJob> logger,
        IConfiguration configuration)
    {
        _services = services;
        _logger = logger;
        _intervalMinutes = configuration.GetValue<int>("OverdueJob:IntervalMinutes", 60);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Chờ một chút cho app khởi động xong trước khi chạy lần đầu
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Job đánh dấu hợp đồng quá hạn gặp lỗi");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.Now;
        var today = now.Date;

        // Kỳ trả nợ quá hạn & chưa thanh toán đủ (CHUA_THANH_TOAN /
        // THANH_TOAN_MOT_PHAN / TRE_HAN). Chỉ loại DA_THANH_TOAN.
        var overdueRows = await context.LichTraNos
            .Where(l => l.NgayDenHan < today
                        && l.TrangThai != "DA_THANH_TOAN")
            .Select(l => new { l.MaHopDong, l.NgayDenHan })
            .ToListAsync(ct);

        // Gom theo hợp đồng, lấy ngày đến hạn muộn nhất (kỳ trễ nhất quyết định)
        var byContract = overdueRows
            .GroupBy(r => r.MaHopDong)
            .Select(g => new
            {
                MaHopDong = g.Key,
                SoNgayTre = (today - g.Max(r => r.NgayDenHan).Date).Days
            })
            .ToList();

        if (byContract.Count == 0) return;

        var contractIds = byContract.Select(b => b.MaHopDong).ToList();

        var contracts = await context.HopDongVays
            .Where(h => contractIds.Contains(h.MaHopDong))
            .ToListAsync(ct);

        var changed = 0;
        foreach (var contract in contracts)
        {
            // Đã đóng hoặc hủy → không đụng
            if (contract.TrangThai == "DA_DONG" || contract.TrangThai == "DA_HUY") continue;

            var info = byContract.First(b => b.MaHopDong == contract.MaHopDong);
            var newStatus = info.SoNgayTre switch
            {
                > 60 => "QUA_HAN_60",
                > 30 => "QUA_HAN_30",
                _ => "QUA_HAN",
            };

            if (contract.TrangThai == newStatus) continue;

            contract.TrangThai = newStatus;
            contract.NgayCapNhat = DateTime.Now;
            changed++;
        }

        if (changed > 0)
        {
            await context.SaveChangesAsync(ct);
            _logger.LogInformation("Job quá hạn: cập nhật {Count} hợp đồng", changed);
        }
    }
}
