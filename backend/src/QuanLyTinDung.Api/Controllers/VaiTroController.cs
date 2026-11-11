using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyTinDung.Infrastructure.Data;

namespace QuanLyTinDung.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VaiTroController : ControllerBase
{
    private readonly AppDbContext _context;

    public VaiTroController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy tất cả vai trò — chỉ ADMIN (dropdown chọn role khi tạo/sửa user).
    /// Trước đây chỉ [Authorize] nên mọi vai đã đăng nhập đều đọc được.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = QuanLyTinDung.Api.Authorization.Roles.AdminOnly)]
    public async Task<ActionResult<IEnumerable<VaiTroDto>>> GetAll([FromQuery] bool? activeOnly = null)
    {
        var query = _context.VaiTros.AsQueryable();
        if (activeOnly == true)
            query = query.Where(v => v.TrangThai);

        var data = await query
            .OrderBy(v => v.MaVaiTro)
            .Select(v => new VaiTroDto
            {
                MaVaiTro = v.MaVaiTro,
                MaVaiTroHienThi = v.MaVaiTroHienThi,
                TenVaiTro = v.TenVaiTro,
                MoTa = v.MoTa,
                LaVaiTroHeThong = v.LaVaiTroHeThong,
                TrangThai = v.TrangThai
            })
            .ToListAsync();

        return Ok(data);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VaiTroDto>> GetById(long id)
    {
        var v = await _context.VaiTros.FindAsync(id);
        if (v == null) return NotFound();

        return Ok(new VaiTroDto
        {
            MaVaiTro = v.MaVaiTro,
            MaVaiTroHienThi = v.MaVaiTroHienThi,
            TenVaiTro = v.TenVaiTro,
            MoTa = v.MoTa,
            LaVaiTroHeThong = v.LaVaiTroHeThong,
            TrangThai = v.TrangThai
        });
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<VaiTroDto>> Create([FromBody] CreateVaiTroDto dto)
    {
        var v = new Domain.Entities.VaiTro
        {
            MaVaiTroHienThi = dto.MaVaiTroHienThi,
            TenVaiTro = dto.TenVaiTro,
            MoTa = dto.MoTa,
            LaVaiTroHeThong = false,
            TrangThai = true,
            NgayTao = DateTime.Now,
            NgayCapNhat = DateTime.Now
        };

        _context.VaiTros.Add(v);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = v.MaVaiTro }, new VaiTroDto
        {
            MaVaiTro = v.MaVaiTro,
            MaVaiTroHienThi = v.MaVaiTroHienThi,
            TenVaiTro = v.TenVaiTro,
            MoTa = v.MoTa,
            LaVaiTroHeThong = v.LaVaiTroHeThong,
            TrangThai = v.TrangThai
        });
    }
}

public class VaiTroDto
{
    public long MaVaiTro { get; set; }
    public string MaVaiTroHienThi { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }
    public bool LaVaiTroHeThong { get; set; }
    public bool TrangThai { get; set; }
}

public class CreateVaiTroDto
{
    public string MaVaiTroHienThi { get; set; } = string.Empty;
    public string TenVaiTro { get; set; } = string.Empty;
    public string? MoTa { get; set; }
}
