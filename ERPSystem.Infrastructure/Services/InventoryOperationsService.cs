using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class InventoryOperationsService(
    ErpDbContext context,
    IChinaContainerRepository containerRepository,
    IInventoryRepository inventoryRepository) : IInventoryOperationsService
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

    public async Task ReserveForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default)
    {
        await ValidateContainerForSaleAsync(invoice.ChinaContainerId, cancellationToken);

        foreach (var item in invoice.Items)
        {
            var stock = await context.WarehouseStocks
                .FirstOrDefaultAsync(s =>
                    s.WarehouseId == invoice.WarehouseId &&
                    s.ContainerId == invoice.ChinaContainerId &&
                    s.FabricItemId == item.FabricItemId &&
                    s.FabricColorId == item.FabricColorId, cancellationToken)
                ?? throw new InventoryException("Warehouse stock row not found for reservation.");

            var rolls = await context.FabricRolls
                .Where(r =>
                    r.ContainerId == invoice.ChinaContainerId &&
                    r.WarehouseId == invoice.WarehouseId &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId &&
                    r.Status == (int)FabricRollStatus.Available)
                .OrderBy(r => r.RollNumber)
                .Take(item.RollCount)
                .ToListAsync(cancellationToken);

            if (rolls.Count < item.RollCount)
                throw new InventoryException("Not enough available rolls to reserve.");

            var reserveMeters = rolls.Sum(r => r.RemainingLengthMeters);
            if (reserveMeters > stock.AvailableMeters)
                throw new InventoryException("Insufficient available meters to reserve.");

            foreach (var roll in rolls)
                roll.Status = (int)FabricRollStatus.Reserved;

            stock.ReservedMeters += reserveMeters;
            stock.AvailableMeters -= reserveMeters;
        }
    }

    public async Task AssignFabricRollsOnDetailingAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default)
    {
        foreach (var item in invoice.Items)
        {
            var reservedRolls = await context.FabricRolls.AsNoTracking()
                .Where(r =>
                    r.ContainerId == invoice.ChinaContainerId &&
                    r.WarehouseId == invoice.WarehouseId &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId &&
                    r.Status == (int)FabricRollStatus.Reserved)
                .OrderBy(r => r.RollNumber)
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            var details = invoice.RollDetails.Where(d => d.SalesInvoiceItemId == item.Id).ToList();
            for (var i = 0; i < details.Count && i < reservedRolls.Count; i++)
            {
                if (!details[i].FabricRollId.HasValue)
                    invoice.AssignFabricRollToDetail(details[i].Id, reservedRolls[i]);
            }
        }
    }

    public async Task<decimal> DeductForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default)
    {
        decimal cogsTotal = 0m;

        foreach (var detail in invoice.RollDetails.Where(d => d.HasValidLength))
        {
            var item = invoice.Items.First(i => i.Id == detail.SalesInvoiceItemId);
            var meters = detail.LengthMeters.Value;

            FabricRollEntity? roll = null;
            if (detail.FabricRollId.HasValue)
            {
                roll = await context.FabricRolls
                    .FirstOrDefaultAsync(r => r.Id == detail.FabricRollId.Value, cancellationToken);
            }

            roll ??= await context.FabricRolls
                .Where(r =>
                    r.ContainerId == invoice.ChinaContainerId &&
                    r.WarehouseId == invoice.WarehouseId &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId &&
                    (r.Status == (int)FabricRollStatus.Reserved || r.Status == (int)FabricRollStatus.Available))
                .OrderBy(r => r.RollNumber)
                .FirstOrDefaultAsync(cancellationToken);

            if (roll is null)
                throw new InventoryException("Fabric roll not found for deduction.");

            if (meters > roll.RemainingLengthMeters)
                throw new InventoryException("Cannot sell more meters than remaining on roll.");

            cogsTotal += meters * roll.CostPerMeter;
            roll.RemainingLengthMeters -= meters;
            if (roll.RemainingLengthMeters <= 0)
            {
                roll.RemainingLengthMeters = 0;
                roll.Status = (int)FabricRollStatus.Sold;
            }

            var stock = await context.WarehouseStocks
                .FirstAsync(s =>
                    s.WarehouseId == invoice.WarehouseId &&
                    s.ContainerId == invoice.ChinaContainerId &&
                    s.FabricItemId == item.FabricItemId &&
                    s.FabricColorId == item.FabricColorId, cancellationToken);

            stock.TotalMeters -= meters;
            if (stock.ReservedMeters >= meters)
                stock.ReservedMeters -= meters;
            else
                stock.AvailableMeters = Math.Max(0, stock.AvailableMeters - (meters - stock.ReservedMeters));

            stock.ReservedMeters = Math.Max(0, stock.ReservedMeters);

            if (roll.Status == (int)FabricRollStatus.Sold && stock.RollCount > 0)
                stock.RollCount -= 1;
        }

        await context.StockMovements.AddAsync(new StockMovementEntity
        {
            Id = Guid.NewGuid(),
            MovementNumber = $"SAL-{invoice.InvoiceNumber.Value}-{DateTime.UtcNow:yyyyMMddHHmmss}",
            MovementDate = DateTime.UtcNow,
            Type = (int)MovementType.Sale,
            WarehouseId = invoice.WarehouseId,
            ReferenceType = (int)DocumentType.SalesInvoice,
            ReferenceId = invoice.Id,
            Status = (int)StockMovementStatus.Posted,
            PostedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return cogsTotal;
    }

    public async Task ReleaseForInvoiceAsync(SalesInvoiceAggregate invoice, CancellationToken cancellationToken = default)
    {
        if (invoice.Status is not (SalesInvoiceStatus.AwaitingDetailing or SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
            return;

        foreach (var item in invoice.Items)
        {
            var reservedRolls = await context.FabricRolls
                .Where(r =>
                    r.ContainerId == invoice.ChinaContainerId &&
                    r.WarehouseId == invoice.WarehouseId &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId &&
                    r.Status == (int)FabricRollStatus.Reserved)
                .ToListAsync(cancellationToken);

            var stock = await context.WarehouseStocks
                .FirstOrDefaultAsync(s =>
                    s.WarehouseId == invoice.WarehouseId &&
                    s.ContainerId == invoice.ChinaContainerId &&
                    s.FabricItemId == item.FabricItemId &&
                    s.FabricColorId == item.FabricColorId, cancellationToken);

            if (stock is null)
                continue;

            var releaseMeters = reservedRolls.Sum(r => r.RemainingLengthMeters);
            foreach (var roll in reservedRolls)
                roll.Status = (int)FabricRollStatus.Available;

            stock.ReservedMeters = Math.Max(0, stock.ReservedMeters - releaseMeters);
            stock.AvailableMeters += releaseMeters;
        }
    }
}
