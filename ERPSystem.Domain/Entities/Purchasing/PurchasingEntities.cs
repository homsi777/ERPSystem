using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Purchasing;

public class PurchaseInvoice
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string InvoiceNumber { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public string? SupplierReference { get; private set; }
    public DateTime InvoiceDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public Guid? WarehouseId { get; private set; }
    public string CurrencyCode { get; private set; } = "USD";
    public PurchaseInvoiceStatus Status { get; private set; }
    public Money SubTotal { get; private set; } = Money.Zero();
    public Money DiscountAmount { get; private set; } = Money.Zero();
    public Money TaxAmount { get; private set; } = Money.Zero();
    public Money TotalAmount { get; private set; } = Money.Zero();
    public Money PaidAmount { get; private set; } = Money.Zero();
    public Money Remaining { get; private set; } = Money.Zero();
    public Guid? PurchaseOrderId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedByUserId { get; private set; }

    private readonly List<PurchaseInvoiceItem> _items = [];
    public IReadOnlyList<PurchaseInvoiceItem> Items => _items.AsReadOnly();

    private PurchaseInvoice() { }

    public static PurchaseInvoice CreateDraft(
        Guid companyId,
        Guid branchId,
        string invoiceNumber,
        Guid supplierId,
        DateTime invoiceDate,
        DateTime dueDate,
        string currencyCode = "USD",
        Guid? warehouseId = null,
        Guid? purchaseOrderId = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        InvoiceNumber = invoiceNumber,
        SupplierId = supplierId,
        InvoiceDate = invoiceDate,
        DueDate = dueDate,
        CurrencyCode = currencyCode,
        WarehouseId = warehouseId,
        PurchaseOrderId = purchaseOrderId,
        Status = PurchaseInvoiceStatus.Draft,
        Remaining = Money.Zero()
    };

    public void UpdateHeader(
        Guid supplierId,
        DateTime invoiceDate,
        DateTime dueDate,
        string? supplierReference,
        Guid? warehouseId,
        string currencyCode,
        decimal discountAmount,
        decimal taxAmount,
        string? notes)
    {
        EnsureDraft();
        SupplierId = supplierId;
        InvoiceDate = invoiceDate;
        DueDate = dueDate;
        SupplierReference = supplierReference?.Trim();
        WarehouseId = warehouseId;
        CurrencyCode = string.IsNullOrWhiteSpace(currencyCode) ? "USD" : currencyCode.Trim();
        DiscountAmount = new Money(discountAmount, CurrencyCode);
        TaxAmount = new Money(taxAmount, CurrencyCode);
        Notes = notes?.Trim();
        RecalculateTotals();
    }

    public void ReplaceItems(IEnumerable<PurchaseInvoiceItem> items)
    {
        EnsureDraft();
        _items.Clear();
        foreach (var item in items)
            _items.Add(item);
        RecalculateTotals();
    }

    public void AddItem(PurchaseInvoiceItem item)
    {
        EnsureDraft();
        _items.Add(item);
        RecalculateTotals();
    }

    public void HydrateItems(IEnumerable<PurchaseInvoiceItem> items)
    {
        _items.Clear();
        foreach (var item in items)
            _items.Add(item);
    }

    public void Post(Guid userId)
    {
        if (Status != PurchaseInvoiceStatus.Draft)
            throw new AccountingException("Only draft purchase invoices can be posted.");
        if (_items.Count == 0)
            throw new ValidationException("Purchase invoice must have at least one line.");
        if (TotalAmount.Amount <= 0)
            throw new ValidationException("Purchase invoice total must be greater than zero.");

        Status = PurchaseInvoiceStatus.Posted;
        Remaining = TotalAmount;
        PaidAmount = Money.Zero(CurrencyCode);
        PostedAt = DateTime.UtcNow;
        PostedByUserId = userId;
    }

    public void Cancel()
    {
        if (Status != PurchaseInvoiceStatus.Draft)
            throw new AccountingException("Only draft purchase invoices can be cancelled.");
        Status = PurchaseInvoiceStatus.Cancelled;
    }

    public void ApplyPayment(decimal amount)
    {
        if (Status is not (PurchaseInvoiceStatus.Posted or PurchaseInvoiceStatus.PartiallyPaid))
            throw new AccountingException("Payments can only be applied to posted invoices.");

        var payment = Math.Min(amount, Remaining.Amount);
        PaidAmount = PaidAmount.Add(new Money(payment, CurrencyCode));
        Remaining = Remaining.Subtract(new Money(payment, CurrencyCode));

        Status = Remaining.Amount <= 0
            ? PurchaseInvoiceStatus.Paid
            : PurchaseInvoiceStatus.PartiallyPaid;
    }

    private void RecalculateTotals()
    {
        var subtotal = _items.Sum(i => i.LineTotal.Amount);
        SubTotal = new Money(subtotal, CurrencyCode);
        var total = subtotal - DiscountAmount.Amount + TaxAmount.Amount;
        TotalAmount = new Money(Math.Max(0, total), CurrencyCode);
        if (Status == PurchaseInvoiceStatus.Draft)
            Remaining = TotalAmount;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseInvoiceStatus.Draft)
            throw new AccountingException("Posted purchase invoices are immutable.");
    }
}

