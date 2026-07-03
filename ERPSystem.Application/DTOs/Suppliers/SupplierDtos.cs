using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Suppliers;

public sealed class SupplierListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string? Country { get; init; }
    public string? Phone { get; init; }
    public decimal Balance { get; init; }
    public int PaymentTermsDays { get; init; }
    public SupplierStatus Status { get; init; }
    public bool IsActive { get; init; }
    public bool OpeningBalancePosted { get; init; }
    public string PaymentTermsDisplay { get; init; } = "";
}

public sealed class SupplierDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string CurrencyCode { get; init; } = "SAR";
    public int PaymentTermsDays { get; init; }
    public decimal CreditLimit { get; init; }
    public string? TaxNumber { get; init; }
    public Guid PayablesAccountId { get; init; }
    public string? PayablesAccountName { get; init; }
    public string? Notes { get; init; }
    public decimal Balance { get; init; }
    public SupplierStatus Status { get; init; }
    public bool IsActive { get; init; }
    public bool OpeningBalancePosted { get; init; }
    public string PaymentTermsDisplay { get; init; } = "";
}

public sealed class SupplierStatementDto
{
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public decimal OpeningBalance { get; init; }
    public decimal TotalDebit { get; init; }
    public decimal TotalCredit { get; init; }
    public decimal ClosingBalance { get; init; }
    public int PaymentTermsDays { get; init; }
    public decimal CreditLimit { get; init; }
    public string PaymentTermsDisplay { get; init; } = "";
    public IReadOnlyList<SupplierStatementLineDto> Lines { get; init; } = [];
}

public sealed class SupplierStatementLineDto
{
    public DateTime EntryDate { get; init; }
    public DocumentType DocumentType { get; init; }
    public string DocumentNumber { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
}

public sealed class SupplierInvoiceListDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateTime InvoiceDate { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class SupplierPaymentListDto
{
    public Guid Id { get; init; }
    public string VoucherNumber { get; init; } = "";
    public DateTime VoucherDate { get; init; }
    public decimal Amount { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class SupplierOperationsCenterDto
{
    public SupplierDetailsDto Supplier { get; init; } = null!;
    public decimal PurchasesYtd { get; init; }
    public decimal OutstandingBalance { get; init; }
    public decimal OverdueAmount { get; init; }
    public DateTime? LastTransactionDate { get; init; }
    public int OpenInvoicesCount { get; init; }
    public IReadOnlyList<SupplierInvoiceListDto> RecentInvoices { get; init; } = [];
    public IReadOnlyList<SupplierPaymentListDto> RecentPayments { get; init; } = [];
}

public sealed class SupplierOpeningBalanceResultDto
{
    public string JournalEntryNumber { get; init; } = "";
    public DateTime PostedDate { get; init; }
    public decimal Amount { get; init; }
}
