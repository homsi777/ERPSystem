using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Sales;

public class SalesInvoiceItem
{
    public Guid Id { get; private set; }
    public int LineNumber { get; private set; }
    public Guid ChinaContainerId { get; private set; }
    public Guid FabricItemId { get; private set; }
    public Guid FabricColorId { get; private set; }
    public int RollCount { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();

    /// <summary>Catalog (card) price captured at line entry; the baseline for discount calculation.</summary>
    public Money OriginalUnitPrice { get; private set; } = Money.Zero();

    public string Unit { get; private set; } = "meter";
    public Money LineTotal { get; private set; } = Money.Zero();

    /// <summary>Computed contra-revenue discount = (OriginalUnitPrice − UnitPrice) × sold meters, floored at zero.</summary>
    public Money DiscountAmount { get; private set; } = Money.Zero();

    public string? DiscountReason { get; private set; }
    public Guid? PriceModifiedByUserId { get; private set; }
    public DateTime? PriceModifiedAt { get; private set; }
    public string? Notes { get; private set; }

    private SalesInvoiceItem() { }

    public static SalesInvoiceItem Create(
        int lineNumber,
        Guid chinaContainerId,
        Guid fabricItemId,
        Guid fabricColorId,
        int rollCount,
        Money unitPrice,
        string? notes = null,
        Money? originalUnitPrice = null,
        string? discountReason = null,
        Guid? priceModifiedByUserId = null,
        DateTime? priceModifiedAt = null)
    {
        if (unitPrice.Amount <= 0)
            throw new ValidationException("سعر البيع يجب أن يكون أكبر من صفر.");

        if (chinaContainerId == Guid.Empty)
            throw new ValidationException("China container is required for every sales invoice line.");

        var baseline = originalUnitPrice is { Amount: > 0 } original ? original : unitPrice;

        return new()
        {
            Id = Guid.NewGuid(),
            LineNumber = lineNumber,
            ChinaContainerId = chinaContainerId,
            FabricItemId = fabricItemId,
            FabricColorId = fabricColorId,
            RollCount = rollCount,
            UnitPrice = unitPrice,
            OriginalUnitPrice = baseline,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            DiscountReason = string.IsNullOrWhiteSpace(discountReason) ? null : discountReason.Trim(),
            PriceModifiedByUserId = priceModifiedByUserId,
            PriceModifiedAt = priceModifiedAt
        };
    }

    public void RecalculateTotal(IReadOnlyList<SalesInvoiceRollDetail> rollDetails)
    {
        var lineDetails = rollDetails.Where(d => d.SalesInvoiceItemId == Id).ToList();
        var meters = lineDetails.Sum(d => d.LengthMeters.Value);
        LineTotal = new Money(meters * UnitPrice.Amount, UnitPrice.Currency);

        var perMeterDiscount = OriginalUnitPrice.Amount - UnitPrice.Amount;
        DiscountAmount = perMeterDiscount > 0
            ? new Money(meters * perMeterDiscount, UnitPrice.Currency)
            : Money.Zero();
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

    /// <summary>
    /// Unresolved serial the employee has typed so far (partial save). Not validated against
    /// inventory and not tied to <see cref="FabricRollId"/> — purely for re-populating the UI on
    /// a later visit. Cleared once the roll is finally resolved via <see cref="EnterLength"/>.
    /// </summary>
    public int? DraftRollNumber { get; private set; }

    /// <summary>Unresolved manual length the employee has typed so far (partial save). See <see cref="DraftRollNumber"/>.</summary>
    public decimal? DraftLengthMeters { get; private set; }

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

    /// <summary>Persists whatever the employee has typed so far, without resolving or validating it.</summary>
    public void SaveDraft(int? rollNumber, decimal? lengthMeters)
    {
        DraftRollNumber = rollNumber;
        DraftLengthMeters = lengthMeters;
    }

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

public class ReceiptInvoicePayment
{
    public Guid Id { get; private set; }
    public Guid SalesInvoiceId { get; private set; }
    public Guid ReceiptVoucherId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime AppliedAt { get; private set; }

    private ReceiptInvoicePayment() { }

    public static ReceiptInvoicePayment Create(
        Guid salesInvoiceId,
        Guid receiptVoucherId,
        Money amount) => new()
    {
        Id = Guid.NewGuid(),
        SalesInvoiceId = salesInvoiceId,
        ReceiptVoucherId = receiptVoucherId,
        Amount = amount,
        AppliedAt = DateTime.UtcNow
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
