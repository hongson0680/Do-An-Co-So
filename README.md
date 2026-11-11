# QLTD Bank - Hệ thống Quản lý Tín dụng

## 🏦 Giới thiệu
Hệ thống quản lý tín dụng ngân hàng, hỗ trợ quản lý khách hàng, hồ sơ vay, thẩm định, phê duyệt và theo dõi thanh toán.

## 🚀 Công nghệ sử dụng

### Frontend
- **React 18** + TypeScript
- **Vite** - Build tool
- **Tailwind CSS** - Styling
- **React Router v6** - Routing
- **Zustand** - State management
- **Axios** - HTTP client
- **Lucide React** - Icons

### Backend (ASP.NET Core 8.0)
- Clean Architecture
- Entity Framework Core
- JWT Authentication
- SQL Server

## 📁 Cấu trúc dự án

```
DEMO_DACS4/
├── src/
│   ├── api/              # API clients
│   ├── components/       # Shared components
│   │   ├── common/      # Button, Input, Modal, Table...
│   │   └── layout/      # Sidebar, Header
│   ├── features/        # Feature modules
│   │   ├── auth/
│   │   ├── customers/
│   │   ├── loans/
│   │   ├── appraisal/
│   │   ├── contracts/
│   │   ├── payments/
│   │   ├── products/
│   │   ├── branches/
│   │   ├── users/
│   │   └── dashboard/
│   ├── layouts/         # Page layouts
│   ├── routes/          # Routing config
│   ├── stores/          # Zustand stores
│   ├── types/           # TypeScript types
│   └── utils/           # Utilities
├── scripts/             # Database scripts
└── README.md
```

## ⚡ Cài đặt

### 1. Clone và cài đặt dependencies
```bash
cd DEMO_DACS4
npm install
```

### 2. Cấu hình Database
Chạy script SQL trong SQL Server Management Studio:
```bash
# File: scripts/init-database.sql
# File: scripts/seed-data.sql
```

### 3. Cấu hình Backend
```json
// appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=SON-SUGOI\\SQLEXPRESS;Database=QuanLyTinDung;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### 4. Chạy ứng dụng
```bash
# Frontend
npm run dev

# Backend
dotnet run --project src/QuanLyTinDung.Api
```

## 🔐 Tài khoản mặc định

| Username | Password | Vai trò |
|----------|----------|---------|
| admin | Admin@123 | ADMIN |
| credit | Credit@123 | CREDIT_OFFICER |

## 📋 Tính năng chính

- [x] Đăng nhập / Đăng xuất
- [x] Dashboard thống kê
- [x] Quản lý khách hàng
- [x] Quản lý hồ sơ vay
- [x] Thẩm định tín dụng
- [x] Phê duyệt khoản vay
- [x] Quản lý hợp đồng
- [x] Quản lý thanh toán
- [x] Quản lý sản phẩm vay
- [x] Quản lý chi nhánh
- [x] Quản lý người dùng

## 📡 API Endpoints

| Module | Endpoint | Mô tả |
|--------|----------|--------|
| Auth | `/api/auth/login` | Đăng nhập |
| Khách hàng | `/api/khachhang` | CRUD khách hàng |
| Hồ sơ vay | `/api/hosovay` | CRUD hồ sơ vay |
| Thẩm định | `/api/thamdinh` | Thẩm định tín dụng |
| Phê duyệt | `/api/pheduyet` | Phê duyệt hồ sơ |
| Hợp đồng | `/api/hopdong` | Quản lý hợp đồng |
| Thanh toán | `/api/thanhtoan` | Ghi nhận thanh toán |
| Dashboard | `/api/dashboard` | Thống kê |

## 📄 License
MIT
