namespace ERPSystem.Application.DTOs.Inventory;

public sealed class WarehouseListExtendedDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public string City { get; init; } = "";
    public string? Manager { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public int RollCount { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal InventoryValue { get; init; }
}

public sealed class WarehouseDetailDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string? NameEn { get; init; }
    public string? Description { get; init; }
    public string City { get; init; } = "";
    public string? Address { get; init; }
    public string? Manager { get; init; }
    public Guid? CostCenterId { get; init; }
    public string? Notes { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public bool IsArchived { get; init; }
    public int? CapacityRolls { get; init; }
    public int RollCount { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal InventoryValue { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public string? LastMovement { get; init; }
    public string? LastStocktake { get; init; }
    public IReadOnlyList<InventoryTimelineDto> RecentTimeline { get; init; } = [];
    public IReadOnlyList<InventoryAuditDto> RecentAudit { get; init; } = [];
}

public sealed class StorageLocationDto
{
    public Guid Id { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid? ParentId { get; init; }
    public string LocationType { get; init; } = "";
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal? CapacityMeters { get; init; }
    public string Status { get; init; } = "";
    public int Priority { get; init; }
}

public sealed class FabricStockBalanceDto
{
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = "";
    public Guid FabricItemId { get; init; }
    public string FabricCode { get; init; } = "";
    public string FabricName { get; init; } = "";
    public Guid FabricColorId { get; init; }
    public string ColorName { get; init; } = "";
    public string ContainerNumber { get; init; } = "";
    public int RollCount { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal ReservedMeters { get; init; }
    public decimal AvailableMeters { get; init; }
    public decimal InventoryValue { get; init; }
}

public sealed class FabricRollListDto
{
    public Guid Id { get; init; }
    public int RollNumber { get; init; }
    public string? Barcode { get; init; }
    public string FabricName { get; init; } = "";
    public string ColorName { get; init; } = "";
    public decimal LengthMeters { get; init; }
    public decimal RemainingLengthMeters { get; init; }
    public decimal CostPerMeter { get; init; }
    public decimal CurrentValue { get; init; }
    public string Status { get; init; } = "";
    public string? BatchNumber { get; init; }
    public string? LocationCode { get; init; }
}

public sealed class StockMovementListDto
{
    public Guid Id { get; init; }
    public string MovementNumber { get; init; } = "";
    public DateTime MovementDate { get; init; }
    public string Type { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public string? Reference { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal TotalValue { get; init; }
    public string Status { get; init; } = "";
}

public sealed class InventoryAlertDto
{
    public Guid Id { get; init; }
    public string AlertType { get; init; } = "";
    public string Severity { get; init; } = "";
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string? WarehouseName { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsAcknowledged { get; init; }
}

public sealed class InventoryDashboardDto
{
    public decimal TotalInventoryValue { get; init; }
    public int WarehouseCount { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal ReservedMeters { get; init; }
    public int LowStockCount { get; init; }
    public int PendingTransfers { get; init; }
    public int PendingStocktakes { get; init; }
    public int ActiveAlerts { get; init; }
    public IReadOnlyList<FabricStockBalanceDto> TopFabrics { get; init; } = [];
    public IReadOnlyList<InventoryAlertDto> RecentAlerts { get; init; } = [];
}

public sealed class InventoryOperationsCenterDto
{
    public WarehouseListExtendedDto Warehouse { get; init; } = null!;
    public IReadOnlyList<FabricStockBalanceDto> Stock { get; init; } = [];
    public IReadOnlyList<FabricRollListDto> Rolls { get; init; } = [];
    public IReadOnlyList<StorageLocationDto> Locations { get; init; } = [];
    public IReadOnlyList<StockMovementListDto> RecentMovements { get; init; } = [];
    public IReadOnlyList<InventoryAlertDto> Alerts { get; init; } = [];
    public IReadOnlyList<InventoryAuditDto> RecentAudit { get; init; } = [];
    public IReadOnlyList<InventoryTimelineDto> Timeline { get; init; } = [];
    public int PendingTransfers { get; init; }
    public int PendingStocktakes { get; init; }
    public decimal InventoryValue { get; init; }
}

public sealed class InventoryAuditDto
{
    public DateTime RecordedAt { get; init; }
    public string Action { get; init; } = "";
    public string Username { get; init; } = "";
    public string? FieldName { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string? Reason { get; init; }
}

public sealed class InventoryTimelineDto
{
    public DateTime OccurredAt { get; init; }
    public string EventType { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string Username { get; init; } = "";
}

public sealed class StockTransferListDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = "";
    public string FromWarehouse { get; init; } = "";
    public string ToWarehouse { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime Date { get; init; }
}

public sealed class StockTransferLineDetailDto
{
    public Guid Id { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public Guid? FabricRollId { get; init; }
    public string FabricName { get; init; } = "";
    public string ColorName { get; init; } = "";
    public int RollNumber { get; init; }
    public string? BatchNumber { get; init; }
    public string? LocationCode { get; init; }
    public decimal QuantityMeters { get; init; }
    public int RollCount { get; init; }
    public decimal UnitValue { get; init; }
}

public sealed class StockTransferDetailDto
{
    public Guid Id { get; init; }
    public string Number { get; init; } = "";
    public Guid FromWarehouseId { get; init; }
    public Guid ToWarehouseId { get; init; }
    public string FromWarehouse { get; init; } = "";
    public string ToWarehouse { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Notes { get; init; }
    public DateTime Date { get; init; }
    public IReadOnlyList<StockTransferLineDetailDto> Lines { get; init; } = [];
    public decimal TotalMeters { get; init; }
    public decimal TotalValue { get; init; }
    public int TotalRolls { get; init; }
}

public sealed class WarehouseTransferRollDto
{
    public Guid Id { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public Guid? FabricBatchId { get; init; }
    public int RollNumber { get; init; }
    public string? Barcode { get; init; }
    public string FabricName { get; init; } = "";
    public string ColorName { get; init; } = "";
    public string? BatchNumber { get; init; }
    public string? LocationCode { get; init; }
    public decimal RemainingLengthMeters { get; init; }
    public decimal CostPerMeter { get; init; }
    public decimal CurrentValue { get; init; }
    public string Status { get; init; } = "";
}

public sealed class StocktakeListDto
{
    public Guid Id { get; init; }
    public string SessionNumber { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public string Responsible { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime Date { get; init; }
}

public sealed class StocktakeLineDto
{
    public Guid Id { get; init; }
    public Guid? FabricRollId { get; init; }
    public int RollNumber { get; init; }
    public string FabricName { get; init; } = "";
    public string ColorName { get; init; } = "";
    public string? BatchNumber { get; init; }
    public string? LocationCode { get; init; }
    public decimal SystemMeters { get; init; }
    public decimal CountedMeters { get; init; }
    public decimal DifferenceMeters { get; init; }
}

public sealed class StocktakeDetailDto
{
    public Guid Id { get; init; }
    public string SessionNumber { get; init; } = "";
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = "";
    public string Responsible { get; init; } = "";
    public string Status { get; init; } = "";
    public string? Notes { get; init; }
    public DateTime Date { get; init; }
    public IReadOnlyList<StocktakeLineDto> Lines { get; init; } = [];
    public decimal TotalSystemMeters { get; init; }
    public decimal TotalCountedMeters { get; init; }
    public decimal TotalVarianceMeters { get; init; }
    public int LinesWithVariance { get; init; }
}

public sealed class OpeningStockListDto
{
    public Guid Id { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string WarehouseName { get; init; } = "";
    public DateTime OpeningDate { get; init; }
    public string Status { get; init; } = "";
    public decimal TotalValue { get; init; }
}
