namespace QuanLyTinDung.Domain.Entities;

public class PheDuyet
{
    public long MaPheDuyet { get; set; }
    public long MaHoSo { get; set; }
    public long MaNguoiPheDuyet { get; set; }
    public int CapPheDuyet { get; set; }
    public string QuyetDinh { get; set; } = string.Empty;
    public decimal? SoTienDuocDuyet { get; set; }
    public decimal? LaiSuatDuocDuyet { get; set; }
    public string? DieuKienPheDuyet { get; set; }
    public string? LyDoTuChoi { get; set; }
    public DateTime NgayPheDuyet { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual HoSoVay? HoSoVay { get; set; }
    public virtual NguoiDung? NguoiPheDuyet { get; set; }
}
