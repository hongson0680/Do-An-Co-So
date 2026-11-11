# HỆ THỐNG QUẢN LÝ TÍN DỤNG NGÂN HÀNG - ĐỒ ÁN CƠ SỞ

> **Câu hỏi - Đáp án** cho buổi báo cáo đồ án cơ sở. Tập trung vào **phân tích nghiệp vụ quản lý tín dụng** và **phân tích thiết kế hệ thống thông tin**. Mỗi câu trả lời ngắn gọn 2-4 dòng.

---

## 📌 PHẦN 1: BỐI CẢNH & LÝ DO XÂY DỰNG HỆ THỐNG

### Q1: Tại sao ngân hàng cần hệ thống quản lý tín dụng thay vì quản lý thủ công?
Vì quy trình tín dụng có nhiều bước liên phòng ban, nhiều chứng từ, nhiều khách hàng. Thủ công dễ thất lạc hồ sơ, sai sót số liệu, khó tra cứu lịch sử và tổng hợp báo cáo quản trị.

### Q2: Đối tượng phục vụ chính của hệ thống là ai?
- Cán bộ tín dụng (tiếp nhận hồ sơ)
- Thẩm định viên (đánh giá rủi ro)
- Cấp phê duyệt (L1, L2)
- Kế toán/ngân quỹ (giải ngân, thu nợ)
- Quản trị hệ thống (quản lý user, danh mục)

### Q3: Những khó khăn nào của nghiệp vụ tín dụng mà hệ thống giải quyết?
- Thất lạc chứng từ, giấy tờ pháp lý
- Tính toán lịch trả nợ sai
- Khó theo dõi nợ quá hạn
- Phê duyệt không có trace (ai duyệt, lúc nào, lý do)
- Báo cáo chậm, dữ liệu phân tán

---

## 📌 PHẦN 2: PHÂN TÍCH NGHIỆP VỤ

### Q4: Quy trình cấp tín dụng gồm những bước nào?
7 bước: (1) Tiếp nhận KH → (2) Tạo hồ sơ vay → (3) Upload tài liệu → (4) Thêm TSBĐ → (5) Thẩm định → (6) Phê duyệt → (7) Lập HĐ & giải ngân.

### Q5: Trạng thái hồ sơ vay có những gì? Chuyển trạng thái thế nào?
11 trạng thái theo state machine: `MOI → DA_NOP → DANG_XEM_XET → DU_TAI_LIEU → DANG_THAM_DINH → CHO_PHE_DUYET → DA_PHE_DUYET/TU_CHOI → DA_LAP_HOP_DONG → DA_GIAI_NGAN → DA_DONG`. Trigger SQL RB42 ngăn chuyển trạng thái sai.

### Q6: Thẩm định tín dụng đánh giá những yếu tố nào?
4 yếu tố chính: (1) **Thu nhập & dòng tiền** (DTI), (2) **Tài sản đảm bảo** (LTV), (3) **Lịch sử tín dụng CIC**, (4) **Mức rủi ro tổng hợp** (điểm 0-100). Hệ thống tự cập nhật mức rủi ro KH dựa trên điểm.

### Q7: Quyết định phê duyệt có những loại nào?
3 loại: **DA_PHE_DUYET** (duyệt, kèm số tiền + lãi suất + điều kiện), **TU_CHOI** (kèm lý do), **YEU_CAU_BO_SUNG** (yêu cầu thêm thông tin). Mỗi quyết định đều có cấp duyệt, người duyệt, thời gian.

### Q8: Ràng buộc nghiệp vụ quan trọng nhất trong hệ thống?
**RB38**: Tổng giá trị cho vay tối đa từ TSBĐ phải ≥ số tiền vay đề nghị. Đảm bảo mỗi khoản vay đều có đủ tài sản bảo đảm, tránh cấp tín dụng không có cover.

### Q9: Quy trình tài sản đảm bảo diễn ra như thế nào?
Khi thêm TSBĐ cho hồ sơ, hệ thống tính `tổng_cho_vay_tối_đa = Σ(giá_trị_TS × LTV)`. Nếu tổng < số tiền vay → báo lỗi "Cần bổ sung X đ". Đảm bảo không bỏ sót bước thế chấp.

