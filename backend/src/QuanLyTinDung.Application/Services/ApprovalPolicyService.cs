using Microsoft.Extensions.Configuration;

namespace QuanLyTinDung.Application.Services;

/// <summary>
/// Hạn mức & cấp phê duyệt theo vai trò.
///
/// Tách khỏi controller để test được không cần HTTP context / database.
/// </summary>
public interface IApprovalPolicyService
{
    /// <summary>
    /// Hạn mức (đồng) của vai. null = không giới hạn (ADMIN).
    /// </summary>
    decimal? GetLimit(string? role);

    /// <summary>
    /// Cấp phê duyệt ghi vào DB (cap_phe_duyet): 1 = L1, 2 = L2.
    /// ADMIN phê duyệt ở cấp 1 mặc định.
    /// </summary>
    int GetApprovalLevel(string? role);

    /// <summary>
    /// Vai có được phê duyệt không.
    /// </summary>
    bool CanApprove(string? role);

    /// <summary>
    /// Số tiền có vượt hạn mức của vai không. false nếu vai không giới hạn.
    /// </summary>
    bool ExceedsLimit(string? role, decimal soTien);
}

public class ApprovalPolicyService : IApprovalPolicyService
{
    // Mã vai trò khớp RoleConstants ở API layer.
    private const string Admin = "ADMIN";
    private const string ApproverL1 = "APPROVER_L1";
    private const string ApproverL2 = "APPROVER_L2";

    // Hạn mức mặc định. CreditPolicy (API layer) đọc cùng giá trị từ
    // appsettings.json → CreditPolicy:ApproverL1Limit / ApproverL2Limit.
    private const decimal DefaultApproverL1Limit = 500_000_000m;
    private const decimal DefaultApproverL2Limit = 5_000_000_000m;

    private readonly IConfiguration _config;

    public ApprovalPolicyService(IConfiguration config)
    {
        _config = config;
        var section = _config.GetSection("CreditPolicy");
        ApproverL1Limit = section.GetValue<decimal?>("ApproverL1Limit") ?? DefaultApproverL1Limit;
        ApproverL2Limit = section.GetValue<decimal?>("ApproverL2Limit") ?? DefaultApproverL2Limit;
    }

    public decimal ApproverL1Limit { get; }
    public decimal ApproverL2Limit { get; }

    public decimal? GetLimit(string? role) => role switch
    {
        ApproverL1 => ApproverL1Limit,
        ApproverL2 => ApproverL2Limit,
        _ => null, // ADMIN + các vai khác: không giới hạn
    };

    public int GetApprovalLevel(string? role) => role switch
    {
        ApproverL2 => 2,
        _ => 1, // L1 và ADMIN phê duyệt ở cấp 1 mặc định
    };

    public bool CanApprove(string? role) => role switch
    {
        Admin or ApproverL1 or ApproverL2 => true,
        _ => false,
    };

    public bool ExceedsLimit(string? role, decimal soTien)
    {
        var limit = GetLimit(role);
        return limit.HasValue && soTien > limit.Value;
    }
}
