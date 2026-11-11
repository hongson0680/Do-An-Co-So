namespace QuanLyTinDung.Application.DTOs;

public class DashboardDto
{
    public int TongKhachHang { get; set; }
    public int TongHoSoVay { get; set; }
    public int TongHopDong { get; set; }
    public decimal TongGiaiNgan { get; set; }
    public int HoSoMoi { get; set; }
    public int HoSoDangXyLy { get; set; }
    public int HoSoDaDuyet { get; set; }
    public int HoSoTuChoi { get; set; }
    public decimal TienGiaiNganThangNay { get; set; }
    public decimal TiLeQuaHan { get; set; }
}

public class ChartDataDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class TopKhachHangDto
{
    public long MaKhachHang { get; set; }
    public string TenKh { get; set; } = string.Empty;
    public string SoGiayTo { get; set; } = string.Empty;
    public decimal TongVay { get; set; }
    public int SoHopDong { get; set; }
}

public class HoSoTheoTrangThaiDto
{
    public string TrangThai { get; set; } = string.Empty;
    public int SoLuong { get; set; }
    public decimal TongTien { get; set; }
}
