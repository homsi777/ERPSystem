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