public class PurchaseInvoiceItem
{
    public Guid Id { get; private set; }
    public PurchaseLineType LineType { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public Guid? FabricColorId { get; private set; }
    public Guid? ExpenseAccountId { get; private set; }
    public string Description { get; private set; } = "";
    public LengthInMeters Quantity { get; private set; } = null!;
    public int RollCount { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money LineTotal { get; private set; } = Money.Zero();

    private PurchaseInvoiceItem() { }

    public static PurchaseInvoiceItem CreateInventoryLine(
        Guid fabricItemId,
        Guid? fabricColorId,
        LengthInMeters quantity,
        int rollCount,
        Money unitPrice,
        string description = "") => new()
    {
        Id = Guid.NewGuid(),
        LineType = PurchaseLineType.Inventory,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        Description = description,
        Quantity = quantity,
        RollCount = Math.Max(1, rollCount),
        UnitPrice = unitPrice,
        LineTotal = unitPrice.Multiply(quantity.Value)
    };

    public static PurchaseInvoiceItem CreateExpenseLine(
        Guid expenseAccountId,
        Money amount,
        string description) => new()
    {
        Id = Guid.NewGuid(),
        LineType = PurchaseLineType.Expense,
        ExpenseAccountId = expenseAccountId,
        Description = description,
        Quantity = new LengthInMeters(1),
        RollCount = 0,
        UnitPrice = amount,
        LineTotal = amount
    };
}

public class PurchaseOrder
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string OrderNumber { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public PurchaseOrderStatus Status { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero();
    public string? Notes { get; private set; }

    private readonly List<PurchaseOrderLine> _lines = [];
    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    private PurchaseOrder() { }

    public static PurchaseOrder CreateDraft(
        Guid companyId,
        Guid branchId,
        string orderNumber,
        Guid supplierId,
        DateTime orderDate) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        OrderNumber = orderNumber,
        SupplierId = supplierId,
        OrderDate = orderDate,
        Status = PurchaseOrderStatus.Draft
    };

    public void Update(Guid supplierId, DateTime? expectedDelivery, string? notes)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new AccountingException("Only draft purchase orders can be edited.");
        SupplierId = supplierId;
        ExpectedDeliveryDate = expectedDelivery;
        Notes = notes?.Trim();
    }

    public void ReplaceLines(IEnumerable<PurchaseOrderLine> lines)
    {
        if (Status != PurchaseOrderStatus.Draft)
            throw new AccountingException("Only draft purchase orders can be edited.");
        HydrateLines(lines);
    }

    public void HydrateLines(IEnumerable<PurchaseOrderLine> lines)
    {
        _lines.Clear();
        foreach (var line in lines)
            _lines.Add(line);
        TotalAmount = new Money(_lines.Sum(l => l.LineTotal.Amount));
    }

    public void MarkSent() => Status = PurchaseOrderStatus.Sent;
    public void MarkReceived() => Status = PurchaseOrderStatus.Received;
    public void Cancel() => Status = PurchaseOrderStatus.Cancelled;
}

public class PurchaseOrderLine
{
    public Guid Id { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public string Description { get; private set; } = "";
    public decimal Quantity { get; private set; }
    public Money UnitCost { get; private set; } = Money.Zero();
    public Money LineTotal { get; private set; } = Money.Zero();

    private PurchaseOrderLine() { }

    public static PurchaseOrderLine Create(
        Guid? fabricItemId,
        string description,
        decimal quantity,
        Money unitCost) => new()
    {
        Id = Guid.NewGuid(),
        FabricItemId = fabricItemId,
        Description = description,
        Quantity = quantity,
        UnitCost = unitCost,
        LineTotal = unitCost.Multiply(quantity)
    };
}

public class PurchaseReturn
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string ReturnNumber { get; private set; } = "";
    public Guid OriginalInvoiceId { get; private set; }
    public DateTime ReturnDate { get; private set; }
    public Money TotalAmount { get; private set; } = Money.Zero();
    public PurchaseReturnStatus Status { get; private set; }
    public string? Notes { get; private set; }
    public DateTime? PostedAt { get; private set; }

