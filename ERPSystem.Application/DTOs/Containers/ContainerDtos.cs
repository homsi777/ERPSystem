using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Containers;

public sealed class ContainerListDto
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public ChinaContainerStatus Status { get; init; }
    public DateTime ShipmentDate { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public string SupplierName { get; init; } = "";
}

public sealed class ContainerDetailsDto
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public ChinaContainerStatus Status { get; init; }
    public Guid SupplierId { get; init; }
    public DateTime ShipmentDate { get; init; }
    public DateTime? ArrivalDate { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal? TotalWeightKg { get; init; }
    public LandingCostDto? LandingCost { get; init; }
    public IReadOnlyList<ContainerItemDto> Items { get; init; } = [];
}

public sealed class ContainerItemDto
{
    public int LineNumber { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }
    public bool IsValid { get; init; }
}

public sealed class LandingCostDto
{
    public decimal TotalLengthMeters { get; init; }
    public decimal ContainerWeightKg { get; init; }
    public decimal CustomsAmount { get; init; }
    public decimal Shipping { get; init; }
    public decimal Clearance { get; init; }
    public decimal OtherExpenses { get; init; }
    public decimal TotalImportExpenses { get; init; }
    public decimal CustomsCostPerMeter { get; init; }
    public decimal ExpenseCostPerMeter { get; init; }
    public decimal AvgGramPerMeter { get; init; }
    public LandingCostStatus Status { get; init; }
}

public sealed class ContainerOperationsCenterDto
{
    public ContainerDetailsDto Container { get; init; } = null!;
    public bool CanApprove { get; init; }
    public bool CanMoveToWarehouse { get; init; }
    public bool CanCalculateLandingCost { get; init; }
}
