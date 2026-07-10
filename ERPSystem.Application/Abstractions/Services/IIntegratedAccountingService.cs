using ERPSystem.Application.Posting;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;

namespace ERPSystem.Application.Abstractions.Services;

/// <summary>A single balanced journal line spec used by generic posting APIs.</summary>
public sealed record JournalLineSpec(Guid AccountId, decimal Debit, decimal Credit, string Narrative, Guid? PartyId);

public interface IIntegratedAccountingService
{
    /// <summary>Posting requests awaiting SaveChanges recovery since last consume.</summary>
    IReadOnlyList<PostingRequest> ConsumePendingPostingRequests();
    /// <summary>
    /// Posts the journal entry for a unified opening balance document
    /// (DocumentType.FinanceOpeningBalance). Idempotent per document id.
    /// Returns the journal entry number.
    /// </summary>
    Task<string> PostOpeningBalanceDocumentAsync(
        Guid documentId,
        string documentNumber,
        string description,
        DateTime postingDate,
        IReadOnlyList<JournalLineSpec> lines,
        CancellationToken cancellationToken = default);

    Task PostContainerApprovalAsync(ContainerAggregate container, CancellationToken cancellationToken = default);
    Task PostInventoryActivationAsync(
        ContainerAggregate container,
        Guid warehouseId,
        decimal inventoryValue,
        CancellationToken cancellationToken = default);
    Task PostSalesInvoiceApprovalAsync(SalesInvoiceAggregate invoice, decimal cogsAmount, CancellationToken cancellationToken = default);

    Task PostReceiptVoucherAsync(
        Guid voucherId,
        string voucherNumber,
        Guid customerId,
        Guid debitAccountId,
        decimal totalAmount,
        decimal allocatedToArAmount,
        bool isReversal = false,
        Guid? originalVoucherId = null,
        CancellationToken cancellationToken = default);

    Task PostPaymentVoucherAsync(
        Guid voucherId,
        string voucherNumber,
        Guid supplierId,
        Guid payablesAccountId,
        Guid cashAccountId,
        decimal amount,
        CancellationToken cancellationToken = default);

    Task PostExpensePaymentAsync(
        Guid expenseId,
        Guid paymentId,
        decimal amountBase,
        string description,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy direct journal posting for supplier opening balance.
    /// Do not use — route through <see cref="IOpeningBalanceEngine.PostPartyOpeningBalanceAsync"/> instead.
    /// </summary>
    [Obsolete("Use IOpeningBalanceEngine.PostPartyOpeningBalanceAsync via OpeningBalanceUiService. This bypasses the unified OpeningBalanceDocument workflow.")]
    Task<string> PostSupplierOpeningBalanceAsync(
        Guid supplierId,
        Guid payablesAccountId,
        decimal amount,
        DateTime postingDate,
        string referenceNote,
        CancellationToken cancellationToken = default);

    Task<string> PostPurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        Guid payablesAccountId,
        CancellationToken cancellationToken = default);

    Task<string> PostPurchaseReturnAsync(
        PurchaseReturn purchaseReturn,
        Guid payablesAccountId,
        CancellationToken cancellationToken = default);

    Task<string> ReversePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        Guid payablesAccountId,
        CancellationToken cancellationToken = default);

    Task<string> PostCashboxTransferAsync(
        Guid transferId,
        string transferNumber,
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        DateTime transferDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Legacy direct journal posting for customer opening balance.
    /// Do not use — route through <see cref="IOpeningBalanceEngine.PostPartyOpeningBalanceAsync"/> instead.
    /// </summary>
    [Obsolete("Use IOpeningBalanceEngine.PostPartyOpeningBalanceAsync via OpeningBalanceUiService. This bypasses the unified OpeningBalanceDocument workflow.")]
    Task<string> PostCustomerOpeningBalanceAsync(
        Guid customerId,
        Guid receivablesAccountId,
        decimal amount,
        DateTime postingDate,
        string referenceNote,
        CancellationToken cancellationToken = default);

    Task<string> PostSalesReturnAsync(
        SalesReturnAggregate salesReturn,
        decimal cogsReversalAmount,
        decimal taxReversalAmount,
        IReadOnlyList<(Guid AccountId, decimal Amount)> taxReversalByAccount,
        CancellationToken cancellationToken = default);
}
