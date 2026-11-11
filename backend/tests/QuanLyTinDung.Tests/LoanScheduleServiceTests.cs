using QuanLyTinDung.Application.Services;

namespace QuanLyTinDung.Tests;

/// <summary>
/// Test công thức lịch trả nợ (dư nợ giảm dần).
///
/// Ràng buộc quan trọng nhất: Tổng gốc trả = Tổng gốc vay (không mất/thêm
/// tiền do làm tròn), và Tổng phải trả = Tổng gốc + Tổng lãi.
/// </summary>
public class LoanScheduleServiceTests
{
    private readonly LoanScheduleService _service = new();

    private static DateTime NgayBatDau() => new(2024, 1, 1);

    [Fact]
    public void BuildSchedule_SoKy_BangSoThangGiuaHaiNgay()
    {
        var (ky, _) = _service.BuildSchedule(
            tienGoc: 12_000_000m,
            laiSuatNam: 0.12m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 12, 1));

        Assert.Equal(11, ky.Count);
        Assert.Equal(1, ky[0].KyThanhToan);
        Assert.Equal(11, ky[^1].KyThanhToan);
    }

    [Fact]
    public void BuildSchedule_TongGocBangDungTienGocVay()
    {
        // Gốc chia đều + kỳ cuối điều chỉnh → tổng gốc phải khớp tuyệt đối
        var tienGoc = 10_000_000m;
        var (ky, tongPhaiTra) = _service.BuildSchedule(
            tienGoc: tienGoc,
            laiSuatNam: 0.10m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 6, 1));

        var tongGoc = ky.Sum(k => k.GocPhaiTra);
        Assert.Equal(tienGoc, tongGoc);
        Assert.Equal(tienGoc, ky[^1].GocPhaiTra + ky.Take(ky.Count - 1).Sum(k => k.GocPhaiTra));
    }

    [Fact]
    public void BuildSchedule_TongPhaiTraBangTongGocCongTongLai()
    {
        var (ky, tongPhaiTra) = _service.BuildSchedule(
            tienGoc: 50_000_000m,
            laiSuatNam: 0.12m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2025, 1, 1));

        var tongGoc = ky.Sum(k => k.GocPhaiTra);
        var tongLai = ky.Sum(k => k.LaiPhaiTra);

        Assert.Equal(tongGoc + tongLai, tongPhaiTra);
    }

    [Fact]
    public void BuildSchedule_LaiGiamDầnTheoDưNo()
    {
        // Dư nợ giảm dần → lãi mỗi kỳ phải nhỏ hơn kỳ trước
        var (ky, _) = _service.BuildSchedule(
            tienGoc: 120_000_000m,
            laiSuatNam: 0.12m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 12, 1));

        for (int i = 1; i < ky.Count; i++)
        {
            Assert.True(ky[i].LaiPhaiTra < ky[i - 1].LaiPhaiTra,
                $"Kỳ {i + 1} lãi ({ky[i].LaiPhaiTra}) phải < kỳ {i} ({ky[i - 1].LaiPhaiTra})");
        }
    }

    [Fact]
    public void BuildSchedule_LaiKyDauBangDuNoNhanhTien()
    {
        // Kỳ 1: lãi = tổng gốc × (lãi suất năm / 12)
        var tienGoc = 12_000_000m;
        var laiSuatNam = 0.12m;

        var (ky, _) = _service.BuildSchedule(
            tienGoc: tienGoc,
            laiSuatNam: laiSuatNam,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 12, 1));

        var expected = Math.Round(tienGoc * (laiSuatNam / 12m), 2);
        Assert.Equal(expected, ky[0].LaiPhaiTra);
    }

    [Fact]
    public void BuildSchedule_KyCuoiDuNoVeDungZero()
    {
        var (ky, _) = _service.BuildSchedule(
            tienGoc: 33_333_333m, // cố tình chia không đều để bắt lỗi làm tròn
            laiSuatNam: 0.11m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 10, 1));

        // Dư nợ sau kỳ cuối = tổng gốc - Σ(gốc) = 0
        var duNoConLai = 33_333_333m - ky.Sum(k => k.GocPhaiTra);
        Assert.Equal(0m, duNoConLai);
    }

    [Fact]
    public void BuildSchedule_NgayDenHanTangDungMotThangMoiKy()
    {
        var (ky, _) = _service.BuildSchedule(
            tienGoc: 6_000_000m,
            laiSuatNam: 0.10m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 6, 1));

        Assert.Equal(new DateTime(2024, 2, 1), ky[0].NgayDenHan);
        Assert.Equal(new DateTime(2024, 6, 1), ky[^1].NgayDenHan);
    }

    [Fact]
    public void BuildSchedule_KyHanKhongHopLe_PhaiCoItNhatMotKy()
    {
        // Ngày đáo hạn <= ngày bắt đầu → vẫn phải ra 1 kỳ, không chia cho 0
        var (ky, _) = _service.BuildSchedule(
            tienGoc: 1_000_000m,
            laiSuatNam: 0.10m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: NgayBatDau());

        Assert.Single(ky);
        Assert.Equal(1_000_000m, ky[0].GocPhaiTra);
    }

    [Fact]
    public void BuildSchedule_TienGocBangKhongSinhLai()
    {
        var (ky, tongPhaiTra) = _service.BuildSchedule(
            tienGoc: 0m,
            laiSuatNam: 0.10m,
            ngayBatDau: NgayBatDau(),
            ngayDaoHan: new DateTime(2024, 6, 1));

        Assert.All(ky, k => Assert.Equal(0m, k.GocPhaiTra));
        Assert.All(ky, k => Assert.Equal(0m, k.LaiPhaiTra));
        Assert.Equal(0m, tongPhaiTra);
    }
}
