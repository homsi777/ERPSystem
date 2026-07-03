using ERPSystem.Domain.Entities.Purchasing;

namespace ERPSystem.Application.Abstractions.Services;

public interface IPurchaseInventoryService
{
    Task PostPurchaseInvoiceStockAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task ReversePurchaseReturnStockAsync(
        PurchaseReturn purchaseReturn,
        PurchaseInvoice originalInvoice,
        CancellationToken cancellationToken = default);
}
