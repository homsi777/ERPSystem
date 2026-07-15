using ERPSystem.Application.Common;
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
    public Guid? CashboxId { get; init; }
    public IReadOnlyList<SalesInvoiceLineCommand> Lines { get; init; } = [];
}

public sealed class SalesInvoiceLineCommand
{
    public int LineNumber { get; init; }
    public Guid ChinaContainerId { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal UnitPrice { get; init; }

    /// <summary>Catalog (card) price shown to the user before any manual override.</summary>
    public decimal OriginalUnitPrice { get; init; }
    /// <summary>Length unit for this line: "meter" or "yard" (from container DPL).</summary>
    public string Unit { get; init; } = SaleLengthUnitHelper.MeterStorage;
    public string? DiscountReason { get; init; }
    public Guid? TaxCodeId { get; init; }
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
    public Guid? CashboxId { get; init; }
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
    /// <summary>Optional DPL/inventory roll serial. When set, length is resolved from the fabric roll.</summary>
    public int? RollNumber { get; init; }
    /// <summary>Manual length in meters. Required when <see cref="RollNumber"/> is not provided.</summary>
    public decimal LengthMeters { get; init; }
}

/// <summary>
/// Persists partial detailing progress (whatever the employee has typed so far) without requiring
/// every roll line to be complete and without changing invoice status. Distinct from
/// <see cref="CompleteWarehouseDetailingCommand"/>, which remains the sole all-or-nothing gate
/// before status becomes Detailed.
/// </summary>
public sealed class SaveWarehouseDetailingDraftCommand
{
    public Guid InvoiceId { get; init; }
    public IReadOnlyList<RollDraftEntryCommand> RollEntries { get; init; } = [];
}

public sealed class RollDraftEntryCommand
{
    public Guid RollDetailId { get; init; }
    public int? RollNumber { get; init; }
    public decimal? LengthMeters { get; init; }
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
