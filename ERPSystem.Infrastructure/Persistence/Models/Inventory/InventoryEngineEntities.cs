namespace ERPSystem.Infrastructure.Persistence.Models.Inventory;

public class FabricBatchEntity : PersistenceEntity
{
    public string BatchNumber { get; set; } = "";
    public Guid? SupplierId { get; set; }
    public Guid? ContainerId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public DateTime ArrivalDate { get; set; }
    public decimal LandingCostPerMeter { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalMeters { get; set; }
    public int RollCount { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public int QualityStatus { get; set; }
    public int Status { get; set; }
}

public class InventoryReservationEntity : PersistenceEntity
{
    public Guid WarehouseId { get; set; }
    public Guid? FabricRollId { get; set; }
    public Guid? FabricBatchId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public decimal ReservedMeters { get; set; }
    public int RollCount { get; set; }
    public int Status { get; set; }
    public int Strategy { get; set; }
    public int ReferenceType { get; set; }
    public Guid ReferenceId { get; set; }
    public Guid? ReferenceLineId { get; set; }
}

public class StockTransferDocumentEntity : CancellablePersistenceEntity
{
    public string Number { get; set; } = "";
    public Guid FromWarehouseId { get; set; }
    public Guid ToWarehouseId { get; set; }
    public Guid? FromLocationId { get; set; }
    public Guid? ToLocationId { get; set; }
    public int Status { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class StockTransferLineEntity : PersistenceEntity
{
    public Guid TransferId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public Guid? FabricRollId { get; set; }
    public Guid? FabricBatchId { get; set; }
    public decimal QuantityMeters { get; set; }
    public int RollCount { get; set; }
}

public class StocktakeSessionEntity : CancellablePersistenceEntity
{
    public string SessionNumber { get; set; } = "";
    public DateTime Date { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? LocationId { get; set; }
    public string Responsible { get; set; } = "";
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class StocktakeLineEntity : PersistenceEntity
{
    public Guid SessionId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public Guid? FabricRollId { get; set; }
    public decimal SystemMeters { get; set; }
    public decimal CountedMeters { get; set; }
    public decimal DifferenceMeters { get; set; }
}

public class OpeningStockDocumentEntity : CancellablePersistenceEntity
{
    public string DocumentNumber { get; set; } = "";
    public Guid WarehouseId { get; set; }
    public DateTime OpeningDate { get; set; }
    public string? Reference { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class OpeningStockLineEntity : PersistenceEntity
{
    public Guid DocumentId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public Guid? FabricRollId { get; set; }
    public Guid? FabricBatchId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public decimal QuantityMeters { get; set; }
    public int RollCount { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
}

public class InventoryRuleEntity : PersistenceEntity
{
    public Guid BranchId { get; set; }
    public Guid? FabricItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public decimal? MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }
    public decimal? SafetyStock { get; set; }
    public decimal? ReorderPoint { get; set; }
    public Guid? PreferredWarehouseId { get; set; }
    public Guid? PreferredLocationId { get; set; }
    public int PreferredBatchStrategy { get; set; }
    public int? LeadTimeDays { get; set; }
}

public class InventoryAlertEntity : PersistenceEntity
{
    public Guid BranchId { get; set; }
    public int AlertType { get; set; }
    public int Severity { get; set; }
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public Guid? WarehouseId { get; set; }
    public Guid? FabricItemId { get; set; }
    public Guid? FabricRollId { get; set; }
    public Guid? FabricBatchId { get; set; }
    public bool IsAcknowledged { get; set; }
}

public class InventoryAuditEntryEntity : PersistenceEntity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = "";
    public string Action { get; set; } = "";
    public string Username { get; set; } = "";
    public string? FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? SourceModule { get; set; }
    public Guid? ReferenceDocumentId { get; set; }
    public DateTime RecordedAt { get; set; }
}

public class InventoryTimelineEventEntity : PersistenceEntity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = "";
    public string EventType { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string Username { get; set; } = "";
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class InventoryValuationSnapshotEntity : PersistenceEntity
{
    public Guid WarehouseId { get; set; }
    public Guid? FabricItemId { get; set; }
    public Guid? FabricColorId { get; set; }
    public Guid? ContainerId { get; set; }
    public int Method { get; set; }
    public decimal QuantityMeters { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime SnapshotDate { get; set; }
    public Guid? MovementId { get; set; }
}
