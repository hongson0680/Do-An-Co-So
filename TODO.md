# TODO — Hệ thống Quản lý Tín dụng

Cập nhật: sau Batch 1–4 (fix nghiệp vụ) + push lên GitHub.

Trạng thái build hiện tại: **backend 0 error**, **frontend `tsc` 0 error**.

---

## 🔴 P1 — Bảo mật (làm trước tiên)

### 1. Route frontend không chặn theo quyền
- **File:** `src/routes/index.tsx`
- **Vấn đề:** `ProtectedRoute` chỉ kiểm tra `isAuthenticated`, không kiểm tra vai trò.
  Ai đăng nhập được cũng gõ `/users`, `/branches`, `/audit-log` vào URL là thấy trang.
- **Hệ quả:** Backend có chặn (401/403) nên **không lộ dữ liệu**, nhưng UI vẫn hiện rồi mới báo lỗi → UX tệ.
- **Cách sửa:** Thêm component `RoleRoute` (hoặc mở rộng `ProtectedRoute`) nhận `perm` và kiểm tra `can(PERM.X)` từ `usePermission()`.

### 2. `GET /nguoi-dung`, `GET /vai-tro`, `GET /chi-nhanh` không giới hạn vai
- **Files:**
  - `backend/src/QuanLyTinDung.Api/Controllers/NguoiDungController.cs:26`
  - `backend/src/QuanLyTinDung.Api/Controllers/VaiTroController.cs:23`
  - `backend/src/QuanLyTinDung.Api/Controllers/ChiNhanhController.cs:55`
- **Vấn đề:** Chỉ có `[Authorize]` (bất kỳ ai đã đăng nhập đều đọc được).
- **Đối chiếu RBAC:** `MANAGE_USERS` / `MANAGE_BRANCHES` phải thuộc ADMIN.
  (`NhatKyHeThongController` đã chặn ADMIN ✓ — dùng làm mẫu.)
- **Cách sửa:** Thêm `[Authorize(Roles = Roles.AdminOnly)]` cho các endpoint danh sách này.

### 3. JWT secret từng nằm trong repo (đã xử lý một phần)
- **Đã làm:** `appsettings.json` giờ chỉ có placeholder; secret thật nằm ở `appsettings.Development.json` (git-ignored).
- **Còn lại:**
  - [ ] **Đổi giá trị secret** — giá trị cũ đã public qua commit đầu tiên.
  - [ ] Dùng biến môi trường / `dotnet user-secrets` cho môi trường thật (production).
  - [ ] Ghi hướng dẫn setup vào `README.md` (bước tạo `appsettings.Development.json` khi clone).

### 4. Không có lockout / rate-limit đăng nhập
- **File:** `backend/src/QuanLyTinDung.Api/Controllers/AuthController.cs`
- **Vấn đề:** `Login` không giới hạn số lần thử → đoán mật khẩu không bị chặn.
- **Rủi ro thực tế:** Mật khẩu seed yếu (`admin123`, `nhanvien123`, `duyet1123`…).
- **Cách sửa:** Thêm bộ đếm số lần thử sai theo username/IP, khóa tài khoản tạm thời sau N lần. Có thể dùng `IMemoryCache` (không cần đổi cấu trúc DB).

---

## 🟠 P2 — Chất lượng & vận hành

### 5. Không có test project nào
- **Hiện trạng:** `backend/src/` chỉ có 4 project (Api / Application / Domain / Infrastructure), **không có project test**.
- **Hệ quả:** Mọi fix đều chỉ verify bằng `dotnet build` + `tsc`, không có regression test.
- **Nên test trước:**
  - [ ] `CreateLichTraNo` — công thức dư nợ giảm dần, `Tổng gốc = Tổng lãi + Gốc` khi làm tròn.
  - [ ] Luồng phê duyệt: `YEU_CAU_BO_SUNG` không bị ghi đè thành `TU_CHOI` (đã fix nhưng chưa có test).
  - [ ] Hạn mức phê duyệt L1/L2 (`CreditPolicy.ApproverL1Limit` / `ApproverL2Limit`).

### 6. Không có git trước đây — ✅ ĐÃ XỬ LÝ
- Đã `git init`, commit `c550e2b`, push lên `hongson0680/Do-An-Co-So` (branch `main`).
- Đã thêm `.gitignore` loại `node_modules/`, `bin/`, `obj/`, `dist/`, file secret.

### 7. Token hết hạn 24 giờ, không có refresh token
- **File:** `backend/src/QuanLyTinDung.Api/Controllers/AuthController.cs:152` (`expires: DateTime.Now.AddHours(24)`)
- **Hệ quả:** Mỗi 24 giờ user bị đá ra, phải đăng nhập lại.
- **Cách sửa:** Thêm refresh token (cần cân nhắc thêm bảng → **DDL**, hoặc dùng token stateless dài hạn + cơ chế refresh in-memory).

### 8. `GET /api/nguoi-dung` trả toàn bộ user, không phân trang
- **File:** `backend/src/QuanLyTinDung.Api/Controllers/NguoiDungController.cs:26`
- **Cách sửa:** Thêm `pageNumber`/`pageSize` + trả `PagedResult<NguoiDungDto>` (pattern đã có sẵn ở các controller khác).

