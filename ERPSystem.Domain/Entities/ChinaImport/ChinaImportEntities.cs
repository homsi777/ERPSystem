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
    public WeightInKg? WeightKg { get; private set; }
    public string? LotCode { get; private set; }
    public Guid? BuyerCustomerId { get; private set; }
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
        Guid? buyerCustomerId = null) => new()
    {
        Id = Guid.NewGuid(),
        LineNumber = lineNumber,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        RollCount = rollCount,
        LengthMeters = lengthMeters,
        WeightKg = weightKg,
        LotCode = lotCode,
        BuyerCustomerId = buyerCustomerId,
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

public class LandingCost
{
    public Guid Id { get; private set; }
    public LengthInMeters TotalLengthFromInvoice { get; private set; } = null!;
    public WeightInKg ContainerWeight { get; private set; } = null!;
    public Money CustomsAmountPaid { get; private set; } = Money.Zero();
    public Money Shipping { get; private set; } = Money.Zero();
    public Money Clearance { get; private set; } = Money.Zero();
    public Money OtherExpenses { get; private set; } = Money.Zero();
    public LandingCostStatus Status { get; private set; }
    public DateTime? CalculatedAt { get; private set; }
    public Guid? CalculatedByUserId { get; private set; }
    public List<LandingCostExpense> Expenses { get; private set; } = [];

    private LandingCost() { }

    public Money TotalImportExpenses =>
        CustomsAmountPaid.Add(Shipping).Add(Clearance).Add(OtherExpenses);

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
        Money otherExpenses) => new()
    {
        Id = Guid.NewGuid(),
        TotalLengthFromInvoice = totalLength,
        ContainerWeight = containerWeight,
        CustomsAmountPaid = customsAmount,
        Shipping = shipping,
        Clearance = clearance,
        OtherExpenses = otherExpenses,
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
