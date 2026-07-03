namespace ERPSystem.Infrastructure.Persistence.Models.Inventory;

public class WarehouseEntity : PersistenceEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string? NameEn { get; set; }
    public string? Description { get; set; }
    public string City { get; set; } = "";
    public string? Address { get; set; }
    public string? Manager { get; set; }
    public Guid? CostCenterId { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public int? CapacityRolls { get; set; }
}

public class WarehouseLocationEntity : PersistenceEntity
{
    public Guid WarehouseId { get; set; }
    public Guid? ParentId { get; set; }
    public int LocationType { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Zone { get; set; } = "";
    public string BinCode { get; set; } = "";
    public decimal? CapacityMeters { get; set; }
    public int Status { get; set; }
    public int Priority { get; set; }
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
}

public class WarehouseStockEntity : PersistenceEntity
{
    public Guid WarehouseId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public Guid ContainerId { get; set; }
    public int RollCount { get; set; }
    public decimal TotalMeters { get; set; }
    public decimal ReservedMeters { get; set; }
    public decimal AvailableMeters { get; set; }
}

public class StockMovementEntity : CancellablePersistenceEntity
{
    public string MovementNumber { get; set; } = "";
    public DateTime MovementDate { get; set; }
    public int Type { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? SourceWarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public Guid? SourceLocationId { get; set; }
    public Guid? DestinationLocationId { get; set; }
    public int? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public int Status { get; set; }
    public string? Reason { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class StockMovementLineEntity : PersistenceEntity
{
    public Guid MovementId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public Guid? FabricRollId { get; set; }
    public Guid? FabricBatchId { get; set; }
    public Guid ContainerId { get; set; }
    public int RollCount { get; set; }
    public decimal QuantityMeters { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}
