namespace QuanLyTinDung.Api.Authorization;

/// <summary>
/// MÃ VAI TRÒ (khớp 1-1 với ma_vai_tro_hien_thi đã seed trong DB / init-database.sql).
/// Dùng chung cho [Authorize(Roles=...)] ở mọi controller và (qua API) cho frontend,
/// tránh "magic string" lệch giữa các file.
/// </summary>
public static class Roles
{
    public const string Admin          = "ADMIN";
    public const string CreditOfficer  = "CREDIT_OFFICER";
    public const string Appraiser      = "APPRAISER";
    public const string ApproverL1     = "APPROVER_L1";
    public const string ApproverL2     = "APPROVER_L2";
    public const string Accountant     = "ACCOUNTANT";
    public const string ReportViewer   = "REPORT_VIEWER";

    // ---- Nhóm quyền (tập hợp vai trò được phép) ----
    // Phê duyệt / từ chối khoản vay
    public const string Approvers = $"{Admin},{ApproverL1},{ApproverL2}";

    // Thẩm định tín dụng (lập kết quả + TSBĐ định giá)
    public const string Appraisers = $"{Admin},{Appraiser},{CreditOfficer}";

    // Mở nguồn / tạo khách hàng & hồ sơ vay
    public const string Originators = $"{Admin},{CreditOfficer}";

    // Ngân quỹ: lập hợp đồng, giải ngân, ghi nhận thanh toán
    public const string Finance = $"{Admin},{Accountant}";

    // Quản trị (users, branches, products, roles)
    public const string AdminOnly = Admin;

    // Báo cáo / xuất dữ liệu (đọc-only) — mọi vai ngoại trừ CREDIT_OFFICER
    public const string Reporters = $"{Admin},{Appraiser},{ApproverL1},{ApproverL2},{Accountant},{ReportViewer}";

    // Danh mục / TSBĐ: tạo & sửa (originators + appraiser)
    public const string CollateralManagers = $"{Admin},{CreditOfficer},{Appraiser}";
}

/// <summary>
/// Hạn mức phê duyệt theo cấp (số tiền đề nghị). Có thể ghi đè qua cấu hình
/// (section "CreditPolicy"). Đơn vị: VND.
/// </summary>
public static class CreditPolicy
{
    public static decimal ApproverL1Limit = 500_000_000m;   // APPROVER_L1 duyệt tới 500 triệu
    public static decimal ApproverL2Limit = 5_000_000_000m;  // APPROVER_L2 duyệt tới 5 tỷ
}
