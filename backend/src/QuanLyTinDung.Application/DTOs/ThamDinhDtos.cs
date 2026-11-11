using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class ThamDinhTinDungDto
{
    public long MaThamDinh { get; set; }
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public long MaCanBoThamDinh { get; set; }
    public string? TenCanBoThamDinh { get; set; }
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
    public DateTime NgayThamDinh { get; set; }
}

public class CreateThamDinhDto
{
    [Required(ErrorMessage = "Mã hồ sơ không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã hồ sơ không hợp lệ")]
    public long MaHoSo { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Thu nhập xác minh không hợp lệ")]
    public decimal? ThuNhapXacMinh { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Chi phí hàng tháng không hợp lệ")]
    public decimal? ChiPhiHangThang { get; set; }

    [Range(0, 100, ErrorMessage = "Điểm thu nhập phải từ 0 đến 100")]
    public decimal? DiemThuNhap { get; set; }

    [Range(0, 100, ErrorMessage = "Điểm tài sản bảo đảm phải từ 0 đến 100")]
    public decimal? DiemTaiSanBaoDam { get; set; }

    [Range(0, 1000, ErrorMessage = "Điểm CIC phải từ 0 đến 1000")]
    public decimal? DiemCic { get; set; }

    public DateTime? NgayTraCuuCic { get; set; }

    [Required(ErrorMessage = "Điểm rủi ro không được để trống")]
    [Range(0, 100, ErrorMessage = "Điểm rủi ro phải từ 0 đến 100")]
    public decimal DiemRuiRo { get; set; }

    [StringLength(100, ErrorMessage = "Khuyến nghị không được vượt quá 100 ký tự")]
    public string? KhuyenNgh { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? GhiChu { get; set; }
}
