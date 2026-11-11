using QuanLyTinDung.Domain.Enums;

namespace QuanLyTinDung.Domain.Entities;

public class HoSoVay
{
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public long MaKhachHang { get; set; }
    public long MaSanPham { get; set; }
    public long MaCanBo { get; set; }
    public decimal SoTienDeNghi { get; set; }
    public int KyHanThang { get; set; }
    public string MucDich { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "MOI";
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime? NgayNop { get; set; }
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual KhachHang? KhachHang { get; set; }
    public virtual SanPhamVay? SanPhamVay { get; set; }
    public virtual NguoiDung? CanBo { get; set; }
    public virtual ThamDinhTinDung? ThamDinhTinDung { get; set; }
    public virtual PheDuyet? PheDuyet { get; set; }
    public virtual HopDongVay? HopDongVay { get; set; }
}
