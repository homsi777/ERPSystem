using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Sales;

public sealed class CreateSalesInvoiceDraftCommand
{
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public string? InvoiceNumber { get; init; }
    public Guid CustomerId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid ChinaContainerId { get; init; }
    public PaymentType PaymentType { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal? PartialPaymentAmount { get; init; }
    public IReadOnlyList<SalesInvoiceLineCommand> Lines { get; init; } = [];
}

public sealed class SalesInvoiceLineCommand
{
    public int LineNumber { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal UnitPrice { get; init; }

    /// <summary>Catalog (card) price shown to the user before any manual override.</summary>
    public decimal OriginalUnitPrice { get; init; }

    public string? DiscountReason { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateSalesInvoiceDraftCommand
{
    public Guid InvoiceId { get; init; }
    public Guid CustomerId { get; init; }
    public Guid WarehouseId { get; init; }
    public Guid ChinaContainerId { get; init; }
    public PaymentType PaymentType { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal? PartialPaymentAmount { get; init; }
    public IReadOnlyList<SalesInvoiceLineCommand> Lines { get; init; } = [];
}

public sealed class UpdateSalesInvoiceDiscountCommand
{
    public Guid InvoiceId { get; init; }
    public decimal DiscountAmount { get; init; }
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

public sealed class ConfirmSalesInvoiceDeliveryCommand
{
    public Guid InvoiceId { get; init; }
    public DateTime DeliveryDate { get; init; }
    public string? ReceivedByName { get; init; }
    public string? DriverName { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateSalesInvoiceWarehouseCommand
{
    public Guid InvoiceId { get; init; }
    public Guid WarehouseId { get; init; }
}
