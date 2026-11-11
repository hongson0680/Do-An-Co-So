namespace QuanLyTinDung.Domain.Entities;

public class ThanhToan
{
    public long MaThanhToan { get; set; }
    public long MaLichTraNo { get; set; }
    public long MaHopDong { get; set; }
    public long MaHoSo { get; set; }
    public decimal SoTienDaTra { get; set; }
    public decimal SoTienApGoc { get; set; }
    public decimal SoTienApLai { get; set; }
    public decimal SoTienApPhi { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public string? MaGiaoDich { get; set; }
    public string? KenhThanhToan { get; set; }
    public long? NguoiThu { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual LichTraNo? LichTraNo { get; set; }
    public virtual HopDongVay? HopDongVay { get; set; }
    public virtual HoSoVay? HoSoVay { get; set; }
}
