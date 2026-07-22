namespace ERPSystem.Application.DTOs.Inventory;

public sealed class ContainerInventoryMetricsDto
{
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal ReservedMeters { get; init; }
    public decimal SoldMeters { get; init; }
    public decimal AvailableMeters { get; init; }
    public int ReservedRolls { get; init; }
    public int SoldRolls { get; init; }
    public int AvailableRolls { get; init; }
    public decimal CostPerMeter { get; init; }
    public decimal InventoryValuation { get; init; }
    public bool IsStockPosted { get; init; }
}

public sealed class FabricRollInventoryDto
{
    public Guid Id { get; init; }
    public Guid ContainerId { get; init; }
    public int RollNumber { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string FabricCode { get; init; } = "";
    public string FabricName { get; init; } = "";
    public string ColorName { get; init; } = "";
    public decimal? SalePricePerMeter { get; init; }
    public decimal LengthMeters { get; init; }
    public decimal RemainingLengthMeters { get; init; }
    public decimal CostPerMeter { get; init; }
    public Guid WarehouseId { get; init; }
    public string Status { get; init; } = "";
}

public sealed class ContainerInventoryReportDto
{
    public Guid ContainerId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public ContainerInventoryMetricsDto Metrics { get; init; } = null!;
    public decimal LandingCostPerMeter { get; init; }
    public decimal AverageCostPerMeter { get; init; }
    public decimal SoldRevenuePotential { get; init; }
}

/// <summary>Container that currently has available rolls for sales picking.</summary>
public sealed class SellableContainerDto
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public string? Notes { get; init; }
    public Domain.Enums.DplQuantityUnit? DplQuantityUnit { get; init; }
}
