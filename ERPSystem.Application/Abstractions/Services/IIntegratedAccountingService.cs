using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;

namespace ERPSystem.Application.Abstractions.Services;

public interface IIntegratedAccountingService
{
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
        decimal amount,
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

    Task<string> PostSalesReturnAsync(
        SalesReturnAggregate salesReturn,
        decimal cogsReversalAmount,
        CancellationToken cancellationToken = default);
}
