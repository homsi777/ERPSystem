using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class InventoryRepository(ErpDbContext context) : IInventoryRepository
{
    public async Task<bool> IsStockPostedForContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default) =>
        await context.WarehouseStocks.AsNoTracking()
            .AnyAsync(s => s.ContainerId == containerId, cancellationToken);

    public async Task<ContainerInventoryMetricsDto?> GetContainerMetricsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.ContainerId == containerId)
            .ToListAsync(cancellationToken);
        if (stocks.Count == 0)
            return null;

        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.ContainerId == containerId)
            .ToListAsync(cancellationToken);

        var totalMeters = stocks.Sum(s => s.TotalMeters);
        var reservedMeters = stocks.Sum(s => s.ReservedMeters);
        var availableMeters = stocks.Sum(s => s.AvailableMeters);
        var soldMeters = rolls
            .Where(r => r.Status == (int)FabricRollStatus.Sold)
            .Sum(r => r.LengthMeters - r.RemainingLengthMeters);
        soldMeters += rolls
            .Where(r => r.Status == (int)FabricRollStatus.Sold && r.RemainingLengthMeters <= 0)
            .Sum(r => 0m);
        soldMeters = Math.Max(0, totalMeters - availableMeters - reservedMeters);

        var avgCost = rolls.Count > 0 ? rolls.Average(r => r.CostPerMeter) : 0m;

        return new ContainerInventoryMetricsDto
        {
            IsStockPosted = true,
            TotalRolls = rolls.Count > 0 ? rolls.Count : stocks.Sum(s => s.RollCount),
            TotalMeters = totalMeters,
            ReservedMeters = reservedMeters,
            AvailableMeters = availableMeters,
            SoldMeters = soldMeters,
            ReservedRolls = rolls.Count(r => r.Status == (int)FabricRollStatus.Reserved),
            SoldRolls = rolls.Count(r => r.Status == (int)FabricRollStatus.Sold),
            AvailableRolls = rolls.Count(r => r.Status == (int)FabricRollStatus.Available),
            CostPerMeter = avgCost,
            InventoryValuation = availableMeters * avgCost
        };
    }

    public async Task<IReadOnlyList<FabricRollInventoryDto>> GetAvailableRollsForContainerAsync(
        Guid containerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.ContainerId == containerId &&
                        r.WarehouseId == warehouseId &&
                        r.Status == (int)FabricRollStatus.Available &&
                        r.RemainingLengthMeters > 0)
            .OrderBy(r => r.RollNumber)
            .ToListAsync(cancellationToken);

        if (rolls.Count == 0)
            return [];

        var fabricIds = rolls.Select(r => r.FabricItemId).Distinct().ToList();
        var colorIds = rolls.Select(r => r.FabricColorId).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return rolls.Select(r => new FabricRollInventoryDto
        {
            Id = r.Id,
            ContainerId = r.ContainerId,
            RollNumber = r.RollNumber,
            FabricItemId = r.FabricItemId,
            FabricColorId = r.FabricColorId,
            FabricCode = fabrics.GetValueOrDefault(r.FabricItemId)?.Code ?? "—",
            FabricName = fabrics.GetValueOrDefault(r.FabricItemId)?.NameAr ?? "—",
            ColorName = colors.GetValueOrDefault(r.FabricColorId)?.NameAr ?? "—",
            LengthMeters = r.LengthMeters,
            RemainingLengthMeters = r.RemainingLengthMeters,
            CostPerMeter = r.CostPerMeter,
            SalePricePerMeter = r.SalePricePerMeter,
            WarehouseId = r.WarehouseId,
            Status = ((FabricRollStatus)r.Status).ToString()
        }).ToList();
    }

    public async Task<IReadOnlyList<Guid>> GetWarehousesWithContainerStockAsync(
        Guid containerId,
        CancellationToken cancellationToken = default) =>
        await context.FabricRolls.AsNoTracking()
            .Where(r => r.ContainerId == containerId &&
                        r.Status == (int)FabricRollStatus.Available &&
                        r.RemainingLengthMeters > 0)
            .Select(r => r.WarehouseId)
            .Distinct()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetSellableContainerIdsAsync(
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.FabricRolls.AsNoTracking()
            .Where(r => r.Status == (int)FabricRollStatus.Available &&
                        r.RemainingLengthMeters > 0);
        if (warehouseId is { } wh && wh != Guid.Empty)
            query = query.Where(r => r.WarehouseId == wh);

        return await query
            .Select(r => r.ContainerId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SellableContainerDto>> GetSellableContainersAsync(
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var sellableIds = await GetSellableContainerIdsAsync(warehouseId, cancellationToken);
        if (sellableIds.Count == 0)
            return [];

        var result = new List<SellableContainerDto>();
        if (sellableIds.Contains(Guid.Empty))
        {
            result.Add(new SellableContainerDto
            {
                Id = Guid.Empty,
                ContainerNumber = "مخزون أول المدة",
                Notes = "حاوية مواد أول المدة",
                DplQuantityUnit = DplQuantityUnit.Meters
            });
        }

        var realIds = sellableIds.Where(id => id != Guid.Empty).ToList();
        if (realIds.Count == 0)
            return result;

        // Ignore soft-delete filter so sellable opening-stock stubs always resolve by Id.
        var containers = await context.Containers.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(c => realIds.Contains(c.Id) && !c.IsArchived)
            .Select(c => new SellableContainerDto
            {
                Id = c.Id,
                ContainerNumber = c.ContainerNumber,
                Notes = c.Notes,
                DplQuantityUnit = c.DplQuantityUnit.HasValue
                    ? (DplQuantityUnit)c.DplQuantityUnit.Value
                    : null
            })
            .OrderBy(c => c.ContainerNumber)
            .ToListAsync(cancellationToken);

        result.AddRange(containers);
        return result;
    }

    public async Task<int> CountLowStockItemsAsync(
        Guid branchId,
        decimal thresholdMeters = 50m,
        CancellationToken cancellationToken = default)
    {
        var warehouseIds = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId)
            .Select(w => w.Id)
            .ToListAsync(cancellationToken);

        return await context.WarehouseStocks.AsNoTracking()
            .Where(s => warehouseIds.Contains(s.WarehouseId) && s.AvailableMeters > 0 && s.AvailableMeters <= thresholdMeters)
            .CountAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, decimal>> GetRollCostsAsync(
        IReadOnlyCollection<Guid> rollIds,
        CancellationToken cancellationToken = default)
    {
        if (rollIds.Count == 0)
            return new Dictionary<Guid, decimal>();

        return await context.FabricRolls.AsNoTracking()
            .Where(r => rollIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.CostPerMeter, cancellationToken);
    }
}
