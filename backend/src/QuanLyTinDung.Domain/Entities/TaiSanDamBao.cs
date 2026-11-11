namespace QuanLyTinDung.Domain.Entities;

public class TaiSanDamBao
{
    public long MaTaiSan { get; set; }
    public long MaHoSo { get; set; }
    public string LoaiTaiSan { get; set; } = string.Empty;        // BĐS, Ô tô, Sổ tiết kiệm, ...
    public string? ChuSoHuu { get; set; }                        // Tên chủ sở hữu
    public string? SoGiayChungNhan { get; set; }                 // Số GCN / số đăng ký
    public string? DiaChiTaiSan { get; set; }
    public decimal GiaTriUocTinh { get; set; }                   // Giá trị định giá
    public decimal TyLeVayGiaTriTS { get; set; }                 // LTV ratio (0-1)
    public string? TinhTrangPhapLy { get; set; }                 // Sở hữu riêng, chung, thế chấp
    public DateTime? NgayDinhGia { get; set; }
    public string? DonViDinhGia { get; set; }
    public string? SoHopDongBaoHiem { get; set; }
    public DateTime? NgayHetHanBaoHiem { get; set; }
    public DateTime NgayTao { get; set; } = DateTime.Now;
    public DateTime NgayCapNhat { get; set; } = DateTime.Now;

    // Navigation
    public virtual HoSoVay? HoSoVay { get; set; }
}
