using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuanLyTinDung.Application.Services;
using QuanLyTinDung.Infrastructure.Data;
using QuanLyTinDung.Api.Middlewares;
using QuanLyTinDung.Api.Authorization;
using QuanLyTinDung.Domain.Entities;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// HttpContextAccessor + AuditLogService (singleton)
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<INotificationService, NotificationService>();
builder.Services.AddSingleton<QuanLyTinDung.Api.Services.IExportService, QuanLyTinDung.Api.Services.ExportService>();

// Memory cache - dùng cho lockout đăng nhập (LoginAttemptService)
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ILoginAttemptService, LoginAttemptService>();

// Lịch trả nợ (công thức dư nợ giảm dần) - tách khỏi controller để test được
builder.Services.AddSingleton<ILoanScheduleService, LoanScheduleService>();

// Hạn mức & cấp phê duyệt theo vai - tách khỏi controller để test được
builder.Services.AddSingleton<IApprovalPolicyService, ApprovalPolicyService>();

// Job nền: đánh dấu hợp đồng quá hạn (trước đây không có gì set trạng thái này)
builder.Services.AddHostedService<QuanLyTinDung.Api.Services.OverdueContractJob>();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Authentication - Require configuration, no fallback for security
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key must be configured in appsettings.json");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QLTDBank";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QLTDBankClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Hạn mức phê duyệt theo cấp (đọc từ appsettings, có giá trị mặc định)
var creditPolicy = builder.Configuration.GetSection("CreditPolicy");
CreditPolicy.ApproverL1Limit = creditPolicy.GetValue<decimal?>("ApproverL1Limit") ?? CreditPolicy.ApproverL1Limit;
CreditPolicy.ApproverL2Limit = creditPolicy.GetValue<decimal?>("ApproverL2Limit") ?? CreditPolicy.ApproverL2Limit;

// CORS - Restricted to specific origins
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? new[] { "http://localhost:5173", "http://localhost:3000" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials(); // Required for cookies/auth
    });
});

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QLTD Bank API",
        Version = "v1",
        Description = "API for Credit Management System"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed Data - Tạo dữ liệu mặc định
