using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Containers;

public sealed class ContainerListDto
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public ChinaContainerStatus Status { get; init; }
    public DateTime ShipmentDate { get; init; }
    public DateTime? ExpectedArrival { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal? TotalWeightKg { get; init; }
    public int CodeCount { get; init; }
    public int ColorCount { get; init; }
    public decimal ExchangeRateToLocalCurrency { get; init; }
    public string SupplierName { get; init; } = "";
    public DplQuantityUnit? DplQuantityUnit { get; init; }
    public string LengthUnitDisplay => SaleLengthUnitHelper.DisplayArabic(DplQuantityUnit);
}

public sealed class ContainerDetailsDto
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public ChinaContainerStatus Status { get; init; }
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public DateTime ShipmentDate { get; init; }
    public DateTime? ArrivalDate { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal? TotalWeightKg { get; init; }
    public decimal ExchangeRateToLocalCurrency { get; init; }
    public decimal ChinaInvoiceAmountUsd { get; init; }
    public DplQuantityUnit? DplQuantityUnit { get; init; }
    public decimal FinancialTaxReserveUsd { get; init; }
    public decimal? FinancialTaxReservePostedLocal { get; init; }
    public LandingCostDto? LandingCost { get; init; }
    public IReadOnlyList<ContainerFabricTypeLineDto> FabricTypeLines { get; init; } = [];
    public IReadOnlyList<ContainerItemDto> Items { get; init; } = [];
}

public sealed class ContainerItemDto
{
    public int LineNumber { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }
    public decimal? DplQuantityNative { get; init; }
    public DplQuantityUnit? DplQuantityUnit { get; init; }
    public bool IsValid { get; init; }
}

public sealed class LandingCostDto
{
    public decimal TotalLengthMeters { get; init; }
    public decimal ContainerWeightKg { get; init; }
    public decimal CustomsAmount { get; init; }
    public decimal Shipping { get; init; }
    public decimal Insurance { get; init; }
    public decimal Clearance { get; init; }
    public decimal OtherExpenses { get; init; }
    public decimal OtherExpense1 { get; init; }
    public decimal OtherExpense2 { get; init; }
    public decimal OtherExpense3 { get; init; }
    public decimal OtherExpense4 { get; init; }
    public bool UsesWeightedAllocation { get; init; }
    public decimal TotalImportExpenses { get; init; }
    public decimal CustomsCostPerMeter { get; init; }
    public decimal ExpenseCostPerMeter { get; init; }
    public decimal AvgGramPerMeter { get; init; }
    public LandingCostStatus Status { get; init; }
}

public sealed class ContainerOperationsCenterDto
{
    public ContainerDetailsDto Container { get; init; } = null!;
    public ContainerInventoryMetricsDto? Inventory { get; init; }
    public bool CanApprove { get; init; }
    public bool CanSetSalePrices { get; init; }
    public bool CanMoveToWarehouse { get; init; }
    public bool CanCalculateLandingCost { get; init; }
    public bool IsReadyForSale { get; init; }
    public Guid? LinkedPurchaseInvoiceId { get; init; }
    public string? LinkedPurchaseInvoiceNumber { get; init; }
}

public sealed class ContainerExcelParseResultDto
{
    public string FileName { get; init; } = "";
    public string? SupplierNameFromFile { get; init; }
    public DplQuantityUnit DetectedQuantityUnit { get; init; } = DplQuantityUnit.Meters;
    public string DetectedQuantityUnitDisplay =>
        DetectedQuantityUnit == DplQuantityUnit.Yards ? "يارد (YDS)" : "متر (M)";
    public PackingListGrandTotalDto GrandTotal { get; init; } = new();
    public IReadOnlyList<PackingListGroupDto> Groups { get; init; } = [];
    public bool HasUnresolvedGroups { get; init; }
}

public sealed class PackingListGrandTotalDto
{
    public decimal? DeclaredTotalMeters { get; init; }
    public int? DeclaredTotalRolls { get; init; }
    public decimal ParsedTotalMeters { get; init; }
    public int ParsedTotalRolls { get; init; }
    public bool MetersMatch { get; init; }
    public bool RollsMatch { get; init; }
    public string MatchIndicator => MetersMatch && RollsMatch ? "✅" : "⚠️";
    public string SummaryText { get; init; } = "";
}

public sealed class PackingListGroupDto
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public decimal DeclaredTotalMeters { get; init; }
    public int DeclaredTotalRolls { get; init; }
    public decimal ParsedTotalMeters { get; init; }
    public int ParsedTotalRolls { get; init; }
    public bool MetersMatch { get; init; }
    public bool RollsMatch { get; init; }
    public string MetersMatchIndicator => MetersMatch ? "✅" : "⚠️";
    public string RollsMatchIndicator => RollsMatch ? "✅" : "⚠️";
    public bool FabricResolved { get; init; }
    public bool ColorResolved { get; init; }
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public string? ResolutionError { get; init; }
    public IReadOnlyList<PackingListRollDto> Rolls { get; init; } = [];
    public IReadOnlyList<PackingListResolutionIssueDto> ResolutionIssues { get; init; } = [];
}

public sealed class PackingListRollDto
{
    public int SequenceNumber { get; init; }
    public int GroupIndex { get; init; }
    public int RollNumber { get; init; }
    /// <summary>Raw quantity from DPL file (source of truth for sales).</summary>
    public decimal QuantityNative { get; init; }
    public DplQuantityUnit QuantityUnit { get; init; } = DplQuantityUnit.Meters;
    /// <summary>Meter equivalent used for costing, invoice matching, and inventory.</summary>
    public decimal QuantityMeters { get; init; }
    public string LotCode { get; init; } = "";
    public bool IsValid { get; init; } = true;
    public string? InvalidReason { get; init; }

    public string QuantityDisplay =>
        QuantityUnit == DplQuantityUnit.Yards
            ? $"{QuantityNative:N2} yd ({QuantityMeters:N2} m)"
            : $"{QuantityNative:N2} m";
}

public sealed class PackingListResolutionIssueDto
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public int? RollNumber { get; init; }
    public string Reason { get; init; } = "";
}
