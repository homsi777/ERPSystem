namespace ERPSystem.Infrastructure.Persistence.Models.Inventory;

public class WarehouseEntity : PersistenceEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string City { get; set; } = "";
    public int? CapacityRolls { get; set; }
}

public class WarehouseLocationEntity : PersistenceEntity
{
    public Guid WarehouseId { get; set; }
    public string Zone { get; set; } = "";
    public string BinCode { get; set; } = "";
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
    public int? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public int Status { get; set; }
    public DateTime? PostedAt { get; set; }
}
