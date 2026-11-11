using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class ThongBaoDto
{
    public long MaThongBao { get; set; }
    public long MaNguoiNhan { get; set; }
    public string LoaiNguoiNhan { get; set; } = "NGUOI_DUNG";
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string Loai { get; set; } = "INAPP";
    public bool DaDoc { get; set; }
    public long? MaHoSo { get; set; }
    public string? MaHoSoHienThi { get; set; }
    public DateTime NgayGui { get; set; }
}

public class ThongBaoThongKeDto
{
    public int TongSoThongBao { get; set; }
    public int ChuaDoc { get; set; }
    public Dictionary<string, int> TheoLoai { get; set; } = new();
}

public class CreateThongBaoDto
{
    [Required(ErrorMessage = "Mã người nhận không được để trống")]
    public long MaNguoiNhan { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự")]
    public string TieuDe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    public string NoiDung { get; set; } = string.Empty;

    public string Loai { get; set; } = "INAPP"; // INAPP, EMAIL, SMS, PUSH, PAGE

    public long? MaHoSo { get; set; }
}

public class BroadcastThongBaoDto
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    public string TieuDe { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    public string NoiDung { get; set; } = string.Empty;

    public string Loai { get; set; } = "INAPP";

    /// <summary>
    /// Nếu null/rỗng → gửi cho tất cả user. Nếu có → chỉ gửi cho các role này.
    /// </summary>
    public string? MaVaiTroHienThi { get; set; }

    /// <summary>
    /// Nếu null/rỗng → gửi cho tất cả chi nhánh.
    /// </summary>
    public long? MaChiNhanh { get; set; }
}
