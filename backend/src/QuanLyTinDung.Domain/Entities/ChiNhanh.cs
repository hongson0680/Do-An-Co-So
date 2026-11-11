namespace QuanLyTinDung.Domain.Entities;

public class ChiNhanh
{
    public long MaChiNhanh { get; set; }
    public string MaChiNhanhHienThi { get; set; } = string.Empty;
    public string TenChiNhanh { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? TruongChiNhanh { get; set; }
    public string? VungDiaLy { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
