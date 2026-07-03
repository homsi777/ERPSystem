using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class InventoryEngine(
    ErpDbContext context,
    IIntegratedAccountingService accountingService) : IInventoryEngine
{
    public async Task PostContainerImportAsync(
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

        var costPerMeter = CalculateContainerCostPerMeter(container);
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
        var batchNumber = $"BATCH-{container.ContainerNumber.Value}-{now:yyyyMMdd}";
        var batch = new FabricBatchEntity
        {
            Id = Guid.NewGuid(),
            BatchNumber = batchNumber,
            ContainerId = container.Id,
            ArrivalDate = now,
            LandingCostPerMeter = costPerMeter,
            CurrencyCode = "USD",
            TotalMeters = stockGroups.Sum(g => g.TotalMeters),
            RollCount = stockGroups.Sum(g => g.RollCount),
            WarehouseId = warehouseId,
            QualityStatus = (int)InventoryQualityStatus.Good,
            Status = (int)FabricBatchStatus.Active,
            CreatedAt = now
        };
        await context.FabricBatches.AddAsync(batch, cancellationToken);

        var movementId = Guid.NewGuid();
        var movementNumber = BuildMovementNumber(container.ContainerNumber.Value, now);
        var movementLines = new List<StockMovementLineEntity>();

        foreach (var item in validItems)
        {
            var perRollMeters = item.RollCount > 0
                ? item.LengthMeters.Value / item.RollCount
                : item.LengthMeters.Value;
            var rollsToCreate = Math.Max(1, item.RollCount);

            for (var i = 0; i < rollsToCreate; i++)
            {
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

                var rollId = Guid.NewGuid();
                await context.FabricRolls.AddAsync(new FabricRollEntity
                {
                    Id = rollId,
                    ContainerId = container.Id,
                    ContainerItemId = item.Id,
                    FabricBatchId = batch.Id,
                    FabricItemId = item.FabricItemId,
                    FabricColorId = item.FabricColorId,
                    WarehouseId = warehouseId,
                    RollNumber = item.LineNumber * 1000 + i + 1,
                    Barcode = $"ROLL-{rollId:N}"[..20],
                    LengthMeters = length,
                    RemainingLengthMeters = length,
                    CostPerMeter = rollCost,
                    SalePricePerMeter = rollSalePrice,
                    WeightKg = item.WeightKg?.Value,
                    LotCode = item.LotCode,
                    Status = (int)FabricRollStatus.Available,
                    QualityStatus = (int)InventoryQualityStatus.Good,
                    ReservationStatus = (int)InventoryReservationStatus.Available,
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

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = group.FabricItemId,
                FabricColorId = group.FabricColorId,
                FabricBatchId = batch.Id,
                ContainerId = container.Id,
                RollCount = group.RollCount,
                QuantityMeters = group.TotalMeters,
                UnitCost = costPerMeter,
                TotalValue = group.TotalMeters * costPerMeter,
                CurrencyCode = "USD",
                CreatedAt = now
            });
        }

        await PostMovementAsync(movementId, movementNumber, MovementType.Import, warehouseId,
            DocumentType.ChinaContainer, container.Id, movementLines, now, cancellationToken);

        var inventoryValue = stockGroups.Sum(g => g.TotalMeters) * costPerMeter;
        await accountingService.PostInventoryActivationAsync(container, warehouseId, inventoryValue, cancellationToken);
        await RecordValuationSnapshotAsync(warehouseId, movementId, ValuationMethod.LandingCost, cancellationToken);
    }

    public async Task PostPurchaseInvoiceAsync(
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
        var batchNumber = $"BATCH-{lotCode}";
        var rollSequence = await context.FabricRolls.AsNoTracking().CountAsync(cancellationToken);
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();
        decimal totalBatchMeters = 0;
        var totalRolls = 0;

        var batch = new FabricBatchEntity
        {
            Id = Guid.NewGuid(),
            BatchNumber = batchNumber,
            SupplierId = invoice.SupplierId,
            PurchaseInvoiceId = invoice.Id,
            ArrivalDate = invoice.InvoiceDate,
            LandingCostPerMeter = inventoryLines.Average(l => l.UnitPrice.Amount),
            CurrencyCode = invoice.CurrencyCode,
            WarehouseId = warehouseId,
            QualityStatus = (int)InventoryQualityStatus.Good,
            Status = (int)FabricBatchStatus.Active,
            CreatedAt = now
        };

        foreach (var line in inventoryLines)
        {
            if (line.FabricItemId is not Guid fabricItemId || line.Quantity.Value <= 0)
                continue;

            var colorId = line.FabricColorId ?? await ResolveDefaultColorIdAsync(fabricItemId, cancellationToken);
            var rollsToCreate = Math.Max(1, line.RollCount);
            var perRollMeters = line.Quantity.Value / rollsToCreate;
            var costPerMeter = line.UnitPrice.Amount;
            totalBatchMeters += line.Quantity.Value;
            totalRolls += rollsToCreate;

            for (var i = 0; i < rollsToCreate; i++)
            {
                rollSequence++;
                var length = rollsToCreate == 1
                    ? line.Quantity.Value
                    : (i == rollsToCreate - 1
                        ? line.Quantity.Value - perRollMeters * (rollsToCreate - 1)
                        : perRollMeters);

                var rollId = Guid.NewGuid();
                await context.FabricRolls.AddAsync(new FabricRollEntity
                {
                    Id = rollId,
                    ContainerId = Guid.Empty,
                    FabricBatchId = batch.Id,
                    FabricItemId = fabricItemId,
                    FabricColorId = colorId,
                    WarehouseId = warehouseId,
                    RollNumber = rollSequence,
                    Barcode = $"ROLL-{rollId:N}"[..20],
                    LengthMeters = length,
                    RemainingLengthMeters = length,
                    CostPerMeter = costPerMeter,
                    LotCode = lotCode,
                    Status = (int)FabricRollStatus.Available,
                    QualityStatus = (int)InventoryQualityStatus.Good,
                    ReservationStatus = (int)InventoryReservationStatus.Available,
                    CreatedAt = now
                }, cancellationToken);
            }

            await UpsertWarehouseStockAsync(warehouseId, fabricItemId, colorId, Guid.Empty,
                rollsToCreate, line.Quantity.Value, now, cancellationToken);

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = fabricItemId,
                FabricColorId = colorId,
                FabricBatchId = batch.Id,
                RollCount = rollsToCreate,
                QuantityMeters = line.Quantity.Value,
                UnitCost = costPerMeter,
                TotalValue = line.Quantity.Value * costPerMeter,
                CurrencyCode = invoice.CurrencyCode,
                CreatedAt = now
            });
        }

        batch.TotalMeters = totalBatchMeters;
        batch.RollCount = totalRolls;
        await context.FabricBatches.AddAsync(batch, cancellationToken);

        await PostMovementAsync(movementId, $"PUR-{invoice.InvoiceNumber}-{now:yyyyMMddHHmmss}",
            MovementType.Purchase, warehouseId, DocumentType.PurchaseInvoice, invoice.Id,
            movementLines, now, cancellationToken);
        await RecordValuationSnapshotAsync(warehouseId, movementId, ValuationMethod.SpecificCost, cancellationToken);
    }

    public async Task ReversePurchaseReturnAsync(
        PurchaseReturn purchaseReturn,
        PurchaseInvoice originalInvoice,
        CancellationToken cancellationToken = default)
    {
        if (!originalInvoice.WarehouseId.HasValue)
            return;

        var warehouseId = originalInvoice.WarehouseId.Value;
        var lotCode = $"PINV-{originalInvoice.InvoiceNumber}";
        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();

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
                     s.ContainerId == Guid.Empty, cancellationToken);
            if (stock is not null)
            {
                stock.AvailableMeters = Math.Max(0, stock.AvailableMeters - line.QuantityMeters);
                stock.TotalMeters = Math.Max(0, stock.TotalMeters - line.QuantityMeters);
                stock.UpdatedAt = now;
            }

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = fabricItemId,
                FabricColorId = colorId,
                QuantityMeters = -line.QuantityMeters,
                UnitCost = line.UnitPrice.Amount,
                TotalValue = -line.QuantityMeters * line.UnitPrice.Amount,
                CurrencyCode = originalInvoice.CurrencyCode,
                CreatedAt = now
            });
        }

        await PostMovementAsync(movementId, $"PRET-{purchaseReturn.ReturnNumber}-{now:yyyyMMddHHmmss}",
            MovementType.PurchaseReturn, warehouseId, DocumentType.PurchaseReturn, purchaseReturn.Id,
            movementLines, now, cancellationToken);
    }

    public async Task ReserveForInvoiceAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default)
    {
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
            {
                roll.Status = (int)FabricRollStatus.Reserved;
                roll.ReservationStatus = (int)InventoryReservationStatus.Reserved;
            }

            stock.ReservedMeters += reserveMeters;
            stock.AvailableMeters -= reserveMeters;

            await context.InventoryReservations.AddAsync(new InventoryReservationEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = invoice.WarehouseId,
                FabricItemId = item.FabricItemId,
                FabricColorId = item.FabricColorId,
                ReservedMeters = reserveMeters,
                RollCount = item.RollCount,
                Status = (int)InventoryReservationStatus.Reserved,
                Strategy = (int)AllocationStrategy.Fifo,
                ReferenceType = (int)DocumentType.SalesInvoice,
                ReferenceId = invoice.Id,
                ReferenceLineId = item.Id,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
    }

    public async Task AssignFabricRollsOnDetailingAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default)
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

    public async Task<decimal> IssueForInvoiceAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default)
    {
        decimal cogsTotal = 0m;
        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();

        foreach (var detail in invoice.RollDetails.Where(d => d.HasValidLength))
        {
            var item = invoice.Items.First(i => i.Id == detail.SalesInvoiceItemId);
            var meters = detail.LengthMeters.Value;

            FabricRollEntity? roll = null;
            if (detail.FabricRollId.HasValue)
                roll = await context.FabricRolls.FirstOrDefaultAsync(r => r.Id == detail.FabricRollId.Value, cancellationToken);

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
                roll.ReservationStatus = (int)InventoryReservationStatus.Sold;
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

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = item.FabricItemId,
                FabricColorId = item.FabricColorId,
                FabricRollId = roll.Id,
                FabricBatchId = roll.FabricBatchId,
                ContainerId = invoice.ChinaContainerId,
                QuantityMeters = -meters,
                UnitCost = roll.CostPerMeter,
                TotalValue = -meters * roll.CostPerMeter,
                CurrencyCode = "USD",
                CreatedAt = now
            });
        }

        await PostMovementAsync(movementId, $"SAL-{invoice.InvoiceNumber.Value}-{now:yyyyMMddHHmmss}",
            MovementType.Sale, invoice.WarehouseId, DocumentType.SalesInvoice, invoice.Id,
            movementLines, now, cancellationToken);
        await RecordValuationSnapshotAsync(invoice.WarehouseId, movementId, ValuationMethod.SpecificCost, cancellationToken);

        return cogsTotal;
    }

    public async Task ReleaseForInvoiceAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default)
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

            if (stock is null) continue;

            var releaseMeters = reservedRolls.Sum(r => r.RemainingLengthMeters);
            foreach (var roll in reservedRolls)
            {
                roll.Status = (int)FabricRollStatus.Available;
                roll.ReservationStatus = (int)InventoryReservationStatus.Available;
            }

            stock.ReservedMeters = Math.Max(0, stock.ReservedMeters - releaseMeters);
            stock.AvailableMeters += releaseMeters;
        }

        var reservations = await context.InventoryReservations
            .Where(r => r.ReferenceType == (int)DocumentType.SalesInvoice && r.ReferenceId == invoice.Id)
            .ToListAsync(cancellationToken);
        foreach (var r in reservations)
            r.Status = (int)InventoryReservationStatus.Cancelled;
    }

    public async Task<Guid> PostOpeningStockAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        var doc = await context.OpeningStockDocuments
            .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new ValidationException("Opening stock document not found.");

        if (doc.Status == (int)InventoryDocumentStatus.Posted)
            throw new ValidationException("Opening stock already posted.");

        var lines = await context.OpeningStockLines
            .Where(l => l.DocumentId == documentId)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
            throw new ValidationException("Opening stock has no lines.");

        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();
        var rollSequence = await context.FabricRolls.AsNoTracking().CountAsync(cancellationToken);

        foreach (var line in lines)
        {
            var rollsToCreate = Math.Max(1, line.RollCount);
            var perRoll = line.QuantityMeters / rollsToCreate;

            for (var i = 0; i < rollsToCreate; i++)
            {
                rollSequence++;
                var length = rollsToCreate == 1 ? line.QuantityMeters : perRoll;
                var rollId = Guid.NewGuid();
                await context.FabricRolls.AddAsync(new FabricRollEntity
                {
                    Id = rollId,
                    ContainerId = Guid.Empty,
                    FabricBatchId = line.FabricBatchId,
                    FabricItemId = line.FabricItemId,
                    FabricColorId = line.FabricColorId,
                    WarehouseId = doc.WarehouseId,
                    StorageLocationId = line.StorageLocationId,
                    RollNumber = rollSequence,
                    Barcode = $"ROLL-{rollId:N}"[..20],
                    LengthMeters = length,
                    RemainingLengthMeters = length,
                    CostPerMeter = line.UnitCost,
                    Status = (int)FabricRollStatus.Available,
                    QualityStatus = (int)InventoryQualityStatus.Good,
                    ReservationStatus = (int)InventoryReservationStatus.Available,
                    CreatedAt = now
                }, cancellationToken);
            }

            await UpsertWarehouseStockAsync(doc.WarehouseId, line.FabricItemId, line.FabricColorId,
                Guid.Empty, rollsToCreate, line.QuantityMeters, now, cancellationToken);

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = line.FabricItemId,
                FabricColorId = line.FabricColorId,
                FabricBatchId = line.FabricBatchId,
                RollCount = rollsToCreate,
                QuantityMeters = line.QuantityMeters,
                UnitCost = line.UnitCost,
                TotalValue = line.TotalValue,
                CurrencyCode = doc.CurrencyCode,
                CreatedAt = now
            });
        }

        await PostMovementAsync(movementId, doc.DocumentNumber, MovementType.OpeningBalance,
            doc.WarehouseId, DocumentType.OpeningBalance, doc.Id, movementLines, now, cancellationToken);

        doc.Status = (int)InventoryDocumentStatus.Posted;
        doc.PostedAt = now;
        await RecordValuationSnapshotAsync(doc.WarehouseId, movementId, ValuationMethod.AverageCost, cancellationToken);
        return movementId;
    }

    public async Task<Guid> CompleteTransferAsync(Guid transferId, CancellationToken cancellationToken = default)
    {
        var transfer = await context.StockTransfers
            .FirstOrDefaultAsync(t => t.Id == transferId, cancellationToken)
            ?? throw new ValidationException("Transfer not found.");

        if (transfer.Status == (int)InventoryDocumentStatus.Completed)
            throw new ValidationException("Transfer already completed.");

        var lines = await context.StockTransferLines
            .Where(l => l.TransferId == transferId)
            .ToListAsync(cancellationToken);
        if (lines.Count == 0)
            throw new ValidationException("Transfer has no lines.");

        var now = DateTime.UtcNow;
        var outMovementId = Guid.NewGuid();
        var inMovementId = Guid.NewGuid();
        var outLines = new List<StockMovementLineEntity>();
        var inLines = new List<StockMovementLineEntity>();

        foreach (var line in lines)
        {
            if (line.FabricRollId.HasValue)
            {
                var roll = await context.FabricRolls.FirstAsync(r => r.Id == line.FabricRollId.Value, cancellationToken);
                roll.WarehouseId = transfer.ToWarehouseId;
                roll.StorageLocationId = transfer.ToLocationId;
            }

            var fromStock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == transfer.FromWarehouseId &&
                     s.FabricItemId == line.FabricItemId &&
                     s.FabricColorId == line.FabricColorId, cancellationToken);
            if (fromStock is not null)
            {
                fromStock.TotalMeters = Math.Max(0, fromStock.TotalMeters - line.QuantityMeters);
                fromStock.AvailableMeters = Math.Max(0, fromStock.AvailableMeters - line.QuantityMeters);
                fromStock.UpdatedAt = now;
            }

            await UpsertWarehouseStockAsync(transfer.ToWarehouseId, line.FabricItemId, line.FabricColorId,
                Guid.Empty, line.RollCount, line.QuantityMeters, now, cancellationToken);

            var unitCost = fromStock is not null && fromStock.TotalMeters > 0 ? 0m : 0m;
            outLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(), MovementId = outMovementId,
                FabricItemId = line.FabricItemId, FabricColorId = line.FabricColorId,
                FabricRollId = line.FabricRollId, FabricBatchId = line.FabricBatchId,
                QuantityMeters = -line.QuantityMeters, RollCount = line.RollCount,
                UnitCost = unitCost, TotalValue = -line.QuantityMeters * unitCost,
                CurrencyCode = "USD", CreatedAt = now
            });
            inLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(), MovementId = inMovementId,
                FabricItemId = line.FabricItemId, FabricColorId = line.FabricColorId,
                FabricRollId = line.FabricRollId, FabricBatchId = line.FabricBatchId,
                QuantityMeters = line.QuantityMeters, RollCount = line.RollCount,
                UnitCost = unitCost, TotalValue = line.QuantityMeters * unitCost,
                CurrencyCode = "USD", CreatedAt = now
            });
        }

        await PostMovementAsync(outMovementId, $"{transfer.Number}-OUT", MovementType.Transfer,
            transfer.FromWarehouseId, DocumentType.StockTransfer, transfer.Id, outLines, now,
            cancellationToken, destinationWarehouseId: transfer.ToWarehouseId);
        await PostMovementAsync(inMovementId, $"{transfer.Number}-IN", MovementType.Transfer,
            transfer.ToWarehouseId, DocumentType.StockTransfer, transfer.Id, inLines, now,
            cancellationToken, sourceWarehouseId: transfer.FromWarehouseId);

        transfer.Status = (int)InventoryDocumentStatus.Completed;
        transfer.CompletedAt = now;
        return outMovementId;
    }

    public async Task<Guid> PostStocktakeAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await context.StocktakeSessions
            .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken)
            ?? throw new ValidationException("Stocktake session not found.");

        var lines = await context.StocktakeLines
            .Where(l => l.SessionId == sessionId && l.DifferenceMeters != 0)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();

        foreach (var line in lines)
        {
            var stock = await context.WarehouseStocks.FirstOrDefaultAsync(
                s => s.WarehouseId == session.WarehouseId &&
                     s.FabricItemId == line.FabricItemId &&
                     s.FabricColorId == line.FabricColorId, cancellationToken);
            if (stock is not null)
            {
                stock.TotalMeters += line.DifferenceMeters;
                stock.AvailableMeters += line.DifferenceMeters;
                stock.UpdatedAt = now;
            }

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(), MovementId = movementId,
                FabricItemId = line.FabricItemId, FabricColorId = line.FabricColorId,
                FabricRollId = line.FabricRollId,
                QuantityMeters = line.DifferenceMeters,
                UnitCost = 0, TotalValue = 0, CurrencyCode = "USD", CreatedAt = now
            });
        }

        await PostMovementAsync(movementId, session.SessionNumber, MovementType.Stocktake,
            session.WarehouseId, DocumentType.Stocktake, session.Id, movementLines, now, cancellationToken);

        session.Status = (int)InventoryDocumentStatus.Posted;
        session.PostedAt = now;
        return movementId;
    }

    public async Task RecordValuationSnapshotAsync(
        Guid warehouseId,
        Guid movementId,
        ValuationMethod method,
        CancellationToken cancellationToken = default)
    {
        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;

        foreach (var stock in stocks.Where(s => s.TotalMeters > 0))
        {
            var rolls = await context.FabricRolls.AsNoTracking()
                .Where(r => r.WarehouseId == warehouseId &&
                            r.FabricItemId == stock.FabricItemId &&
                            r.FabricColorId == stock.FabricColorId &&
                            r.RemainingLengthMeters > 0)
                .ToListAsync(cancellationToken);
            var unitCost = rolls.Count > 0 ? rolls.Average(r => r.CostPerMeter) : 0m;

            await context.InventoryValuationSnapshots.AddAsync(new InventoryValuationSnapshotEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                FabricItemId = stock.FabricItemId,
                FabricColorId = stock.FabricColorId,
                ContainerId = stock.ContainerId,
                Method = (int)method,
                QuantityMeters = stock.TotalMeters,
                UnitCost = unitCost,
                TotalValue = stock.TotalMeters * unitCost,
                CurrencyCode = "USD",
                SnapshotDate = now,
                MovementId = movementId,
                CreatedAt = now
            }, cancellationToken);
        }
    }

    private async Task PostMovementAsync(
        Guid movementId,
        string movementNumber,
        MovementType type,
        Guid warehouseId,
        DocumentType? referenceType,
        Guid? referenceId,
        IReadOnlyList<StockMovementLineEntity> lines,
        DateTime now,
        CancellationToken cancellationToken,
        Guid? sourceWarehouseId = null,
        Guid? destinationWarehouseId = null)
    {
        if (referenceType.HasValue && referenceId.HasValue)
        {
            var exists = await context.StockMovements.AsNoTracking()
                .AnyAsync(m => m.ReferenceType == (int)referenceType &&
                               m.ReferenceId == referenceId &&
                               m.Type == (int)type &&
                               m.Status == (int)StockMovementStatus.Posted, cancellationToken);
            if (exists && type is MovementType.Import or MovementType.Purchase or MovementType.OpeningBalance)
                throw new ValidationException("Stock movement already posted for this reference.");
        }

        await context.StockMovements.AddAsync(new StockMovementEntity
        {
            Id = movementId,
            MovementNumber = movementNumber,
            MovementDate = now,
            Type = (int)type,
            WarehouseId = warehouseId,
            SourceWarehouseId = sourceWarehouseId,
            DestinationWarehouseId = destinationWarehouseId,
            ReferenceType = referenceType.HasValue ? (int)referenceType : null,
            ReferenceId = referenceId,
            Status = (int)StockMovementStatus.Posted,
            PostedAt = now,
            CreatedAt = now
        }, cancellationToken);

        foreach (var line in lines)
            await context.StockMovementLines.AddAsync(line, cancellationToken);
    }

    private async Task UpsertWarehouseStockAsync(
        Guid warehouseId,
        Guid fabricItemId,
        Guid fabricColorId,
        Guid containerId,
        int rollCount,
        decimal meters,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var existing = await context.WarehouseStocks.FirstOrDefaultAsync(
            s => s.WarehouseId == warehouseId &&
                 s.FabricItemId == fabricItemId &&
                 s.FabricColorId == fabricColorId &&
                 s.ContainerId == containerId, cancellationToken);

        if (existing is null)
        {
            await context.WarehouseStocks.AddAsync(new WarehouseStockEntity
            {
                Id = Guid.NewGuid(),
                WarehouseId = warehouseId,
                FabricItemId = fabricItemId,
                FabricColorId = fabricColorId,
                ContainerId = containerId,
                RollCount = rollCount,
                TotalMeters = meters,
                ReservedMeters = 0m,
                AvailableMeters = meters,
                CreatedAt = now
            }, cancellationToken);
        }
        else
        {
            existing.RollCount += rollCount;
            existing.TotalMeters += meters;
            existing.AvailableMeters += meters;
            existing.UpdatedAt = now;
        }
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

    private static decimal CalculateContainerCostPerMeter(ContainerAggregate container)
    {
        if (container.LandingCost is null || container.TotalMeters.Value <= 0)
            return 0m;
        var perMeter = container.LandingCost.TotalSharedExpenses.Amount / container.TotalMeters.Value;
        return perMeter * container.ExchangeRateToLocalCurrency;
    }

    private static string BuildMovementNumber(string containerNumber, DateTime utcNow)
    {
        var safe = new string(containerNumber.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(safe)) safe = "CONT";
        return $"IMP-{safe}-{utcNow:yyyyMMddHHmmss}";
    }
}
