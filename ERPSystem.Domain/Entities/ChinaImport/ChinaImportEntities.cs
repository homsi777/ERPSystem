using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.ChinaImport;

public class ChinaOrder
{
    public Guid Id { get; private set; }
    public string OrderNumber { get; private set; } = "";
    public Guid ChinaSupplierId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? ExpectedShipDate { get; private set; }
    public ApprovalStatus Status { get; private set; }

    private ChinaOrder() { }

    public static ChinaOrder Create(string orderNumber, Guid chinaSupplierId, DateTime orderDate) => new()
    {
        Id = Guid.NewGuid(),
        OrderNumber = orderNumber,
        ChinaSupplierId = chinaSupplierId,
        OrderDate = orderDate,
        Status = ApprovalStatus.Pending
    };
}

public class ChinaContainerItem
{
    public Guid Id { get; private set; }
    public int LineNumber { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public int RollCount { get; private set; }
    public LengthInMeters LengthMeters { get; private set; } = null!;
    public decimal? DplQuantityNative { get; private set; }
    public DplQuantityUnit? DplQuantityUnit { get; private set; }
    public WeightInKg? WeightKg { get; private set; }
    public string? LotCode { get; private set; }
    public Guid? BuyerCustomerId { get; private set; }
    public int? SupplierRollNumber { get; private set; }
    public string RowStatus { get; private set; } = "Valid";

    private ChinaContainerItem() { }

    public static ChinaContainerItem Create(
        int lineNumber,
        Guid fabricItemId,
        Guid fabricColorId,
        int rollCount,
        LengthInMeters lengthMeters,
        WeightInKg? weightKg = null,
        string? lotCode = null,
        Guid? buyerCustomerId = null,
        int? supplierRollNumber = null,
        decimal? dplQuantityNative = null,
        DplQuantityUnit? dplQuantityUnit = null) => new()
    {
        Id = Guid.NewGuid(),
        LineNumber = lineNumber,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        RollCount = rollCount,
        LengthMeters = lengthMeters,
        DplQuantityNative = dplQuantityNative,
        DplQuantityUnit = dplQuantityUnit,
        WeightKg = weightKg,
        LotCode = lotCode,
        BuyerCustomerId = buyerCustomerId,
        SupplierRollNumber = supplierRollNumber,
        RowStatus = ContainerImportRowStatus.Valid
    };

    public bool IsValid => RowStatus.Equals(ContainerImportRowStatus.Valid, StringComparison.OrdinalIgnoreCase);
}

public class ChinaImportBatch
{
    public Guid Id { get; private set; }
    public string BatchNumber { get; private set; } = "";
    public string FileName { get; private set; } = "";
    public DateTime ImportedAt { get; private set; }
    public Guid ImportedByUserId { get; private set; }
    public int ValidRowCount { get; private set; }
    public int ErrorRowCount { get; private set; }

    private ChinaImportBatch() { }

    public static ChinaImportBatch Create(string batchNumber, string fileName, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        BatchNumber = batchNumber,
        FileName = fileName,
        ImportedAt = DateTime.UtcNow,
        ImportedByUserId = userId
    };

    public void SetCounts(int valid, int errors)
    {
        ValidRowCount = valid;
        ErrorRowCount = errors;
    }
}

public class ChinaImportRow
{
    public Guid Id { get; private set; }
    public int RowNumber { get; private set; }
    public string RawData { get; private set; } = "";
    public string? ValidationErrors { get; private set; }
    public bool IsAccepted { get; private set; }

    private ChinaImportRow() { }

    public static ChinaImportRow Create(int rowNumber, string rawData) => new()
    {
        Id = Guid.NewGuid(),
        RowNumber = rowNumber,
        RawData = rawData
    };
}

public class ContainerCustomerDistribution
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public int RollCount { get; private set; }
    public LengthInMeters Meters { get; private set; } = null!;

    private ContainerCustomerDistribution() { }

    public static ContainerCustomerDistribution Create(
        Guid customerId,
        Guid fabricItemId,
        Guid fabricColorId,
        int rollCount,
        LengthInMeters meters) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = customerId,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        RollCount = rollCount,
        Meters = meters
    };
}

public class LandingCostExpense
{
    public Guid Id { get; private set; }
    public string ExpenseType { get; private set; } = "";
    public Money Amount { get; private set; } = Money.Zero();
    public string? Notes { get; private set; }

    private LandingCostExpense() { }

    public static LandingCostExpense Create(string expenseType, Money amount, string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseType = expenseType,
        Amount = amount,
        Notes = notes
    };
}

public class ContainerFabricTypeLine
{
    public Guid Id { get; private set; }
    public int LineNumber { get; private set; }
    public string TypeDisplayName { get; private set; } = "";
    public string MatchKey { get; private set; } = "";
    public Guid? FabricItemId { get; private set; }
    public Guid? FabricColorId { get; private set; }
    public decimal LengthMeters { get; private set; }
    public int RollCount { get; private set; }
    public decimal NetWeightKg { get; private set; }
    public decimal Cbm { get; private set; }
    public decimal ChinaUnitPriceUsd { get; private set; }
    public decimal InvoiceLineAmountUsd { get; private set; }
    public decimal ExpenseShareUsd { get; private set; }
    public decimal LandedCostPerMeterUsd { get; private set; }
    public decimal MarginPerMeterUsd { get; private set; }
    public decimal SalePricePerMeterUsd { get; private set; }
    public bool HasInvoiceMatch { get; private set; }
    public bool HasPlMatch { get; private set; }
    public bool HasDplMatch { get; private set; }
    public string? MatchWarnings { get; private set; }
    public bool UsesWeightedAllocation { get; private set; }