### Q10: Lãi suất và kỳ hạn vay được quản lý thế nào?
Lưu ở **sản phẩm vay** (master data). Mỗi SP có: lãi suất (0-30%), kỳ hạn min-max, hạn mức min-max, yêu cầu TSBĐ. Hồ sơ vay **phải nằm trong** giới hạn SP (trigger RB38/39 enforce).

### Q11: Sau khi giải ngân, hệ thống quản lý trả nợ ra sao?
Tự động sinh **lịch trả nợ** theo công thức dư nợ giảm dần. Mỗi kỳ ghi: gốc phải trả, lãi phải trả, ngày đến hạn, trạng thái. Khi khách trả → ghi nhận thanh toán, cập nhật dư nợ.

### Q12: Phân biệt 3 vai trò chính trong quy trình?
- **Cán bộ tín dụng**: tạo HS, tiếp nhận KH, upload tài liệu
- **Thẩm định viên**: đánh giá rủi ro, tính điểm, đề xuất
- **Phê duyệt**: ra quyết định cuối (duyệt/từ chối), chịu trách nhiệm

### Q13: Khách hàng cá nhân và doanh nghiệp khác nhau thế nào trong hệ thống?
Phân biệt qua `loai_khach_hang = CA_NHAN | DOANH_NGHIEP`. Cùng bảng `khach_hang` nhưng có field riêng. Sản phẩm vay quy định đối tượng (`doi_tuong_kh`).

---

## 📌 PHẦN 3: PHÂN TÍCH DỮ LIỆU

### Q14: Hệ thống quản lý bao nhiêu loại đối tượng dữ liệu?
**15 bảng**, chia 5 nhóm:
- **Danh mục**: vai_tro, chi_nhanh, san_pham_vay
- **Chủ thể**: nguoi_dung, khach_hang
- **Nghiệp vụ chính**: ho_so_vay, tham_dinh_tin_dung, phe_duyet, hop_dong_vay, lich_tra_no, thanh_toan
- **Hỗ trợ**: tai_san_dam_bao, tai_lieu_vay
- **Hệ thống**: thong_bao, nhat_ky_he_thong

### Q15: Vì sao tách bảng `ho_so_vay` và `hop_dong_vay`?
Vì **1 hồ sơ vay chỉ tạo 1 hợp đồng** (quan hệ 1-1) nhưng về mặt nghiệp vụ: hồ sơ là giai đoạn xét duyệt, hợp đồng là giai đoạn giải ngân & trả nợ. Tách để quản lý vòng đời khác nhau và truy vết.

### Q16: Vì sao `tham_dinh_tin_dung` và `phe_duyet` tách riêng khỏi `ho_so_vay`?
Vì đây là **2 giai đoạn độc lập** với chu kỳ sống riêng: thẩm định có thể thực hiện nhiều lần, phê duyệt có thể yêu cầu bổ sung nhiều lần. Tách ra để lưu lịch sử, dễ audit.

### Q17: Có những ràng buộc toàn vẹn nào? Phân loại ra sao?
**44 ràng buộc (RB1-RB44)** chia 4 nhóm:
- **Miền giá trị** (RB1-RB21): lãi suất 0-30%, CCCD 9-12 số, SĐT 10 số, điểm 0-100
- **Liên thuộc tính** (RB22-RB25): email format, mật khẩu không trống
- **Liên bộ** (RB26-RB27): tổng tiền, tổng đã trả
- **Liên quan hệ** (RB28-RB44): FK, state machine, điều kiện nghiệp vụ

### Q18: Trigger SQL nào đóng vai trò bảo vệ nghiệp vụ?
4 trigger quan trọng:
- **T1**: Kiểm tra số tiền/kỳ hạn nằm trong sản phẩm (RB38/39)
- **T2**: State machine hồ sơ vay (RB42)
- **T3**: Bắt buộc có thẩm định trước khi phê duyệt (RB44)
- **T4**: Tự động cập nhật mức rủi ro KH theo điểm (RB40)

### Q19: Vì sao dùng trigger mà không chỉ validate ở C#?
Vì trigger bảo vệ ở tầng database, kể cả khi có ứng dụng khác (SSMS, tool báo cáo) truy cập trực tiếp. Đảm bảo **tính toàn vẹn nghiệp vụ bền vững** không phụ thuộc code ứng dụng.

