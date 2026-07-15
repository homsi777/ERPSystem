using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Customers;

public sealed class CustomerListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerType Type { get; init; }
    public CustomerStatus Status { get; init; }
    public decimal Balance { get; init; }
    public decimal CreditLimit { get; init; }
    public bool CreditLimitEnabled { get; init; }
    public bool IsActive { get; init; }
    public bool OpeningBalancePosted { get; init; }

    /// <summary>Posted + approved (not yet posted) customer-receivable opening balance.</summary>
    public decimal OpeningBalanceAmount { get; init; }

    /// <summary>Approved opening balance awaiting GL post.</summary>
    public decimal PendingOpeningBalanceAmount { get; init; }

    /// <summary>Sum of approved+ sales invoices.</summary>
    public decimal TotalInvoiced { get; init; }

    /// <summary>Sum of posted receipt vouchers.</summary>
    public decimal TotalReceipts { get; init; }

    public int PostedReceiptCount { get; init; }
    public int OpenInvoicesCount { get; init; }

    /// <summary>Opening + invoiced − receipts (matches account statement logic).</summary>
    public decimal ComputedBalance { get; init; }

    public DateTime? LastReceiptDate { get; init; }
}

/// <summary>Batch financial aggregates for customer list rows.</summary>
public sealed record CustomerListFinancialSummary(
    decimal PostedOpeningBalanceAmount,
    decimal PendingOpeningBalanceAmount,
    decimal TotalInvoiced,
    int InvoiceCount,
    decimal TotalReceipts,
    int PostedReceiptCount,
    int OpenInvoicesCount,
    DateTime? LastReceiptDate)
{
    public decimal OpeningBalanceAmount => PostedOpeningBalanceAmount + PendingOpeningBalanceAmount;
}

public sealed class CustomerOpeningBalanceResultDto
{
    public string JournalEntryNumber { get; init; } = "";
    public DateTime PostedDate { get; init; }
    public decimal Amount { get; init; }
}

public sealed class CustomerDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerType Type { get; init; }
    public CustomerStatus Status { get; init; }
    public decimal Balance { get; init; }
    public decimal CreditLimit { get; init; }
    public bool CreditLimitEnabled { get; init; }
    public int PaymentTermsDays { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
    public bool OpeningBalancePosted { get; init; }
    public decimal OpeningBalanceAmount { get; init; }
    public decimal PendingOpeningBalanceAmount { get; init; }
    public decimal TotalInvoiced { get; init; }
    public decimal TotalReceipts { get; init; }
    public int PostedReceiptCount { get; init; }
    public int OpenInvoicesCount { get; init; }
    public decimal ComputedBalance { get; init; }
    public DateTime? LastReceiptDate { get; init; }
}

public sealed class CustomerStatementDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public IReadOnlyList<CustomerStatementLineDto> Lines { get; init; } = [];
}

public sealed class CustomerStatementLineDto
{
    public DateTime EntryDate { get; init; }
    public DocumentType DocumentType { get; init; }
    public string DocumentNumber { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
}

public sealed class CustomerOperationsCenterDto
{
    public CustomerDetailsDto Customer { get; init; } = null!;
    public int OpenInvoicesCount { get; init; }
    public decimal TotalOutstanding { get; init; }
    public int PendingReceiptsCount { get; init; }
}

public sealed class CustomerSalesDetailDto
{
    public DateTime SaleDate { get; init; }
    public string FabricName { get; init; } = "";
    public string FabricCode { get; init; } = "";
    public string ColorName { get; init; } = "";
    public decimal UnitPrice { get; init; }
}
