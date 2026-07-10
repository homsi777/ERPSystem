using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Services;

public interface IInventoryOperationsService
{
    Task ValidateContainerForSaleAsync(Guid containerId, CancellationToken cancellationToken = default);
    Task ValidateInvoiceLinesAsync(
        Guid warehouseId,
        IReadOnlyList<(Guid ChinaContainerId, Guid FabricItemId, Guid FabricColorId, int RollCount)> lines,
        CancellationToken cancellationToken = default);
    Task ReserveForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task<decimal> DeductForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task ReleaseForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);
    Task AssignFabricRollsOnDetailingAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves roll serial numbers to inventory fabric rolls and returns the length to enter
    /// for each detailing line. Pins <see cref="SalesInvoiceAggregate"/> roll details to the matched rolls.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, decimal>> ResolveDetailingEntriesAsync(
        SalesInvoiceAggregate invoice,
        IReadOnlyList<(Guid RollDetailId, int? RollNumber, decimal LengthMeters)> entries,
        CancellationToken cancellationToken = default);

    Task<decimal> ReceiveSalesReturnAsync(SalesReturnAggregate salesReturn, SalesInvoiceAggregate originalInvoice, CancellationToken cancellationToken = default);
}
