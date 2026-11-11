using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class HoSoVayDto
{
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public long MaKhachHang { get; set; }
    public string TenKhachHang { get; set; } = string.Empty;
    public long MaSanPham { get; set; }
    public string TenSanPham { get; set; } = string.Empty;
    public decimal SoTienDeNghi { get; set; }
    public int KyHanThang { get; set; }
    public string MucDich { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "MOI";
    public DateTime NgayTao { get; set; }
    public DateTime? NgayNop { get; set; }
    public long MaCanBo { get; set; }
    public string TenCanBo { get; set; } = string.Empty;
}

public class CreateHoSoVayDto
{
    [Required(ErrorMessage = "Mã khách hàng không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã khách hàng không hợp lệ")]
    public long MaKhachHang { get; set; }

    [Required(ErrorMessage = "Mã sản phẩm không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã sản phẩm không hợp lệ")]
    public long MaSanPham { get; set; }

    [Required(ErrorMessage = "Số tiền đề nghị không được để trống")]
    [Range(1, double.MaxValue, ErrorMessage = "Số tiền đề nghị phải lớn hơn 0")]
    public decimal SoTienDeNghi { get; set; }

    [Required(ErrorMessage = "Kỳ hạn không được để trống")]
    [Range(1, 360, ErrorMessage = "Kỳ hạn phải từ 1 đến 360 tháng")]
    public int KyHanThang { get; set; }

    [Required(ErrorMessage = "Mục đích không được để trống")]
    [StringLength(200, ErrorMessage = "Mục đích không được vượt quá 200 ký tự")]
    public string MucDich { get; set; } = string.Empty;
}

public class UpdateTrangThaiDto
{
    [Required(ErrorMessage = "Trạng thái không được để trống")]
    public string TrangThai { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? GhiChu { get; set; }
}
