using ERPSystem.Domain.Enums;

namespace ERPSystem.Core.ChinaImport;

public static class ChinaImportWorkflow
{
    public static IReadOnlyList<(string Label, bool Completed, bool Current)> BuildStepper(ChinaContainerStatus status)
    {
        var stage = StageIndex(status);
        var labels = new[]
        {
            "استيراد",
            "تحليل الملف",
            "إدخال التكلفة",
            "Landing Cost",
            "اعتماد",
            "مخزن",
            "جاهز للبيع"
        };

        return labels.Select((label, i) =>
        {
            var completed = i < stage;
            var current = i == stage;
            return (label, completed, current);
        }).ToList();
    }

    public static string ResolveRouteForStatus(ChinaContainerStatus status) => status switch
    {
        ChinaContainerStatus.Draft => "NewImport",
        ChinaContainerStatus.UnderReview => "LandingCost",
        ChinaContainerStatus.LandingCostReviewed => "LandingCost",
        ChinaContainerStatus.Approved => "MoveToWarehouse",
        ChinaContainerStatus.InWarehouse => "ReadyForSale",
        _ => "Containers"
    };

    public static bool CanAccessRoute(string routeKey, ChinaContainerStatus? containerStatus, bool hasParseSession, Guid? containerId, bool isDplUnitConfirmed = false)
    {
        return routeKey switch
        {
            "Containers" or "NewImport" => true,
            "DplUnitSelection" => hasParseSession && !isDplUnitConfirmed,
            "FileAnalysis" => hasParseSession && isDplUnitConfirmed,
            "CostEntry" => hasParseSession && isDplUnitConfirmed,
            "LandingCost" => containerId.HasValue && IsAllowedForLandingCost(containerStatus),
            "SalePrice" => containerId.HasValue && IsAllowedForSalePrice(containerStatus),
            "MoveToWarehouse" => containerId.HasValue && IsAllowedForWarehouseTransfer(containerStatus),
            "ReadyForSale" => containerId.HasValue && IsAllowedForReadyForSale(containerStatus),
            "Distribution" or "Stocktake" => containerId.HasValue &&
                (containerStatus is null
                    or ChinaContainerStatus.InWarehouse
                    or ChinaContainerStatus.Closed),
            _ => true
        };
    }

    private static bool IsAllowedForLandingCost(ChinaContainerStatus? status) =>
        status is null
            or ChinaContainerStatus.UnderReview
            or ChinaContainerStatus.LandingCostReviewed
            or ChinaContainerStatus.Approved
            or ChinaContainerStatus.InWarehouse
            or ChinaContainerStatus.Closed;

    private static bool IsAllowedForSalePrice(ChinaContainerStatus? status) =>
        status is null or ChinaContainerStatus.LandingCostReviewed;

    private static bool IsAllowedForWarehouseTransfer(ChinaContainerStatus? status) =>
        status is null or ChinaContainerStatus.Approved;

    private static bool IsAllowedForReadyForSale(ChinaContainerStatus? status) =>
        status is null or ChinaContainerStatus.InWarehouse;

    private static int StageIndex(ChinaContainerStatus status) => status switch
    {
        ChinaContainerStatus.Draft => 0,
        ChinaContainerStatus.InTransit or ChinaContainerStatus.Arrived => 1,
        ChinaContainerStatus.UnderReview => 2,
        ChinaContainerStatus.LandingCostReviewed => 3,
        ChinaContainerStatus.Approved => 4,
        ChinaContainerStatus.InWarehouse => 6,
        ChinaContainerStatus.Closed or ChinaContainerStatus.Archived => 6,
        _ => 0
    };
}
