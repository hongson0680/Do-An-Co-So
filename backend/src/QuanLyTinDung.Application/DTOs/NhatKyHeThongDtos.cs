using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class NhatKyHeThongDto
{
    public long MaNhatKy { get; set; }
    public long? MaNguoiDung { get; set; }
    public string? TenNguoiDung { get; set; }
    public string HanhDong { get; set; } = string.Empty;
    public string TenThucThe { get; set; } = string.Empty;
    public string MaThucThe { get; set; } = string.Empty;
    public string? GiaTriCu { get; set; }
    public string? GiaTriMoi { get; set; }
    public string? TruongThayDoi { get; set; }
    public string? IpNguon { get; set; }
    public string? TrinhDuyet { get; set; }
    public DateTime NgayTao { get; set; }
}

public class NhatKyThongKeDto
{
    public int TongSoLog { get; set; }
    public int SoLuongHomNay { get; set; }
    public int SoLuongTuanNay { get; set; }
    public int SoLuongThangNay { get; set; }
    public Dictionary<string, int> TheoHanhDong { get; set; } = new();
    public Dictionary<string, int> TheoThucThe { get; set; } = new();
    public List<NguoiDungHoatDongDto> NguoiDungHoatDong { get; set; } = new();
}

public class NguoiDungHoatDongDto
{
    public long MaNguoiDung { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string HoTen { get; set; } = string.Empty;
    public int SoLuotThaoTac { get; set; }
    public DateTime? LanCuoi { get; set; }
}
