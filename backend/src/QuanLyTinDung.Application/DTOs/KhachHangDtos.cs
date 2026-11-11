using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class KhachHangDto
{
    public long MaKhachHang { get; set; }
    public string LoaiKhachHang { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string SoGiayTo { get; set; } = string.Empty;
    public string? GioiTinh { get; set; }
    public DateTime? NgaySinh { get; set; }
    public string? SoDienThoai { get; set; }
    public string? Email { get; set; }
    public string? DiaChi { get; set; }
    public string? NgheNghiep { get; set; }
    public string MucDoRuiRo { get; set; } = "TrungBinh";
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}

public class CreateKhachHangDto
{
    [Required(ErrorMessage = "Loại khách hàng không được để trống")]
    public string LoaiKhachHang { get; set; } = "CA_NHAN";

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Số giấy tờ không được để trống")]
    [StringLength(20, ErrorMessage = "Số giấy tờ không được vượt quá 20 ký tự")]
    public string SoGiayTo { get; set; } = string.Empty;

    public string? GioiTinh { get; set; }
    public DateTime? NgaySinh { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
    public string? SoDienThoai { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
    public string? DiaChi { get; set; }

    [StringLength(100, ErrorMessage = "Nghề nghiệp không được vượt quá 100 ký tự")]
    public string? NgheNghiep { get; set; }
}

public class UpdateKhachHangDto : CreateKhachHangDto { }
