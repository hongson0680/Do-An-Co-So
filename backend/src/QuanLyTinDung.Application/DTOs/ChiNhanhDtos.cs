using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class ChiNhanhDto
{
    public long MaChiNhanh { get; set; }
    public string MaChiNhanhHienThi { get; set; } = string.Empty;
    public string TenChiNhanh { get; set; } = string.Empty;
    public string? DiaChi { get; set; }
    public string? SoDienThoai { get; set; }
    public string? TruongChiNhanh { get; set; }
    public string? VungDiaLy { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime NgayTao { get; set; }
}

public class CreateChiNhanhDto
{
    [Required(ErrorMessage = "Mã chi nhánh không được để trống")]
    [StringLength(20, ErrorMessage = "Mã chi nhánh không được vượt quá 20 ký tự")]
    public string MaChiNhanhHienThi { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên chi nhánh không được để trống")]
    [StringLength(100, ErrorMessage = "Tên chi nhánh không được vượt quá 100 ký tự")]
    public string TenChiNhanh { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Địa chỉ không được vượt quá 200 ký tự")]
    public string? DiaChi { get; set; }

    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [StringLength(15, ErrorMessage = "Số điện thoại không được vượt quá 15 ký tự")]
    public string? SoDienThoai { get; set; }

    [StringLength(100, ErrorMessage = "Tên trưởng chi nhánh không được vượt quá 100 ký tự")]
    public string? TruongChiNhanh { get; set; }

    [StringLength(50, ErrorMessage = "Vùng địa lý không được vượt quá 50 ký tự")]
    public string? VungDiaLy { get; set; }

    public string? TrangThai { get; set; }
}