### Q20: Tại sao bảng `khach_hang` lại có field `muc_do_rui_ro`?
Vì mức rủi ro được **tự động cập nhật** mỗi khi có thẩm định mới (trigger T4). Lưu ở KH giúp tra cứu nhanh, không cần JOIN với thẩm định khi xem lịch sử quan hệ tín dụng.

### Q21: Có thể xóa khách hàng đã có hồ sơ vay không?
Không - vì ràng buộc FK từ `ho_so_vay.ma_khach_hang`. Hệ thống dùng **soft delete** hoặc cấm xóa để giữ lịch sử tín dụng lâu dài (phục vụ CIC).

### Q22: Vì sao lưu `ma_ho_so_hien_thi` riêng biệt với `ma_ho_so`?
`ma_ho_so` là khóa tự tăng (BIGINT) để máy đọc, `ma_ho_so_hien_thi` (vd: `HSV-20260608-329C`) là mã nghiệp vụ có ngày + GUID rút gọn để người dùng trao đổi, in ấn.

---

## 📌 PHẦN 4: PHÂN TÍCH CHỨC NĂNG

### Q23: Hệ thống có những nhóm chức năng chính nào?
10 nhóm: (1) Quản lý tài khoản, (2) Quản lý KH, (3) Quản lý sản phẩm vay, (4) Quản lý hồ sơ vay, (5) Quản lý TSBĐ & tài liệu, (6) Thẩm định, (7) Phê duyệt, (8) Quản lý HĐ & giải ngân, (9) Quản lý trả nợ, (10) Báo cáo & thống kê.

### Q24: Quản lý khách hàng cần những thông tin gì?
- **Cá nhân**: họ tên, CCCD/CMND (9-12 số), ngày sinh, SĐT, email, nghề nghiệp, địa chỉ
- **Doanh nghiệp**: tên DN, MST, ngành nghề, người đại diện
- Tự động tính mức rủi ro dựa trên lịch sử tín dụng

### Q25: Khi tạo hồ sơ vay, cần nhập những gì?
Thông tin bắt buộc: khách hàng, sản phẩm vay, số tiền đề nghị, kỳ hạn (tháng), mục đích vay. Hệ thống tự động gán mã hồ sơ, ngày tạo, cán bộ tạo (từ user đăng nhập), trạng thái ban đầu = MOI.

### Q26: Phê duyệt khoản vay hoạt động thế nào?
1. Hồ sơ phải ở trạng thái `CHO_PHE_DUYET`
2. Cán bộ phê duyệt chọn: Duyệt / Từ chối / Yêu cầu bổ sung
3. Nếu duyệt: nhập số tiền duyệt, lãi suất, điều kiện
4. Trigger RB44 tự động kiểm tra đã có thẩm định chưa
5. Hệ thống ghi log + thông báo cho cán bộ tạo hồ sơ

### Q27: Theo dõi nợ quá hạn được thực hiện ra sao?
- Bảng `hop_dong_vay` có `trang_thai`: HOAT_DONG, QUA_HAN, QUA_HAN_30, QUA_HAN_60, DA_DONG
- Bảng `lich_tra_no` có `ngay_den_han` và `trang_thai`: CHUA_THANH_TOAN, DA_THANH_TOAN, TRE_HAN
- Dashboard thống kê tỷ lệ quá hạn
- (Tương lai: cron job tự động cập nhật trạng thái mỗi ngày)

### Q28: Lịch sử thao tác (audit log) được lưu thế nào?
Mỗi thao tác CREATE/UPDATE/DELETE/APPROVE/REJECT đều ghi vào `nhat_ky_he_thong`: người thực hiện, thời gian, đối tượng tác động, giá trị cũ/mới, IP, trình duyệt. Phục vụ truy vết trách nhiệm.

---

## 📌 PHẦN 5: BÁO CÁO & THỐNG KÊ

### Q29: Dashboard cung cấp những thông tin gì?
- Tổng quan: tổng KH, tổng hồ sơ, tổng HĐ, tổng giải ngân
- Theo trạng thái: HS mới, đang xử lý, đã duyệt, từ chối
- Giải ngân: tổng tiền giải ngân tháng này
- Rủi ro: tỷ lệ nợ quá hạn
- Biểu đồ: giải ngân 12 tháng, top 10 KH vay nhiều

