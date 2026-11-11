using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class HopDongVayDto
{
    public long MaHopDong { get; set; }
    public string SoHopDong { get; set; } = string.Empty;
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public decimal TienGoc { get; set; }
    public decimal LaiSuat { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayDaoHan { get; set; }
    public DateTime? NgayGiaiNgan { get; set; }
    public decimal? TongPhaiTra { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public string? GhiChu { get; set; }
}

public class CreateHopDongDto
{
    [Required(ErrorMessage = "Mã hồ sơ không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã hồ sơ không hợp lệ")]
    public long MaHoSo { get; set; }

    [Required(ErrorMessage = "Ngày bắt đầu không được để trống")]
    public DateTime NgayBatDau { get; set; }

    [Required(ErrorMessage = "Ngày đáo hạn không được để trống")]
    public DateTime NgayDaoHan { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? GhiChu { get; set; }
}
