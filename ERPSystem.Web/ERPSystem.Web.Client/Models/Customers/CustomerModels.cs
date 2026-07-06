namespace ERPSystem.Web.Client.Models.Customers;

public enum CustomerTypeModel
{
    Cash = 0,
    Credit = 1
}

public enum CustomerStatusModel
{
    Active = 0,
    Suspended = 1,
    Blocked = 2
}

public enum DocumentTypeModel
{
    SalesInvoice = 0,
    SalesReturn = 1,
    PurchaseInvoice = 2,
    PurchaseReturn = 3,
    ReceiptVoucher = 4,
    PaymentVoucher = 5,
    JournalEntry = 6,
    StockMovement = 7,
    DeliveryNote = 8,
    ChinaContainer = 9,
    ExpensePayment = 10,
    SupplierOpeningBalance = 11,
    OpeningBalance = 12,
    StockTransfer = 13,
    Stocktake = 14,
    PurchaseInvoiceReversal = 15,
    CustomerOpeningBalance = 16,
    CashboxTransfer = 17,
    FinanceOpeningBalance = 18
}

public sealed class CustomerListModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerTypeModel Type { get; init; }
    public CustomerStatusModel Status { get; init; }
    public decimal Balance { get; init; }
    public decimal CreditLimit { get; init; }
    public bool CreditLimitEnabled { get; init; }
    public bool IsActive { get; init; }
    public bool OpeningBalancePosted { get; init; }
}

public sealed class CustomerDetailsModel
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerTypeModel Type { get; init; }
    public CustomerStatusModel Status { get; init; }
    public decimal Balance { get; init; }
    public decimal CreditLimit { get; init; }
    public bool CreditLimitEnabled { get; init; }
    public int PaymentTermsDays { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public bool OpeningBalancePosted { get; init; }
}

public sealed class CustomerSalesDetailModel
{
    public DateTime SaleDate { get; init; }
    public string FabricName { get; init; } = "";
    public string FabricCode { get; init; } = "";
    public string ColorName { get; init; } = "";
    public decimal UnitPrice { get; init; }
}

public sealed class CustomerStatementModel
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public IReadOnlyList<CustomerStatementLineModel> Lines { get; init; } = [];
}

public sealed class CustomerStatementLineModel
{
    public DateTime EntryDate { get; init; }
    public DocumentTypeModel DocumentType { get; init; }
    public string DocumentNumber { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
}
