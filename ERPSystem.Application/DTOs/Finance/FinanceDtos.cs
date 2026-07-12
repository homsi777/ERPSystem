using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Finance;

public sealed class ReceiptVoucherDto
{
    public Guid Id { get; init; }
    public string VoucherNumber { get; init; } = "";
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public Guid CashboxId { get; init; }
    public decimal Amount { get; init; }
    public DateTime VoucherDate { get; init; }
    public VoucherStatus Status { get; init; }
}

public sealed class PaymentVoucherDto
{
    public Guid Id { get; init; }
    public string VoucherNumber { get; init; } = "";
    public Guid SupplierId { get; init; }
    public string SupplierName { get; init; } = "";
    public Guid CashboxId { get; init; }
    public decimal Amount { get; init; }
    public DateTime VoucherDate { get; init; }
    public VoucherStatus Status { get; init; }
}

/// <summary>Enriched read model for printing a receipt voucher — every field traces to a real stored record (voucher, cashbox, payment method, allocated invoices). No free-text fields are fabricated.</summary>
public sealed class ReceiptVoucherPrintDto
{
    public Guid Id { get; init; }
    public string VoucherNumber { get; init; } = "";
    public DateTime VoucherDate { get; init; }
    public VoucherStatus Status { get; init; }
    public string CustomerName { get; init; } = "";
    public string? CustomerPhone { get; init; }
    public string CashboxName { get; init; } = "";
    public string Currency { get; init; } = "USD";
    public decimal Amount { get; init; }
    public string PaymentMethodName { get; init; } = "";
    public IReadOnlyList<ReceiptVoucherAllocationDto> Allocations { get; init; } = [];
}

public sealed class ReceiptVoucherAllocationDto
{
    public string InvoiceNumber { get; init; } = "";
    public decimal Amount { get; init; }
}

/// <summary>Enriched read model for printing a payment voucher — every field traces to a real stored record (voucher, cashbox, supplier).</summary>
public sealed class PaymentVoucherPrintDto
{
    public Guid Id { get; init; }
    public string VoucherNumber { get; init; } = "";
    public DateTime VoucherDate { get; init; }
    public VoucherStatus Status { get; init; }
    public string SupplierName { get; init; } = "";
    public string CashboxName { get; init; } = "";
    public string Currency { get; init; } = "USD";
    public decimal Amount { get; init; }
}

public sealed class JournalEntryDto
{
    public Guid Id { get; init; }
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public JournalEntryStatus Status { get; init; }
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public IReadOnlyList<JournalEntryLineDto> Lines { get; init; } = [];
}

public sealed class JournalEntryLineDto
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string Narrative { get; init; } = "";
}

public sealed class CashboxListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public Guid? AccountId { get; init; }
    public string BalanceDisplay => $"{Balance:N2} {Currency}";
    public string StatusDisplay => IsActive ? "نشط" : "معطل";
}

public sealed class CashboxDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "USD";
    public bool IsActive { get; init; }
    public decimal TodayReceipts { get; init; }
    public decimal TodayPayments { get; init; }
}

public sealed class CashboxMovementDto
{
    public DateTime MovementDate { get; init; }
    public string ReferenceType { get; init; } = "";
    public string ReferenceNumber { get; init; } = "";
    public string Description { get; init; } = "";
    public decimal Amount { get; init; }
    public bool IsInbound { get; init; }
    public string DirectionDisplay => IsInbound ? "وارد" : "صادر";
}

public sealed class CashboxTransferListDto
{
    public Guid Id { get; init; }
    public string TransferNumber { get; init; } = "";
    public string FromCashboxName { get; init; } = "";
    public string ToCashboxName { get; init; } = "";
    public DateTime TransferDate { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public VoucherStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class CashboxOperationsCenterDto
{
    public CashboxDetailsDto Cashbox { get; init; } = null!;
    public IReadOnlyList<CashboxMovementDto> RecentMovements { get; init; } = [];
    public IReadOnlyList<CashboxTransferListDto> RecentTransfers { get; init; } = [];
}

public sealed class ReceiptTenderLineDto
{
    public Guid PaymentMethodId { get; init; }
    public Guid? CashboxId { get; init; }
    public Guid? BankAccountId { get; init; }
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public string? Reference { get; init; }
    public string? ChequeNumber { get; init; }
    public DateTime? ChequeDate { get; init; }
    public string? CardReference { get; init; }
}

public sealed class PaymentMethodDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public PaymentMethodKind Kind { get; init; }
    public bool RequiresCashbox { get; init; }
    public bool RequiresBankAccount { get; init; }
    public bool RequiresReference { get; init; }
}

public sealed class BankAccountListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string BankName { get; init; } = "";
    public Guid GlAccountId { get; init; }
    public string Currency { get; init; } = "USD";
    public bool IsActive { get; init; }
}

public sealed class CashboxBalanceReportDto
{
    public Guid CashboxId { get; init; }
    public string CashboxCode { get; init; } = "";
    public string CashboxName { get; init; } = "";
    public Guid? AccountId { get; init; }
    public decimal OpeningBalance { get; init; }
    public decimal PostedReceipts { get; init; }
    public decimal PostedPayments { get; init; }
    public decimal Reversals { get; init; }
    public decimal GlBalance { get; init; }
    public decimal OperationalBalance { get; init; }
    public decimal Difference { get; init; }
}

public sealed class CashboxReconciliationRowDto
{
    public Guid CashboxId { get; init; }
    public string CashboxCode { get; init; } = "";
    public string CashboxName { get; init; } = "";
    public Guid? CashboxAccountId { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal OperationalBalance { get; init; }
    public decimal? GlBalance { get; init; }
    public decimal? Difference { get; init; }
    public string Classification { get; init; } = "";
    public string? LastTransaction { get; init; }
    public int UnpostedVoucherCount { get; init; }
    public int ReversedVoucherCount { get; init; }
}
