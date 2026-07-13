using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class InventoryOperationsService(
    ErpDbContext context,
    IChinaContainerRepository containerRepository,
    IInventoryRepository inventoryRepository,
    IInventoryEngine engine) : IInventoryOperationsService
{
    public Task ValidateContainerForSaleAsync(Guid containerId, CancellationToken cancellationToken = default) =>
        ContainerSaleValidator.EnsureReadyForSaleAsync(containerRepository, inventoryRepository, containerId, cancellationToken);

    public async Task ValidateInvoiceLinesAsync(
        Guid warehouseId,
        IReadOnlyList<(Guid ChinaContainerId, Guid FabricItemId, Guid FabricColorId, int RollCount)> lines,
        CancellationToken cancellationToken = default)
    {
        foreach (var containerId in lines.Select(l => l.ChinaContainerId).Distinct())
        {
            if (containerId != Guid.Empty)
                await ValidateContainerForSaleAsync(containerId, cancellationToken);
        }

        foreach (var line in lines)
        {
            if (line.ChinaContainerId == Guid.Empty)
            {
                var invalidLegacyRows = await context.FabricRolls.AsNoTracking()
                    .AnyAsync(r =>
                        r.ContainerId == Guid.Empty &&
                        r.WarehouseId == warehouseId &&
                        r.FabricItemId == line.FabricItemId &&
                        r.FabricColorId == line.FabricColorId &&
                        r.Status == (int)FabricRollStatus.Available &&
                        !r.IsLegacyOpeningBalance, cancellationToken);
                if (invalidLegacyRows)
                    throw new ValidationException("Container-less sales stock must be tagged as legacy opening balance.");
            }

            var availableRolls = await context.FabricRolls.AsNoTracking()
                .CountAsync(r =>
                    r.ContainerId == line.ChinaContainerId &&
                    r.WarehouseId == warehouseId &&
                    r.FabricItemId == line.FabricItemId &&
                    r.FabricColorId == line.FabricColorId &&
                    r.Status == (int)FabricRollStatus.Available, cancellationToken);

            if (availableRolls < line.RollCount)
                throw new ValidationException($"Insufficient available rolls (requested {line.RollCount}, available {availableRolls}).");
        }
    }

    public Task ReserveForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default) =>
        engine.ReserveForInvoiceAsync(invoice, cancellationToken);

    public Task AssignFabricRollsOnDetailingAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default) =>
        engine.AssignFabricRollsOnDetailingAsync(invoice, cancellationToken);

    public Task<IReadOnlyDictionary<Guid, decimal>> ResolveDetailingEntriesAsync(
        SalesInvoiceAggregate invoice,
        IReadOnlyList<(Guid RollDetailId, int? RollNumber, decimal LengthMeters)> entries,
        CancellationToken cancellationToken = default) =>
        engine.ResolveDetailingEntriesAsync(invoice, entries, cancellationToken);

    public Task<decimal> DeductForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default) =>
        engine.IssueForInvoiceAsync(invoice, cancellationToken);

    public Task ReleaseForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default) =>
        engine.ReleaseForInvoiceAsync(invoice, cancellationToken);

    public Task<decimal> ReceiveSalesReturnAsync(
        SalesReturnAggregate salesReturn,
        SalesInvoiceAggregate originalInvoice,
        CancellationToken cancellationToken = default) =>
        engine.ReceiveSalesReturnAsync(salesReturn, originalInvoice, cancellationToken);
}
