namespace ERPSystem.Infrastructure.Persistence.Models.Catalog;

public class FabricCategoryEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public class FabricItemEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid CategoryId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string DefaultUnit { get; set; } = "meter";
}

public class FabricColorEntity : PersistenceEntity
{
    public Guid FabricItemId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
}

public class FabricRollEntity : PersistenceEntity
{
    public Guid ContainerId { get; set; }
    public Guid? ContainerItemId { get; set; }
    public Guid? FabricBatchId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? StorageLocationId { get; set; }
    public int RollNumber { get; set; }
    public string? Barcode { get; set; }
    public string? QrCode { get; set; }
    public decimal LengthMeters { get; set; }
    public decimal RemainingLengthMeters { get; set; }
    public decimal CostPerMeter { get; set; }
    public decimal? SalePricePerMeter { get; set; }
    public decimal? WeightKg { get; set; }
    public string? LotCode { get; set; }
    public int Status { get; set; }
    public int QualityStatus { get; set; }
    public int ReservationStatus { get; set; }
    public bool IsLegacyOpeningBalance { get; set; }
    public bool LegacyLengthConfirmed { get; set; }
}
