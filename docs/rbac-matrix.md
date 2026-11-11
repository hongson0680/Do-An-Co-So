# Bảng phân quyền (RBAC) — QLTD Bank

> Mô hình: **RBAC theo vai, không dùng bảng quyền động (không DDL)**.
> Một nguồn duy nhất định nghĩa tập vai: `backend/src/QuanLyTinDung.Api/Authorization/RoleConstants.cs`.
> Enforce 2 lớp: **backend** = thẩm quyền cuối (`[Authorize(Roles)]`) · **frontend** = UX (ẩn menu/nút, Batch B2b).

## 1. Bảy vai trò (khớp `vai_tro` trong DB)

| Mã vai trò | Tên | Trách nhiệm |
|---|---|---|
| `ADMIN` | Quản trị viên | Toàn quyền: CRUD, người dùng/chi nhánh/vai trò, audit, export |
| `CREDIT_OFFICER` | Cán bộ tín dụng | Mở nguồn: khách hàng, hồ sơ vay, tài liệu, TSBĐ |
| `APPRAISER` | Thẩm định viên | Lập thẩm định, TSBĐ định giá |
| `APPROVER_L1` | Phê duyệt cấp 1 | Duyệt khoản vay **≤ 500.000.000đ** |
| `APPROVER_L2` | Phê duyệt cấp 2 | Duyệt khoản vay **≤ 5.000.000.000đ** |
| `ACCOUNTANT` | Kế toán / Ngân quỹ | Hợp đồng, giải ngân, thanh toán |
| `REPORT_VIEWER` | Xem báo cáo | Chỉ đọc + export (không write) |

## 2. Ma trận thao tác × vai trò

Legend: ✔ đầy đủ · C=create · U=update · R=read · – không được

| Thao tác (endpoint chính) | ADMIN | CREDIT_OFFICER | APPRAISER | APPROVER_L1 | APPROVER_L2 | ACCOUNTANT | REPORT_VIEWER |
|---|---|---|---|---|---|---|---|
| Khách hàng: GET | ✔ | C+U | R | R | R | R | R |
| Khách hàng: POST/PUT | ✔ | C+U | – | – | – | – | – |
| Khách hàng: DELETE | ✔ | – | – | – | – | – | – |
| Hồ sơ vay: GET (cô lập theo CN\*) | ✔ | C+U+ĐổiTT (riêng CN mình) | R | R | R | R | R |
| Hồ sơ vay: POST/PUT(trang-thai) | ✔ | C+U | – | – | – | – | – |
| Hồ sơ vay: DELETE | ✔ | – | – | – | – | – | – |
| Thẩm định: GET | ✔ | R | R | R | R | R | R |
| Thẩm định: POST | ✔ | C | C | – | – | – | – |
| **Phê duyệt: POST** (hạn mức L1/L2\*\*) | ✔ | – | – | C ≤500tr | C ≤5 tỷ | – | – |
| **Hợp đồng: POST + giải ngân** | ✔ | – | – | – | – | C | – |
| **Thanh toán: POST** | ✔ | – | – | – | – | C | – |
| Tài liệu vay / TSBĐ: POST/PUT | ✔ | C+U | C+U | – | – | – | R |
| Tài liệu vay / TSBĐ: DELETE | ✔ | – | – | – | – | – | – |
| Người dùng: POST/PUT/DELETE | ✔ | – | – | – | – | – | – |
| Chi nhánh: POST/PUT/DELETE | ✔ | R | R | R | R | R | R |
| Sản phẩm vay: POST/PUT/DELETE | ✔ | R | R | R | R | R | R |
| Vai trò: POST | ✔ | – | – | – | – | – | – |
| Nhật ký hệ thống (audit) | ✔ | – | – | – | – | – | – |
| Export (khách hàng, hồ sơ, HĐ, thanh toán, dashboard) | ✔ | – | ✔ | ✔ | ✔ | ✔ | ✔ |

\* **Cô lập theo chi nhánh** — chỉ `CREDIT_OFFICER` bị giới hạn: chỉ thấy/xử lý hồ sơ vay do cán bộ **cùng chi nhánh** tạo (filter theo `ho_so_vay.ma_can_bo` ∈ user chi nhánh đó). Các vai trung tâm (thẩm định, phê duyệt, ngân quỹ, báo cáo) và ADMIN thấy dữ liệu xuyên chi nhánh. Cơ chế: claim `branch` trong JWT (claim OIDC chuẩn) → helper `ClaimsHelper.ShouldScopeByBranch()`. **Danh sách khách hàng dùng chung** (bảng `khach_hang` không có cột chi nhánh; nếu cần cô lập ở cấp khách hàng thì phải thêm cột → DDL).

\*\* **Hạn mức phê duyệt** — check trong `PheDuyetController.Create` theo `CreditPolicy` (mặc định L1: 500tr, L2: 5 tỷ; ghi đè qua `appsettings.json → CreditPolicy`). Vượt hạn mức → `400` kèm hướng dẫn chuyển cấp cao hơn.

## 3. Nhóm quyền (constant tập trung)

| Nhóm trong `Roles` | Thành viên | Áp dụng cho |
|---|---|---|
| `Roles.Approvers` | ADMIN, L1, L2 | POST phê duyệt |
| `Roles.Appraisers` | ADMIN, APPRAISER, CREDIT_OFFICER | POST thẩm định |
| `Roles.Originators` | ADMIN, CREDIT_OFFICER | khách hàng C+U, hồ sơ vay C+đổi trạng thái |
| `Roles.Finance` | ADMIN, ACCOUNTANT | hợp đồng C, giải ngân, thanh toán |
| `Roles.CollateralManagers` | ADMIN, CREDIT_OFFICER, APPRAISER | tài liệu + TSBĐ C+U |
| `Roles.Reporters` | mọi vai **trừ** CREDIT_OFFICER | export báo cáo/dữ liệu |
| `Roles.AdminOnly` | ADMIN | user/branch/product DELETE, TSBĐ & hồ sơ & khách hàng DELETE |

## 4. Ghi chú triển khai

1. **Backend là lớp an toàn cuối** — frontend chỉ ẩn menu/nút; khách sửa code client vẫn bị 403.
2. JWT hiện mang 4 claim: `NameIdentifier` (id user), `Name` (username), `Role` (mã vai), `branch` (ma chi nhánh).
3. Đổi hạn mức: `appsettings.json` → `CreditPolicy:ApproverL1Limit/ApproverL2Limit` (không cần build lại).
4. Khi thêm vai mới: (a) seed `vai_tro`, (b) cập nhật `RoleConstants` — không sửa controller.
