using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class ContainerWarehouseImportService(
    ErpDbContext context,
    IIntegratedAccountingService accountingService) : IContainerWarehouseImportService
{
    public async Task PostContainerStockAsync(
        Guid warehouseId,
        ContainerAggregate container,
        CancellationToken cancellationToken = default)
    {
        var warehouseExists = await context.Warehouses.AsNoTracking()
            .AnyAsync(w => w.Id == warehouseId, cancellationToken);
        if (!warehouseExists)
            throw new ValidationException("Warehouse not found.");

        var alreadyPosted = await context.WarehouseStocks.AsNoTracking()
            .AnyAsync(s => s.ContainerId == container.Id, cancellationToken);
        if (alreadyPosted)
            throw new ValidationException("Container stock was already posted to warehouse.");

        var validItems = container.Items.Where(i => i.IsValid).ToList();
        if (validItems.Count == 0)
            throw new ValidationException("Container has no valid items to post.");

        var costPerMeter = CalculateCostPerMeter(container);
        var typeLineCosts = container.FabricTypeLines
            .Where(t => t.FabricItemId.HasValue && t.FabricColorId.HasValue)
            .ToDictionary(t => (t.FabricItemId!.Value, t.FabricColorId!.Value));
        var stockGroups = validItems
            .GroupBy(i => (i.FabricItemId, i.FabricColorId))
            .Select(g => new
            {
                g.Key.FabricItemId,
                g.Key.FabricColorId,
                RollCount = g.Sum(x => x.RollCount),
                TotalMeters = g.Sum(x => x.LengthMeters.Value)
            })
            .Where(g => g.TotalMeters > 0)
            .ToList();

        if (stockGroups.Count == 0)
            throw new ValidationException("Container has no meter quantity to post.");

        var now = DateTime.UtcNow;
        var rollSequence = 0;

        foreach (var item in validItems)
        {
            var perRollMeters = item.RollCount > 0
                ? item.LengthMeters.Value / item.RollCount
                : item.LengthMeters.Value;
            var rollsToCreate = Math.Max(1, item.RollCount);

            for (var i = 0; i < rollsToCreate; i++)
            {
                rollSequence++;
                var length = rollsToCreate == 1
                    ? item.LengthMeters.Value
                    : (i == rollsToCreate - 1
                        ? item.LengthMeters.Value - perRollMeters * (rollsToCreate - 1)
                        : perRollMeters);

                var key = (item.FabricItemId, item.FabricColorId);
                var rollCost = typeLineCosts.TryGetValue(key, out var typeLine) && typeLine.LandedCostPerMeterUsd > 0
                    ? typeLine.LandedCostPerMeterUsd * container.ExchangeRateToLocalCurrency
                    : costPerMeter;
                var rollSalePrice = typeLine is not null && typeLine.SalePricePerMeterUsd > 0
                    ? typeLine.SalePricePerMeterUsd * container.ExchangeRateToLocalCurrency
                    : (decimal?)null;

                await context.FabricRolls.AddAsync(new FabricRollEntity
                {
                    Id = Guid.NewGuid(),
                    ContainerId = container.Id,
                    ContainerItemId = item.Id,
                    FabricItemId = item.FabricItemId,
                    FabricColorId = item.FabricColorId,
                    WarehouseId = warehouseId,
                    RollNumber = item.LineNumber * 1000 + i + 1,
                    LengthMeters = length,
                    RemainingLengthMeters = length,
                    CostPerMeter = rollCost,
                    SalePricePerMeter = rollSalePrice,
                    WeightKg = item.WeightKg?.Value,
                    LotCode = item.LotCode,
                    Status = (int)FabricRollStatus.Available,
                    CreatedAt = now
                }, cancellationToken);
            }
        }

        foreach (var group in stockGroups)
        {
            await context.WarehouseStocks.AddAsync(new WarehouseStockEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                FabricItemId = group.FabricItemId,
                FabricColorId = group.FabricColorId,
                ContainerId = container.Id,
                RollCount = group.RollCount,
                TotalMeters = group.TotalMeters,
                ReservedMeters = 0m,
                AvailableMeters = group.TotalMeters,
                CreatedAt = now
            }, cancellationToken);
        }

        await context.StockMovements.AddAsync(new StockMovementEntity
        {
            Id = Guid.NewGuid(),
            MovementNumber = BuildMovementNumber(container.ContainerNumber.Value, now),
            MovementDate = now,
            Type = (int)MovementType.Import,
            WarehouseId = warehouseId,
            ReferenceType = (int)DocumentType.ChinaContainer,
            ReferenceId = container.Id,
            Status = (int)StockMovementStatus.Posted,
            PostedAt = now,
            CreatedAt = now
        }, cancellationToken);

        var inventoryValue = stockGroups.Sum(g => g.TotalMeters) * costPerMeter;
        await accountingService.PostInventoryActivationAsync(container, warehouseId, inventoryValue, cancellationToken);
    }

    private static decimal CalculateCostPerMeter(ContainerAggregate container)
    {
        if (container.LandingCost is null || container.TotalMeters.Value <= 0)
            return 0m;

        var landing = container.LandingCost;
        var perMeter = landing.TotalSharedExpenses.Amount / container.TotalMeters.Value;
        return perMeter * container.ExchangeRateToLocalCurrency;
    }

    private static string BuildMovementNumber(string containerNumber, DateTime utcNow)
    {
        var safe = new string(containerNumber.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(safe))
            safe = "CONT";
        return $"IMP-{safe}-{utcNow:yyyyMMddHHmmss}";
    }
}
