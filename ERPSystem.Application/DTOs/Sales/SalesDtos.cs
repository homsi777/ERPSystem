using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Sales;

public sealed class SalesInvoiceDto
{
    public Guid Id { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public SalesInvoiceStatus Status { get; init; }
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public Guid WarehouseId { get; init; }
    public Guid ChinaContainerId { get; init; }
    public DateTime InvoiceDate { get; init; }
    public PaymentType PaymentType { get; init; }
    public decimal PartialPaymentAmount { get; init; }
    public Guid? CashboxId { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal TaxTotal { get; init; }
    public decimal GrandTotal { get; init; }
    public DateTime? SentToWarehouseAt { get; init; }
    public DateTime? DetailedAt { get; init; }
    public DateTime? ApprovedAt { get; init; }
    public DateTime? PrintedAt { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string? DeliveredToName { get; init; }
    public string? DeliveryDriverName { get; init; }
    public string? DeliveryNotes { get; init; }
    public string? CancelReason { get; init; }
    public IReadOnlyList<SalesInvoiceLineDto> Lines { get; init; } = [];
}

public sealed class SalesInvoiceLineDto
{
    public Guid Id { get; init; }
    public int LineNumber { get; init; }
    public Guid ChinaContainerId { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string FabricDisplayName { get; init; } = "";
    public string FabricCode { get; init; } = "";
    public string ColorDisplayName { get; init; } = "";
    public int RollCount { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal OriginalUnitPrice { get; init; }
    public decimal TotalLengthMeters { get; init; }
    public decimal LineTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public string? DiscountReason { get; init; }
    public string? Notes { get; init; }
}

public sealed class SalesInvoiceBelowCostLineDto
{
    public int LineNumber { get; init; }
    public string FabricDisplayName { get; init; } = "";
    public string ColorDisplayName { get; init; } = "";
    public decimal AppliedPrice { get; init; }
    public decimal CostPerMeter { get; init; }
    public decimal Meters { get; init; }
    public decimal LossPerMeter => CostPerMeter - AppliedPrice;
    public decimal TotalLoss => LossPerMeter * Meters;
}

public sealed class WarehouseDetailingDto
{
    public Guid InvoiceId { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public string CustomerName { get; init; } = "";
    public Guid WarehouseId { get; init; }
    public Guid ChinaContainerId { get; init; }
    public DateTime? SentToWarehouseAt { get; init; }
    public decimal? RepresentativeUnitPrice { get; init; }
    public WarehouseDetailingStatus Status { get; init; }
    public IReadOnlyList<WarehouseDetailingRollDto> Rolls { get; init; } = [];
}

public sealed class WarehouseDetailingRollDto
{
    public Guid RollDetailId { get; init; }
    public Guid SalesInvoiceItemId { get; init; }
    public int RollSequence { get; init; }
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string FabricDisplayName { get; init; } = "";
    public string FabricCode { get; init; } = "";
    public string ColorDisplayName { get; init; } = "";
    public decimal LengthMeters { get; init; }
    public bool HasValidLength { get; init; }

    /// <summary>
    /// The invoice LINE's own container (from the parent SalesInvoiceItem), which may differ from
    /// the invoice header's primary container on multi-container invoices. Additive — the header
    /// ChinaContainerId on <see cref="WarehouseDetailingDto"/> is unchanged and still primary/display.
    /// </summary>
    public Guid ChinaContainerId { get; init; }
    public string ContainerDisplay { get; init; } = "";

    /// <summary>Unresolved partial-save values (see Part 4) for pre-populating the UI on revisit.</summary>
    public int? DraftRollNumber { get; init; }
    public decimal? DraftLengthMeters { get; init; }
}

public sealed class SalesWarehouseStockOptionDto
{
    public Guid FabricItemId { get; init; }
    public Guid FabricColorId { get; init; }
    public string FabricDisplayName { get; init; } = "";
    public string FabricCode { get; init; } = "";
    public string ColorDisplayName { get; init; } = "";
    public int AvailableRollCount { get; init; }
    public decimal AvailableMeters { get; init; }
    public decimal? SalePricePerMeter { get; init; }

    public string Display =>
        $"{FabricDisplayName} / {ColorDisplayName} — {AvailableRollCount} توب — {AvailableMeters:N1} م";
}

public sealed class SalesInvoiceOperationsCenterDto
{
    public SalesInvoiceDto Invoice { get; init; } = null!;
    public WarehouseDetailingDto? Detailing { get; init; }
    public bool CanSendToWarehouse { get; init; }
    public bool CanCompleteDetailing { get; init; }
    public bool CanApprove { get; init; }
    public bool CanCancel { get; init; }
    public IReadOnlyList<JournalEntryDto> JournalEntries { get; init; } = [];
    public IReadOnlyList<ReceiptInvoicePaymentDto> Payments { get; init; } = [];
    public decimal CollectedAmount { get; init; }
    public decimal RemainingBalance { get; init; }
    public IReadOnlyList<SalesReturnDto> Returns { get; init; } = [];
    public string? WarehouseName { get; init; }
    public string? CustomerPhone { get; init; }
}
