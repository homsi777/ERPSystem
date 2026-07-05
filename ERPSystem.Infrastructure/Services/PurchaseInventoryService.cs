using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;

namespace ERPSystem.Infrastructure.Services;

internal sealed class PurchaseInventoryService(IInventoryEngine engine) : IPurchaseInventoryService
{
    public Task PostPurchaseInvoiceStockAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default) =>
        engine.PostPurchaseInvoiceAsync(invoice, cancellationToken);

    public Task ReversePurchaseReturnStockAsync(
        PurchaseReturn purchaseReturn,
        PurchaseInvoice originalInvoice,
        CancellationToken cancellationToken = default) =>
        engine.ReversePurchaseReturnAsync(purchaseReturn, originalInvoice, cancellationToken);

    public Task ReversePurchaseInvoiceStockAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default) =>
        engine.ReversePurchaseInvoiceAsync(invoice, cancellationToken);
}
