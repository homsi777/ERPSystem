using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Finance;

public class ReceiptVoucher
{
    public Guid Id { get; private set; }
    public string VoucherNumber { get; private set; } = "";
    public Guid CustomerId { get; private set; }
    public Guid CashboxId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime VoucherDate { get; private set; }
    public VoucherStatus Status { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }

    private readonly List<ReceiptAllocation> _allocations = [];
    public IReadOnlyList<ReceiptAllocation> Allocations => _allocations.AsReadOnly();

    private ReceiptVoucher() { }

    public static ReceiptVoucher CreateDraft(
        string voucherNumber,
        Guid customerId,
        Guid cashboxId,
        Money amount) => new()
    {
        Id = Guid.NewGuid(),
        VoucherNumber = voucherNumber,
        CustomerId = customerId,
        CashboxId = cashboxId,
        Amount = amount,
        VoucherDate = DateTime.UtcNow,
        Status = VoucherStatus.Draft
    };

    public void Approve()
    {
        if (Status != VoucherStatus.Draft)
            throw new Exceptions.AccountingException("Only draft vouchers can be approved.");
        Status = VoucherStatus.Approved;
    }

    public void Post()
    {
        if (Status != VoucherStatus.Approved)
            throw new Exceptions.AccountingException("Voucher must be approved before posting.");
        Status = VoucherStatus.Posted;
        PostedAt = DateTime.UtcNow;
    }

    public void Cancel(string reason)
    {
        if (Status == VoucherStatus.Posted)
            throw new Exceptions.AccountingException("Posted vouchers must be reversed.");
        Status = VoucherStatus.Cancelled;
        CancelReason = reason;
        CancelledAt = DateTime.UtcNow;
    }

    public void Allocate(Guid invoiceId, Money amount) =>
        _allocations.Add(new ReceiptAllocation(invoiceId, amount));
}

public record ReceiptAllocation(Guid SalesInvoiceId, Money Amount);

public class PaymentVoucher
{
    public Guid Id { get; private set; }
    public string VoucherNumber { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public Guid CashboxId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime VoucherDate { get; private set; }
    public VoucherStatus Status { get; private set; }

    private PaymentVoucher() { }

    public static PaymentVoucher CreateDraft(
        string voucherNumber,
        Guid supplierId,
        Guid cashboxId,
        Money amount) => new()
    {
        Id = Guid.NewGuid(),
        VoucherNumber = voucherNumber,
        SupplierId = supplierId,
        CashboxId = cashboxId,
        Amount = amount,
        VoucherDate = DateTime.UtcNow,
        Status = VoucherStatus.Draft
    };

    public void Approve() => Status = VoucherStatus.Approved;
    public void Post() => Status = VoucherStatus.Posted;
}

public class Cashbox
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public Guid BranchId { get; private set; }
    public Money Balance { get; private set; } = Money.Zero();
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; } = true;

    private Cashbox() { }

    public static Cashbox Create(Guid branchId, string code, string name) => new()
    {
        Id = Guid.NewGuid(),
        BranchId = branchId,
        Code = code,
        Name = name
    };

    public void ApplyReceipt(Money amount) => Balance = Balance.Add(amount);
    public void ApplyPayment(Money amount) => Balance = Balance.Subtract(amount);
}

public class CashboxMovement
{
    public Guid Id { get; private set; }
    public Guid CashboxId { get; private set; }
    public DocumentType ReferenceType { get; private set; }
    public Guid ReferenceId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public bool IsInbound { get; private set; }
    public DateTime MovementDate { get; private set; }

    private CashboxMovement() { }

    public static CashboxMovement Create(
        Guid cashboxId,
        DocumentType referenceType,
        Guid referenceId,
        Money amount,
        bool isInbound) => new()
    {
        Id = Guid.NewGuid(),
        CashboxId = cashboxId,
        ReferenceType = referenceType,
        ReferenceId = referenceId,
        Amount = amount,
        IsInbound = isInbound,
        MovementDate = DateTime.UtcNow
    };
}

public class CashboxTransfer
{
    public Guid Id { get; private set; }
    public string Number { get; private set; } = "";
    public Guid FromCashboxId { get; private set; }
    public Guid ToCashboxId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public VoucherStatus Status { get; private set; }
    public DateTime TransferDate { get; private set; }

    private CashboxTransfer() { }

    public static CashboxTransfer Create(string number, Guid fromId, Guid toId, Money amount) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        FromCashboxId = fromId,
        ToCashboxId = toId,
        Amount = amount,
        TransferDate = DateTime.UtcNow,
        Status = VoucherStatus.Draft
    };
}
