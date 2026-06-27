using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Purchasing;

public class PurchaseInvoice
{
    public Guid Id { get; private set; }
    public string InvoiceNumber { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero();
    public Money Remaining { get; private set; } = Money.Zero();
    public PurchaseInvoiceStatus Status { get; private set; }

    private readonly List<PurchaseInvoiceItem> _items = [];
    public IReadOnlyList<PurchaseInvoiceItem> Items => _items.AsReadOnly();

    private PurchaseInvoice() { }

    public static PurchaseInvoice CreateDraft(string invoiceNumber, Guid supplierId) => new()
    {
        Id = Guid.NewGuid(),
        InvoiceNumber = invoiceNumber,
        SupplierId = supplierId,
        InvoiceDate = DateTime.UtcNow,
        Status = PurchaseInvoiceStatus.Draft
    };

    public void AddItem(PurchaseInvoiceItem item) => _items.Add(item);
}

public class PurchaseInvoiceItem
{
    public Guid Id { get; private set; }
    public Guid FabricItemId { get; private set; }
    public LengthInMeters Quantity { get; private set; } = null!;
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money LineTotal { get; private set; } = Money.Zero();

    private PurchaseInvoiceItem() { }

    public static PurchaseInvoiceItem Create(
        Guid fabricItemId,
        LengthInMeters quantity,
        Money unitPrice) => new()
    {
        Id = Guid.NewGuid(),
        FabricItemId = fabricItemId,
        Quantity = quantity,
        UnitPrice = unitPrice,
        LineTotal = unitPrice.Multiply(quantity.Value)
    };
}

public class PurchaseReturn
{
    public Guid Id { get; private set; }
    public string ReturnNumber { get; private set; } = "";
    public Guid OriginalInvoiceId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public VoucherStatus Status { get; private set; }

    private PurchaseReturn() { }

    public static PurchaseReturn Create(string returnNumber, Guid originalInvoiceId, Money amount) => new()
    {
        Id = Guid.NewGuid(),
        ReturnNumber = returnNumber,
        OriginalInvoiceId = originalInvoiceId,
        Amount = amount,
        Status = VoucherStatus.Draft
    };
}
