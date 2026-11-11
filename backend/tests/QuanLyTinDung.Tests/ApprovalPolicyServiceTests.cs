using Microsoft.Extensions.Configuration;
using QuanLyTinDung.Application.Services;

namespace QuanLyTinDung.Tests;

/// <summary>
/// Test hạn mức & cấp phê duyệt theo vai trò.
///
/// Trước đây logic nằm inline trong controller dưới dạng switch expression,
/// không test được. Đã tách sang IApprovalPolicyService để kiểm chứng được:
/// - L1 vượt hạn mức → phải chuyển cấp L2
/// - ADMIN không giới hạn
/// - cap_phe_duyet ghi đúng cấp theo vai
/// </summary>
public class ApprovalPolicyServiceTests
{
    private static ApprovalPolicyService CreateService(decimal? l1 = null, decimal? l2 = null)
    {
        var values = new Dictionary<string, string?>();
        if (l1.HasValue) values["CreditPolicy:ApproverL1Limit"] = l1.Value.ToString();
        if (l2.HasValue) values["CreditPolicy:ApproverL2Limit"] = l2.Value.ToString();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        return new ApprovalPolicyService(config);
    }

    [Fact]
    public void GetLimit_MacDinh_L1La500tr_L2La5Ty()
    {
        var service = CreateService();

        Assert.Equal(500_000_000m, service.GetLimit("APPROVER_L1"));
        Assert.Equal(5_000_000_000m, service.GetLimit("APPROVER_L2"));
    }

    [Fact]
    public void GetLimit_DocTuConfiguration()
    {
        var service = CreateService(l1: 100_000_000m, l2: 1_000_000_000m);

        Assert.Equal(100_000_000m, service.GetLimit("APPROVER_L1"));
        Assert.Equal(1_000_000_000m, service.GetLimit("APPROVER_L2"));
    }

    [Fact]
    public void GetLimit_AdminKhongGioiHan()
    {
        var service = CreateService();

        Assert.Null(service.GetLimit("ADMIN"));
        Assert.Null(service.GetLimit("CREDIT_OFFICER"));
        Assert.Null(service.GetLimit("ACCOUNTANT"));
        Assert.Null(service.GetLimit(null));
    }

    [Fact]
    public void ExceedsLimit_L1VuotHanMuc()
    {
        var service = CreateService();

        Assert.True(service.ExceedsLimit("APPROVER_L1", 600_000_000m));
        Assert.False(service.ExceedsLimit("APPROVER_L1", 400_000_000m));
    }

    [Fact]
    public void ExceedsLimit_DungBangHanMucThiKhongVuot()
    {
        // Số tiền = đúng hạn mức → vẫn được duyệt (không phải "vượt")
        var service = CreateService();

        Assert.False(service.ExceedsLimit("APPROVER_L1", 500_000_000m));
        Assert.False(service.ExceedsLimit("APPROVER_L2", 5_000_000_000m));
    }

    [Fact]
    public void ExceedsLimit_AdminKhongBaoGio()
    {
        var service = CreateService();

        Assert.False(service.ExceedsLimit("ADMIN", 100_000_000_000m));
    }

    [Fact]
    public void ExceedsLimit_L2VuotL1()
    {
        var service = CreateService();

        // 700tr: L1 vượt, L2 chưa
        Assert.True(service.ExceedsLimit("APPROVER_L1", 700_000_000m));
        Assert.False(service.ExceedsLimit("APPROVER_L2", 700_000_000m));
    }

    [Fact]
    public void GetApprovalLevel_TheoVai()
    {
        var service = CreateService();

        Assert.Equal(1, service.GetApprovalLevel("APPROVER_L1"));
        Assert.Equal(2, service.GetApprovalLevel("APPROVER_L2"));
        Assert.Equal(1, service.GetApprovalLevel("ADMIN"));
    }

    [Fact]
    public void CanApprove_ChiVaiPheDuyet()
    {
        var service = CreateService();

        Assert.True(service.CanApprove("ADMIN"));
        Assert.True(service.CanApprove("APPROVER_L1"));
        Assert.True(service.CanApprove("APPROVER_L2"));

        Assert.False(service.CanApprove("CREDIT_OFFICER"));
        Assert.False(service.CanApprove("APPRAISER"));
        Assert.False(service.CanApprove("ACCOUNTANT"));
        Assert.False(service.CanApprove("REPORT_VIEWER"));
    }
}
