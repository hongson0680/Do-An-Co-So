namespace QuanLyTinDung.Domain.Entities;

public class ThamDinhTinDung
{
    public long MaThamDinh { get; set; }
    public long MaHoSo { get; set; }
    public long MaCanBoThamDinh { get; set; }
    public decimal? ThuNhapXacMinh { get; set; }
    public decimal? ChiPhiHangThang { get; set; }
    public decimal? TyLeNoThuNhap { get; set; }
    public decimal? DiemThuNhap { get; set; }
    public decimal? DiemTaiSanBaoDam { get; set; }
    public decimal? DiemCic { get; set; }
    public DateTime? NgayTraCuuCic { get; set; }
    public decimal DiemRuiRo { get; set; }
    public string? KhuyenNgh { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayThamDinh { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual HoSoVay? HoSoVay { get; set; }
    public virtual NguoiDung? CanBoThamDinh { get; set; }
}
