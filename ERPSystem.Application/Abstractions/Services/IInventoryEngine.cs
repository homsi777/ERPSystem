using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Abstractions.Services;

public sealed record InventoryReceiveLineRequest(
    Guid FabricItemId,
    Guid FabricColorId,
    decimal QuantityMeters,
    int RollCount,
    decimal UnitCost,
    Guid ContainerId = default,
    Guid? FabricBatchId = null,
    Guid? StorageLocationId = null,
    string? LotCode = null);

public sealed record InventoryIssueLineRequest(
    Guid FabricItemId,
    Guid FabricColorId,
    decimal QuantityMeters,
    Guid? FabricRollId = null,
    Guid ContainerId = default);

public interface IInventoryEngine
{
    Task PostContainerImportAsync(
        Guid warehouseId,
        ContainerAggregate container,
        CancellationToken cancellationToken = default);

    Task PostPurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task ReversePurchaseReturnAsync(
        PurchaseReturn purchaseReturn,
        PurchaseInvoice originalInvoice,
        CancellationToken cancellationToken = default);

    Task ReversePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default);

    Task ReserveForInvoiceAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default);

    Task<decimal> IssueForInvoiceAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default);

    Task ReleaseForInvoiceAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default);

    Task AssignFabricRollsOnDetailingAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default);

    Task<decimal> ReceiveSalesReturnAsync(
        SalesReturnAggregate salesReturn,
        SalesInvoiceAggregate originalInvoice,
        CancellationToken cancellationToken = default);

    Task<Guid> PostOpeningStockAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Posts inventory movements for a finance opening-balance stock document.
    /// Idempotent — returns existing movement ids if already posted.
    /// </summary>
    Task<IReadOnlyList<Guid>> PostFinanceOpeningBalanceStockAsync(
        Guid openingBalanceDocumentId,
        CancellationToken cancellationToken = default);

    Task<Guid> CompleteTransferAsync(
        Guid transferId,
        CancellationToken cancellationToken = default);

    Task<Guid> PostStocktakeAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task RecordValuationSnapshotAsync(
        Guid warehouseId,
        Guid movementId,
        ValuationMethod method,
        CancellationToken cancellationToken = default);
}
