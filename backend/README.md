# QLTD Bank Backend

## Công nghệ
- **.NET 8.0**
- **ASP.NET Core Web API**
- **Entity Framework Core 8.0**
- **SQL Server**
- **JWT Authentication**

## Cấu trúc Project

```
QuanLyTinDung/
├── src/
│   ├── QuanLyTinDung.Api/          # Controllers, Middleware
│   ├── QuanLyTinDung.Application/ # DTOs, Services
│   ├── QuanLyTinDung.Domain/      # Entities, Enums
│   └── QuanLyTinDung.Infrastructure/ # DbContext, Repositories
└── QuanLyTinDung.slnx
```

## Cách chạy

### 1. Cài đặt .NET 8.0
Đảm bảo đã cài đặt .NET 8.0 SDK

### 2. Cấu hình Database
Chỉnh sửa `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SON-SUGOI\\SQLEXPRESS;Database=QuanLyTinDung;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 3. Build và chạy
```bash
cd src/QuanLyTinDung.Api
dotnet restore
dotnet build
dotnet run
```

### 4. Truy cập Swagger
Mở: `http://localhost:5071/swagger`

## API Endpoints

| Module | Endpoint | Mô tả |
|--------|----------|--------|
| Auth | `/api/auth/login` | Đăng nhập |
| Auth | `/api/auth/profile` | Lấy thông tin user |
| Khách hàng | `/api/khachhang` | CRUD khách hàng |
| Hồ sơ vay | `/api/hosovay` | CRUD hồ sơ vay |
| Sản phẩm | `/api/sanphamvay` | CRUD sản phẩm vay |
| Chi nhánh | `/api/chinhanh` | CRUD chi nhánh |
| Người dùng | `/api/nguoidung` | CRUD người dùng |
| Thẩm định | `/api/thamdinh` | Thẩm định tín dụng |
| Hợp đồng | `/api/hopdong` | Quản lý hợp đồng |
| Thanh toán | `/api/thanhtoan` | Ghi nhận thanh toán |
| Dashboard | `/api/dashboard` | Thống kê |

## Tài khoản mặc định

| Username | Password | Vai trò |
|----------|----------|---------|
| admin | Admin@123 | ADMIN |

## Tạo Database

Chạy script SQL trong SQL Server Management Studio:
- File: `scripts/init-database.sql`
