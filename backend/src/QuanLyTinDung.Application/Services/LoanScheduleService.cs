using QuanLyTinDung.Domain.Entities;

namespace QuanLyTinDung.Application.Services;

/// <summary>
/// Tính lịch trả nợ theo công thức dư nợ giảm dần (Reducing Balance).
///
/// Tách khỏi controller để test được không cần database: hàm thuần, không
/// đụng EF Core, chỉ nhận số liệu và trả về danh sách kỳ trả nợ.
///
/// Quy tắc nghiệp vụ:
///  - Lãi suất tháng = lãi suất năm / 12
///  - Gốc mỗi kỳ chia đều = Tổng gốc / số kỳ (làm tròn 2 chữ số)
///  - Lãi kỳ n = Dư nợ đầu kỳ × lãi suất tháng
///  - Kỳ cuối: gốc = dư nợ còn lại → dư nợ về đúng 0, tránh sai số làm tròn
///  - Tổng phải trả = Σ(gốc kỳ + lãi kỳ)  (KHÔNG dùng công thức gốc/lãi phẳng)
/// </summary>
public interface ILoanScheduleService
{
    /// <summary>
    /// Sinh lịch trả nợ. Trả về (các kỳ trả nợ, tổng phải trả).
    /// </summary>
    (IReadOnlyList<LichTraNo> KyTraNo, decimal TongPhaiTra) BuildSchedule(
        decimal tienGoc, decimal laiSuatNam, DateTime ngayBatDau, DateTime ngayDaoHan);
}

public class LoanScheduleService : ILoanScheduleService
{
    public (IReadOnlyList<LichTraNo>, decimal) BuildSchedule(
        decimal tienGoc, decimal laiSuatNam, DateTime ngayBatDau, DateTime ngayDaoHan)
    {
        // Số kỳ = số tháng giữa 2 ngày
        var months = ((ngayDaoHan.Year - ngayBatDau.Year) * 12)
                   + (ngayDaoHan.Month - ngayBatDau.Month);

        if (months <= 0) months = 1; // bảo vệ chia cho 0
        if (tienGoc < 0) tienGoc = 0;

        var laiSuatThang = laiSuatNam / 12m;
        var gocMoiKy = Math.Round(tienGoc / months, 2);
        var duNoHienTai = tienGoc;
        decimal tongPhaiTra = 0m;

        var lichTraNo = new List<LichTraNo>(months);

        for (int i = 1; i <= months; i++)
        {
            // Lãi kỳ này = dư nợ hiện tại × lãi suất tháng
            var laiKy = Math.Round(duNoHienTai * laiSuatThang, 2);

            // Kỳ cuối: điều chỉnh gốc để dư nợ về đúng 0 (tránh sai số làm tròn)
            var gocKy = i == months ? duNoHienTai : gocMoiKy;

            duNoHienTai -= gocKy;
            if (duNoHienTai < 0) duNoHienTai = 0;

            var tongKy = gocKy + laiKy;
            tongPhaiTra += tongKy;

            lichTraNo.Add(new LichTraNo
            {
                KyThanhToan = i,
                NgayDenHan = ngayBatDau.AddMonths(i),
                GocPhaiTra = gocKy,
                LaiPhaiTra = laiKy,
                PhiPhat = 0,
                TrangThai = "CHUA_THANH_TOAN",
                NgayTao = DateTime.Now
            });
        }

        return (lichTraNo, tongPhaiTra);
    }
}
