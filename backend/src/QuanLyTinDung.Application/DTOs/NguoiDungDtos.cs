using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class NguoiDungDto
{
    public long MaNguoiDung { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? SoDienThoai { get; set; }
    public long MaVaiTro { get; set; }
    public string TenVaiTro { get; set; } = string.Empty;
    public long MaChiNhanh { get; set; }
    public string TenChiNhanh { get; set; } = string.Empty;
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime? LanDangNhapCuoi { get; set; }
    public DateTime NgayTao { get; set; }
}

public class CreateNguoiDungDto
{
    [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
    [StringLength(50, MinimumLength = 4, ErrorMessage = "Tên đăng nhập phải từ 4 đến 50 ký tự")]
    public string TenDangNhap { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu phải từ 6 đến 100 ký tự")]
    public string MatKhau { get; set; } = string.Empty;

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự")]
    public string HoTen { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
    public string? SoDienThoai { get; set; }

    [Required(ErrorMessage = "Mã vai trò không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã vai trò không hợp lệ")]
    public long MaVaiTro { get; set; }

    [Required(ErrorMessage = "Mã chi nhánh không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã chi nhánh không hợp lệ")]
    public long MaChiNhanh { get; set; }
}
