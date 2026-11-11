using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class TaiSanDamBaoDto
{
    public long MaTaiSan { get; set; }
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public string LoaiTaiSan { get; set; } = string.Empty;
    public string? ChuSoHuu { get; set; }
    public string? SoGiayChungNhan { get; set; }
    public string? DiaChiTaiSan { get; set; }
    public decimal GiaTriUocTinh { get; set; }
    public decimal TyLeVayGiaTriTS { get; set; }
    public string? TinhTrangPhapLy { get; set; }
    public DateTime? NgayDinhGia { get; set; }
    public string? DonViDinhGia { get; set; }
    public string? SoHopDongBaoHiem { get; set; }
    public DateTime? NgayHetHanBaoHiem { get; set; }
    public DateTime NgayTao { get; set; }
    public DateTime NgayCapNhat { get; set; }

    // Computed fields
    public decimal GiaTriChoVayToiDa => GiaTriUocTinh * TyLeVayGiaTriTS;
}

public class CreateTaiSanDamBaoDto
{
    [Required(ErrorMessage = "Mã hồ sơ không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã hồ sơ không hợp lệ")]
    public long MaHoSo { get; set; }

    [Required(ErrorMessage = "Loại tài sản không được để trống")]
    [StringLength(50, ErrorMessage = "Loại tài sản không được vượt quá 50 ký tự")]
    public string LoaiTaiSan { get; set; } = string.Empty;

    [StringLength(255, ErrorMessage = "Chủ sở hữu không được vượt quá 255 ký tự")]
    public string? ChuSoHuu { get; set; }

    [StringLength(100, ErrorMessage = "Số GCN không được vượt quá 100 ký tự")]
    public string? SoGiayChungNhan { get; set; }

    public string? DiaChiTaiSan { get; set; }

    [Required(ErrorMessage = "Giá trị ước tính không được để trống")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị ước tính phải > 0 (RB19)")]
    public decimal GiaTriUocTinh { get; set; }

    [Required(ErrorMessage = "Tỷ lệ LTV không được để trống")]
    [Range(0.0001, 1, ErrorMessage = "Tỷ lệ LTV phải nằm trong (0, 1] (RB18)")]
    public decimal TyLeVayGiaTriTS { get; set; }

    [StringLength(30, ErrorMessage = "Tình trạng PL không được vượt quá 30 ký tự")]
    public string? TinhTrangPhapLy { get; set; }

    public DateTime? NgayDinhGia { get; set; }

    [StringLength(255, ErrorMessage = "Đơn vị định giá không được vượt quá 255 ký tự")]
    public string? DonViDinhGia { get; set; }

    [StringLength(100, ErrorMessage = "Số HĐ bảo hiểm không được vượt quá 100 ký tự")]
    public string? SoHopDongBaoHiem { get; set; }

    public DateTime? NgayHetHanBaoHiem { get; set; }
}

public class UpdateTaiSanDamBaoDto : CreateTaiSanDamBaoDto { }
