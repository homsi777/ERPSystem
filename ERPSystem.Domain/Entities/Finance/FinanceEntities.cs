using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Entities.Finance;

public class ReceiptVoucher
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string VoucherNumber { get; private set; } = "";
    public Guid CustomerId { get; private set; }
    public Guid CashboxId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime VoucherDate { get; private set; }
    public VoucherStatus Status { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }
    public string? CancelReason { get; private set; }
    public Guid? ReversalOfId { get; private set; }
    public string? ReversalReason { get; private set; }
    public DateTime? ReversedAt { get; private set; }

    private readonly List<ReceiptAllocation> _allocations = [];
    public IReadOnlyList<ReceiptAllocation> Allocations => _allocations.AsReadOnly();

    private ReceiptVoucher() { }

    public static ReceiptVoucher CreateDraft(
        Guid companyId,
        Guid branchId,
        string voucherNumber,
        Guid customerId,
        Guid cashboxId,
        Guid paymentMethodId,
        Money amount) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        VoucherNumber = voucherNumber,
        CustomerId = customerId,
        CashboxId = cashboxId,
        PaymentMethodId = paymentMethodId,
        Amount = amount,
        VoucherDate = DateTime.UtcNow,
        Status = VoucherStatus.Draft
    };

    public static ReceiptVoucher CreateReversalDraft(
        Guid companyId,
        Guid branchId,
        string voucherNumber,
        Guid customerId,
        Guid cashboxId,
        Guid paymentMethodId,
        Money amount,
        Guid reversalOfId) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        VoucherNumber = voucherNumber,
        CustomerId = customerId,
        CashboxId = cashboxId,
        PaymentMethodId = paymentMethodId,
        Amount = amount,
        VoucherDate = DateTime.UtcNow,
        Status = VoucherStatus.Draft,
        ReversalOfId = reversalOfId
    };

    public void Submit()
    {
        if (Status != VoucherStatus.Draft)
            throw new Exceptions.AccountingException("Only draft vouchers can be submitted.");
        Status = VoucherStatus.Submitted;
    }

    public void Approve()
    {
        if (Status is not (VoucherStatus.Draft or VoucherStatus.Submitted))
            throw new Exceptions.AccountingException("Only draft or submitted vouchers can be approved.");
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
        if (Status == VoucherStatus.Reversed)
            throw new Exceptions.AccountingException("Reversed vouchers cannot be cancelled.");
        Status = VoucherStatus.Cancelled;
        CancelReason = reason;
        CancelledAt = DateTime.UtcNow;
    }

    public void MarkReversed(string reason)
    {
        if (Status != VoucherStatus.Posted)
            throw new Exceptions.AccountingException("Only posted vouchers can be reversed.");
        Status = VoucherStatus.Reversed;
        ReversalReason = reason;
        ReversedAt = DateTime.UtcNow;
    }

    public void Allocate(Guid invoiceId, Money amount) =>
        _allocations.Add(new ReceiptAllocation(invoiceId, amount));
}

public record ReceiptAllocation(Guid SalesInvoiceId, Money Amount);

public class ReceiptTenderLine
{
    public Guid Id { get; private set; }
    public Guid ReceiptVoucherId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public Guid? CashboxId { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public string Currency { get; private set; } = "USD";
    public decimal ExchangeRate { get; private set; } = 1m;
    public decimal BaseAmount { get; private set; }
    public string? Reference { get; private set; }
    public string? ChequeNumber { get; private set; }
    public DateTime? ChequeDate { get; private set; }
    public string? CardReference { get; private set; }

    private ReceiptTenderLine() { }

    public static ReceiptTenderLine CreateCash(
        Guid receiptVoucherId,
        Guid paymentMethodId,
        Guid cashboxId,
        Money amount,
        string currency = "USD",
        decimal exchangeRate = 1m) => new()
    {
        Id = Guid.NewGuid(),
        ReceiptVoucherId = receiptVoucherId,
        PaymentMethodId = paymentMethodId,
        CashboxId = cashboxId,
        Amount = amount,
        Currency = currency,
        ExchangeRate = exchangeRate,
        BaseAmount = amount.Amount * exchangeRate
    };

    public static ReceiptTenderLine CreateBank(
        Guid receiptVoucherId,
        Guid paymentMethodId,
        Guid bankAccountId,
        Money amount,
        string reference,
        string currency = "USD",
        decimal exchangeRate = 1m) => new()
    {
        Id = Guid.NewGuid(),
        ReceiptVoucherId = receiptVoucherId,
        PaymentMethodId = paymentMethodId,
        BankAccountId = bankAccountId,
        Amount = amount,
        Currency = currency,
        ExchangeRate = exchangeRate,
        BaseAmount = amount.Amount * exchangeRate,
        Reference = reference
    };
}

public class BankAccount
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string BankName { get; private set; } = "";
    public string? Iban { get; private set; }
    public string? AccountNumberMasked { get; private set; }
    public Guid GlAccountId { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; } = true;