// Mặc Đ ĐỊNH BẬT (bảo toàn hành vi cũ). Muốn chạy với DB sạch (không chèn
// bất kỳ row nào) đặt: "Seed:Enabled": false trong appsettings.json.
bool seedEnabled = builder.Configuration.GetValue<bool>("Seed:Enabled", true);
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!seedEnabled)
    {
        Console.WriteLine("[Seed] Đã tắt (Seed:Enabled=false) — không chèn dữ liệu mặc định.");
    }

    // Chỉ seed nếu chưa có dữ liệu
    if (seedEnabled && !context.VaiTros.Any())
    {
        // Seed VaiTro (Roles) - dùng MÃ VAITRO HIENTHI khớp với seed của
        // init-database.sql (CREDIT_OFFICER, APPROVER_L1) để 2 nguồn thống nhất.
        context.VaiTros.AddRange(
            new VaiTro { MaVaiTroHienThi = "ADMIN", TenVaiTro = "Quản trị viên", MoTa = "Quản lý toàn bộ hệ thống", LaVaiTroHeThong = true, TrangThai = true, NgayTao = DateTime.Now },
            new VaiTro { MaVaiTroHienThi = "CREDIT_OFFICER", TenVaiTro = "Cán bộ tín dụng", MoTa = "Xử lý hồ sơ vay", LaVaiTroHeThong = false, TrangThai = true, NgayTao = DateTime.Now },
            new VaiTro { MaVaiTroHienThi = "APPRAISER", TenVaiTro = "Thẩm định viên", MoTa = "Thẩm định hồ sơ vay và định giá tài sản bảo đảm", LaVaiTroHeThong = false, TrangThai = true, NgayTao = DateTime.Now },
            new VaiTro { MaVaiTroHienThi = "APPROVER_L1", TenVaiTro = "Phê duyệt cấp 1", MoTa = "Phê duyệt hồ sơ vay đến hạn mức cấp 1", LaVaiTroHeThong = false, TrangThai = true, NgayTao = DateTime.Now },
            new VaiTro { MaVaiTroHienThi = "APPROVER_L2", TenVaiTro = "Phê duyệt cấp 2", MoTa = "Phê duyệt hồ sơ vay vượt hạn mức cấp 1", LaVaiTroHeThong = false, TrangThai = true, NgayTao = DateTime.Now },
            new VaiTro { MaVaiTroHienThi = "ACCOUNTANT", TenVaiTro = "Kế toán / Ngân quỹ", MoTa = "Lập hợp đồng, giải ngân và ghi nhận thanh toán", LaVaiTroHeThong = false, TrangThai = true, NgayTao = DateTime.Now },
            new VaiTro { MaVaiTroHienThi = "REPORT_VIEWER", TenVaiTro = "Xem báo cáo", MoTa = "Chỉ đọc dữ liệu và xuất báo cáo", LaVaiTroHeThong = false, TrangThai = true, NgayTao = DateTime.Now }
        );
        context.SaveChanges();
    }

    if (seedEnabled && !context.ChiNhanhs.Any())
    {
        // Seed ChiNhanh (Branches)
        context.ChiNhanhs.AddRange(
            new ChiNhanh { MaChiNhanhHienThi = "CN001", TenChiNhanh = "Chi nhánh TP.HCM", DiaChi = "123 Đường Lê Lợi, Quận 1, TP.HCM", SoDienThoai = "02812345678", TruongChiNhanh = "Nguyễn Văn A", VungDiaLy = "Miền Nam", TrangThai = "HOAT_DONG", NgayTao = DateTime.Now },
            new ChiNhanh { MaChiNhanhHienThi = "CN002", TenChiNhanh = "Chi nhánh Hà Nội", DiaChi = "456 Đường Nguyễn Trãi, Quận Thanh Xuân, Hà Nội", SoDienThoai = "02412345678", TruongChiNhanh = "Trần Văn B", VungDiaLy = "Miền Bắc", TrangThai = "HOAT_DONG", NgayTao = DateTime.Now }
        );
        context.SaveChanges();
    }

    if (seedEnabled && !context.NguoiDungs.Any())
    {
    // Trước đây seed mật khẩu yếu cố định (admin123, nhanvien123…) nằm trong
    // source — dễ đoán, dễ brute-force. Giờ sinh mật khẩu ngẫu nhiên khi seed
    // và in ra console để admin biết rồi đổi. Chỉ chạy với DB mới (chưa có user).
    string RandomPassword(int length = 14)
    {
        const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#%";
        var bytes = new byte[length];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return new string(bytes.Select(b => chars[b % chars.Length]).ToArray());
    }
    var seedCredentials = new List<(string User, string Pass)>();

    var adminPw = RandomPassword();
    var cbvPw = RandomPassword();
    seedCredentials.Add(("admin", adminPw));
    seedCredentials.Add(("nhanvien", cbvPw));

    var adminVaiTro = context.VaiTros.FirstOrDefault(v => v.MaVaiTroHienThi == "ADMIN");
    var cbvVaiTro = context.VaiTros.FirstOrDefault(v => v.MaVaiTroHienThi == "CREDIT_OFFICER");
    var cnHCM = context.ChiNhanhs.FirstOrDefault(c => c.MaChiNhanhHienThi == "CN001");

    // Chỉ tạo user nếu có đủ VaiTro và ChiNhanh
    if (adminVaiTro != null && cnHCM != null)
    {
        context.NguoiDungs.Add(new NguoiDung
        {
            TenDangNhap = "admin",
            MatKhauBam = BCrypt.Net.BCrypt.HashPassword(adminPw),
            HoTen = "Administrator",
            Email = "admin@qltdbank.com",
            MaVaiTro = adminVaiTro.MaVaiTro,
            MaChiNhanh = cnHCM.MaChiNhanh,
            TrangThai = "HOAT_DONG",
            NgayTao = DateTime.Now
        });
        context.SaveChanges();
    }

    if (cbvVaiTro != null && cnHCM != null)
    {
        context.NguoiDungs.Add(new NguoiDung
        {
            TenDangNhap = "nhanvien",
            MatKhauBam = BCrypt.Net.BCrypt.HashPassword(cbvPw),
            HoTen = "Nguyễn Thị Nhân Viên",
            Email = "nhanvien@qltdbank.com",
            MaVaiTro = cbvVaiTro.MaVaiTro,
            MaChiNhanh = cnHCM.MaChiNhanh,
            TrangThai = "HOAT_DONG",
            NgayTao = DateTime.Now
        });
        context.SaveChanges();
    }

    // User cho các vai còn lại để có đủ người vận hành từng bước luồng.
    // Mỗi tài khoản sinh mật khẩu ngẫu nhiên riêng.
    var taiKhoanMau = new (string TenDangNhap, string HoTen, string VaiTro)[]
    {
        ("thamdinh", "Lê Thị Thẩm Định", "APPRAISER"),
        ("duyet1", "Phạm Văn Duyệt Một", "APPROVER_L1"),
        ("duyet2", "Võ Thị Duyệt Hai", "APPROVER_L2"),
        ("ketoan", "Đỗ Thị Kế Toán", "ACCOUNTANT"),
        ("baocao", "Bùn Văn Báo Cáo", "REPORT_VIEWER"),
    };

    foreach (var tk in taiKhoanMau)
    {
        var vaiTro = context.VaiTros.FirstOrDefault(v => v.MaVaiTroHienThi == tk.VaiTro);
        if (vaiTro == null || cnHCM == null) continue;

        var pw = RandomPassword();
        seedCredentials.Add((tk.TenDangNhap, pw));

        context.NguoiDungs.Add(new NguoiDung
        {
            TenDangNhap = tk.TenDangNhap,
            MatKhauBam = BCrypt.Net.BCrypt.HashPassword(pw),
            HoTen = tk.HoTen,
            Email = $"{tk.TenDangNhap}@qltdbank.com",
            MaVaiTro = vaiTro.MaVaiTro,
            MaChiNhanh = cnHCM.MaChiNhanh,
            TrangThai = "HOAT_DONG",
            NgayTao = DateTime.Now
        });
    }
    context.SaveChanges();

    // In credentials vừa tạo để admin lưu trữ / đổi mật khẩu.
    Console.WriteLine("\n[Seed] Đã tạo tài khoản mặc định (DB mới). Mật khẩu ngẫu nhiên, dùng 1 lần, VUI LÒNG ĐỔI SAU KHI ĐĂNG NHẬP:");
    foreach (var (user, pass) in seedCredentials)
        Console.WriteLine($"   {user,-10} {pass}");
    Console.WriteLine();
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowSpecificOrigins");

// Global Exception Handler
app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
