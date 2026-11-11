namespace QuanLyTinDung.Domain.Entities;

public class NhatKyHeThong
{
    public long MaNhatKy { get; set; }
    public long? MaNguoiDung { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string TenThucThe { get; set; } = string.Empty;
    public string MaThucThe { get; set; } = string.Empty;
    public string? GiaTriCu { get; set; }
    public string? GiaTriMoi { get; set; }
    public string? TruongThayDoi { get; set; }
    public string? IpNguon { get; set; }
    public string? TrinhDuyet { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
}
