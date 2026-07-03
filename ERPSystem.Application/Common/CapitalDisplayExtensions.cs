using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class CapitalDisplayExtensions
{
    public static string ToArabic(this PartnerStatus status) => status switch
    {
        PartnerStatus.Active => "نشط",
        PartnerStatus.Inactive => "غير نشط",
        PartnerStatus.Archived => "مؤرشف",
        _ => status.ToString()
    };

    public static string ToArabic(this PartnerRiskLevel level) => level switch
    {
        PartnerRiskLevel.Low => "منخفض",
        PartnerRiskLevel.Medium => "متوسط",
        PartnerRiskLevel.High => "مرتفع",
        _ => level.ToString()
    };

    public static string ToArabic(this PartnershipScope scope) => scope switch
    {
        PartnershipScope.Company => "شركة",
        PartnershipScope.Project => "مشروع",
        PartnershipScope.Container => "حاوية",
        _ => scope.ToString()
    };

    public static string ToArabic(this CapitalTransactionType type) => type switch
    {
        CapitalTransactionType.InitialInvestment => "استثمار أولي",
        CapitalTransactionType.AdditionalInvestment => "استثمار إضافي",
        CapitalTransactionType.CapitalIncrease => "زيادة رأس المال",
        CapitalTransactionType.PartialWithdrawal => "سحب جزئي",
        CapitalTransactionType.FullWithdrawal => "سحب كامل",
        CapitalTransactionType.InvestmentTransfer => "تحويل استثمار",
        CapitalTransactionType.ManualAdjustment => "تعديل يدوي",
        CapitalTransactionType.CurrencyAdjustment => "تعديل عملة",
        CapitalTransactionType.ProfitDistribution => "توزيع أرباح",
        CapitalTransactionType.LossDistribution => "توزيع خسائر",
        _ => type.ToString()
    };

    public static string ToArabic(this CapitalApprovalStatus status) => status switch
    {
        CapitalApprovalStatus.Pending => "بانتظار الاعتماد",
        CapitalApprovalStatus.Approved => "معتمد",
        CapitalApprovalStatus.Rejected => "مرفوض",
        _ => status.ToString()
    };

    public static string ToArabic(this DistributionStatus status) => status switch
    {
        DistributionStatus.Draft => "مسودة",
        DistributionStatus.Calculated => "محسوب",
        DistributionStatus.PendingApproval => "بانتظار الاعتماد",
        DistributionStatus.Approved => "معتمد",
        DistributionStatus.Posted => "مرحّل",
        DistributionStatus.Closed => "مغلق",
        _ => status.ToString()
    };
}
