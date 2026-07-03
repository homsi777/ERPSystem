namespace ERPSystem.Domain.Enums;

public enum StorageLocationType
{
    Zone = 0,
    Rack = 1,
    Shelf = 2,
    Bin = 3
}

public enum StorageLocationStatus
{
    Active = 0,
    Inactive = 1,
    Full = 2,
    Blocked = 3
}

public enum InventoryReservationStatus
{
    Available = 0,
    Reserved = 1,
    Allocated = 2,
    Picked = 3,
    Packed = 4,
    Sold = 5,
    Returned = 6,
    Cancelled = 7
}

public enum AllocationStrategy
{
    Manual = 0,
    Fifo = 1,
    Lifo = 2,
    SpecificRoll = 3,
    SpecificBatch = 4,
    PreferredWarehouse = 5
}

public enum ValuationMethod
{
    AverageCost = 0,
    SpecificCost = 1,
    LandingCost = 2,
    Fifo = 3,
    Lifo = 4
}

public enum InventoryDocumentStatus
{
    Draft = 0,
    Counting = 1,
    Review = 2,
    Approved = 3,
    InTransit = 4,
    Received = 5,
    Posted = 6,
    Completed = 7,
    Closed = 8,
    Cancelled = 9
}

public enum FabricBatchStatus
{
    Active = 0,
    Quarantine = 1,
    Expired = 2,
    Depleted = 3,
    Archived = 4
}

public enum InventoryQualityStatus
{
    Good = 0,
    Damaged = 1,
    Rejected = 2,
    Rework = 3
}

public enum InventoryAlertType
{
    LowStock = 0,
    NegativeStock = 1,
    OverReserved = 2,
    ExpiredBatch = 3,
    DamagedRoll = 4,
    SlowMoving = 5,
    DeadStock = 6,
    WarehouseCapacity = 7,
    LocationCapacity = 8
}

public enum InventoryAlertSeverity
{
    Info = 0,
    Warning = 1,
    Critical = 2
}
