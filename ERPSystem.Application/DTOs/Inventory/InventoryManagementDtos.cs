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
    public Guid ContainerId { get; init; }
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
    public string? LotCode { get; init; }
}

public sealed class FabricRollSalesReservationDto
{
    public Guid FabricRollId { get; init; }
    public Guid SalesInvoiceId { get; init; }
    public string SalesInvoiceNumber { get; init; } = "";
    public int SalesInvoiceStatus { get; init; }
}

/// <summary>
/// Candidate physical roll for warehouse detailing, filtered by the exact same warehouse +
/// container + fabric + color + status match that <c>InventoryEngine.ResolveDetailingEntriesAsync</c>
/// uses at completion time, so a roll shown here is guaranteed to be accepted by the resolver.
/// Reservation-by-another-invoice status is included in the same payload so the UI needs only
/// one round trip to render both the roll and its warning state.
/// </summary>
public sealed class DetailingCandidateRollDto
{
    public Guid FabricRollId { get; init; }
    public int RollNumber { get; init; }
    public decimal RemainingLengthMeters { get; init; }
    public string Status { get; init; } = "";
    public Guid? ReservedInSalesInvoiceId { get; init; }
    public string? ReservedInSalesInvoiceNumber { get; init; }
    public int? ReservedInSalesInvoiceStatus { get; init; }
}

public sealed class PaginatedFabricRollDto
{
    public IReadOnlyList<FabricRollListDto> Items { get; init; } = [];
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
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
    public string? CostCenterName { get; init; }
    public WarehouseExecutiveDashboardDto Executive { get; init; } = null!;
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

public sealed class WarehouseExecutiveDashboardDto
{
    public decimal TotalInventoryValue { get; init; }
    public decimal ValueTrendPercent30d { get; init; }
    public IReadOnlyList<WarehouseValueSliceDto> ValueByFabric { get; init; } = [];
    public IReadOnlyList<WarehouseValueSliceDto> ValueByCategory { get; init; } = [];
    public IReadOnlyList<decimal> ValueSparkline30d { get; init; } = [];
    public WarehouseQuantityMetricsDto Quantities { get; init; } = null!;
    public IReadOnlyList<WarehouseMovementCardDto> RecentMovements { get; init; } = [];
    public WarehouseMovementCardDto? LastTransaction { get; init; }
    public IReadOnlyList<WarehouseTopFabricDto> TopMovingFabrics { get; init; } = [];
    public IReadOnlyList<WarehouseAlertCardDto> Alerts { get; init; } = [];
    public IReadOnlyList<WarehouseDocumentCardDto> RecentDocuments { get; init; } = [];
    public WarehouseUserActivityDto? LastUserActivity { get; init; }
    public IReadOnlyList<WarehouseDailyActivityDto> Activity30Days { get; init; } = [];
}

public sealed class WarehouseValueSliceDto
{
    public string Label { get; init; } = "";
    public decimal Value { get; init; }
    public decimal Percent { get; init; }
}

public sealed class WarehouseQuantityMetricsDto
{
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal AvailableMeters { get; init; }
    public decimal ReservedMeters { get; init; }
    public decimal DamagedMeters { get; init; }
    public decimal BlockedMeters { get; init; }
}

public sealed class WarehouseMovementCardDto
{
    public Guid Id { get; init; }
    public string MovementNumber { get; init; } = "";
    public string Type { get; init; } = "";
    public string TypeIcon { get; init; } = "";
    public string FromLabel { get; init; } = "";
    public string ToLabel { get; init; } = "";
    public decimal QuantityMeters { get; init; }
    public decimal TotalValue { get; init; }
    public DateTime Timestamp { get; init; }
    public string Username { get; init; } = "";
    public string? ReferenceType { get; init; }
    public Guid? ReferenceId { get; init; }
    public string? ReferenceNumber { get; init; }
}

public sealed class WarehouseTopFabricDto
{
    public string FabricName { get; init; } = "";
    public decimal MetersMoved { get; init; }
    public int MovementCount { get; init; }
}

public sealed class WarehouseAlertCardDto
{
    public string AlertType { get; init; } = "";
    public string Severity { get; init; } = "";
    public string Title { get; init; } = "";
    public string Message { get; init; } = "";
    public string? NavigationTarget { get; init; }
    public Guid? DocumentId { get; init; }
}

public sealed class WarehouseDocumentCardDto
{
    public string DocumentType { get; init; } = "";
    public Guid Id { get; init; }
    public string Number { get; init; } = "";
    public string Status { get; init; } = "";
    public DateTime Date { get; init; }
    public string NavigationTarget { get; init; } = "";
}

public sealed class WarehouseUserActivityDto
{
    public string Username { get; init; } = "";
    public string ActionType { get; init; } = "";
    public DateTime Timestamp { get; init; }
}

public sealed class WarehouseDailyActivityDto
{
    public DateTime Date { get; init; }
    public decimal IncomingMeters { get; init; }
    public decimal OutgoingMeters { get; init; }
    public decimal NetMeters { get; init; }
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