    private readonly List<PurchaseReturnLine> _lines = [];
    public IReadOnlyList<PurchaseReturnLine> Lines => _lines.AsReadOnly();

    private PurchaseReturn() { }

    public static PurchaseReturn CreateDraft(
        Guid companyId,
        Guid branchId,
        string returnNumber,
        Guid originalInvoiceId,
        DateTime returnDate) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        ReturnNumber = returnNumber,
        OriginalInvoiceId = originalInvoiceId,
        ReturnDate = returnDate,
        Status = PurchaseReturnStatus.Draft
    };

    public void ReplaceLines(IEnumerable<PurchaseReturnLine> lines)
    {
        if (Status != PurchaseReturnStatus.Draft)
            throw new AccountingException("Posted returns are immutable.");
        HydrateLines(lines);
    }

    public void UpdateNotes(string? notes)
    {
        if (Status != PurchaseReturnStatus.Draft)
            throw new AccountingException("Posted returns are immutable.");
        Notes = notes?.Trim();
    }

    public void HydrateLines(IEnumerable<PurchaseReturnLine> lines)
    {
        _lines.Clear();
        foreach (var line in lines)
            _lines.Add(line);
        TotalAmount = new Money(_lines.Sum(l => l.LineTotal.Amount));
    }

    public void Post()
    {
        if (Status != PurchaseReturnStatus.Draft)
            throw new AccountingException("Return already posted.");
        if (_lines.Count == 0 || TotalAmount.Amount <= 0)
            throw new ValidationException("Return must have lines with amount.");
        Status = PurchaseReturnStatus.Posted;
        PostedAt = DateTime.UtcNow;
    }
}

public class PurchaseReturnLine
{
    public Guid Id { get; private set; }
    public Guid OriginalInvoiceItemId { get; private set; }
    public Guid? FabricItemId { get; private set; }
    public Guid? FabricColorId { get; private set; }
    public PurchaseLineType LineType { get; private set; }
    public decimal QuantityMeters { get; private set; }
    public Money UnitPrice { get; private set; } = Money.Zero();
    public Money LineTotal { get; private set; } = Money.Zero();

    private PurchaseReturnLine() { }

    public static PurchaseReturnLine Create(
        Guid originalInvoiceItemId,
        PurchaseLineType lineType,
        Guid? fabricItemId,
        Guid? fabricColorId,
        decimal quantityMeters,
        Money unitPrice) => new()
    {
        Id = Guid.NewGuid(),
        OriginalInvoiceItemId = originalInvoiceItemId,
        LineType = lineType,
        FabricItemId = fabricItemId,
        FabricColorId = fabricColorId,
        QuantityMeters = quantityMeters,
        UnitPrice = unitPrice,
        LineTotal = unitPrice.Multiply(quantityMeters)
    };
}

public class PurchaseInvoicePayment
{
    public Guid Id { get; private set; }
    public Guid PurchaseInvoiceId { get; private set; }
    public Guid PaymentVoucherId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime AppliedAt { get; private set; }

    private PurchaseInvoicePayment() { }

    public static PurchaseInvoicePayment Create(
        Guid purchaseInvoiceId,
        Guid paymentVoucherId,
        Money amount) => new()
    {
        Id = Guid.NewGuid(),
        PurchaseInvoiceId = purchaseInvoiceId,
        PaymentVoucherId = paymentVoucherId,
        Amount = amount,
        AppliedAt = DateTime.UtcNow
    };
}