    private BankAccount() { }

    public static BankAccount Create(
        Guid companyId,
        Guid branchId,
        string code,
        string name,
        string bankName,
        Guid glAccountId,
        string currency = "USD") => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        Code = code,
        Name = name,
        BankName = bankName,
        GlAccountId = glAccountId,
        Currency = currency
    };
}

public class PaymentMethod
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public PaymentMethodKind Kind { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool RequiresCashbox { get; private set; }
    public bool RequiresBankAccount { get; private set; }
    public bool RequiresReference { get; private set; }
    public bool AllowsMixedTender { get; private set; }
    public bool RequiresClearingAccount { get; private set; }
    public bool IsActive { get; private set; } = true;
}

public class PaymentVoucher
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string VoucherNumber { get; private set; } = "";
    public Guid SupplierId { get; private set; }
    public Guid? CashboxId { get; private set; }
    public Guid? BankAccountId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public Guid? PurchaseInvoiceId { get; private set; }
    public string? Reference { get; private set; }
    public Money Amount { get; private set; } = Money.Zero();
    public DateTime VoucherDate { get; private set; }
    public VoucherStatus Status { get; private set; }

    private PaymentVoucher() { }

    public static PaymentVoucher CreateDraft(
        Guid companyId,
        Guid branchId,
        string voucherNumber,
        Guid supplierId,
        Guid? cashboxId,
        Guid? bankAccountId,
        Guid paymentMethodId,
        Money amount,
        Guid? purchaseInvoiceId = null,
        string? reference = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        VoucherNumber = voucherNumber,
        SupplierId = supplierId,
        CashboxId = cashboxId,
        BankAccountId = bankAccountId,
        PaymentMethodId = paymentMethodId,
        PurchaseInvoiceId = purchaseInvoiceId,
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim(),
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
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public Guid BranchId { get; private set; }
    public Money Balance { get; private set; } = Money.Zero();
    public string Currency { get; private set; } = "USD";
    public bool IsActive { get; private set; } = true;
    public Guid? AccountId { get; private set; }
    public bool AllowNegativeBalance { get; private set; }
    public DateTime? OpeningDate { get; private set; }

    private Cashbox() { }

    public static Cashbox Create(Guid companyId, Guid branchId, string code, string name, string currency = "USD") => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        BranchId = branchId,
        Code = code,
        Name = name,
        Currency = currency,
        OpeningDate = DateTime.UtcNow.Date
    };

    public void UpdateProfile(string code, string name, string currency)
    {
        Code = code;
        Name = name;
        Currency = currency;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void LinkAccount(Guid accountId) => AccountId = accountId;
    public void SetAllowNegativeBalance(bool allow) => AllowNegativeBalance = allow;

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
    public string? Notes { get; private set; }

    private CashboxTransfer() { }

    public static CashboxTransfer Create(string number, Guid fromId, Guid toId, Money amount, string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        Number = number,
        FromCashboxId = fromId,
        ToCashboxId = toId,
        Amount = amount,
        TransferDate = DateTime.UtcNow,
        Status = VoucherStatus.Draft,
        Notes = notes
    };

    public void Approve()
    {
        if (Status != VoucherStatus.Draft)
            throw new Exceptions.AccountingException("Only draft transfers can be approved.");
        Status = VoucherStatus.Approved;
    }

    public void Post()
    {
        if (Status != VoucherStatus.Approved && Status != VoucherStatus.Draft)
            throw new Exceptions.AccountingException("Transfer cannot be posted.");
        Status = VoucherStatus.Posted;
    }
}
