namespace ERPSystem.Application.Commands.Containers;

public sealed class CreateChinaContainerCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid SupplierId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public DateTime ShipmentDate { get; init; }
    public DateTime? ExpectedArrival { get; init; }
    public string? Notes { get; init; }
    public decimal ExchangeRateToLocalCurrency { get; init; } = 1m;
    public decimal ChinaInvoiceAmountUsd { get; init; }
    public string? ImportFileName { get; init; }
    public IReadOnlyList<ImportContainerLineCommand> Lines { get; init; } = [];
}

public sealed class ImportContainerLineCommand
{
    public int LineNumber { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; } = 1;
    public decimal LengthMeters { get; init; }
    public decimal? WeightKg { get; init; }
    public string? LotCode { get; init; }
    public Guid? BuyerCustomerId { get; init; }
}

public sealed class CalculateLandingCostCommand
{
    public Guid ContainerId { get; init; }
    public decimal TotalLengthMeters { get; init; }
    public decimal ContainerWeightKg { get; init; }
    public decimal CustomsClearanceAmount { get; init; }
    public decimal Shipping { get; init; }
    public decimal Insurance { get; init; }
    public decimal OtherExpense1 { get; init; }
    public decimal OtherExpense2 { get; init; }
    public decimal OtherExpense3 { get; init; }
    public decimal OtherExpense4 { get; init; }
    public bool UsesWeightedAllocation { get; init; }
    public IReadOnlyList<ContainerFabricTypeLineCommand> TypeLines { get; init; } = [];

    // Legacy flat-rate fields (DPL-only fallback)
    public decimal CustomsAmount { get; init; }
    public decimal Clearance { get; init; }
    public decimal OtherExpenses { get; init; }
}

public sealed class ContainerFabricTypeLineCommand
{
    public int LineNumber { get; init; }
    public string TypeDisplayName { get; init; } = "";
    public string MatchKey { get; init; } = "";
    public Guid? FabricItemId { get; init; }
    public Guid? FabricColorId { get; init; }
    public decimal LengthMeters { get; init; }
    public int RollCount { get; init; }
    public decimal NetWeightKg { get; init; }
    public decimal Cbm { get; init; }
    public decimal ChinaUnitPriceUsd { get; init; }
    public decimal InvoiceLineAmountUsd { get; init; }
    public bool HasInvoiceMatch { get; init; }
    public bool HasPlMatch { get; init; }
    public bool HasDplMatch { get; init; }
    public string? MatchWarnings { get; init; }
}

public sealed class SetContainerTypeSalePricesCommand
{
    public Guid ContainerId { get; init; }
    public IReadOnlyList<ContainerTypeSalePriceCommand> Lines { get; init; } = [];
}

public sealed class ContainerTypeSalePriceCommand
{
    public Guid TypeLineId { get; init; }
    public decimal MarginPerMeterUsd { get; init; }
}

public sealed class ApproveContainerCommand
{
    public Guid ContainerId { get; init; }
}

public sealed class MoveContainerToWarehouseCommand
{
    public Guid ContainerId { get; init; }
    public Guid WarehouseId { get; init; }
}

public sealed class ArchiveContainerCommand
{
    public Guid ContainerId { get; init; }
}

public sealed class SaveFabricTypeAliasCommand
{
    public Guid CompanyId { get; init; }
    public Guid SupplierId { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string DplMatchKey { get; init; } = "";
    public string InvoiceDescriptionMatchKey { get; init; } = "";
    public string InvoiceDescription { get; init; } = "";
}
