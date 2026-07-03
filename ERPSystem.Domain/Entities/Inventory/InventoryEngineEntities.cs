using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities.Inventory;

public class FabricBatch
{
    public Guid Id { get; private set; }
    public string BatchNumber { get; private set; } = "";
    public Guid? SupplierId { get; private set; }
    public Guid? ContainerId { get; private set; }
    public Guid? PurchaseInvoiceId { get; private set; }
    public DateTime ArrivalDate { get; private set; }
    public decimal LandingCostPerMeter { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public decimal TotalMeters { get; private set; }
    public int RollCount { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? StorageLocationId { get; private set; }
    public InventoryQualityStatus QualityStatus { get; private set; }
    public FabricBatchStatus Status { get; private set; }

    private FabricBatch() { }

    public static FabricBatch Create(
        string batchNumber,
        Guid warehouseId,
        decimal totalMeters,
        int rollCount,
        decimal landingCostPerMeter,
        string currencyCode = "USD",
        Guid? supplierId = null,
        Guid? containerId = null,
        Guid? purchaseInvoiceId = null,
        Guid? storageLocationId = null) => new()
    {
        Id = Guid.NewGuid(),
        BatchNumber = batchNumber,
        WarehouseId = warehouseId,
        TotalMeters = totalMeters,
        RollCount = rollCount,
        LandingCostPerMeter = landingCostPerMeter,
        CurrencyCode = currencyCode,
        SupplierId = supplierId,
        ContainerId = containerId,
        PurchaseInvoiceId = purchaseInvoiceId,
        StorageLocationId = storageLocationId,
        ArrivalDate = DateTime.UtcNow,
        QualityStatus = InventoryQualityStatus.Good,
        Status = FabricBatchStatus.Active
    };
}

public class InventoryReservation
{
    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? FabricRollId { get; private set; }
    public Guid? FabricBatchId { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public decimal ReservedMeters { get; private set; }
    public int RollCount { get; private set; }
    public InventoryReservationStatus Status { get; private set; }
    public AllocationStrategy Strategy { get; private set; }
    public DocumentType ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public Guid? ReferenceLineId { get; private set; }

    private InventoryReservation() { }

    public static InventoryReservation Create(
        Guid warehouseId,
        Guid fabricItemId,
        Guid fabricColorId,
        decimal reservedMeters,
        int rollCount,
        DocumentType referenceType,
        Guid referenceId,
        AllocationStrategy strategy = AllocationStrategy.Fifo,
        Guid? fabricRollId = null,
        Guid? fabricBatchId = null,
        Guid? referenceLineId = null) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        ReservedMeters = reservedMeters,
        RollCount = rollCount,
        ReferenceType = referenceType,
        ReferenceId = referenceId,
        ReferenceLineId = referenceLineId,
        FabricRollId = fabricRollId,
        FabricBatchId = fabricBatchId,
        Strategy = strategy,
        Status = InventoryReservationStatus.Reserved
    };

    public void Allocate() => Status = InventoryReservationStatus.Allocated;
    public void Pick() => Status = InventoryReservationStatus.Picked;
    public void Pack() => Status = InventoryReservationStatus.Packed;
    public void Complete() => Status = InventoryReservationStatus.Sold;
    public void Cancel() => Status = InventoryReservationStatus.Cancelled;
    public void Return() => Status = InventoryReservationStatus.Returned;
}

public class InventoryAuditEntry
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string EntityType { get; private set; } = "";
    public string Action { get; private set; } = "";
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = "";
    public string? FieldName { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public string? SourceModule { get; private set; }
    public Guid? ReferenceDocumentId { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private InventoryAuditEntry() { }

    public static InventoryAuditEntry Record(
        Guid entityId,
        string entityType,
        string action,
        Guid userId,
        string username,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null,
        string? sourceModule = null,
        Guid? referenceDocumentId = null) => new()
    {
        Id = Guid.NewGuid(),
        EntityId = entityId,
        EntityType = entityType,
        Action = action,
        UserId = userId,
        Username = username,
        FieldName = fieldName,
        PreviousValue = previousValue,
        NewValue = newValue,
        Reason = reason,
        SourceModule = sourceModule,
        ReferenceDocumentId = referenceDocumentId,
        RecordedAt = DateTime.UtcNow
    };
}

public class InventoryTimelineEvent
{
    public Guid Id { get; private set; }
    public Guid EntityId { get; private set; }
    public string EntityType { get; private set; } = "";
    public string EventType { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public Guid UserId { get; private set; }
    public string Username { get; private set; } = "";
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public string? Reason { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private InventoryTimelineEvent() { }

    public static InventoryTimelineEvent Record(
        Guid entityId,
        string entityType,
        string eventType,
        string title,
        Guid userId,
        string username,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null) => new()
    {
        Id = Guid.NewGuid(),
        EntityId = entityId,
        EntityType = entityType,
        EventType = eventType,
        Title = title,
        Description = description,
        UserId = userId,
        Username = username,
        PreviousValue = previousValue,
        NewValue = newValue,
        Reason = reason,
        OccurredAt = DateTime.UtcNow
    };
}

public class InventoryValuationSnapshot
{
    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public Guid? FabricColorId { get; private set; }
    public Guid? ContainerId { get; private set; }
    public ValuationMethod Method { get; private set; }
    public decimal QuantityMeters { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalValue { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public DateTime SnapshotDate { get; private set; }
    public Guid? MovementId { get; private set; }

    private InventoryValuationSnapshot() { }

    public static InventoryValuationSnapshot Create(
        Guid warehouseId,
        decimal quantityMeters,
        decimal unitCost,
        ValuationMethod method,
        string currencyCode = "USD",
        Guid? fabricItemId = null,
        Guid? fabricColorId = null,
        Guid? containerId = null,
        Guid? movementId = null) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        ContainerId = containerId,
        Method = method,
        QuantityMeters = quantityMeters,
        UnitCost = unitCost,
        TotalValue = quantityMeters * unitCost,
        CurrencyCode = currencyCode,
        SnapshotDate = DateTime.UtcNow,
        MovementId = movementId
    };
}

public class InventoryRule
{
    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public decimal? MinimumStock { get; private set; }
    public decimal? MaximumStock { get; private set; }
    public decimal? SafetyStock { get; private set; }
    public decimal? ReorderPoint { get; private set; }
    public Guid? PreferredWarehouseId { get; private set; }
    public Guid? PreferredLocationId { get; private set; }
    public AllocationStrategy PreferredBatchStrategy { get; private set; }
    public int? LeadTimeDays { get; private set; }
    public bool IsActive { get; private set; } = true;

    private InventoryRule() { }

    public static InventoryRule Create(Guid branchId, Guid? fabricItemId = null, Guid? warehouseId = null) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = branchId,
        FabricItemId = fabricItemId,
        WarehouseId = warehouseId,
        PreferredBatchStrategy = AllocationStrategy.Fifo
    };
}

public class InventoryAlert
{
    public Guid Id { get; private set; }
    public Guid BranchId { get; private set; }
    public InventoryAlertType AlertType { get; private set; }
    public InventoryAlertSeverity Severity { get; private set; }
    public string Title { get; private set; } = "";
    public string Message { get; private set; } = "";
    public Guid? WarehouseId { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public Guid? FabricRollId { get; private set; }
    public Guid? FabricBatchId { get; private set; }
    public bool IsAcknowledged { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private InventoryAlert() { }

    public static InventoryAlert Create(
        Guid branchId,
        InventoryAlertType alertType,
        InventoryAlertSeverity severity,
        string title,
        string message,
        Guid? warehouseId = null,
        Guid? fabricItemId = null,
        Guid? fabricRollId = null,
        Guid? fabricBatchId = null) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = branchId,
        AlertType = alertType,
        Severity = severity,
        Title = title,
        Message = message,
        WarehouseId = warehouseId,
        FabricItemId = fabricItemId,
        FabricRollId = fabricRollId,
        FabricBatchId = fabricBatchId,
        CreatedAt = DateTime.UtcNow
    };

    public void Acknowledge() => IsAcknowledged = true;
}
