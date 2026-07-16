namespace ERPSystem.Infrastructure.Persistence.Models.Finance;

public class ReceiptVoucherEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string VoucherNumber { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid CashboxId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public decimal Amount { get; set; }
    public DateTime VoucherDate { get; set; }
    public int Status { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? ReversalOfId { get; set; }
    public string? ReversalReason { get; set; }
    public DateTime? ReversedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class PaymentMethodEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public int Kind { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool RequiresCashbox { get; set; }
    public bool RequiresBankAccount { get; set; }
    public bool RequiresReference { get; set; }
    public bool AllowsMixedTender { get; set; }
    public bool RequiresClearingAccount { get; set; }
}

public class BankAccountEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string BankName { get; set; } = "";
    public string? Iban { get; set; }
    public string? AccountNumberMasked { get; set; }
    public Guid GlAccountId { get; set; }
    public string Currency { get; set; } = "USD";
}

public class ReceiptTenderLineEntity : PersistenceEntity
{
    public Guid ReceiptVoucherId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid? CashboxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal BaseAmount { get; set; }
    public string? Reference { get; set; }
    public string? ChequeNumber { get; set; }
    public DateTime? ChequeDate { get; set; }
    public string? CardReference { get; set; }
}

public class PaymentVoucherEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string VoucherNumber { get; set; } = "";
    public Guid SupplierId { get; set; }
    public Guid? CashboxId { get; set; }
    public Guid? BankAccountId { get; set; }
    public Guid PaymentMethodId { get; set; }
    public Guid? PurchaseInvoiceId { get; set; }
    public string? Reference { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal Amount { get; set; }
    public DateTime VoucherDate { get; set; }
    public int Status { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class CashboxEntity : PersistenceEntity
{
    public Guid? CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid? AccountId { get; set; }
    public bool AllowNegativeBalance { get; set; }
    public DateTime? OpeningDate { get; set; }
}

public class CashboxTransferEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string TransferNumber { get; set; } = "";
    public Guid FromCashboxId { get; set; }
    public Guid ToCashboxId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime TransferDate { get; set; }
    public int Status { get; set; }
    public string? Notes { get; set; }
    public DateTime? PostedAt { get; set; }
}
