using QuanLyTinDung.Domain.Enums;

namespace QuanLyTinDung.Domain.Entities;

public class KhachHang
{
    public long MaKhachHang { get; set; }
    public LoaiKhachHang LoaiKhachHang { get; set; }
    public string HoTen { get; set; } = string.Empty;
    public string SoGiayTo { get; set; } = string.Empty;
    public string? GioiTinh { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public string? NgheNghiep { get; set; }
    public MucDoRuiRo MucDoRuiRo { get; set; } = MucDoRuiRo.TrungBinh;
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual ICollection<HoSoVay> HoSoVays { get; set; } = new List<HoSoVay>();
}
