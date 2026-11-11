using System.ComponentModel.DataAnnotations;

namespace QuanLyTinDung.Application.DTOs;

public class SanPhamVayDto
{
    public long MaSanPham { get; set; }
    public string MaSanPhamVay { get; set; } = string.Empty;
    public string TenSanPham { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public decimal LaiSuat { get; set; }
    public int KyHanToiThieu { get; set; }
    public int KyHanToiDa { get; set; }
    public decimal HanMucToiThieu { get; set; }
    public decimal? HanMucToiDa { get; set; }
    public string? DoiTuongKh { get; set; }
    public bool YeuCauTsdb { get; set; }
    public string TrangThai { get; set; } = "HOAT_DONG";
    public DateTime NgayTao { get; set; }
}

public class CreateSanPhamVayDto
{
    [Required(ErrorMessage = "Mã sản phẩm vay không được để trống")]
    [StringLength(20, ErrorMessage = "Mã sản phẩm không được vượt quá 20 ký tự")]
    public string MaSanPhamVay { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
    public string TenSanPham { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? MoTa { get; set; }

    [Required(ErrorMessage = "Lãi suất không được để trống")]
    [Range(0, 100, ErrorMessage = "Lãi suất phải từ 0 đến 100%")]
    public decimal LaiSuat { get; set; }

    [Required(ErrorMessage = "Kỳ hạn tối thiểu không được để trống")]
    [Range(1, 360, ErrorMessage = "Kỳ hạn tối thiểu phải từ 1 đến 360 tháng")]
    public int KyHanToiThieu { get; set; }

    [Required(ErrorMessage = "Kỳ hạn tối đa không được để trống")]
    [Range(1, 360, ErrorMessage = "Kỳ hạn tối đa phải từ 1 đến 360 tháng")]
    public int KyHanToiDa { get; set; }

    [Required(ErrorMessage = "Hạn mức tối thiểu không được để trống")]
    [Range(0, double.MaxValue, ErrorMessage = "Hạn mức tối thiểu không hợp lệ")]
    public decimal HanMucToiThieu { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Hạn mức tối đa không hợp lệ")]
    public decimal? HanMucToiDa { get; set; }

    [StringLength(200, ErrorMessage = "Đối tượng KH không được vượt quá 200 ký tự")]
    public string? DoiTuongKh { get; set; }

    public bool YeuCauTsdb { get; set; }
}
