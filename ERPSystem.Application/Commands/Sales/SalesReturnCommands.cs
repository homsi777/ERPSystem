using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Sales;

public sealed class CreateSalesReturnCommand
{
    public Guid OriginalInvoiceId { get; init; }
    public DateTime ReturnDate { get; init; }
    public SalesReturnReason Reason { get; init; }
    public string? ReasonNotes { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<SalesReturnLineCommand> Lines { get; init; } = [];
}

public sealed class UpdateSalesReturnCommand
{
    public Guid ReturnId { get; init; }
    public SalesReturnReason Reason { get; init; }
    public string? ReasonNotes { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<SalesReturnLineCommand> Lines { get; init; } = [];
}

public sealed class SalesReturnLineCommand
{
    public int LineNumber { get; init; }
    public Guid OriginalInvoiceItemId { get; init; }
    public decimal ReturnMeters { get; init; }
}

public sealed class PostSalesReturnCommand
{
    public Guid ReturnId { get; init; }
}

public sealed class CancelSalesReturnCommand
{
    public Guid ReturnId { get; init; }
}