### Q30: Hệ thống có xuất báo cáo Excel/PDF không?
Có. Dùng **ClosedXML** xuất Excel và **QuestPDF** xuất PDF. Có thể xuất danh sách KH, danh sách hồ sơ, báo cáo giải ngân, báo cáo nợ quá hạn.

### Q31: Cán bộ quản lý có thể xem báo cáo gì?
- Báo cáo tổng hợp theo chi nhánh, theo thời gian
- Báo cáo hiệu quả cán bộ tín dụng (số HS xử lý, tỷ lệ duyệt)
- Báo cáo rủi ro (nợ quá hạn, phân bổ rủi ro KH)
- Dashboard KPI

---

## 📌 PHẦN 6: BẢO MẬT & PHÂN QUYỀN

### Q32: Hệ thống đảm bảo an toàn thông tin khách hàng thế nào?
- Mật khẩu mã hóa BCrypt (không lưu plain text)
- JWT xác thực với thời hạn 24h
- Phân quyền theo vai trò (RBAC)
- Audit log mọi thao tác
- CORS chỉ cho phép origin cụ thể

### Q33: 7 vai trò trong hệ thống phân chia quyền thế nào?
- **ADMIN**: Toàn quyền (cấu hình, user, danh mục)
- **CREDIT_OFFICER**: Tạo HS, thêm TSBĐ, upload tài liệu
- **APPRAISER**: Thẩm định, đánh giá rủi ro
- **APPROVER_L1, L2**: Phê duyệt khoản vay theo hạn mức
- **ACCOUNTANT**: Giải ngân, ghi nhận thanh toán
- **REPORT_VIEWER**: Chỉ xem báo cáo

### Q34: Vì sao cần phân quyền chi tiết theo chức năng?
Để đảm bảo **tách biệt trách nhiệm** (segregation of duties): người tạo HS không thể tự duyệt HS của mình, người giải ngân không thể tự tạo thanh toán. Giảm rủi ro gian lận nội bộ.

---

## 📌 PHẦN 7: KIỂM THỬ & DEMO

### Q35: Cách đăng nhập hệ thống demo?
- URL: `http://localhost:3000`
- Username: `admin` / Password: `admin123`

### Q36: Hệ thống có sẵn dữ liệu mẫu để demo không?
Có: 3 chi nhánh (Hội sở, HCM, HN), 7 vai trò, 5 sản phẩm vay, 1 tài khoản admin. Khi cần có thể thêm KH, hồ sơ vay, hợp đồng để demo luồng nghiệp vụ.

### Q37: Quy trình demo nên trình bày thế nào?
1. Đăng nhập với tài khoản admin
2. Tạo khách hàng mới
3. Tạo hồ sơ vay cho khách
4. Upload tài liệu, thêm TSBĐ
5. Chuyển trạng thái hồ sơ
6. Tạo thẩm định (chấm điểm rủi ro)
7. Phê duyệt khoản vay
8. Lập hợp đồng → sinh lịch trả nợ
9. Ghi nhận thanh toán
10. Xem dashboard thống kê

---

## 📌 PHẦN 8: ĐÁNH GIÁ & HƯỚNG PHÁT TRIỂN

### Q38: Hệ thống đã đáp ứng được mục tiêu đề ra chưa?
Phần lớn đạt: quản lý KH, sản phẩm vay, hồ sơ, thẩm định, phê duyệt, hợp đồng, lịch trả nợ, thanh toán, báo cáo, audit log. Một số tính năng nâng cao chưa có (AI chấm điểm, cảnh báo tự động, mobile app).

### Q39: Hạn chế của hệ thống?
- Chỉ hỗ trợ 1 cấp phê duyệt (chưa multi-level L1→L2 thật sự)
- Chưa tích hợp Core Banking thật
- Chưa có AI/ML chấm điểm tín dụng
- Chưa có email/SMS thật (chỉ thông báo in-app)
- Chưa tự động cập nhật nợ quá hạn (cần cron job)
- Upload file chỉ lưu đường dẫn, chưa có storage riêng

