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
    public string? ImportFileName { get; init; }
    public IReadOnlyList<ImportContainerLineCommand> Lines { get; init; } = [];
}

public sealed class ImportContainerExcelCommand
{
    public Guid ContainerId { get; init; }
    public string FileName { get; init; } = "";
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
    public decimal CustomsAmount { get; init; }
    public decimal Shipping { get; init; }
    public decimal Clearance { get; init; }
    public decimal OtherExpenses { get; init; }
}

public sealed class ApproveContainerCommand
{
    public Guid ContainerId { get; init; }
}

public sealed class MoveContainerToWarehouseCommand
{
    public Guid ContainerId { get; init; }
}
