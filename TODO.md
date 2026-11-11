# TODO — Hệ thống Quản lý Tín dụng

Cập nhật: sau Batch 5 (vá bảo mật + chất lượng).

Trạng thái build hiện tại: **backend 0 error**, **frontend `tsc` 0 error**, **test 18/18 pass**.

---

## ✅ Hoàn thành — Batch 5

| # | Vấn đề | Đã làm |
|---|--------|--------|
| 1 | Route frontend không chặn theo quyền | Component `RoleRoute` (check `can(perm)`), bọc toàn bộ route trong `src/routes/index.tsx` |
| 2 | `GET /nguoi-dung`, `/vai-tro` không giới hạn vai | Thêm `[Authorize(Roles = Roles.AdminOnly)]`. `GET /chi-nhanh` **giữ mở** — matrix RBAC quy định mọi vai được R chi nhánh |
| 3 | JWT secret public qua git | Đổi key mới (openssl rand 48 bytes) trong `appsettings.Development.json` (git-ignored) |
| 4 | Không lockout đăng nhập | `LoginAttemptService` (IMemoryCache): khóa 15 phút sau 5 lần sai, trả 429. Cấu hình `LoginSecurity:*` |
| 5 | Không có test project | `backend/tests/QuanLyTinDung.Tests` (xUnit): 9 test `LoanScheduleService` (tách logic lãi suất ra khỏi controller), 9 test `ApprovalPolicyService` (tách logic hạn mức L1/L2) |
| 6 | Token 24h, không refresh | Access 8h + refresh 7 ngày (stateless, không DDL). Endpoint `POST /auth/refresh`, interceptor axios tự refresh nền + retry |
| 7 | `GET /nguoi-dung` không phân trang | Thêm `pageNumber`/`pageSize`/`search`, frontend UserList có ô tìm + Pagination |
| 8 | Dashboard lộ số liệu toàn hệ thống | Cô lập theo chi nhánh (CREDIT_OFFICER chỉ thấy CN mình, cùng cơ chế `ShouldScopeByBranch()`). Không chặn role vì matrix cho 7 vai đều xem dashboard |
| 9 | `QUA_HAN` không có logic tự chuyển | Hosted service `OverdueContractJob`: quét `lich_tra_no` quá hạn mỗi 60 phút, set QUA_HAN / _30 / _60. Trigger không đụng `hop_dong_vay` nên không xung đột |
| 10 | Mật khẩu seed yếu | Seed DB mới: mật khẩu ngẫu nhiên 14 ký tự, in ra console khi khởi động, không còn `admin123` trong source |
| 12 | `MucDoRuiRo` 2 format | Kiểm tra DB: 0 row, trigger + converter đã ghi format có dấu khớp CHECK constraint → **không cần migration** |

---

## 🟡 P3 — Còn lại (ưu tiên thấp)

- [ ] **Danh sách khách hàng dùng chung** — bảng `khach_hang` không có cột chi nhánh.
  Muốn cô lập theo chi nhánh thì **bắt buộc phải DDL** (ngoài phạm vi đã cho phép). Đã ghi rõ trong `docs/rbac-matrix.md`.
- [ ] **PDF export chưa chạy thật** — `QuestPDF` đã cài, build OK, nhưng **chưa verify** file PDF render đúng. Cần chạy app + xuất thử.
- [ ] **`thanhToanApi.getByLichTraNo` đã xoá** — backend có `GET /hop-dong/{id}/lich-tra-no` nhưng không có route theo lịch trả nợ. Nếu cần màn hình "lịch trả nợ" thì đang thiếu endpoint.
- [ ] **Refresh token không revocable** — stateless (không bảng lưu) nên không thu hồi được khi đổi mật khẩu / logout. Nếu cần, phải thêm bảng (DDL).
- [ ] **Login lockout reset khi restart API** — bộ đếm ở IMemoryCache, thiết kế có chủ đích (không đụng DB). Muốn durable thì cần thêm bảng.

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
