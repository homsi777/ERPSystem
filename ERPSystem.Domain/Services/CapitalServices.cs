using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.Services;

public static class DistributionLifecycle
{
    private static readonly IReadOnlyDictionary<DistributionStatus, DistributionStatus[]> Allowed = new Dictionary<DistributionStatus, DistributionStatus[]>
    {
        [DistributionStatus.Draft] = [DistributionStatus.Calculated],
        [DistributionStatus.Calculated] = [DistributionStatus.PendingApproval, DistributionStatus.Draft],
        [DistributionStatus.PendingApproval] = [DistributionStatus.Approved, DistributionStatus.Calculated],
        [DistributionStatus.Approved] = [DistributionStatus.Posted],
        [DistributionStatus.Posted] = [DistributionStatus.Closed],
        [DistributionStatus.Closed] = []
    };

    public static void ValidateTransition(DistributionStatus from, DistributionStatus to)
    {
        if (from == to) return;
        if (!GetAllowedTransitions(from).Contains(to))
            throw new DomainException($"Cannot transition distribution from '{from}' to '{to}'.");
    }

    public static IReadOnlyList<DistributionStatus> GetAllowedTransitions(DistributionStatus current) =>
        Allowed.TryGetValue(current, out var next) ? next : [];

    public static IReadOnlyList<(string Label, bool Completed, bool Current)> BuildStepper(DistributionStatus status)
    {
        var labels = new[] { "مسودة", "محسوب", "بانتظار الاعتماد", "معتمد", "مرحّل", "مغلق" };
        var stage = status switch
        {
            DistributionStatus.Draft => 0,
            DistributionStatus.Calculated => 1,
            DistributionStatus.PendingApproval => 2,
            DistributionStatus.Approved => 3,
            DistributionStatus.Posted => 4,
            DistributionStatus.Closed => 5,
            _ => 0
        };
        return labels.Select((label, i) => (label, i < stage, i == stage)).ToList();
    }
}

public sealed class ProfitDistributionInput
{
    public decimal GrossRevenue { get; init; }
    public decimal TotalCosts { get; init; }
    public IReadOnlyList<ProfitDistributionPartnerInput> Partners { get; init; } = [];
}

public sealed class ProfitDistributionPartnerInput
{
    public Guid PartnerId { get; init; }
    public decimal OwnershipPercentage { get; init; }
}

public sealed class ProfitDistributionResult
{
    public decimal GrossRevenue { get; init; }
    public decimal TotalCosts { get; init; }
    public decimal NetProfit { get; init; }
    public decimal NetLoss { get; init; }
    public IReadOnlyList<ProfitDistributionLineResult> Lines { get; init; } = [];
}

public sealed class ProfitDistributionLineResult
{
    public Guid PartnerId { get; init; }
    public decimal OwnershipPercentage { get; init; }
    public decimal PartnerShare { get; init; }
    public decimal CompanyShare { get; init; }
}

public static class ProfitDistributionCalculator
{
    public static ProfitDistributionResult Calculate(ProfitDistributionInput input)
    {
        var net = input.GrossRevenue - input.TotalCosts;
        var isProfit = net >= 0;
        var distributable = Math.Abs(net);

        var totalOwnership = input.Partners.Sum(p => p.OwnershipPercentage);
        if (totalOwnership <= 0)
            throw new ValidationException("Total ownership percentage must be greater than zero.");

        var lines = input.Partners.Select(p =>
        {
            var ratio = p.OwnershipPercentage / totalOwnership;
            var partnerShare = Math.Round(distributable * ratio, 4);
            var companyShare = Math.Round(distributable - partnerShare, 4);
            return new ProfitDistributionLineResult
            {
                PartnerId = p.PartnerId,
                OwnershipPercentage = p.OwnershipPercentage,
                PartnerShare = isProfit ? partnerShare : -partnerShare,
                CompanyShare = isProfit ? companyShare : -companyShare
            };
        }).ToList();

        return new ProfitDistributionResult
        {
            GrossRevenue = input.GrossRevenue,
            TotalCosts = input.TotalCosts,
            NetProfit = isProfit ? net : 0,
            NetLoss = isProfit ? 0 : Math.Abs(net),
            Lines = lines
        };
    }
}

public static class CapitalLedgerCalculator
{
    public static decimal CurrentBalance(IEnumerable<decimal> signedBaseAmounts) =>
        signedBaseAmounts.Sum();

    public static decimal TotalInvestments(IEnumerable<decimal> signedBaseAmounts) =>
        signedBaseAmounts.Where(a => a > 0).Sum();

    public static decimal TotalWithdrawals(IEnumerable<decimal> signedBaseAmounts) =>
        signedBaseAmounts.Where(a => a < 0).Sum(a => Math.Abs(a));
}
