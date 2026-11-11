namespace QuanLyTinDung.Domain.Entities;

public class SanPhamVay
{
    public long MaSanPham { get; set; }
    public string MaSanPhamVay { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public decimal LaiSuat { get; set; }
    public int KyHanToiThieu { get; set; }
    public int KyHanToiDa { get; set; }
    public decimal HanMucToiThieu { get; set; }
    public decimal? HanMucToiDa { get; set; }
    public string? DoiTuongKh { get; set; }
    public bool YeuCauTsdb { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual ICollection<HoSoVay> HoSoVays { get; set; } = new List<HoSoVay>();
}
