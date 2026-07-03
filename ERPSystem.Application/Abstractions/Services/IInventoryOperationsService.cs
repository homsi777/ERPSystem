using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Services;

public interface IInventoryOperationsService
{
    Task ValidateContainerForSaleAsync(Guid containerId, CancellationToken cancellationToken = default);
    Task ValidateInvoiceLinesAsync(
        Guid warehouseId,
        Guid containerId,
        IReadOnlyList<(Guid FabricItemId, Guid FabricColorId, int RollCount)> lines,
        CancellationToken cancellationToken = default);
    Task ReserveForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task<decimal> DeductForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task ReleaseForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task AssignFabricRollsOnDetailingAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task<decimal> ReceiveSalesReturnAsync(SalesReturnAggregate salesReturn, SalesInvoiceAggregate originalInvoice, CancellationToken cancellationToken = default);
}
