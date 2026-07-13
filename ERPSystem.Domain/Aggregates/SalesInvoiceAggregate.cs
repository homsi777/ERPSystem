using ERPSystem.Domain.Common;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Events.Sales;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Aggregates;

/// <summary>
/// Aggregate root: Sales invoice with items, roll details, and detailing session.
/// </summary>
public sealed class SalesInvoiceAggregate : AggregateRoot
{
    public InvoiceNumber InvoiceNumber { get; private set; } = null!;
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public Guid ChinaContainerId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public PaymentType PaymentType { get; private set; }
    public Money? PartialPaymentAmount { get; private set; }
    public Guid? CashboxId { get; private set; }
    public SalesInvoiceStatus Status { get; private set; }
    public Money SubTotal { get; private set; } = Money.Zero();
    public Money DiscountTotal { get; private set; } = Money.Zero();
    public Money TaxTotal { get; private set; } = Money.Zero();
    public Money GrandTotal { get; private set; } = Money.Zero();
    public decimal RoundingDifference { get; private set; }
    public bool IsLegacyUntaxed { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? SentToWarehouseAt { get; private set; }
    public DateTime? DetailedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? PrintedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public string? DeliveredToName { get; private set; }
    public string? DeliveryDriverName { get; private set; }
    public string? DeliveryNotes { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? ReversedByJournalEntryId { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<SalesInvoiceItem> _items = [];
    private readonly List<SalesInvoiceRollDetail> _rollDetails = [];
    private readonly List<SalesInvoiceItemTaxSnapshot> _itemTaxSnapshots = [];
    private WarehouseDetailingSession? _detailingSession;

    public IReadOnlyList<SalesInvoiceItem> Items => _items.AsReadOnly();
    public IReadOnlyList<SalesInvoiceRollDetail> RollDetails => _rollDetails.AsReadOnly();
    public IReadOnlyList<SalesInvoiceItemTaxSnapshot> ItemTaxSnapshots => _itemTaxSnapshots.AsReadOnly();
    public WarehouseDetailingSession? DetailingSession => _detailingSession;

    /// <summary>Total contra-revenue sales discount across all lines (gross price − applied price).</summary>
    public Money TotalLineDiscount =>
        _items.Aggregate(Money.Zero(), (sum, item) => sum.Add(item.DiscountAmount));

    private SalesInvoiceAggregate() { }

    public static SalesInvoiceAggregate CreateDraft(
        InvoiceNumber invoiceNumber,
        Guid companyId,
        Guid branchId,
        Guid customerId,
        Guid warehouseId,
        Guid chinaContainerId,
        PaymentType paymentType,
        Guid createdByUserId,
        DateTime? invoiceDate = null)
    {
        if (warehouseId == Guid.Empty)
            throw new ValidationException("Warehouse is required.");
        var aggregate = new SalesInvoiceAggregate
        {
            InvoiceNumber = invoiceNumber,
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            ChinaContainerId = chinaContainerId,
            PaymentType = paymentType,
            CreatedByUserId = createdByUserId,
            InvoiceDate = invoiceDate ?? DateTime.UtcNow,
            Status = SalesInvoiceStatus.Draft
        };
        aggregate.Raise(new SalesInvoiceCreated(aggregate.Id, invoiceNumber.Value));
        return aggregate;
    }

    public void AddItem(SalesInvoiceItem item)
    {
        EnsureEditable();
        _items.Add(item);
        foreach (var seq in Enumerable.Range(1, item.RollCount))
            _rollDetails.Add(SalesInvoiceRollDetail.Create(item.Id, new RollNumber(seq)));
    }

    public void UpdateDraftHeader(
        Guid customerId,
        Guid warehouseId,
        Guid chinaContainerId,
        PaymentType paymentType,
        Guid? cashboxId = null)
    {
        EnsureEditable();
        if (customerId == Guid.Empty)
            throw new ValidationException("Customer is required.");
        if (warehouseId == Guid.Empty)
            throw new ValidationException("Warehouse is required.");
        CustomerId = customerId;
        WarehouseId = warehouseId;
        ChinaContainerId = chinaContainerId;
        PaymentType = paymentType;
        SetCashboxId(cashboxId);
        if (paymentType == PaymentType.Cash)
            PartialPaymentAmount = null;
    }

    public void SetCashboxId(Guid? cashboxId)
    {
        EnsureEditable();
        CashboxId = cashboxId is Guid id && id != Guid.Empty ? id : null;
    }

    public void SetPartialPaymentAmount(Money? amount)
    {
        EnsureEditable();
        if (PaymentType == PaymentType.Cash)
        {
            PartialPaymentAmount = null;
            return;
        }

        if (amount is not null && amount.Amount < 0)
            throw new ValidationException("Partial payment cannot be negative.");

        if (amount is not null && GrandTotal.Amount > 0 && amount.Amount > GrandTotal.Amount)
            throw new ValidationException("Partial payment cannot exceed invoice total.");

        PartialPaymentAmount = amount is null || amount.Amount == 0 ? null : amount;
    }

    public void ReplaceDraftLines(IReadOnlyList<SalesInvoiceItem> lines)
    {
        EnsureEditable();
        if (lines.Count == 0)
            throw new ValidationException("At least one line item is required.");

        _items.Clear();
        _rollDetails.Clear();
        foreach (var item in lines.OrderBy(l => l.LineNumber))
            AddItem(item);
    }

    public void SendToWarehouse()
    {
        EnsureStatus(SalesInvoiceStatus.Draft);
        if (_items.Count == 0)
            throw new InvalidInvoiceWorkflowException("Invoice must have at least one line item.");
        Status = SalesInvoiceStatus.AwaitingDetailing;
        SentToWarehouseAt = DateTime.UtcNow;
        _detailingSession = WarehouseDetailingSession.CreatePending(Id);
        Raise(new SalesInvoiceSentToWarehouse(Id, InvoiceNumber.Value));
    }

    public void StartDetailing(Guid officerUserId)
    {
        EnsureStatus(SalesInvoiceStatus.AwaitingDetailing);
        _detailingSession ??= WarehouseDetailingSession.CreatePending(Id);
        _detailingSession.Start(officerUserId);
    }

    public void EnterRollLength(Guid rollDetailId, LengthInMeters length, Guid userId)
    {
        EnsureStatus(SalesInvoiceStatus.AwaitingDetailing);
        var detail = _rollDetails.FirstOrDefault(d => d.Id == rollDetailId)
            ?? throw new WarehouseDetailingException("Roll detail not found.");
        detail.EnterLength(length, userId);
    }

    /// <summary>
    /// Persists partial detailing progress (some or all roll lines) without requiring every
    /// line to be complete and without changing invoice status away from AwaitingDetailing.
    /// This is a parallel, non-authoritative save — the final resolver/completion gate in
    /// <see cref="CompleteDetailing"/> is unaffected. The first draft save also transitions the
    /// detailing session from Pending to InProgress, since there is now something "in progress".
    /// </summary>
    public void SaveDetailingDraft(
        IReadOnlyList<(Guid RollDetailId, int? RollNumber, decimal? LengthMeters)> entries,
        Guid officerUserId)
    {
        EnsureStatus(SalesInvoiceStatus.AwaitingDetailing);

        foreach (var entry in entries)
        {
            var detail = _rollDetails.FirstOrDefault(d => d.Id == entry.RollDetailId)
                ?? throw new WarehouseDetailingException("Roll detail not found.");
            detail.SaveDraft(entry.RollNumber, entry.LengthMeters);
        }

        _detailingSession ??= WarehouseDetailingSession.CreatePending(Id);
        if (_detailingSession.Status == WarehouseDetailingStatus.Pending)
            _detailingSession.Start(officerUserId);
    }

    public void CompleteDetailing()
    {
        EnsureStatus(SalesInvoiceStatus.AwaitingDetailing);
        if (_rollDetails.Any(d => !d.HasValidLength))
            throw new WarehouseDetailingException("All roll lengths must be entered and greater than zero.");

        _detailingSession?.Complete();
        foreach (var item in _items)
            item.RecalculateTotal(_rollDetails);

        SubTotal = _items.Aggregate(Money.Zero(), (sum, i) => sum.Add(i.LineTotal));
        RecalculateGrandTotal();
        Status = SalesInvoiceStatus.Detailed;
        DetailedAt = DateTime.UtcNow;
        Raise(new SalesInvoiceDetailed(Id, InvoiceNumber.Value, GrandTotal.Amount));
    }

    public void MarkReadyForApproval()
    {
        EnsureStatus(SalesInvoiceStatus.Detailed);
        Status = SalesInvoiceStatus.ReadyForApproval;
    }

    public void ApplyTaxTotals(
        Money taxTotal,
        Money grandTotal,
        decimal roundingDifference,
        IReadOnlyList<SalesInvoiceItemTaxSnapshot> snapshots,
        bool freezeSnapshots)
    {
        EnsureTaxEditable();
        TaxTotal = taxTotal;
        GrandTotal = grandTotal;
        RoundingDifference = roundingDifference;
        _itemTaxSnapshots.Clear();
        _itemTaxSnapshots.AddRange(snapshots);
        if (freezeSnapshots)
            FreezeTaxSnapshots();
    }

    public void FreezeTaxSnapshots()
    {
        foreach (var snapshot in _itemTaxSnapshots)
            snapshot.Freeze();
    }

    public void MarkLegacyUntaxed() => IsLegacyUntaxed = true;

    public void Approve(Guid approvedByUserId)
    {
        if (Status is not (SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
            throw new InvalidInvoiceWorkflowException("Invoice cannot be approved before detailing is complete.");
        if (_rollDetails.Any(d => !d.HasValidLength))
            throw new InvalidInvoiceWorkflowException("All roll lengths must be valid before approval.");
        if (_itemTaxSnapshots.Any(s => s.IsFrozen is false && TaxTotal.Amount > 0))
            FreezeTaxSnapshots();

        Status = SalesInvoiceStatus.Approved;
        ApprovedByUserId = approvedByUserId;
        ApprovedAt = DateTime.UtcNow;
        Raise(new SalesInvoiceApproved(Id, InvoiceNumber.Value, CustomerId, GrandTotal.Amount));
    }

    public void Print()
    {
        EnsureStatus(SalesInvoiceStatus.Approved, SalesInvoiceStatus.Printed);
        if (Status == SalesInvoiceStatus.Approved)
            Status = SalesInvoiceStatus.Printed;
        PrintedAt = DateTime.UtcNow;
        Raise(new SalesInvoicePrinted(Id, InvoiceNumber.Value));
    }

    public void Deliver(string? receivedByName, DateTime? deliveredAt = null, string? driverName = null, string? notes = null)
    {
        EnsureStatus(SalesInvoiceStatus.Printed, SalesInvoiceStatus.Approved);
        Status = SalesInvoiceStatus.Delivered;
        DeliveredAt = NormalizeUtc(deliveredAt) ?? DateTime.UtcNow;
        DeliveredToName = receivedByName;
        DeliveryDriverName = driverName;
        DeliveryNotes = notes;
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null) return null;
        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value.Value.Date, DateTimeKind.Utc)
                .Add(value.Value.TimeOfDay)
        };
    }

    public void UpdateWarehouse(Guid warehouseId)
    {
        if (Status != SalesInvoiceStatus.Draft)
            throw new InvalidInvoiceWorkflowException("Warehouse can only be changed on draft invoices.");
        if (warehouseId == Guid.Empty)
            throw new ValidationException("Warehouse is required.");
        WarehouseId = warehouseId;
    }

    public void SetDiscountTotal(Money discount)
    {
        if (Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed
            or SalesInvoiceStatus.Delivered or SalesInvoiceStatus.Cancelled
            or SalesInvoiceStatus.Returned or SalesInvoiceStatus.PartiallyReturned)
            throw new InvalidInvoiceWorkflowException("Cannot change discount after the invoice is approved or closed.");

        if (discount.Amount < 0)
            throw new ValidationException("Discount amount cannot be negative.");

        DiscountTotal = discount;
        RecalculateGrandTotal();
    }

    private void RecalculateGrandTotal()
    {
        if (SubTotal.Amount <= 0)
            return;

        GrandTotal = SubTotal.Add(TaxTotal).Subtract(DiscountTotal);
        if (GrandTotal.Amount < 0)
            throw new ValidationException("Discount amount exceeds invoice subtotal.");
    }

    private void EnsureTaxEditable()
    {
        if (_itemTaxSnapshots.Any(s => s.IsFrozen))
            throw new InvalidInvoiceWorkflowException("Tax amounts are frozen on posted invoices.");
    }

    public void ApplyReturn(bool isFullyReturned)
    {
        if (Status is SalesInvoiceStatus.Cancelled or SalesInvoiceStatus.Draft)
            throw new InvalidInvoiceWorkflowException("Cannot apply return to a draft or cancelled invoice.");
        Status = isFullyReturned ? SalesInvoiceStatus.Returned : SalesInvoiceStatus.PartiallyReturned;
    }

    public void Cancel(string reason)
    {
        if (Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered)
            throw new InvalidInvoiceWorkflowException("Posted invoices must be reversed, not cancelled.");
        Status = SalesInvoiceStatus.Cancelled;
        CancelReason = reason;
        CancelledAt = DateTime.UtcNow;
    }

    public LengthInMeters TotalSoldMeters() =>
        _rollDetails.Aggregate(LengthInMeters.Zero, (sum, d) => sum.Add(d.LengthMeters));

    public void AssignFabricRollToDetail(Guid rollDetailId, Guid fabricRollId)
    {
        var detail = _rollDetails.FirstOrDefault(d => d.Id == rollDetailId)
            ?? throw new WarehouseDetailingException("Roll detail not found.");
        detail.AssignFabricRoll(fabricRollId);
    }

    private void EnsureEditable()
    {
        if (Status != SalesInvoiceStatus.Draft)
            throw new InvalidInvoiceWorkflowException("Only draft invoices can be edited.");
    }

    private void EnsureStatus(params SalesInvoiceStatus[] allowed)
    {
        if (!allowed.Contains(Status))
            throw new InvalidInvoiceWorkflowException(
                $"Operation not allowed in status '{Status}'. Allowed: {string.Join(", ", allowed)}.");
    }
}
