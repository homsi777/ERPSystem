namespace ERPSystem.Infrastructure.Persistence.Models.Finance;

public class ReceiptVoucherEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string VoucherNumber { get; set; } = "";
    public Guid CustomerId { get; set; }
    public Guid CashboxId { get; set; }
    public decimal Amount { get; set; }
    public DateTime VoucherDate { get; set; }
    public int Status { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class PaymentVoucherEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string VoucherNumber { get; set; } = "";
    public Guid SupplierId { get; set; }
    public Guid CashboxId { get; set; }
    public decimal Amount { get; set; }
    public DateTime VoucherDate { get; set; }
    public int Status { get; set; }
    public DateTime? PostedAt { get; set; }
}

public class CashboxEntity : PersistenceEntity
{
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public Guid? AccountId { get; set; }
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
