using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Sales;

public sealed class SalesReturnDto
{
    public Guid Id { get; init; }
    public string ReturnNumber { get; init; } = "";
    public Guid OriginalInvoiceId { get; init; }
    public string OriginalInvoiceNumber { get; init; } = "";
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public Guid WarehouseId { get; init; }
    public DateTime ReturnDate { get; init; }
    public SalesReturnReason Reason { get; init; }
    public string? ReasonNotes { get; init; }
    public string? Notes { get; init; }
    public VoucherStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public string? JournalEntryNumber { get; init; }
    public DateTime? PostedAt { get; init; }
    public IReadOnlyList<SalesReturnLineDto> Lines { get; init; } = [];
}

public sealed class SalesReturnLineDto
{
    public Guid Id { get; init; }
    public int LineNumber { get; init; }
    public Guid OriginalInvoiceItemId { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string FabricDisplayName { get; init; } = "";
    public string ColorDisplayName { get; init; } = "";
    public decimal OriginalMeters { get; init; }
    public decimal ReturnMeters { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed class ReceiptInvoicePaymentDto
{
    public Guid SalesInvoiceId { get; init; }
    public Guid ReceiptVoucherId { get; init; }
    public string ReceiptNumber { get; init; } = "";
    public decimal Amount { get; init; }
    public DateTime AppliedAt { get; init; }
}
