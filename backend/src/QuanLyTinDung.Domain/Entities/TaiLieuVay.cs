namespace QuanLyTinDung.Domain.Entities;

public class TaiLieuVay
{
    public long MaTaiLieu { get; set; }
    public long MaHoSo { get; set; }
    public string LoaiTaiLieu { get; set; } = string.Empty;       // CCCD, Hộ khẩu, Giấy ĐKKD, Sao kê, ...
    public string TenTapTin { get; set; } = string.Empty;        // Tên file gốc
    public string DuongDanTapTin { get; set; } = string.Empty;    // Đường dẫn lưu trữ
    public int PhienBan { get; set; } = 1;
    public string TrangThaiXacMinh { get; set; } = "CHO_KIEM_TRA"; // CHO_KIEM_TRA / DA_XAC_MINH / TU_CHOI
    public long? MaNguoiUpload { get; set; }
    public string? GhiChu { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual HoSoVay? HoSoVay { get; set; }
    public virtual NguoiDung? NguoiUpload { get; set; }
}
