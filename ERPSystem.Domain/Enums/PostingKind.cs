namespace ERPSystem.Domain.Enums;

/// <summary>
/// Logical posting identity within a source document. Combined with SourceType + SourceId
/// for idempotent automated journal creation (PostingIdentityVersion = 2).
/// </summary>
public enum PostingKind
{
    ChinaContainerLandingCost = 1,
    ChinaContainerInventoryActivation = 2,
    SalesInvoicePosting = 10,
    ReceiptVoucher = 20,
    SalesReturn = 30,
    PurchaseInvoice = 40,
    PurchaseReturn = 41,
    PurchaseInvoiceReversal = 42,
    PaymentVoucher = 50,
    ExpensePayment = 60,
    CashboxTransfer = 70,
    FinanceOpeningBalance = 80,
    CustomerOpeningBalance = 81,
    SupplierOpeningBalance = 82,
    ManualJournalEntry = 100,
    Reversal = 200
}

public enum PostingAttemptStatus
{
    Posting = 0,
    Posted = 1,
    PostingFailed = 2
}

public enum IdempotencyRecordStatus
{
    InProgress = 0,
    Completed = 1,
    Failed = 2
}
