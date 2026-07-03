using ERPSystem.Domain.Common;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Aggregates;

/// <summary>
/// Aggregate root: Sales return (credit note) that references an approved/delivered
/// invoice. On posting it reverses inventory movement and posts a GL credit note.
/// </summary>
public sealed class SalesReturnAggregate : AggregateRoot
{
    public string ReturnNumber { get; private set; } = "";
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid OriginalInvoiceId { get; private set; }
    public string OriginalInvoiceNumber { get; private set; } = "";
    public Guid CustomerId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public SalesReturnReason Reason { get; private set; }
    public string? ReasonNotes { get; private set; }
    public string? Notes { get; private set; }
    public VoucherStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero();
    public Guid CreatedByUserId { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public string? JournalEntryNumber { get; private set; }

    private readonly List<SalesReturnLine> _lines = [];
    public IReadOnlyList<SalesReturnLine> Lines => _lines.AsReadOnly();

    private SalesReturnAggregate() { }

    public static SalesReturnAggregate CreateDraft(
        string returnNumber,
        Guid companyId,
        Guid branchId,
        Guid originalInvoiceId,
        string originalInvoiceNumber,
        Guid customerId,
        Guid warehouseId,
        DateTime returnDate,
        SalesReturnReason reason,
        string? reasonNotes,
        string? notes,
        Guid createdByUserId,
        IReadOnlyList<SalesReturnLine> lines)
    {
        if (string.IsNullOrWhiteSpace(returnNumber))
            throw new ValidationException("Return number is required.");
        if (originalInvoiceId == Guid.Empty)
            throw new ValidationException("Original invoice is required.");
        if (customerId == Guid.Empty)
            throw new ValidationException("Customer is required.");
        if (warehouseId == Guid.Empty)
            throw new ValidationException("Warehouse is required.");
        if (lines is null || lines.Count == 0)
            throw new ValidationException("At least one return line is required.");

        var aggregate = new SalesReturnAggregate
        {
            ReturnNumber = returnNumber,
            CompanyId = companyId,
            BranchId = branchId,
            OriginalInvoiceId = originalInvoiceId,
            OriginalInvoiceNumber = originalInvoiceNumber,
            CustomerId = customerId,
            WarehouseId = warehouseId,
            ReturnDate = returnDate,
            Reason = reason,
            ReasonNotes = reasonNotes,
            Notes = notes,
            CreatedByUserId = createdByUserId,
            Status = VoucherStatus.Draft
        };
        foreach (var line in lines)
            aggregate._lines.Add(line);
        aggregate.RecalculateTotal();
        return aggregate;
    }

    public void ReplaceLines(IReadOnlyList<SalesReturnLine> lines)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("Only draft returns can be modified.");
        if (lines is null || lines.Count == 0)
            throw new ValidationException("At least one return line is required.");
        _lines.Clear();
        _lines.AddRange(lines);
        RecalculateTotal();
    }

    public void UpdateHeader(SalesReturnReason reason, string? reasonNotes, string? notes)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("Only draft returns can be modified.");
        Reason = reason;
        ReasonNotes = reasonNotes;
        Notes = notes;
    }

    public void Post(Guid postedByUserId, string journalEntryNumber)
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("Only draft returns can be posted.");
        Status = VoucherStatus.Posted;
        PostedByUserId = postedByUserId;
        PostedAt = DateTime.UtcNow;
        JournalEntryNumber = journalEntryNumber;
    }

    public void Cancel()
    {
        if (Status != VoucherStatus.Draft)
            throw new DomainException("Only draft returns can be cancelled.");
        Status = VoucherStatus.Cancelled;
    }

    private void RecalculateTotal()
    {
        var total = _lines.Sum(l => l.LineTotal.Amount);
        TotalAmount = new Money(total);
    }
}

public sealed class SalesReturnLine
{
    public Guid Id { get; private set; }
    public int LineNumber { get; private set; }
    public Guid OriginalInvoiceItemId { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public decimal OriginalMeters { get; private set; }
    public decimal ReturnMeters { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money LineTotal { get; private set; } = Money.Zero();

    private SalesReturnLine() { }

    public static SalesReturnLine Create(
        int lineNumber,
        Guid originalInvoiceItemId,
        Guid fabricItemId,
        Guid fabricColorId,
        decimal originalMeters,
        decimal returnMeters,
        Money unitPrice)
    {
        if (returnMeters <= 0)
            throw new ValidationException("Return quantity must be positive.");
        if (returnMeters > originalMeters + 0.001m)
            throw new ValidationException("Return quantity cannot exceed original quantity.");

        return new SalesReturnLine
        {
            Id = Guid.NewGuid(),
            LineNumber = lineNumber,
            OriginalInvoiceItemId = originalInvoiceItemId,
            FabricItemId = fabricItemId,
            FabricColorId = fabricColorId,
            OriginalMeters = originalMeters,
            ReturnMeters = returnMeters,
            UnitPrice = unitPrice,
            LineTotal = new Money(returnMeters * unitPrice.Amount, unitPrice.Currency)
        };
    }
}

public enum SalesReturnReason
{
    DefectiveGoods = 0,
    WrongOrder = 1,
    CustomerRequest = 2,
    Other = 3
}
