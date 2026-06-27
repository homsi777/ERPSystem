namespace ERPSystem.Application.DTOs.Warehouses;

public sealed class WarehouseListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string City { get; init; } = "";
    public bool IsActive { get; init; }
}

public sealed class WarehouseStockDto
{
    public Guid WarehouseId { get; init; }
    public Guid FabricItemId { get; init; }
    public string FabricItemName { get; init; } = "";
    public Guid FabricColorId { get; init; }
    public string FabricColorName { get; init; } = "";
    public Guid ContainerId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public int RollCount { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal ReservedMeters { get; init; }
    public decimal AvailableMeters { get; init; }
}

public sealed class FabricItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string CategoryName { get; init; } = "";
    public bool IsActive { get; init; }
}

public sealed class WarehouseOperationsCenterDto
{
    public WarehouseListDto Warehouse { get; init; } = null!;
    public IReadOnlyList<WarehouseStockDto> Stock { get; init; } = [];
    public int PendingDetailingCount { get; init; }
}
