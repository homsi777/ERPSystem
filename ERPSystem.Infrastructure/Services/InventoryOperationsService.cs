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
        Guid containerId,
        IReadOnlyList<(Guid FabricItemId, Guid FabricColorId, int RollCount)> lines,
        CancellationToken cancellationToken = default)
    {
        await ValidateContainerForSaleAsync(containerId, cancellationToken);

        foreach (var line in lines)
        {
            var availableRolls = await context.FabricRolls.AsNoTracking()
                .CountAsync(r =>
                    r.ContainerId == containerId &&
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

    public Task<decimal> DeductForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default) =>
        engine.IssueForInvoiceAsync(invoice, cancellationToken);

    public Task ReleaseForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default) =>
        engine.ReleaseForInvoiceAsync(invoice, cancellationToken);
}
