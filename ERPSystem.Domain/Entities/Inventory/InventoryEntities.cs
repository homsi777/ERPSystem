using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Inventory;

public class Warehouse
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string? NameEn { get; private set; }
    public string? Description { get; private set; }
    public Guid BranchId { get; private set; }
    public string City { get; private set; } = "";
    public string? Address { get; private set; }
    public string? Manager { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string? Notes { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; } = true;
    public int? CapacityRolls { get; private set; }

    private Warehouse() { }

    public static Warehouse Create(
        Guid branchId,
        string code,
        string nameAr,
        string city,
        string? nameEn = null,
        string? description = null,
        string? address = null,
        string? manager = null,
        Guid? costCenterId = null,
        string? notes = null,
        bool isDefault = false,
        int? capacityRolls = null) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = branchId,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        Description = description,
        City = city,
        Address = address,
        Manager = manager,
        CostCenterId = costCenterId,
        Notes = notes,
        IsDefault = isDefault,
        CapacityRolls = capacityRolls
    };

    public void Update(
        string nameAr,
        string city,
        string? nameEn = null,
        string? description = null,
        string? address = null,
        string? manager = null,
        Guid? costCenterId = null,
        string? notes = null,
        int? capacityRolls = null)
    {
        NameAr = nameAr;
        City = city;
        NameEn = nameEn;
        Description = description;
        Address = address;
        Manager = manager;
        CostCenterId = costCenterId;
        Notes = notes;
        CapacityRolls = capacityRolls;
    }

    public void SetDefault(bool isDefault) => IsDefault = isDefault;
    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;

    public static Warehouse FromPersistence(
        Guid id, Guid branchId, string code, string nameAr, string city,
        string? nameEn = null, string? description = null, string? address = null,
        string? manager = null, Guid? costCenterId = null, string? notes = null,
        bool isDefault = false, int? capacityRolls = null, bool isActive = true) => new()
    {
        Id = id,
        BranchId = branchId,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        Description = description,
        City = city,
        Address = address,
        Manager = manager,
        CostCenterId = costCenterId,
        Notes = notes,
        IsDefault = isDefault,
        CapacityRolls = capacityRolls,
        IsActive = isActive
    };
}

