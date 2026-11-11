namespace QuanLyTinDung.Domain.Entities;

public class NguoiDung
{
    public long MaNguoiDung { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauBam { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public long MaVaiTro { get; set; }
    public long MaChiNhanh { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public DateTime? LanDangNhapCuoi { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual VaiTro? VaiTro { get; set; }
    public virtual ChiNhanh? ChiNhanh { get; set; }
}
