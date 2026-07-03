using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Sales;

public class SalesInvoiceItem
{
    public Guid Id { get; private set; }
    public int LineNumber { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public int RollCount { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public string Unit { get; private set; } = "meter";
    public Money LineTotal { get; private set; } = Money.Zero();

    private SalesInvoiceItem() { }

    public static SalesInvoiceItem Create(
        int lineNumber,
        Guid fabricItemId,
        Guid fabricColorId,
        int rollCount,
        Money unitPrice) => new()
    {
        Id = Guid.NewGuid(),
        LineNumber = lineNumber,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        RollCount = rollCount,
        UnitPrice = unitPrice
    };

    public void RecalculateTotal(IReadOnlyList<SalesInvoiceRollDetail> rollDetails)
    {
        var lineDetails = rollDetails.Where(d => d.SalesInvoiceItemId == Id).ToList();
        var total = lineDetails.Sum(d => d.LengthMeters.Value * UnitPrice.Amount);
        LineTotal = new Money(total, UnitPrice.Currency);
    }
}

public class SalesInvoiceRollDetail
{
    public Guid Id { get; private set; }
    public Guid SalesInvoiceItemId { get; private set; }
    public RollNumber RollSequence { get; private set; } = null!;
    public Guid? FabricRollId { get; private set; }
    public LengthInMeters LengthMeters { get; private set; } = LengthInMeters.Zero;
    public Guid? EnteredByUserId { get; private set; }
    public DateTime? EnteredAt { get; private set; }

    private SalesInvoiceRollDetail() { }

    public static SalesInvoiceRollDetail Create(Guid itemId, RollNumber rollSequence) => new()
    {
        Id = Guid.NewGuid(),
        SalesInvoiceItemId = itemId,
        RollSequence = rollSequence
    };

    public void EnterLength(LengthInMeters length, Guid userId)
    {
        LengthMeters = length;
        EnteredByUserId = userId;
        EnteredAt = DateTime.UtcNow;
    }

    public void AssignFabricRoll(Guid fabricRollId) => FabricRollId = fabricRollId;

    public bool HasValidLength => LengthMeters.Value > 0;
}

public class WarehouseDetailingSession
{
    public Guid Id { get; private set; }
    public Guid SalesInvoiceId { get; private set; }
    public WarehouseDetailingStatus Status { get; private set; }
    public Guid? AssignedOfficerUserId { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? RejectionReason { get; private set; }

    private WarehouseDetailingSession() { }

    public static WarehouseDetailingSession CreatePending(Guid salesInvoiceId) => new()
    {
        Id = Guid.NewGuid(),
        SalesInvoiceId = salesInvoiceId,
        Status = WarehouseDetailingStatus.Pending
    };

    public void Start(Guid officerUserId)
    {
        Status = WarehouseDetailingStatus.InProgress;
        AssignedOfficerUserId = officerUserId;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete()
    {
        Status = WarehouseDetailingStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    public void Reject(string reason)
    {
        Status = WarehouseDetailingStatus.Rejected;
        RejectionReason = reason;
    }
}

public class SalesReturn
{
    public Guid Id { get; private set; }
    public string ReturnNumber { get; private set; } = "";
    public Guid OriginalInvoiceId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public VoucherStatus Status { get; private set; }

    private SalesReturn() { }

    public static SalesReturn Create(string returnNumber, Guid originalInvoiceId, Money amount) => new()
    {
        Id = Guid.NewGuid(),
        ReturnNumber = returnNumber,
        OriginalInvoiceId = originalInvoiceId,
        Amount = amount,
        Status = VoucherStatus.Draft
    };
}

public class DeliveryNote
{
    public Guid Id { get; private set; }
    public string DeliveryNumber { get; private set; } = "";
    public Guid SalesInvoiceId { get; private set; }
    public DateTime DeliveredAt { get; private set; }
    public string? ReceivedByName { get; private set; }

    private DeliveryNote() { }

    public static DeliveryNote Create(string deliveryNumber, Guid salesInvoiceId, string? receivedBy) => new()
    {
        Id = Guid.NewGuid(),
        DeliveryNumber = deliveryNumber,
        SalesInvoiceId = salesInvoiceId,
        DeliveredAt = DateTime.UtcNow,
        ReceivedByName = receivedBy
    };
}
