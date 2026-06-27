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
    public SalesInvoiceStatus Status { get; private set; }
    public Money SubTotal { get; private set; } = Money.Zero();
    public Money DiscountTotal { get; private set; } = Money.Zero();
    public Money TaxTotal { get; private set; } = Money.Zero();
    public Money GrandTotal { get; private set; } = Money.Zero();
    public Guid CreatedByUserId { get; private set; }
    public Guid? ApprovedByUserId { get; private set; }
    public DateTime? SentToWarehouseAt { get; private set; }
    public DateTime? DetailedAt { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public DateTime? PrintedAt { get; private set; }
    public DateTime? DeliveredAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? ReversedByJournalEntryId { get; private set; }
    public bool IsArchived { get; private set; }

    private readonly List<SalesInvoiceItem> _items = [];
    private readonly List<SalesInvoiceRollDetail> _rollDetails = [];
    private WarehouseDetailingSession? _detailingSession;

    public IReadOnlyList<SalesInvoiceItem> Items => _items.AsReadOnly();
    public IReadOnlyList<SalesInvoiceRollDetail> RollDetails => _rollDetails.AsReadOnly();
    public WarehouseDetailingSession? DetailingSession => _detailingSession;

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
        if (chinaContainerId == Guid.Empty)
            throw new ValidationException("China container is required for imported fabric sales.");

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

    public void CompleteDetailing()
    {
        EnsureStatus(SalesInvoiceStatus.AwaitingDetailing);
        if (_rollDetails.Any(d => !d.HasValidLength))
            throw new WarehouseDetailingException("All roll lengths must be entered and greater than zero.");

        _detailingSession?.Complete();
        foreach (var item in _items)
            item.RecalculateTotal(_rollDetails);

        SubTotal = _items.Aggregate(Money.Zero(), (sum, i) => sum.Add(i.LineTotal));
        GrandTotal = SubTotal.Add(TaxTotal).Subtract(DiscountTotal);
        Status = SalesInvoiceStatus.Detailed;
        DetailedAt = DateTime.UtcNow;
        Raise(new SalesInvoiceDetailed(Id, InvoiceNumber.Value, GrandTotal.Amount));
    }

    public void MarkReadyForApproval()
    {
        EnsureStatus(SalesInvoiceStatus.Detailed);
        Status = SalesInvoiceStatus.ReadyForApproval;
    }

    public void Approve(Guid approvedByUserId)
    {
        if (Status is not (SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
            throw new InvalidInvoiceWorkflowException("Invoice cannot be approved before detailing is complete.");
        if (_rollDetails.Any(d => !d.HasValidLength))
            throw new InvalidInvoiceWorkflowException("All roll lengths must be valid before approval.");

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

    public void Deliver(string? receivedByName)
    {
        EnsureStatus(SalesInvoiceStatus.Printed, SalesInvoiceStatus.Approved);
        Status = SalesInvoiceStatus.Delivered;
        DeliveredAt = DateTime.UtcNow;
        DeliveryNote.Create($"DN-{InvoiceNumber.Value}", Id, receivedByName);
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
