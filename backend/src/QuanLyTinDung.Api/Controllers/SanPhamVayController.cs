using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Application.DTOs;
using QuanLyTinDung.Domain.Entities;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SanPhamVayController : ControllerBase
{
    private readonly AppDbContext _context;

    public SanPhamVayController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<SanPhamVayDto>>> GetAll([FromQuery] int pageSize = 100)
    {
        var items = await _context.SanPhamVays
            .Where(s => s.TrangThai == "HOAT_DONG")
            .OrderBy(s => s.TenSanPham)
            .Take(pageSize)
            .Select(s => new SanPhamVayDto
            {
                MaSanPham = s.MaSanPham,
                MaSanPhamVay = s.MaSanPhamVay,
                TenSanPham = s.TenSanPham,
                MoTa = s.MoTa,
                LaiSuat = s.LaiSuat,
                KyHanToiThieu = s.KyHanToiThieu,
                KyHanToiDa = s.KyHanToiDa,
                HanMucToiThieu = s.HanMucToiThieu,
                HanMucToiDa = s.HanMucToiDa,
                DoiTuongKh = s.DoiTuongKh,
                YeuCauTsdb = s.YeuCauTsdb,
                TrangThai = s.TrangThai,
                NgayTao = s.NgayTao
            })
            .ToListAsync();

        return Ok(new PagedResult<SanPhamVayDto>
        {
            Items = items,
            TotalCount = items.Count,
            PageNumber = 1,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SanPhamVayDto>> GetById(long id)
    {
        var sp = await _context.SanPhamVays.FindAsync(id);
        if (sp == null) return NotFound();

        return Ok(new SanPhamVayDto
        {
            MaSanPham = sp.MaSanPham,
            MaSanPhamVay = sp.MaSanPhamVay,
            TenSanPham = sp.TenSanPham,
            MoTa = sp.MoTa,
            LaiSuat = sp.LaiSuat,
            KyHanToiThieu = sp.KyHanToiThieu,
            KyHanToiDa = sp.KyHanToiDa,
            HanMucToiThieu = sp.HanMucToiThieu,
            HanMucToiDa = sp.HanMucToiDa,
            DoiTuongKh = sp.DoiTuongKh,
            YeuCauTsdb = sp.YeuCauTsdb,
            TrangThai = sp.TrangThai,
            NgayTao = sp.NgayTao
        });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<SanPhamVayDto>> Create([FromBody] CreateSanPhamVayDto dto)
    {
        var sp = new SanPhamVay
        {
            MaSanPhamVay = dto.MaSanPhamVay,
            TenSanPham = dto.TenSanPham,
            MoTa = dto.MoTa,
            LaiSuat = dto.LaiSuat,
            KyHanToiThieu = dto.KyHanToiThieu,
            KyHanToiDa = dto.KyHanToiDa,
            HanMucToiThieu = dto.HanMucToiThieu,
            HanMucToiDa = dto.HanMucToiDa,
            DoiTuongKh = dto.DoiTuongKh,
            YeuCauTsdb = dto.YeuCauTsdb,
            TrangThai = "HOAT_DONG",
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        _context.SanPhamVays.Add(sp);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = sp.MaSanPham }, new SanPhamVayDto
        {
            MaSanPham = sp.MaSanPham,
            MaSanPhamVay = sp.MaSanPhamVay,
            TenSanPham = sp.TenSanPham,
            MoTa = sp.MoTa,
            LaiSuat = sp.LaiSuat,
            KyHanToiThieu = sp.KyHanToiThieu,
            KyHanToiDa = sp.KyHanToiDa,
            HanMucToiThieu = sp.HanMucToiThieu,
            HanMucToiDa = sp.HanMucToiDa,
            DoiTuongKh = sp.DoiTuongKh,
            YeuCauTsdb = sp.YeuCauTsdb,
            TrangThai = sp.TrangThai,
            NgayTao = sp.NgayTao
        });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult<SanPhamVayDto>> Update(long id, [FromBody] CreateSanPhamVayDto dto)
    {
        var sp = await _context.SanPhamVays.FindAsync(id);
        if (sp == null) return NotFound();

        sp.TenSanPham = dto.TenSanPham;
        sp.MoTa = dto.MoTa;
        sp.LaiSuat = dto.LaiSuat;
        sp.KyHanToiThieu = dto.KyHanToiThieu;
        sp.KyHanToiDa = dto.KyHanToiDa;
        sp.HanMucToiThieu = dto.HanMucToiThieu;
        sp.HanMucToiDa = dto.HanMucToiDa;
        sp.DoiTuongKh = dto.DoiTuongKh;
        sp.YeuCauTsdb = dto.YeuCauTsdb;
        sp.NgayCapNhat = DateTime.Now;

        await _context.SaveChangesAsync();

        return Ok(new SanPhamVayDto
        {
            MaSanPham = sp.MaSanPham,
            MaSanPhamVay = sp.MaSanPhamVay,
            TenSanPham = sp.TenSanPham,
            MoTa = sp.MoTa,
            LaiSuat = sp.LaiSuat,
            KyHanToiThieu = sp.KyHanToiThieu,
            KyHanToiDa = sp.KyHanToiDa,
            HanMucToiThieu = sp.HanMucToiThieu,
            HanMucToiDa = sp.HanMucToiDa,
            DoiTuongKh = sp.DoiTuongKh,
            YeuCauTsdb = sp.YeuCauTsdb,
            TrangThai = sp.TrangThai,
            NgayTao = sp.NgayTao
        });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult> Delete(long id)
    {
        var sp = await _context.SanPhamVays.FindAsync(id);
        if (sp == null) return NotFound();

        sp.TrangThai = "NGUNG_HOAT_DONG";
        sp.NgayCapNhat = DateTime.Now;
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
