namespace ERPSystem.Application.Abstractions.Services;

public interface INumberingService
{
    Task<string> NextInvoiceNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextContainerNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextReceiptNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextPaymentNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
    Task<string> NextJournalEntryNumberAsync(Guid branchId, CancellationToken cancellationToken = default);
}
