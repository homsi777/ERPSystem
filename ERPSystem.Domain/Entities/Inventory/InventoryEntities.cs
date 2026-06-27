using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Inventory;

public class Warehouse
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public Guid BranchId { get; private set; }
    public string City { get; private set; } = "";
    public bool IsActive { get; private set; } = true;
    public int? CapacityRolls { get; private set; }

    private Warehouse() { }

    public static Warehouse Create(Guid branchId, string code, string nameAr, string city) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = branchId,
        Code = code,
        NameAr = nameAr,
        City = city
    };
}

public class WarehouseLocation
{
    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Zone { get; private set; } = "";
    public string BinCode { get; private set; } = "";

    private WarehouseLocation() { }

    public static WarehouseLocation Create(Guid warehouseId, string zone, string binCode) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        Zone = zone,
        BinCode = binCode
    };
}

public class WarehouseStockBalance
{
    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public Guid ContainerId { get; private set; }
    public int RollCount { get; private set; }
    public LengthInMeters TotalMeters { get; private set; } = null!;
    public LengthInMeters ReservedMeters { get; private set; } = null!;
    public LengthInMeters AvailableMeters { get; private set; } = null!;

    private WarehouseStockBalance() { }

    public static WarehouseStockBalance Create(
        Guid warehouseId,
        Guid fabricItemId,
        Guid fabricColorId,
        Guid containerId,
        int rollCount,
        LengthInMeters totalMeters) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        ContainerId = containerId,
        RollCount = rollCount,
        TotalMeters = totalMeters,
        ReservedMeters = LengthInMeters.Zero,
        AvailableMeters = totalMeters
    };

    public void Reserve(LengthInMeters meters)
    {
        if (meters.Value > AvailableMeters.Value)
            throw new Exceptions.InventoryException("Insufficient available meters to reserve.");
        ReservedMeters = ReservedMeters.Add(meters);
        AvailableMeters = AvailableMeters.Subtract(meters);
    }

    public void Deduct(LengthInMeters meters)
    {
        if (meters.Value > TotalMeters.Value)
            throw new Exceptions.InventoryException("Insufficient stock to deduct.");
        TotalMeters = TotalMeters.Subtract(meters);
        if (ReservedMeters.Value >= meters.Value)
            ReservedMeters = ReservedMeters.Subtract(meters);
        else
            AvailableMeters = AvailableMeters.Subtract(meters);
    }
}

public class StockMovement
{
    public Guid Id { get; private set; }
    public string MovementNumber { get; private set; } = "";
    public DateTime MovementDate { get; private set; }
    public MovementType Type { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DocumentType? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public StockMovementStatus Status { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private StockMovement() { }

    public static StockMovement CreateDraft(
        string movementNumber,
        MovementType type,
        Guid warehouseId,
        DocumentType? referenceType = null,
        Guid? referenceId = null) => new()
    {
        Id = Guid.NewGuid(),
        MovementNumber = movementNumber,
        MovementDate = DateTime.UtcNow,
        Type = type,
        WarehouseId = warehouseId,
        ReferenceType = referenceType,
        ReferenceId = referenceId,
        Status = StockMovementStatus.Draft
    };

    public void Post()
    {
        if (Status != StockMovementStatus.Draft)
            throw new Exceptions.InventoryException("Only draft movements can be posted.");
        Status = StockMovementStatus.Posted;
        PostedAt = DateTime.UtcNow;
    }
}

public class StockTransfer
{
    public Guid Id { get; private set; }
    public string Number { get; private set; } = "";
    public Guid FromWarehouseId { get; private set; }
    public Guid ToWarehouseId { get; private set; }
    public StockMovementStatus Status { get; private set; }
    public DateTime Date { get; private set; }

    private StockTransfer() { }

    public static StockTransfer Create(string number, Guid fromId, Guid toId) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        FromWarehouseId = fromId,
        ToWarehouseId = toId,
        Date = DateTime.UtcNow,
        Status = StockMovementStatus.Draft
    };
}

public class StocktakeSession
{
    public Guid Id { get; private set; }
    public string SessionNumber { get; private set; } = "";
    public DateTime Date { get; private set; }
    public Guid WarehouseId { get; private set; }
    public string Responsible { get; private set; } = "";
    public StockMovementStatus Status { get; private set; }

    private StocktakeSession() { }

    public static StocktakeSession Create(string sessionNumber, Guid warehouseId, string responsible) => new()
    {
        Id = Guid.NewGuid(),
        SessionNumber = sessionNumber,
        WarehouseId = warehouseId,
        Responsible = responsible,
        Date = DateTime.UtcNow,
        Status = StockMovementStatus.Draft
    };
}
