namespace ERPSystem.Application.Abstractions.Services;

public interface INumberingService
{
    Task<string> NextInvoiceNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextContainerNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextReceiptNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextPaymentNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextJournalEntryNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextCustomerCodeAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextSupplierCodeAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextExpenseCodeAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextCapitalPartnerCodeAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextDistributionCodeAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseInvoiceNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseOrderNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextPurchaseReturnNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
}
