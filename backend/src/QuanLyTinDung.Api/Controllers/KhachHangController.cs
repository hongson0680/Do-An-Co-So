using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Application.Services;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Extensions;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KhachHangController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditLogService _auditLog;

    public KhachHangController(AppDbContext context, IAuditLogService auditLog)
    {
        _context = context;
        _auditLog = auditLog;
    }

    /// <summary>
    /// Parse loại khách hàng mà không ném exception.
    /// Chấp nhận cả dạng có dấu ("Cá nhân", "Doanh nghiệp") lẫn không dấu
    /// ("CA_NHAN", "CA") lẫn viết tắt ("CA", "DN"). Trả về false nếu không
    /// khớp → bên ngoài trả 400 thay vì để Enum.Parse ném 500.
    /// </summary>
    private static bool TryParseLoaiKhachHang(string? raw, out Domain.Enums.LoaiKhachHang value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        // Chuẩn hoá: bỏ khoảng trắng / gạch dưới / gạch nối, bỏ dấu tiếng Việt,
        // để "Cá nhân" == "CA_NHAN" == "CA NHAN" khi so không phân biệt hoa thường.
        var normalized = StripDiacritics(raw)
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        switch (normalized)
        {
            case "CANHAN" or "CA":
                value = Domain.Enums.LoaiKhachHang.CA_NHAN;
                return true;
            case "DOANHNGHIEP" or "DN":
                value = Domain.Enums.LoaiKhachHang.DOANH_NGHIEP;
                return true;
            default:
                // Fallback: parse theo đúng tên enum chuẩn (CA_NHAN / DOANH_NGHIEP)
                return Enum.TryParse(raw, ignoreCase: true, out value);
        }
    }

    /// <summary>
    /// Bỏ dấu tiếng Việt theo cách chuẩn (Unicode NFD rồi bỏ dấu phụ).
    /// </summary>
    private static string StripDiacritics(string s)
    {
        var normalized = s.Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            // Bỏ các ký tự dạng dấu phụ (Mn) — giữ lại chữ cái gốc
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<KhachHangDto>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? loaiKhachHang = null)
    {
        var query = _context.KhachHangs.AsQueryable();

        // Guard: pageNumber/pageSize <= 0 làm Skip/Take âm → 500
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 500);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(k => k.HoTen.Contains(search) || k.SoGiayTo.Contains(search));

        // So khớp theo tên enum (đúng cách client đã gửi ở form: CA_NHAN /
        // DOANH_NGHIEP), bỏ khoảng trắng thừa. Không gọi StripDiacritics trong
        // LINQ-to-SQL vì hàm đó không được dịch được (EF không query được).
        if (!string.IsNullOrEmpty(loaiKhachHang))
        {
            var wanted = loaiKhachHang.Trim().ToUpperInvariant();
            query = query.Where(k => k.LoaiKhachHang.ToString().ToUpper() == wanted);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(k => k.NgayTao)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(k => new KhachHangDto
            {
                MaKhachHang = k.MaKhachHang,
                LoaiKhachHang = k.LoaiKhachHang.ToString(),
                HoTen = k.HoTen,
                SoGiayTo = k.SoGiayTo,
                GioiTinh = k.GioiTinh,
                NgaySinh = k.NgaySinh,
                SoDienThoai = k.SoDienThoai,
                Email = k.Email,
                DiaChi = k.DiaChi,
                NgheNghiep = k.NgheNghiep,
                MucDoRuiRo = k.MucDoRuiRo.ToString(),
                NgayTao = k.NgayTao,
                NgayCapNhat = k.NgayCapNhat
            })
            .ToListAsync();

        return Ok(new PagedResult<KhachHangDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<KhachHangDto>> GetById(long id)
    {
        var kh = await _context.KhachHangs.FindAsync(id);
        if (kh == null) return NotFound();

        return Ok(new KhachHangDto
        {
            MaKhachHang = kh.MaKhachHang,
            LoaiKhachHang = kh.LoaiKhachHang.ToString(),
            HoTen = kh.HoTen,
            SoGiayTo = kh.SoGiayTo,
            GioiTinh = kh.GioiTinh,
            NgaySinh = kh.NgaySinh,
            SoDienThoai = kh.SoDienThoai,
            Email = kh.Email,
            DiaChi = kh.DiaChi,
            NgheNghiep = kh.NgheNghiep,
            MucDoRuiRo = kh.MucDoRuiRo.ToString(),
            NgayTao = kh.NgayTao,
            NgayCapNhat = kh.NgayCapNhat
        });
    }

    [HttpPost]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Originators)]
    public async Task<ActionResult<KhachHangDto>> Create([FromBody] CreateKhachHangDto dto)
    {
        // Enum.Parse ném exception khi giá trị sai → trả 400 thay vì 500.
        // Hỗ trợ cả dạng không dấu ("CA_NHAN") lẫn có dấu ("Cá nhân").
        if (!TryParseLoaiKhachHang(dto.LoaiKhachHang, out var loaiKhachHang))
        {
            return BadRequest(new
            {
                message = $"Loại khách hàng không hợp lệ: '{dto.LoaiKhachHang}'. " +
                          "Chấp nhận: CA_NHAN, DOANH_NGHIEP, CO_QUAN, TO_CHUC_TIN_DUNG"
            });
        }

        var kh = new KhachHang
        {
            LoaiKhachHang = loaiKhachHang,
            HoTen = dto.HoTen,
            SoGiayTo = dto.SoGiayTo,
            GioiTinh = dto.GioiTinh,
            NgaySinh = dto.NgaySinh,
            SoDienThoai = dto.SoDienThoai,
            Email = dto.Email,
            DiaChi = dto.DiaChi,
            NgheNghiep = dto.NgheNghiep,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        _context.KhachHangs.Add(kh);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.CREATE,
            tenThucThe: AuditEntities.KHACH_HANG,
            maThucThe: kh.MaKhachHang.ToString(),
            giaTriMoi: $"Tạo KH: {kh.HoTen} - {kh.SoGiayTo}");

        return CreatedAtAction(nameof(GetById), new { id = kh.MaKhachHang }, new KhachHangDto
        {
            MaKhachHang = kh.MaKhachHang,
            LoaiKhachHang = kh.LoaiKhachHang.ToString(),
            HoTen = kh.HoTen,
            SoGiayTo = kh.SoGiayTo,
            GioiTinh = kh.GioiTinh,
            NgaySinh = kh.NgaySinh,
            SoDienThoai = kh.SoDienThoai,
            Email = kh.Email,
            DiaChi = kh.DiaChi,
            NgheNghiep = kh.NgheNghiep,
            MucDoRuiRo = kh.MucDoRuiRo.ToString(),
            NgayTao = kh.NgayTao,
            NgayCapNhat = kh.NgayCapNhat
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.Originators)]
    public async Task<ActionResult<KhachHangDto>> Update(long id, [FromBody] UpdateKhachHangDto dto)
    {
        if (!TryParseLoaiKhachHang(dto.LoaiKhachHang, out var loaiKhachHangUpdate))
        {
            return BadRequest(new
            {
                message = $"Loại khách hàng không hợp lệ: '{dto.LoaiKhachHang}'. " +
                          "Chấp nhận: CA_NHAN, DOANH_NGHIEP, CO_QUAN, TO_CHUC_TIN_DUNG"
            });
        }

        var kh = await _context.KhachHangs.FindAsync(id);
        if (kh == null) return NotFound();

        // Lưu giá trị cũ để log
        var giaTriCu = $"Tên: {kh.HoTen}, SĐT: {kh.SoDienThoai}, Email: {kh.Email}";

        kh.LoaiKhachHang = loaiKhachHangUpdate;
        kh.HoTen = dto.HoTen;
        kh.SoGiayTo = dto.SoGiayTo;
        kh.GioiTinh = dto.GioiTinh;
        kh.NgaySinh = dto.NgaySinh;
        kh.SoDienThoai = dto.SoDienThoai;
        kh.Email = dto.Email;
        kh.DiaChi = dto.DiaChi;
        kh.NgheNghiep = dto.NgheNghiep;
        kh.NgayCapNhat = DateTime.Now;

        await _context.SaveChangesAsync();

        var giaTriMoi = $"Tên: {kh.HoTen}, SĐT: {kh.SoDienThoai}, Email: {kh.Email}";
        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.UPDATE,
            tenThucThe: AuditEntities.KHACH_HANG,
            maThucThe: kh.MaKhachHang.ToString(),
            giaTriCu: giaTriCu,
            giaTriMoi: giaTriMoi);

        return Ok(new KhachHangDto
        {
            MaKhachHang = kh.MaKhachHang,
            LoaiKhachHang = kh.LoaiKhachHang.ToString(),
            HoTen = kh.HoTen,
            SoGiayTo = kh.SoGiayTo,
            GioiTinh = kh.GioiTinh,
            NgaySinh = kh.NgaySinh,
            SoDienThoai = kh.SoDienThoai,
            Email = kh.Email,
            DiaChi = kh.DiaChi,
            NgheNghiep = kh.NgheNghiep,
            MucDoRuiRo = kh.MucDoRuiRo.ToString(),
            NgayTao = kh.NgayTao,
            NgayCapNhat = kh.NgayCapNhat
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult> Delete(long id)
    {
        var kh = await _context.KhachHangs.FindAsync(id);
        if (kh == null) return NotFound();

        var thongTin = $"{kh.HoTen} - {kh.SoGiayTo}";

        _context.KhachHangs.Remove(kh);
        await _context.SaveChangesAsync();

        await _auditLog.LogFromHttpAsync(HttpContext,
            hanhDong: AuditActions.DELETE,
            tenThucThe: AuditEntities.KHACH_HANG,
            maThucThe: id.ToString(),
            giaTriCu: thongTin);

        return NoContent();
    }
}