    private ContainerFabricTypeLine() { }

    public static ContainerFabricTypeLine Create(
        int lineNumber,
        string typeDisplayName,
        string matchKey,
        Guid? fabricItemId,
        Guid? fabricColorId,
        decimal lengthMeters,
        int rollCount,
        decimal netWeightKg,
        decimal cbm,
        decimal chinaUnitPriceUsd,
        decimal invoiceLineAmountUsd,
        bool hasInvoice,
        bool hasPl,
        bool hasDpl,
        string? matchWarnings,
        bool usesWeightedAllocation) => new()
    {
        Id = Guid.NewGuid(),
        LineNumber = lineNumber,
        TypeDisplayName = typeDisplayName,
        MatchKey = matchKey,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        LengthMeters = lengthMeters,
        RollCount = rollCount,
        NetWeightKg = netWeightKg,
        Cbm = cbm,
        ChinaUnitPriceUsd = chinaUnitPriceUsd,
        InvoiceLineAmountUsd = invoiceLineAmountUsd,
        HasInvoiceMatch = hasInvoice,
        HasPlMatch = hasPl,
        HasDplMatch = hasDpl,
        MatchWarnings = matchWarnings,
        UsesWeightedAllocation = usesWeightedAllocation
    };

    public void ApplyCostAllocation(decimal expenseShareUsd, decimal landedCostPerMeterUsd)
    {
        ExpenseShareUsd = expenseShareUsd;
        LandedCostPerMeterUsd = landedCostPerMeterUsd;
    }

    public void SetSalePrice(decimal marginPerMeterUsd)
    {
        MarginPerMeterUsd = marginPerMeterUsd;
        SalePricePerMeterUsd = LandedCostPerMeterUsd + marginPerMeterUsd;
    }
}

public class LandingCost
{
    public Guid Id { get; private set; }
    public LengthInMeters TotalLengthFromInvoice { get; private set; } = null!;
    public WeightInKg ContainerWeight { get; private set; } = null!;
    public Money CustomsAmountPaid { get; private set; } = Money.Zero();
    public Money Shipping { get; private set; } = Money.Zero();
    public Money Insurance { get; private set; } = Money.Zero();
    public Money Clearance { get; private set; } = Money.Zero();
    public Money OtherExpenses { get; private set; } = Money.Zero();
    public Money OtherExpense1 { get; private set; } = Money.Zero();
    public Money OtherExpense2 { get; private set; } = Money.Zero();
    public Money OtherExpense3 { get; private set; } = Money.Zero();
    public Money OtherExpense4 { get; private set; } = Money.Zero();
    public bool UsesWeightedAllocation { get; private set; }
    public LandingCostStatus Status { get; private set; }
    public DateTime? CalculatedAt { get; private set; }
    public Guid? CalculatedByUserId { get; private set; }
    public List<LandingCostExpense> Expenses { get; private set; } = [];

    private LandingCost() { }

    public Money TotalSharedExpenses =>
        Shipping.Add(Insurance).Add(CustomsAmountPaid).Add(Clearance)
            .Add(OtherExpenses).Add(OtherExpense1).Add(OtherExpense2).Add(OtherExpense3).Add(OtherExpense4);

    public Money TotalImportExpenses => TotalSharedExpenses;

    public decimal CustomsCostPerMeter =>
        TotalLengthFromInvoice.Value > 0 ? CustomsAmountPaid.Amount / TotalLengthFromInvoice.Value : 0;

    public decimal ExpenseCostPerMeter =>
        TotalLengthFromInvoice.Value > 0 ? TotalImportExpenses.Amount / TotalLengthFromInvoice.Value : 0;

    public decimal AvgGramPerMeter =>
        TotalLengthFromInvoice.Value > 0 ? ContainerWeight.ToGrams().Value / TotalLengthFromInvoice.Value : 0;

    public static LandingCost Create(
        LengthInMeters totalLength,
        WeightInKg containerWeight,
        Money customsAmount,
        Money shipping,
        Money clearance,
        Money otherExpenses) =>
        CreateExtended(totalLength, containerWeight, customsAmount, shipping, Money.Zero(), clearance,
            otherExpenses, Money.Zero(), Money.Zero(), Money.Zero(), Money.Zero(), false);

    public static LandingCost CreateExtended(
        LengthInMeters totalLength,
        WeightInKg containerWeight,
        Money customsClearanceAmount,
        Money shipping,
        Money insurance,
        Money clearanceLegacy,
        Money otherExpensesLegacy,
        Money otherExpense1,
        Money otherExpense2,
        Money otherExpense3,
        Money otherExpense4,
        bool usesWeightedAllocation) => new()
    {
        Id = Guid.NewGuid(),
        TotalLengthFromInvoice = totalLength,
        ContainerWeight = containerWeight,
        CustomsAmountPaid = customsClearanceAmount,
        Shipping = shipping,
        Insurance = insurance,
        Clearance = clearanceLegacy,
        OtherExpenses = otherExpensesLegacy,
        OtherExpense1 = otherExpense1,
        OtherExpense2 = otherExpense2,
        OtherExpense3 = otherExpense3,
        OtherExpense4 = otherExpense4,
        UsesWeightedAllocation = usesWeightedAllocation,
        Status = LandingCostStatus.Draft
    };

    public void MarkReviewed(Guid userId)
    {
        CalculatedAt = DateTime.UtcNow;
        CalculatedByUserId = userId;
        Status = LandingCostStatus.Reviewed;
    }

    public void Approve()
    {
        if (Status != LandingCostStatus.Reviewed)
            throw new Exceptions.ContainerApprovalException("Landing cost must be reviewed before approval.");
        Status = LandingCostStatus.Approved;
    }
}