public class WarehouseLocation
{
    public Guid Id { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? ParentId { get; private set; }
    public StorageLocationType LocationType { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string Zone { get; private set; } = "";
    public string BinCode { get; private set; } = "";
    public decimal? CapacityMeters { get; private set; }
    public StorageLocationStatus Status { get; private set; }
    public int Priority { get; private set; }
    public string? Barcode { get; private set; }
    public string? QrCode { get; private set; }
    public bool IsActive { get; private set; } = true;

    private WarehouseLocation() { }

    public static WarehouseLocation Create(
        Guid warehouseId,
        StorageLocationType locationType,
        string code,
        string name,
        Guid? parentId = null,
        string zone = "",
        string binCode = "",
        decimal? capacityMeters = null,
        int priority = 0) => new()
    {
        Id = Guid.NewGuid(),
        WarehouseId = warehouseId,
        ParentId = parentId,
        LocationType = locationType,
        Code = code,
        Name = name,
        Zone = zone,
        BinCode = binCode,
        CapacityMeters = capacityMeters,
        Status = StorageLocationStatus.Active,
        Priority = priority
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
    public Guid? SourceWarehouseId { get; private set; }
    public Guid? DestinationWarehouseId { get; private set; }
    public Guid? SourceLocationId { get; private set; }
    public Guid? DestinationLocationId { get; private set; }
    public DocumentType? ReferenceType { get; private set; }
    public Guid? ReferenceId { get; private set; }
    public StockMovementStatus Status { get; private set; }
    public string? Reason { get; private set; }
    public Guid? UserId { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private readonly List<StockMovementLine> _lines = [];
    public IReadOnlyList<StockMovementLine> Lines => _lines.AsReadOnly();

    private StockMovement() { }

    public static StockMovement CreateDraft(
        string movementNumber,
        MovementType type,
        Guid warehouseId,
        DocumentType? referenceType = null,
        Guid? referenceId = null,
        string? reason = null,
        Guid? userId = null,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null,
        Guid? sourceLocationId = null,
        Guid? destinationLocationId = null) => new()
    {
        Id = Guid.NewGuid(),
        MovementNumber = movementNumber,
        MovementDate = DateTime.UtcNow,
        Type = type,
        WarehouseId = warehouseId,
        SourceWarehouseId = sourceWarehouseId,
        DestinationWarehouseId = destinationWarehouseId,
        SourceLocationId = sourceLocationId,
        DestinationLocationId = destinationLocationId,
        ReferenceType = referenceType,
        ReferenceId = referenceId,
        Reason = reason,
        UserId = userId,
        Status = StockMovementStatus.Draft
    };

    public void AddLine(StockMovementLine line) => _lines.Add(line);

    public void Post()
    {
        if (Status != StockMovementStatus.Draft)
            throw new Exceptions.InventoryException("Only draft movements can be posted.");
        Status = StockMovementStatus.Posted;
        PostedAt = DateTime.UtcNow;
    }
}

public class StockMovementLine
{
    public Guid Id { get; private set; }
    public Guid MovementId { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public Guid? FabricRollId { get; private set; }
    public Guid? FabricBatchId { get; private set; }
    public Guid ContainerId { get; private set; }
    public int RollCount { get; private set; }
    public decimal QuantityMeters { get; private set; }
    public decimal UnitCost { get; private set; }
    public decimal TotalValue { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";

    private StockMovementLine() { }

    public static StockMovementLine Create(
        Guid movementId,
        Guid fabricItemId,
        Guid fabricColorId,
        decimal quantityMeters,
        decimal unitCost,
        Guid containerId = default,
        int rollCount = 0,
        Guid? fabricRollId = null,
        Guid? fabricBatchId = null,
        string currencyCode = "USD") => new()
    {
        Id = Guid.NewGuid(),
        MovementId = movementId,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        FabricRollId = fabricRollId,
        FabricBatchId = fabricBatchId,
        ContainerId = containerId,
        RollCount = rollCount,
        QuantityMeters = quantityMeters,
        UnitCost = unitCost,
        TotalValue = quantityMeters * unitCost,
        CurrencyCode = currencyCode
    };
}

public class StockTransfer
{
    public Guid Id { get; private set; }
    public string Number { get; private set; } = "";
    public Guid FromWarehouseId { get; private set; }
    public Guid ToWarehouseId { get; private set; }
    public Guid? FromLocationId { get; private set; }
    public Guid? ToLocationId { get; private set; }
    public InventoryDocumentStatus Status { get; private set; }
    public DateTime Date { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private StockTransfer() { }

    public static StockTransfer Create(
        string number,
        Guid fromId,
        Guid toId,
        Guid? fromLocationId = null,
        Guid? toLocationId = null,
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        FromWarehouseId = fromId,
        ToWarehouseId = toId,
        FromLocationId = fromLocationId,
        ToLocationId = toLocationId,
        Notes = notes,
        Date = DateTime.UtcNow,
        Status = InventoryDocumentStatus.Draft
    };

    public void Approve(Guid userId)
    {
        Status = InventoryDocumentStatus.Approved;
        ApprovedByUserId = userId;
        ApprovedAt = DateTime.UtcNow;
    }

    public void MarkInTransit() => Status = InventoryDocumentStatus.InTransit;
    public void MarkReceived() => Status = InventoryDocumentStatus.Received;

    public void Complete()
    {
        Status = InventoryDocumentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel() => Status = InventoryDocumentStatus.Cancelled;
}

public class StocktakeSession
{
    public Guid Id { get; private set; }
    public string SessionNumber { get; private set; } = "";
    public DateTime Date { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid? LocationId { get; private set; }
    public string Responsible { get; private set; } = "";
    public InventoryDocumentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private StocktakeSession() { }

    public static StocktakeSession Create(
        string sessionNumber,
        Guid warehouseId,
        string responsible,
        Guid? locationId = null,
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        SessionNumber = sessionNumber,
        WarehouseId = warehouseId,
        LocationId = locationId,
        Responsible = responsible,
        Notes = notes,
        Date = DateTime.UtcNow,
        Status = InventoryDocumentStatus.Draft
    };

    public void StartCounting() => Status = InventoryDocumentStatus.Counting;
    public void SubmitForReview() => Status = InventoryDocumentStatus.Review;
    public void Approve() => Status = InventoryDocumentStatus.Approved;

    public void Post()
    {
        Status = InventoryDocumentStatus.Posted;
        PostedAt = DateTime.UtcNow;
    }

    public void Close() => Status = InventoryDocumentStatus.Closed;
    public void Cancel() => Status = InventoryDocumentStatus.Cancelled;
}

public class OpeningStockDocument
{
    public Guid Id { get; private set; }
    public string DocumentNumber { get; private set; } = "";
    public Guid WarehouseId { get; private set; }
    public DateTime OpeningDate { get; private set; }
    public string? Reference { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public InventoryDocumentStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private OpeningStockDocument() { }

    public static OpeningStockDocument Create(
        string documentNumber,
        Guid warehouseId,
        DateTime openingDate,
        string? reference = null,
        string currencyCode = "USD",
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        DocumentNumber = documentNumber,
        WarehouseId = warehouseId,
        OpeningDate = openingDate,
        Reference = reference,
        CurrencyCode = currencyCode,
        Notes = notes,
        Status = InventoryDocumentStatus.Draft
    };

    public void Post()
    {
        Status = InventoryDocumentStatus.Posted;
        PostedAt = DateTime.UtcNow;
    }

    public void Cancel() => Status = InventoryDocumentStatus.Cancelled;
}
