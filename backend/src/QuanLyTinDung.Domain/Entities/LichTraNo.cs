namespace QuanLyTinDung.Domain.Entities;

public class LichTraNo
{
    public long MaLichTraNo { get; set; }
    public long MaHopDong { get; set; }
    public int KyThanhToan { get; set; }
    public DateTime NgayDenHan { get; set; }
    public decimal GocPhaiTra { get; set; }
    public decimal LaiPhaiTra { get; set; }
    public decimal PhiPhat { get; set; }
    public string TrangThai { get; set; } = "CHUA_THANH_TOAN";
    public DateTime? HanThanhToan { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;

    // Navigation
    public virtual HopDongVay? HopDongVay { get; set; }
    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();
}
