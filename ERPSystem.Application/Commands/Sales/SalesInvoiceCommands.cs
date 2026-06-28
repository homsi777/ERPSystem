using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Sales;

public sealed class CreateSalesInvoiceDraftCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid ChinaContainerId { get; init; }
    public PaymentType PaymentType { get; init; }
    public IReadOnlyList<SalesInvoiceLineCommand> Lines { get; init; } = [];
}

public sealed class SalesInvoiceLineCommand
{
    public int LineNumber { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal UnitPrice { get; init; }
}

public sealed class UpdateSalesInvoiceDraftCommand
{
    public Guid InvoiceId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid ChinaContainerId { get; init; }
    public PaymentType PaymentType { get; init; }
    public IReadOnlyList<SalesInvoiceLineCommand> Lines { get; init; } = [];
}

public sealed class SendSalesInvoiceToWarehouseCommand
{
    public Guid InvoiceId { get; init; }
}

public sealed class CompleteWarehouseDetailingCommand
{
    public Guid InvoiceId { get; init; }
    public IReadOnlyList<RollLengthEntryCommand> RollEntries { get; init; } = [];
}

public sealed class RollLengthEntryCommand
{
    public Guid RollDetailId { get; init; }
    public decimal LengthMeters { get; init; }
}

public sealed class ApproveSalesInvoiceCommand
{
    public Guid InvoiceId { get; init; }
}

public sealed class CancelSalesInvoiceCommand
{
    public Guid InvoiceId { get; init; }
    public string Reason { get; init; } = "";
}
