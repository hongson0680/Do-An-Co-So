namespace QuanLyTinDung.Domain.Entities;

public class ThongBao
{
    public long MaThongBao { get; set; }
    public long MaNguoiNhan { get; set; }
    public string LoaiNguoiNhan { get; set; } = "NGUOI_DUNG";
    public string TieuDe { get; set; } = string.Empty;
    public string NoiDung { get; set; } = string.Empty;
    public string Loai { get; set; } = "INAPP";
    public bool DaDoc { get; set; }
    public long? MaHoSo { get; set; }
    public DateTime NgayGui { get; set; } = DateTime.Now;
}