### Q40: Hướng phát triển trong tương lai?
- AI/ML chấm điểm tín dụng tự động từ dữ liệu lịch sử
- Mobile app cho KH tra cứu khoản vay, lịch trả nợ
- E-signature cho hợp đồng điện tử
- Tích hợp Core Banking qua API
- Background job tự động phát hiện nợ quá hạn, gửi SMS nhắc nợ
- Microservices khi scale lớn

### Q41: Bài học kinh nghiệm rút ra từ đồ án?
- Thiết kế database trước khi code giúp giảm sai sót
- Trigger SQL bảo vệ nghiệp vụ tốt hơn code
- State machine giúp kiểm soát luồng xử lý chặt chẽ
- Phân quyền RBAC là yếu tố bắt buộc trong hệ thống tài chính

---

## 📌 PHẦN 9: CÂU HỎI "NHẤN MẠNH" CÓ THỂ HỎI

### Q42: Điểm mới/độc đáo của đề tài so với các hệ thống tương tự?
- Tích hợp **4 trigger SQL** bảo vệ nghiệp vụ ở tầng database
- **44 ràng buộc toàn vẹn** được enforce cả 2 lớp (SQL + C#)
- Tự động tính **lịch trả nợ dư nợ giảm dần** khi lập hợp đồng
- Tự động cập nhật **mức rủi ro khách hàng** theo điểm thẩm định
- **Audit log đầy đủ** phục vụ truy vết

### Q43: Nếu ngân hàng thay đổi quy trình, hệ thống có dễ điều chỉnh?
- Thêm bước mới → cập nhật state machine trong `TRG_hosovay_trangthai`
- Thêm trường thông tin → migration mới
- Thêm vai trò → cập nhật bảng `vai_tro` và menu frontend
- Thêm sản phẩm vay → cập nhật `san_pham_vay`

### Q44: Tại sao quyết định lưu lịch sử thẩm định thay vì chỉ giữ kết quả cuối?
Vì **mỗi lần thẩm định là một quyết định nghiệp vụ** riêng biệt, có thể dẫn đến kết quả khác nhau (cùng KH, cùng HS nhưng thẩm định lại vì bổ sung thông tin). Lưu lịch sử phục vụ audit, khiếu nại, đào tạo.

### Q45: Hệ thống xử lý trường hợp nhiều người cùng duyệt 1 hồ sơ?
Hiện tại mỗi HS chỉ có 1 bản ghi `phe_duyet` (quan hệ 1-1). Tương lai có thể mở rộng thành bảng `phe_duyet` cho phép nhiều cấp duyệt (L1 → L2) với thứ tự xử lý.

### Q46: Vì sao lại chọn mô hình 3 lớp (frontend/backend/database) thay vì monolith?
- Phân tách team phát triển (FE, BE, DBA)
- Dễ scale từng phần độc lập
- Tái sử dụng backend cho mobile app, partner API
- Bảo trì dễ dàng hơn

### Q47: Hệ thống đảm bảo tuân thủ quy định ngân hàng nào?
- **CIC** (Trung tâm Thông tin Tín dụng): có trường điểm CIC, ngày tra cứu
- **Luật các TCTD**: đầy đủ ràng buộc về hạn mức, lãi suất, kỳ hạn
- **Quy định về tài sản đảm bảo**: đầy đủ thông tin pháp lý TSBĐ
- **Audit & truy vết**: nhật ký thao tác đầy đủ

---

## 🚀 HƯỚNG DẪN CHẠY NHANH

```bash
# Bước 1: Setup Database
Mở SSMS → chạy file scripts/init-database.sql

# Bước 2: Sửa connection string nếu cần
# backend/src/QuanLyTinDung.Api/appsettings.json

# Bước 3: Chạy Backend
cd backend
dotnet run --project src/QuanLyTinDung.Api
# → API chạy ở http://localhost:5071
# → Swagger ở http://localhost:5071/swagger

# Bước 4: Chạy Frontend (terminal mới)
npm install
npm run dev
# → Frontend ở http://localhost:3000

# Bước 5: Đăng nhập
# admin / admin123
```

---

**Tác giả**: [Tên sinh viên]  
**GVHD**: [Tên giảng viên]  
**Năm**: 2026
