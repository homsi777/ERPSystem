using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class PurchaseInventoryService(ErpDbContext context) : IPurchaseInventoryService
{
    public async Task PostPurchaseInvoiceStockAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        if (!invoice.WarehouseId.HasValue)
            throw new ValidationException("Warehouse is required for inventory lines.");

        var warehouseId = invoice.WarehouseId.Value;
        var inventoryLines = invoice.Items.Where(i => i.LineType == PurchaseLineType.Inventory).ToList();
        if (inventoryLines.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var lotCode = $"PINV-{invoice.InvoiceNumber}";
        var rollSequence = await context.FabricRolls.AsNoTracking().CountAsync(cancellationToken);

        foreach (var line in inventoryLines)
        {
            if (line.FabricItemId is not Guid fabricItemId || line.Quantity.Value <= 0)
                continue;

            var colorId = line.FabricColorId ?? await ResolveDefaultColorIdAsync(fabricItemId, cancellationToken);
            var rollsToCreate = Math.Max(1, line.RollCount);
            var perRollMeters = line.Quantity.Value / rollsToCreate;
            var costPerMeter = line.UnitPrice.Amount;

            for (var i = 0; i < rollsToCreate; i++)
            {
                rollSequence++;
                var length = rollsToCreate == 1
                    ? line.Quantity.Value
                    : (i == rollsToCreate - 1
                        ? line.Quantity.Value - perRollMeters * (rollsToCreate - 1)
                        : perRollMeters);

                await context.FabricRolls.AddAsync(new FabricRollEntity
                {
                    Id = Guid.NewGuid(),
                    ContainerId = Guid.Empty,
                    FabricItemId = fabricItemId,
                    FabricColorId = colorId,
                    WarehouseId = warehouseId,
                    RollNumber = rollSequence,
                    LengthMeters = length,
                    RemainingLengthMeters = length,
                    CostPerMeter = costPerMeter,
                    LotCode = lotCode,
                    Status = (int)FabricRollStatus.Available,
                    CreatedAt = now
                }, cancellationToken);
            }

            var existingStock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == warehouseId &&
                     s.FabricItemId == fabricItemId &&
                     s.FabricColorId == colorId &&
                     s.ContainerId == Guid.Empty,
                cancellationToken);

            if (existingStock is null)
            {
                await context.WarehouseStocks.AddAsync(new WarehouseStockEntity
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = warehouseId,
                    FabricItemId = fabricItemId,
                    FabricColorId = colorId,
                    ContainerId = Guid.Empty,
                    RollCount = rollsToCreate,
                    TotalMeters = line.Quantity.Value,
                    ReservedMeters = 0m,
                    AvailableMeters = line.Quantity.Value,
                    CreatedAt = now
                }, cancellationToken);
            }
            else
            {
                existingStock.RollCount += rollsToCreate;
                existingStock.TotalMeters += line.Quantity.Value;
                existingStock.AvailableMeters += line.Quantity.Value;
                existingStock.UpdatedAt = now;
            }
        }

        await context.StockMovements.AddAsync(new StockMovementEntity
        {
            Id = Guid.NewGuid(),
            MovementNumber = $"PUR-{invoice.InvoiceNumber}-{now:yyyyMMddHHmmss}",
            MovementDate = invoice.InvoiceDate,
            Type = (int)MovementType.Import,
            WarehouseId = warehouseId,
            ReferenceType = (int)DocumentType.PurchaseInvoice,
            ReferenceId = invoice.Id,
            Status = (int)StockMovementStatus.Posted,
            PostedAt = now,
            CreatedAt = now
        }, cancellationToken);
    }

    public async Task ReversePurchaseReturnStockAsync(
        PurchaseReturn purchaseReturn,
        PurchaseInvoice originalInvoice,
        CancellationToken cancellationToken = default)
    {
        if (!originalInvoice.WarehouseId.HasValue)
            return;

        var warehouseId = originalInvoice.WarehouseId.Value;
        var lotCode = $"PINV-{originalInvoice.InvoiceNumber}";
        var now = DateTime.UtcNow;

        foreach (var line in purchaseReturn.Lines.Where(l => l.LineType == PurchaseLineType.Inventory))
        {
            if (line.FabricItemId is not Guid fabricItemId || line.QuantityMeters <= 0)
                continue;

            var colorId = line.FabricColorId ?? await ResolveDefaultColorIdAsync(fabricItemId, cancellationToken);
            var rolls = await context.FabricRolls
                .Where(r => r.LotCode == lotCode &&
                            r.FabricItemId == fabricItemId &&
                            r.FabricColorId == colorId &&
                            r.RemainingLengthMeters > 0)
                .OrderByDescending(r => r.RemainingLengthMeters)
                .ToListAsync(cancellationToken);

            var remaining = line.QuantityMeters;
            foreach (var roll in rolls)
            {
                if (remaining <= 0) break;
                var deduct = Math.Min(remaining, roll.RemainingLengthMeters);
                roll.RemainingLengthMeters -= deduct;
                if (roll.RemainingLengthMeters <= 0)
                    roll.Status = (int)FabricRollStatus.Wasted;
                remaining -= deduct;
            }

            var stock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == warehouseId &&
                     s.FabricItemId == fabricItemId &&
                     s.FabricColorId == colorId &&
                     s.ContainerId == Guid.Empty,
                cancellationToken);
            if (stock is not null)
            {
                stock.AvailableMeters = Math.Max(0, stock.AvailableMeters - line.QuantityMeters);
                stock.TotalMeters = Math.Max(0, stock.TotalMeters - line.QuantityMeters);
                stock.UpdatedAt = now;
            }
        }

        await context.StockMovements.AddAsync(new StockMovementEntity
        {
            Id = Guid.NewGuid(),
            MovementNumber = $"PRET-{purchaseReturn.ReturnNumber}-{now:yyyyMMddHHmmss}",
            MovementDate = purchaseReturn.ReturnDate,
            Type = (int)MovementType.Adjustment,
            WarehouseId = warehouseId,
            ReferenceType = (int)DocumentType.PurchaseReturn,
            ReferenceId = purchaseReturn.Id,
            Status = (int)StockMovementStatus.Posted,
            PostedAt = now,
            CreatedAt = now
        }, cancellationToken);
    }

    private async Task<Guid> ResolveDefaultColorIdAsync(Guid fabricItemId, CancellationToken cancellationToken)
    {
        var color = await context.FabricColors.AsNoTracking()
            .Where(c => c.FabricItemId == fabricItemId)
            .OrderBy(c => c.Code)
            .FirstOrDefaultAsync(cancellationToken);
        if (color is null)
            throw new ValidationException("Fabric color is required for inventory lines.");
        return color.Id;
    }
}
