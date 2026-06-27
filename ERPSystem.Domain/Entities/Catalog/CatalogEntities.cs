using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Catalog;

public class FabricCategory
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public Guid? ParentId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private FabricCategory() { }

    public static FabricCategory Create(string code, string nameAr, string nameEn) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn
    };
}

public class FabricItem
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public Guid CategoryId { get; private set; }
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string DefaultUnit { get; private set; } = "meter";
    public bool IsActive { get; private set; } = true;

    private FabricItem() { }

    public static FabricItem Create(Guid categoryId, string code, string nameAr, string nameEn) => new()
    {
        Id = Guid.NewGuid(),
        CategoryId = categoryId,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn
    };
}

public class FabricColor
{
    public Guid Id { get; private set; }
    public Guid FabricItemId { get; private set; }
    public string ColorCode { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public bool IsActive { get; private set; } = true;

    private FabricColor() { }

    public static FabricColor Create(Guid fabricItemId, string colorCode, string nameAr, string nameEn) => new()
    {
        Id = Guid.NewGuid(),
        FabricItemId = fabricItemId,
        ColorCode = colorCode,
        NameAr = nameAr,
        NameEn = nameEn
    };
}

public class FabricRoll
{
    public Guid Id { get; private set; }
    public Guid ContainerId { get; private set; }
    public RollNumber RollNumber { get; private set; } = null!;
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public LengthInMeters LengthMeters { get; private set; } = null!;
    public WeightInKg? WeightKg { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public Guid? LocationId { get; private set; }
    public FabricRollStatus Status { get; private set; }

    private FabricRoll() { }

    public static FabricRoll Create(
        Guid containerId,
        RollNumber rollNumber,
        Guid fabricItemId,
        Guid fabricColorId,
        LengthInMeters lengthMeters) => new()
    {
        Id = Guid.NewGuid(),
        ContainerId = containerId,
        RollNumber = rollNumber,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        LengthMeters = lengthMeters,
        Status = FabricRollStatus.Available
    };

    public void AssignToWarehouse(Guid warehouseId, Guid? locationId)
    {
        WarehouseId = warehouseId;
        LocationId = locationId;
        Status = FabricRollStatus.Available;
    }

    public void Reserve() => Status = FabricRollStatus.Reserved;
    public void MarkSold() => Status = FabricRollStatus.Sold;
}
