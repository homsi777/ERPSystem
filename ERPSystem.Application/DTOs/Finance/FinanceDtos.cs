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
