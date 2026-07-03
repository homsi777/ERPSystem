using ERPSystem.Domain.Aggregates;

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
        decimal amount,
        CancellationToken cancellationToken = default);

    Task PostExpensePaymentAsync(
        Guid expenseId,
        Guid paymentId,
        decimal amountBase,
        string description,
        CancellationToken cancellationToken = default);
}
