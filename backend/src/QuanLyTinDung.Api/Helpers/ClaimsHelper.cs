using System.Security.Claims;
using QuanLyTinDung.Api.Authorization;

namespace QuanLyTinDung.Api.Helpers;

/// <summary>
/// Đọc vai trò & chi nhánh từ JWT của user hiện tại (dùng cho cô lập dữ liệu
/// theo chi nhánh và kiểm tra hạn mức phê duyệt).
/// </summary>
public static class ClaimsHelper
{
    /// <summary>Chi nhánh của user (từ claim "branch"); null nếu token không có.</summary>
    public static long? CurrentBranchId(this ClaimsPrincipal user)
    {
        var v = user.FindFirst("branch")?.Value;
        return long.TryParse(v, out var id) ? id : null;
    }

    /// <summary>Mã vai trò hiện tại (ma_vai_tro_hien_thi).</summary>
    public static string? CurrentRole(this ClaimsPrincipal user)
        => user.FindFirst(ClaimTypes.Role)?.Value;

    public static bool IsRole(this ClaimsPrincipal user, string role)
        => string.Equals(user.CurrentRole(), role, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Có nên cô lập dữ liệu theo chi nhánh không?
    /// Chỉ các vai "đầu nguồn" (CREDIT_OFFICER) bị giới hạn theo chi nhánh;
    /// các vai trung tâm (APPRAISER, APPROVER_*, ACCOUNTANT, REPORT_VIEWER, ADMIN)
    /// cần thấy dữ liệu xuyên chi nhánh để xử lý/phê duyệt/báo cáo.
    /// </summary>
    public static bool ShouldScopeByBranch(this ClaimsPrincipal user)
        => user.IsRole(Roles.CreditOfficer) && user.CurrentBranchId() != null;
}
