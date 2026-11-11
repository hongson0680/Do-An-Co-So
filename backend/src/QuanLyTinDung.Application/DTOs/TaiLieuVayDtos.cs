using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class TaiLieuVayDto
{
    public long MaTaiLieu { get; set; }
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public string LoaiTaiLieu { get; set; } = string.Empty;
    public string TenTapTin { get; set; } = string.Empty;
    public string DuongDanTapTin { get; set; } = string.Empty;
    public int PhienBan { get; set; }
    public string TrangThaiXacMinh { get; set; } = "CHO_KIEM_TRA";
    public long? MaNguoiUpload { get; set; }
    public string? TenNguoiUpload { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }
}

public class CreateTaiLieuVayDto
{
    [Required(ErrorMessage = "Mã hồ sơ không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã hồ sơ không hợp lệ")]
    public long MaHoSo { get; set; }

    [Required(ErrorMessage = "Loại tài liệu không được để trống")]
    [StringLength(50, ErrorMessage = "Loại tài liệu không được vượt quá 50 ký tự")]
    public string LoaiTaiLieu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên tệp không được để trống")]
    [StringLength(255, ErrorMessage = "Tên tệp không được vượt quá 255 ký tự")]
    public string TenTapTin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn tệp không được để trống")]
    public string DuongDanTapTin { get; set; } = string.Empty;

    public int PhienBan { get; set; } = 1;

    [StringLength(20, ErrorMessage = "Trạng thái không hợp lệ")]
    public string TrangThaiXacMinh { get; set; } = "CHO_KIEM_TRA";

    public string? GhiChu { get; set; }
}

public class UpdateTaiLieuVayDto
{
    [Required(ErrorMessage = "Loại tài liệu không được để trống")]
    [StringLength(50, ErrorMessage = "Loại tài liệu không được vượt quá 50 ký tự")]
    public string LoaiTaiLieu { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên tệp không được để trống")]
    [StringLength(255, ErrorMessage = "Tên tệp không được vượt quá 255 ký tự")]
    public string TenTapTin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Đường dẫn tệp không được để trống")]
    public string DuongDanTapTin { get; set; } = string.Empty;

    public int PhienBan { get; set; } = 1;

    [StringLength(20, ErrorMessage = "Trạng thái không hợp lệ")]
    public string TrangThaiXacMinh { get; set; } = "CHO_KIEM_TRA";

    public string? GhiChu { get; set; }
}
