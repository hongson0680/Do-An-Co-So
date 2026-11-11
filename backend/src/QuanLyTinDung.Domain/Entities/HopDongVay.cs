namespace QuanLyTinDung.Domain.Entities;

public class HopDongVay
{
    public long MaHopDong { get; set; }
    public string SoHopDong { get; set; } = string.Empty;
    public long MaHoSo { get; set; }
    public decimal TienGoc { get; set; }
    public decimal LaiSuat { get; set; }
    public DateTime NgayBatDau { get; set; }
    public DateTime NgayDaoHan { get; set; }
    public DateTime? NgayGiaiNgan { get; set; }
    public decimal? TongPhaiTra { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual HoSoVay? HoSoVay { get; set; }
    public virtual ICollection<LichTraNo> LichTraNos { get; set; } = new List<LichTraNo>();
}