### 9. `DashboardController` chưa giới hạn vai
- **File:** `backend/src/QuanLyTinDung.Api/Controllers/DashboardController.cs:11`
- **Vấn đề:** Mọi user đăng nhập đều xem được số liệu toàn hệ thống (tiền vay, tổng giải ngân, top khách hàng).
- **Cách sửa:** Chặn theo `Roles.Reporters` hoặc tách endpoint nhạy cảm (tiền) cho ADMIN.

### 10. Trạng thái `QUA_HAN` của hợp đồng không có logic tự chuyển
- **Hiện trạng:** `QUA_HAN` / `QUA_HAN_30` / `QUA_HAN_60` chỉ được *đọc* ở `DashboardController.cs:51`.
  Không tìm thấy chỗ nào **set** các trạng thái này.
- **Cách sửa:** Thêm job nền (hosted service) quét `lich_tra_no` quá hạn → cập nhật `hop_dong_vay.trang_thai`.
  **Lưu ý:** trigger hiện có có thể đã xử lý → kiểm tra `sys.triggers` trước khi làm.

### 11. Mật khẩu seed yếu + nằm trong source
- **File:** `backend/src/QuanLyTinDung.Api/Program.cs`
- **Hiện trạng:** `admin123`, `nhanvien123`, `thamdinh123`, `duyet1123`, `duyet2123`, `ketoan123`, `baocao123`
- **Đã làm:** Bọc seed trong cờ `Seed:Enabled` (mặc định `true` = giữ hành vi cũ).
  Đặt `"Seed": { "Enabled": false }` trong `appsettings.json` → app **không chèn row nào**.
- **Còn lại:** Đổi mật khẩu mặc định thành chuỗi random in ra console lúc seed, hoặc bắt user đổi mật khẩu sau lần đăng nhập đầu.

---

## 🟡 P3 — Ghi nhận, ưu tiên thấp

- [ ] **Danh sách khách hàng dùng chung** — bảng `khach_hang` không có cột chi nhánh.
  Muốn cô lập theo chi nhánh thì **bắt buộc phải DDL** (ngoài phạm vi đã cho phép). Đã ghi rõ trong `docs/rbac-matrix.md`.
- [ ] **`MucDoRuiRo` lưu 2 format** (có dấu `"Thấp"` / không dấu `"Thap"`) — đã có converter xử lý cả hai nên chạy đúng, nhưng dữ liệu hỗn hợp. Cần migration dữ liệu (UPDATE) nếu muốn thống nhất.
- [ ] **PDF export chưa chạy thật** — `QuestPDF` đã cài, build OK, nhưng **chưa verify** file PDF render đúng. Cần chạy app + xuất thử.
- [ ] **`thanhToanApi.getByLichTraNo` đã xoá** — backend có `GET /hop-dong/{id}/lich-tra-no` nhưng không có route theo lịch trả nợ. Nếu cần màn hình "lịch trả nợ" thì đang thiếu endpoint.

---

## ✅ Đã hoàn thành (Batch 1–4)

**Batch 1–2 (nghiệp vụ):**
- `YEU_CAU_BO_SUNG` không còn bị ghi đè thành `TU_CHOI`
- Lịch trả nợ dư nợ giảm dần, gốc chia đều
- Trigger / check constraint khớp với logic nghiệp vụ

**Batch 3 (frontend):**
- Deep-link `/contracts?maHopDong=` và `/loans?maHoSo=` hoạt động
- ContractList refresh danh sách hồ sơ sau khi tạo HĐ (hết lỗi 400)
- Màu rating Tailwind: chuyển từ class động (`text-${color}-600`) sang class literal
- Nút "Xem chi tiết" ở ApprovalList mở modal thật
- Xoá 3 API method trỏ route 404 (`thamdinhApi.update`, `thanhToanApi.getByLichTraNo`, `hoSoVayApi.getByCustomer`)
  — giữ `nguoidungApi.update` vì backend **có** `PUT /nguoidung/{id}`
- `NotificationBell` truyền `maHoSo` vào link thông báo

**Batch 4 (backend):**
- Dashboard đếm đúng `HoSoDaDuyet` / `HoSoDangXyLy`
- Dashboard sort tháng theo (Năm, Tháng) thay vì sort chuỗi `"MM/yyyy"`
- `Enum.Parse` trả 400 thay vì 500 (`KhachHangController`)
- 3 chỗ leak `ex.Message` (SQL) → log server, client thấy message chung
- Guard phân trang ở 8 controller (`pageNumber`/`pageSize` ≤ 0 → 500)
- `cap_phe_duyet` lấy theo vai thay vì hardcode `1`
- Lãi suất âm bị báo lỗi thay vì lưu NULL âm thầm
- `ghiChu` khi đổi trạng thái được lưu vào audit log
- Bỏ 4 con số % bịa ở Dashboard (thay bằng số liệu thật)
- `ThongBaoController`: thêm guard `userId == 0` cho `read-all` và `thong-ke`
- Seed: thêm cờ `Seed:Enabled`, bổ sung 5 vai trò + tài khoản cho các vai còn thiếu

**Hạ tầng:**
- `git init` + `.gitignore` + push lên `hongson0680/Do-An-Co-So`
- JWT secret + connection string chuyển khỏi `appsettings.json` sang file git-ignored
