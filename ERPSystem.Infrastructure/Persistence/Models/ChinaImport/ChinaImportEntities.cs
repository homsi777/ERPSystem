namespace ERPSystem.Infrastructure.Persistence.Models.ChinaImport;

public class ContainerEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid? ChinaOrderId { get; set; }
    public string ContainerNumber { get; set; } = "";
    public int Status { get; set; }
    public DateTime ShipmentDate { get; set; }
    public DateTime? ExpectedArrival { get; set; }
    public DateTime? ArrivalDate { get; set; }
    public int TotalRolls { get; set; }
    public decimal TotalMeters { get; set; }
    public decimal? TotalWeightKg { get; set; }
    public string? Port { get; set; }
    public string? Notes { get; set; }
    public decimal ExchangeRateToLocalCurrency { get; set; } = 1m;
    public decimal ChinaInvoiceAmountUsd { get; set; }
    public decimal? FinancialTaxReservePostedLocal { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
}

public class ContainerItemEntity : PersistenceEntity
{
    public Guid ContainerId { get; set; }
    public int LineNumber { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public int RollCount { get; set; }
    public decimal LengthMeters { get; set; }
    public decimal? WeightKg { get; set; }
    public string? LotCode { get; set; }
    public Guid? BuyerCustomerId { get; set; }
    public int? SupplierRollNumber { get; set; }
    public string RowStatus { get; set; } = "Valid";
}

public class LandingCostEntity : PersistenceEntity
{
    public Guid ContainerId { get; set; }
    public decimal TotalLengthMeters { get; set; }
    public decimal ContainerWeightKg { get; set; }
    public decimal CustomsAmount { get; set; }
    public decimal Shipping { get; set; }
    public decimal Insurance { get; set; }
    public decimal Clearance { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal OtherExpense1 { get; set; }
    public decimal OtherExpense2 { get; set; }
    public decimal OtherExpense3 { get; set; }
    public decimal OtherExpense4 { get; set; }
    public bool UsesWeightedAllocation { get; set; }
    public int Status { get; set; }
    public DateTime? CalculatedAt { get; set; }
    public Guid? CalculatedByUserId { get; set; }
}

public class ContainerFabricTypeLineEntity : PersistenceEntity
{
    public Guid ContainerId { get; set; }
    public int LineNumber { get; set; }
    public string TypeDisplayName { get; set; } = "";
    public string MatchKey { get; set; } = "";
    public Guid? FabricItemId { get; set; }
    public Guid? FabricColorId { get; set; }
    public decimal LengthMeters { get; set; }
    public int RollCount { get; set; }
    public decimal NetWeightKg { get; set; }
    public decimal Cbm { get; set; }
    public decimal ChinaUnitPriceUsd { get; set; }
    public decimal InvoiceLineAmountUsd { get; set; }
    public decimal ExpenseShareUsd { get; set; }
    public decimal LandedCostPerMeterUsd { get; set; }
    public decimal MarginPerMeterUsd { get; set; }
    public decimal SalePricePerMeterUsd { get; set; }
    public bool HasInvoiceMatch { get; set; }
    public bool HasPlMatch { get; set; }
    public bool HasDplMatch { get; set; }
    public string? MatchWarnings { get; set; }
    public bool UsesWeightedAllocation { get; set; }
}

public class FabricTypeAliasEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid SupplierId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public string DplMatchKey { get; set; } = "";
    public string InvoiceDescriptionMatchKey { get; set; } = "";
    public string InvoiceDescription { get; set; } = "";
}

public class LandingCostExpenseEntity : PersistenceEntity
{
    public Guid LandingCostId { get; set; }
    public string ExpenseType { get; set; } = "";
    public decimal Amount { get; set; }
    public string? Notes { get; set; }
}

public class ImportBatchEntity : PersistenceEntity
{
    public Guid ContainerId { get; set; }
    public string BatchNumber { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime ImportedAt { get; set; }
    public Guid ImportedByUserId { get; set; }
    public int ValidRowCount { get; set; }
    public int ErrorRowCount { get; set; }
}

public class ContainerDistributionEntity : PersistenceEntity
{
    public Guid ContainerId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid FabricItemId { get; set; }
    public Guid FabricColorId { get; set; }
    public int RollCount { get; set; }
    public decimal Meters { get; set; }
}
