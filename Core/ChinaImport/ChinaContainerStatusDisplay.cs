using ERPSystem.Domain.Enums;

namespace ERPSystem.Core.ChinaImport;

public static class ChinaContainerStatusDisplay
{
    public static string ToArabic(this ChinaContainerStatus status) => status switch
    {
        ChinaContainerStatus.Draft => "مسودة",
        ChinaContainerStatus.InTransit => "بالطريق",
        ChinaContainerStatus.Arrived => "واصلة",
        ChinaContainerStatus.UnderReview => "قيد المراجعة",
        ChinaContainerStatus.LandingCostReviewed => "مراجعة التكلفة",
        ChinaContainerStatus.Approved => "معتمدة",
        ChinaContainerStatus.InWarehouse => "في المخزن",
        ChinaContainerStatus.Closed => "مغلقة",
        ChinaContainerStatus.Archived => "مؤرشفة",
        ChinaContainerStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    public static ChinaContainerStatus? FromArabicFilter(string? label) => label switch
    {
        "بالطريق" => ChinaContainerStatus.InTransit,
        "واصلة" => ChinaContainerStatus.Arrived,
        "قيد المراجعة" => ChinaContainerStatus.UnderReview,
        "معتمدة" => ChinaContainerStatus.Approved,
        "مغلقة" => ChinaContainerStatus.Closed,
        "مؤرشفة" => ChinaContainerStatus.Archived,
        _ => null
    };
}
