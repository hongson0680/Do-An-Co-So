using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class ThanhToanDto
{
    public long MaThanhToan { get; set; }
    public long MaLichTraNo { get; set; }
    public long MaHopDong { get; set; }
    public string SoHopDong { get; set; } = string.Empty;
    public long MaHoSo { get; set; }
    public string MaHoSoHienThi { get; set; } = string.Empty;
    public decimal SoTienDaTra { get; set; }
    public decimal SoTienApGoc { get; set; }
    public decimal SoTienApLai { get; set; }
    public decimal SoTienApPhi { get; set; }
    public DateTime NgayThanhToan { get; set; }
    public string? PhuongThucThanhToan { get; set; }
    public string? MaGiaoDich { get; set; }
    public string? KenhThanhToan { get; set; }
    public string? GhiChu { get; set; }
}

public class CreateThanhToanDto
{
    [Required(ErrorMessage = "Mã lịch trả nợ không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã lịch trả nợ không hợp lệ")]
    public long MaLichTraNo { get; set; }

    [Required(ErrorMessage = "Mã hợp đồng không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã hợp đồng không hợp lệ")]
    public long MaHopDong { get; set; }

    [Required(ErrorMessage = "Mã hồ sơ không được để trống")]
    [Range(1, long.MaxValue, ErrorMessage = "Mã hồ sơ không hợp lệ")]
    public long MaHoSo { get; set; }

    [Required(ErrorMessage = "Số tiền đã trả không được để trống")]
    [Range(0, double.MaxValue, ErrorMessage = "Số tiền đã trả không hợp lệ")]
    public decimal SoTienDaTra { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền áp gốc không hợp lệ")]
    public decimal SoTienApGoc { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền áp lãi không hợp lệ")]
    public decimal SoTienApLai { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Số tiền áp phí không hợp lệ")]
    public decimal SoTienApPhi { get; set; }

    [Required(ErrorMessage = "Ngày thanh toán không được để trống")]
    public DateTime NgayThanhToan { get; set; }

    [StringLength(50, ErrorMessage = "Phương thức thanh toán không được vượt quá 50 ký tự")]
    public string? PhuongThucThanhToan { get; set; }

    [StringLength(50, ErrorMessage = "Mã giao dịch không được vượt quá 50 ký tự")]
    public string? MaGiaoDich { get; set; }

    [StringLength(50, ErrorMessage = "Kênh thanh toán không được vượt quá 50 ký tự")]
    public string? KenhThanhToan { get; set; }

    [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự")]
    public string? GhiChu { get; set; }
}

public class LichTraNoDto
{
    public long MaLichTraNo { get; set; }
    public long MaHopDong { get; set; }
    public int KyThanhToan { get; set; }
    public DateTime NgayDenHan { get; set; }
    public decimal GocPhaiTra { get; set; }
    public decimal LaiPhaiTra { get; set; }
    public decimal PhiPhat { get; set; }
    public string TrangThai { get; set; } = "CHUA_THANH_TOAN";
}
