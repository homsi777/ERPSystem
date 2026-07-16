using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Finance;
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
    IIntegratedAccountingService accountingService,
    INumberingService numberingService) : IInventoryEngine
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

        var linkedInvoice = await context.PurchaseInvoices.AsNoTracking()
            .SingleOrDefaultAsync(p => p.SourceContainerId == container.Id && !p.IsArchived, cancellationToken)
            ?? throw new ValidationException("A posted purchase invoice linked to the container is required before inventory activation.");
        if (linkedInvoice.Status == (int)PurchaseInvoiceStatus.Draft || linkedInvoice.TotalAmount <= 0)
            throw new ValidationException("The container purchase invoice must be posted with a positive value before inventory activation.");

        var inventoryValue = linkedInvoice.TotalAmount;
        var totalMeters = validItems.Sum(i => i.LengthMeters.Value);
        var costPerMeter = inventoryValue / totalMeters;
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
        var rolls = new List<FabricRollEntity>();

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
                rolls.Add(new FabricRollEntity
                {
                    Id = rollId,
                    ContainerId = container.Id,
                    ContainerItemId = item.Id,
                    FabricBatchId = batch.Id,
                    FabricItemId = item.FabricItemId,
                    FabricColorId = item.FabricColorId,
                    WarehouseId = warehouseId,
                    RollNumber = item.SupplierRollNumber.HasValue && rollsToCreate == 1
                        ? item.SupplierRollNumber.Value
                        : item.LineNumber * 1000 + i + 1,
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
                    IsLegacyOpeningBalance = false,
                    LegacyLengthConfirmed = true,
                    CreatedAt = now
                });
            }
        }

        ReconcileRollCostsToInvoice(rolls, inventoryValue);
        await context.FabricRolls.AddRangeAsync(rolls, cancellationToken);

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

            var groupRolls = rolls.Where(r => r.FabricItemId == group.FabricItemId && r.FabricColorId == group.FabricColorId).ToList();
            var groupValue = groupRolls.Sum(r => r.LengthMeters * r.CostPerMeter);
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
                UnitCost = groupValue / group.TotalMeters,
                TotalValue = groupValue,
                CurrencyCode = "USD",
                CreatedAt = now
            });
        }

        await PostMovementAsync(movementId, movementNumber, MovementType.Import, warehouseId,
            DocumentType.ChinaContainer, container.Id, movementLines, now, cancellationToken);

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

    public async Task ReversePurchaseInvoiceAsync(
        PurchaseInvoice invoice,
        CancellationToken cancellationToken = default)
    {
        if (!invoice.WarehouseId.HasValue)
            return;

        var warehouseId = invoice.WarehouseId.Value;
        var lotCode = $"PINV-{invoice.InvoiceNumber}";
        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();

        foreach (var line in invoice.Items.Where(l => l.LineType == PurchaseLineType.Inventory))
        {
            if (line.FabricItemId is not Guid fabricItemId || line.Quantity.Value <= 0)
                continue;

            var colorId = line.FabricColorId ?? await ResolveDefaultColorIdAsync(fabricItemId, cancellationToken);
            var rolls = await context.FabricRolls
                .Where(r => r.LotCode == lotCode &&
                            r.FabricItemId == fabricItemId &&
                            r.FabricColorId == colorId &&
                            r.RemainingLengthMeters > 0)
                .OrderByDescending(r => r.RemainingLengthMeters)
                .ToListAsync(cancellationToken);

            var remaining = line.Quantity.Value;
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
                stock.AvailableMeters = Math.Max(0, stock.AvailableMeters - line.Quantity.Value);
                stock.TotalMeters = Math.Max(0, stock.TotalMeters - line.Quantity.Value);
                stock.UpdatedAt = now;
            }

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = fabricItemId,
                FabricColorId = colorId,
                QuantityMeters = -line.Quantity.Value,
                UnitCost = line.UnitPrice.Amount,
                TotalValue = -line.Quantity.Value * line.UnitPrice.Amount,
                CurrencyCode = invoice.CurrencyCode,
                CreatedAt = now
            });
        }

        if (movementLines.Count == 0)
            return;

        await PostMovementAsync(movementId, $"PINVREV-{invoice.InvoiceNumber}-{now:yyyyMMddHHmmss}",
            MovementType.PurchaseReturn, warehouseId, DocumentType.PurchaseInvoiceReversal, invoice.Id,
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
                    s.ContainerId == item.ChinaContainerId &&
                    s.FabricItemId == item.FabricItemId &&
                    s.FabricColorId == item.FabricColorId, cancellationToken)
                ?? throw new InventoryException("Warehouse stock row not found for reservation.");

            var rolls = await context.FabricRolls
                .Where(r =>
                    r.ContainerId == item.ChinaContainerId &&
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

            var details = invoice.RollDetails.Where(d => d.SalesInvoiceItemId == item.Id).ToList();
            for (var index = 0; index < rolls.Count; index++)
            {
                var roll = rolls[index];
                invoice.AssignFabricRollToDetail(details[index].Id, roll.Id);
                await context.InventoryReservations.AddAsync(new InventoryReservationEntity
                {
                    Id = Guid.NewGuid(), WarehouseId = invoice.WarehouseId, FabricRollId = roll.Id,
                    FabricItemId = item.FabricItemId, FabricColorId = item.FabricColorId,
                    ReservedMeters = roll.RemainingLengthMeters, RollCount = 1,
                    Status = (int)InventoryReservationStatus.Reserved,
                    Strategy = (int)AllocationStrategy.SpecificRoll,
                    ReferenceType = (int)DocumentType.SalesInvoice,
                    ReferenceId = invoice.Id, ReferenceLineId = item.Id, CreatedAt = DateTime.UtcNow
                }, cancellationToken);
            }
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
                    r.ContainerId == item.ChinaContainerId &&
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

    public async Task<IReadOnlyDictionary<Guid, decimal>> ResolveDetailingEntriesAsync(
        SalesInvoiceAggregate invoice,
        IReadOnlyList<(Guid RollDetailId, int? RollNumber, decimal LengthMeters)> entries,
        CancellationToken cancellationToken = default)
    {
        var resolved = new Dictionary<Guid, decimal>();
        var claimedRollIds = new HashSet<Guid>();

        var lineContainerIds = invoice.Items.Select(i => i.ChinaContainerId).Distinct().ToList();
        // Prefetch candidate rolls for all invoice line containers once.
        var reservedRolls = await context.FabricRolls
            .Where(r =>
                lineContainerIds.Contains(r.ContainerId) &&
                r.WarehouseId == invoice.WarehouseId &&
                (r.Status == (int)FabricRollStatus.Reserved || r.Status == (int)FabricRollStatus.Available))
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            if (entry.RollNumber is not int || entry.RollNumber <= 0)
                throw new WarehouseDetailingException("A specific fabric roll number is required; length-only sales are not allowed.");
            if (entry.LengthMeters <= 0)
                throw new WarehouseDetailingException("The measured whole-roll length must be greater than zero.");

            var detail = invoice.RollDetails.FirstOrDefault(d => d.Id == entry.RollDetailId)
                ?? throw new WarehouseDetailingException("بند التوب غير موجود في الفاتورة.");

            var item = invoice.Items.FirstOrDefault(i => i.Id == detail.SalesInvoiceItemId)
                ?? throw new WarehouseDetailingException("سطر الفاتورة المرتبط بالتوب غير موجود.");

            if (entry.RollNumber is int serial and > 0)
            {
                var roll = reservedRolls.FirstOrDefault(r =>
                    r.ContainerId == item.ChinaContainerId &&
                    r.RollNumber == serial &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId);

                if (roll is null)
                {
                    // Allow matching by serial within the invoice container/warehouse even if fabric differs —
                    // warehouse staff may pick the physical roll first; still require same fabric/color for safety.
                    roll = reservedRolls.FirstOrDefault(r =>
                        r.ContainerId == item.ChinaContainerId &&
                        r.RollNumber == serial);
                    if (roll is not null &&
                        (roll.FabricItemId != item.FabricItemId || roll.FabricColorId != item.FabricColorId))
                    {
                        throw new InventoryException(
                            $"رقم التوب {serial} موجود لكنه لنوع/لون مختلف عن بند الفاتورة.");
                    }
                }

                if (roll is null)
                    throw new InventoryException($"رقم التوب {serial} غير موجود في مستودع/حاوية هذه الفاتورة.");

                if (!claimedRollIds.Add(roll.Id))
                    throw new InventoryException($"رقم التوب {serial} مُدخل أكثر من مرة في نفس التفصيل.");

                if (roll.RemainingLengthMeters <= 0)
                    throw new InventoryException($"التوب رقم {serial} لا يحتوي أمتاراً متبقية.");

                var previousRollId = detail.FabricRollId;
                var stock = await context.WarehouseStocks.FirstAsync(s =>
                    s.WarehouseId == invoice.WarehouseId && s.ContainerId == item.ChinaContainerId &&
                    s.FabricItemId == item.FabricItemId && s.FabricColorId == item.FabricColorId,
                    cancellationToken);
                if (previousRollId.HasValue && previousRollId.Value != roll.Id)
                {
                    var previous = reservedRolls.First(r => r.Id == previousRollId.Value);
                    previous.Status = (int)FabricRollStatus.Available;
                    previous.ReservationStatus = (int)InventoryReservationStatus.Available;
                    stock.ReservedMeters = Math.Max(0, stock.ReservedMeters - previous.RemainingLengthMeters);
                    stock.AvailableMeters += previous.RemainingLengthMeters;
                }

                // Prefer reserved rolls; if still available, mark reserved for this invoice line.
                if (roll.Status == (int)FabricRollStatus.Available)
                {
                    roll.Status = (int)FabricRollStatus.Reserved;
                    roll.ReservationStatus = (int)InventoryReservationStatus.Reserved;
                    stock.AvailableMeters -= roll.RemainingLengthMeters;
                    stock.ReservedMeters += roll.RemainingLengthMeters;
                }

                invoice.AssignFabricRollToDetail(detail.Id, roll.Id);

                var reservation = await context.InventoryReservations.FirstOrDefaultAsync(r =>
                    r.ReferenceType == (int)DocumentType.SalesInvoice && r.ReferenceId == invoice.Id &&
                    r.ReferenceLineId == item.Id && r.FabricRollId == previousRollId, cancellationToken);
                if (reservation is not null)
                {
                    reservation.FabricRollId = roll.Id;
                    reservation.ReservedMeters = entry.LengthMeters;
                    reservation.Strategy = (int)AllocationStrategy.SpecificRoll;
                }

                // A legacy opening-balance roll carries only an even provisional length until its
                // physical paper label is read for the first time. That first explicit length is
                // authoritative even when greater than the provisional remaining length.

                // Serial pins the physical roll; optional length allows partial sale. China rolls
                // and already-confirmed legacy rolls retain the normal remaining-length ceiling.
                var meters = LegacyOpeningBalanceRollLengthPolicy.ResolveAndValidateSaleLength(
                    roll, entry.LengthMeters);
                if (meters > roll.RemainingLengthMeters)
                {
                    throw new InventoryException(
                        $"التوب رقم {serial}: الطول المدخل ({meters:N2}) أكبر من المتبقي ({roll.RemainingLengthMeters:N2}).");
                }

                resolved[detail.Id] = meters;
                continue;
            }

            if (entry.LengthMeters <= 0)
                throw new WarehouseDetailingException("أدخل رقم التوب (سيريال) أو الطول بالمتر.");

            resolved[detail.Id] = entry.LengthMeters;
        }

        // Length-only lines (no serial): assign a specific reserved roll now — in FIFO order but
        // only rolls with ENOUGH remaining length — so a length/roll mismatch fails here at
        // detailing time with a clear message, instead of silently failing later at approval
        // with "Cannot sell more meters than remaining on roll."
        foreach (var item in invoice.Items)
        {
            var details = invoice.RollDetails
                .Where(d => d.SalesInvoiceItemId == item.Id && !d.FabricRollId.HasValue)
                .ToList();
            if (details.Count == 0)
                continue;

            var pool = reservedRolls
                .Where(r =>
                    r.ContainerId == item.ChinaContainerId &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId &&
                    r.Status == (int)FabricRollStatus.Reserved &&
                    !claimedRollIds.Contains(r.Id))
                .OrderBy(r => r.RollNumber)
                .ToList();

            foreach (var detail in details)
            {
                var requiredMeters = resolved.TryGetValue(detail.Id, out var m) ? m : 0m;
                var match = pool.FirstOrDefault(r =>
                    !claimedRollIds.Contains(r.Id) && r.RemainingLengthMeters >= requiredMeters);

                if (match is null)
                {
                    throw new InventoryException(
                        $"لا يوجد توب محجوز بطول كافٍ ({requiredMeters:N2} م) لهذا الصنف. " +
                        "أدخل رقم التوب (سيريال) مباشرة بدل الطول لتفادي هذا الخطأ.");
                }

                claimedRollIds.Add(match.Id);
                invoice.AssignFabricRollToDetail(detail.Id, match.Id);
            }
        }

        return resolved;
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

            if (roll is null)
                throw new InventoryException("Every sale detail must be pinned to a specific fabric roll.");
            if (roll.Status != (int)FabricRollStatus.Reserved)
                throw new InventoryException("The exact fabric roll is not reserved for this invoice.");

            cogsTotal += meters * roll.CostPerMeter;
            roll.RemainingLengthMeters = 0;
            roll.Status = (int)FabricRollStatus.Sold;
            roll.ReservationStatus = (int)InventoryReservationStatus.Sold;

            var stock = await context.WarehouseStocks
                .FirstAsync(s =>
                    s.WarehouseId == invoice.WarehouseId &&
                    s.ContainerId == item.ChinaContainerId &&
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

            var reservation = await context.InventoryReservations.FirstOrDefaultAsync(r =>
                r.ReferenceType == (int)DocumentType.SalesInvoice && r.ReferenceId == invoice.Id &&
                r.FabricRollId == roll.Id && r.Status == (int)InventoryReservationStatus.Reserved,
                cancellationToken);
            if (reservation is not null)
                reservation.Status = (int)InventoryReservationStatus.Sold;

            movementLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = item.FabricItemId,
                FabricColorId = item.FabricColorId,
                FabricRollId = roll.Id,
                FabricBatchId = roll.FabricBatchId,
                ContainerId = item.ChinaContainerId,
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

    public async Task<decimal> ReceiveSalesReturnAsync(
        SalesReturnAggregate salesReturn,
        SalesInvoiceAggregate originalInvoice,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var lines = new List<StockMovementLineEntity>();
        decimal cogsReversal = 0m;

        foreach (var returnLine in salesReturn.Lines)
        {
            var originalItem = originalInvoice.Items.FirstOrDefault(i => i.Id == returnLine.OriginalInvoiceItemId)
                ?? throw new InventoryException("Original invoice item not found for return line.");

            var soldRolls = await context.FabricRolls
                .Where(r =>
                    r.WarehouseId == originalInvoice.WarehouseId &&
                    r.ContainerId == originalItem.ChinaContainerId &&
                    r.FabricItemId == originalItem.FabricItemId &&
                    r.FabricColorId == originalItem.FabricColorId)
                .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
                .ToListAsync(cancellationToken);

            var avgCost = soldRolls.Count > 0
                ? soldRolls.Average(r => r.CostPerMeter)
                : originalItem.UnitPrice.Amount;

            cogsReversal += returnLine.ReturnMeters * avgCost;

            var stock = await context.WarehouseStocks.FirstOrDefaultAsync(s =>
                s.WarehouseId == salesReturn.WarehouseId &&
                s.ContainerId == originalItem.ChinaContainerId &&
                s.FabricItemId == originalItem.FabricItemId &&
                s.FabricColorId == originalItem.FabricColorId, cancellationToken);

            if (stock is null)
            {
                stock = new WarehouseStockEntity
                {
                    Id = Guid.NewGuid(),
                    WarehouseId = salesReturn.WarehouseId,
                    ContainerId = originalItem.ChinaContainerId,
                    FabricItemId = originalItem.FabricItemId,
                    FabricColorId = originalItem.FabricColorId,
                    TotalMeters = returnLine.ReturnMeters,
                    AvailableMeters = returnLine.ReturnMeters,
                    ReservedMeters = 0,
                    RollCount = 0,
                    CreatedAt = now
                };
                await context.WarehouseStocks.AddAsync(stock, cancellationToken);
            }
            else
            {
                stock.TotalMeters += returnLine.ReturnMeters;
                stock.AvailableMeters += returnLine.ReturnMeters;
                stock.UpdatedAt = now;
            }

            lines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(),
                MovementId = movementId,
                FabricItemId = originalItem.FabricItemId,
                FabricColorId = originalItem.FabricColorId,
                FabricRollId = null,
                ContainerId = originalItem.ChinaContainerId,
                QuantityMeters = returnLine.ReturnMeters,
                UnitCost = avgCost,
                TotalValue = returnLine.ReturnMeters * avgCost,
                CurrencyCode = "USD",
                CreatedAt = now
            });
        }

        await PostMovementAsync(
            movementId,
            $"SRET-{salesReturn.ReturnNumber}-{now:yyyyMMddHHmmss}",
            MovementType.SaleReturn,
            salesReturn.WarehouseId,
            DocumentType.SalesReturn,
            salesReturn.Id,
            lines,
            now,
            cancellationToken);

        return cogsReversal;
    }

    public async Task ReverseInvoiceIssueAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken = default)
    {
        var existingReversal = await context.StockMovements.AsNoTracking().AnyAsync(m =>
            m.ReferenceType == (int)DocumentType.SalesInvoice && m.ReferenceId == invoice.Id &&
            m.Type == (int)MovementType.Correction && m.Status == (int)StockMovementStatus.Posted,
            cancellationToken);
        if (existingReversal)
        {
            await RepairLegacyInvoiceReservationsAsync(invoice, cancellationToken);
            return;
        }
        var saleMovement = await context.StockMovements.AsNoTracking().FirstOrDefaultAsync(m =>
            m.ReferenceType == (int)DocumentType.SalesInvoice && m.ReferenceId == invoice.Id &&
            m.Type == (int)MovementType.Sale && m.Status == (int)StockMovementStatus.Posted,
            cancellationToken) ?? throw new InventoryException("Posted sale inventory movement was not found.");
        var originalLines = await context.StockMovementLines.AsNoTracking()
            .Where(l => l.MovementId == saleMovement.Id).ToListAsync(cancellationToken);
        var reversalLines = new List<StockMovementLineEntity>();
        var now = DateTime.UtcNow;
        foreach (var line in originalLines)
        {
            var meters = Math.Abs(line.QuantityMeters);
            var roll = line.FabricRollId.HasValue
                ? await context.FabricRolls.FirstAsync(r => r.Id == line.FabricRollId.Value, cancellationToken)
                : throw new InventoryException("Sale movement is missing its exact fabric roll.");
            var stock = await context.WarehouseStocks.FirstAsync(s =>
                s.WarehouseId == invoice.WarehouseId && s.ContainerId == line.ContainerId &&
                s.FabricItemId == line.FabricItemId && s.FabricColorId == line.FabricColorId,
                cancellationToken);
            roll.RemainingLengthMeters = meters;
            roll.Status = (int)FabricRollStatus.Available;
            roll.ReservationStatus = (int)InventoryReservationStatus.Available;
            stock.TotalMeters += meters;
            stock.AvailableMeters += meters;
            stock.RollCount += 1;
            reversalLines.Add(new StockMovementLineEntity
            {
                Id = Guid.NewGuid(), MovementId = Guid.Empty, FabricItemId = line.FabricItemId,
                FabricColorId = line.FabricColorId, FabricRollId = line.FabricRollId,
                FabricBatchId = line.FabricBatchId, ContainerId = line.ContainerId,
                QuantityMeters = meters, UnitCost = line.UnitCost, TotalValue = Math.Abs(line.TotalValue),
                CurrencyCode = line.CurrencyCode, CreatedAt = now
            });
        }
        var movementId = Guid.NewGuid();
        foreach (var line in reversalLines) line.MovementId = movementId;
        await PostMovementAsync(movementId, $"SALREV-{invoice.InvoiceNumber.Value}-{now:yyyyMMddHHmmss}",
            MovementType.Correction, invoice.WarehouseId, DocumentType.SalesInvoice, invoice.Id,
            reversalLines, now, cancellationToken);
        await RecordValuationSnapshotAsync(invoice.WarehouseId, movementId, ValuationMethod.SpecificCost, cancellationToken);
        var reservations = await context.InventoryReservations.Where(r =>
            r.ReferenceType == (int)DocumentType.SalesInvoice && r.ReferenceId == invoice.Id)
            .ToListAsync(cancellationToken);
        foreach (var reservation in reservations)
            reservation.Status = (int)InventoryReservationStatus.Returned;
    }

    private async Task RepairLegacyInvoiceReservationsAsync(
        SalesInvoiceAggregate invoice,
        CancellationToken cancellationToken)
    {
        var reservations = await context.InventoryReservations.Where(r =>
            r.ReferenceType == (int)DocumentType.SalesInvoice && r.ReferenceId == invoice.Id &&
            r.Status != (int)InventoryReservationStatus.Cancelled)
            .ToListAsync(cancellationToken);
        foreach (var item in invoice.Items)
        {
            var itemReservations = reservations.Where(r => r.ReferenceLineId == item.Id).ToList();
            foreach (var exactReservation in itemReservations.Where(r => r.FabricRollId.HasValue))
            {
                var exactRoll = await context.FabricRolls.FirstAsync(r => r.Id == exactReservation.FabricRollId!.Value, cancellationToken);
                exactRoll.RemainingLengthMeters = exactReservation.ReservedMeters;
                exactRoll.Status = (int)FabricRollStatus.Available;
                exactRoll.ReservationStatus = (int)InventoryReservationStatus.Available;
            }
            var generic = itemReservations.FirstOrDefault(r => !r.FabricRollId.HasValue);
            var detailRollIds = invoice.RollDetails.Where(d => d.SalesInvoiceItemId == item.Id && d.FabricRollId.HasValue)
                .Select(d => d.FabricRollId!.Value).ToHashSet();
            if (generic is not null && detailRollIds.Count == 1)
            {
                var soldRoll = await context.FabricRolls.FirstAsync(r => detailRollIds.Contains(r.Id), cancellationToken);
                if (soldRoll.Status == (int)FabricRollStatus.Available && soldRoll.RemainingLengthMeters < generic.ReservedMeters)
                    soldRoll.RemainingLengthMeters = generic.ReservedMeters;
            }
            var legacyReservedRolls = await context.FabricRolls.Where(r =>
                r.ContainerId == item.ChinaContainerId && r.WarehouseId == invoice.WarehouseId &&
                r.FabricItemId == item.FabricItemId && r.FabricColorId == item.FabricColorId &&
                r.Status == (int)FabricRollStatus.Reserved && !detailRollIds.Contains(r.Id))
                .OrderBy(r => r.RollNumber).Take(generic?.RollCount ?? 0).ToListAsync(cancellationToken);
            var stock = await context.WarehouseStocks.FirstAsync(s =>
                s.WarehouseId == invoice.WarehouseId && s.ContainerId == item.ChinaContainerId &&
                s.FabricItemId == item.FabricItemId && s.FabricColorId == item.FabricColorId,
                cancellationToken);
            foreach (var roll in legacyReservedRolls)
            {
                roll.Status = (int)FabricRollStatus.Available;
                roll.ReservationStatus = (int)InventoryReservationStatus.Available;
            }
            stock.AvailableMeters += stock.ReservedMeters;
            stock.ReservedMeters = 0;
        }
        foreach (var reservation in reservations)
            reservation.Status = (int)InventoryReservationStatus.Returned;
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
                    r.ContainerId == item.ChinaContainerId &&
                    r.WarehouseId == invoice.WarehouseId &&
                    r.FabricItemId == item.FabricItemId &&
                    r.FabricColorId == item.FabricColorId &&
                    r.Status == (int)FabricRollStatus.Reserved)
                .ToListAsync(cancellationToken);

            var stock = await context.WarehouseStocks
                .FirstOrDefaultAsync(s =>
                    s.WarehouseId == invoice.WarehouseId &&
                    s.ContainerId == item.ChinaContainerId &&
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
        var doc = context.OpeningStockDocuments.Local.FirstOrDefault(d => d.Id == documentId)
            ?? await context.OpeningStockDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId, cancellationToken)
            ?? throw new ValidationException("Opening stock document not found.");

        if (doc.Status == (int)InventoryDocumentStatus.Posted)
        {
            var existingMovementId = await context.StockMovements.AsNoTracking()
                .Where(m => m.ReferenceType == (int)DocumentType.OpeningBalance &&
                            m.ReferenceId == documentId &&
                            m.Type == (int)MovementType.OpeningBalance &&
                            m.Status == (int)StockMovementStatus.Posted)
                .Select(m => m.Id)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingMovementId != Guid.Empty)
                return existingMovementId;
        }

        var lines = context.OpeningStockLines.Local
            .Where(l => l.DocumentId == documentId)
            .ToList();
        if (lines.Count == 0)
        {
            lines = await context.OpeningStockLines
                .Where(l => l.DocumentId == documentId)
                .ToListAsync(cancellationToken);
        }
        if (lines.Count == 0)
            throw new ValidationException("Opening stock has no lines.");

        var now = DateTime.UtcNow;
        var movementId = Guid.NewGuid();
        var movementLines = new List<StockMovementLineEntity>();
        var rollSequence = await context.FabricRolls.AsNoTracking().CountAsync(cancellationToken);

        var rollsToInsert = new List<FabricRollEntity>(lines.Sum(l => Math.Max(1, l.RollCount)));
        foreach (var line in lines)
        {
            var rollsToCreate = Math.Max(1, line.RollCount);
            var perRoll = line.QuantityMeters / rollsToCreate;

            for (var i = 0; i < rollsToCreate; i++)
            {
                rollSequence++;
                var length = rollsToCreate == 1 ? line.QuantityMeters : perRoll;
                var rollId = Guid.NewGuid();
                rollsToInsert.Add(new FabricRollEntity
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
                    IsLegacyOpeningBalance = true,
                    LegacyLengthConfirmed = false,
                    CreatedAt = now
                });
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

        // Track the complete batch once. SaveChanges uses the provider's command batching instead
        // of invoking EF AddAsync for each of the potentially 10,000 legacy rolls.
        context.FabricRolls.AddRange(rollsToInsert);

        await PostMovementAsync(movementId, doc.DocumentNumber, MovementType.OpeningBalance,
            doc.WarehouseId, DocumentType.OpeningBalance, doc.Id, movementLines, now, cancellationToken);

        doc.Status = (int)InventoryDocumentStatus.Posted;
        doc.PostedAt = now;
        await RecordValuationSnapshotAsync(doc.WarehouseId, movementId, ValuationMethod.AverageCost, cancellationToken);
        return movementId;
    }

    public async Task<IReadOnlyList<Guid>> PostFinanceOpeningBalanceStockAsync(
        Guid openingBalanceDocumentId,
        CancellationToken cancellationToken = default)
    {
        var obDoc = await context.OpeningBalanceDocuments.AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == openingBalanceDocumentId, cancellationToken)
            ?? throw new ValidationException("Opening balance document not found.");

        if (obDoc.Type != (int)OpeningBalanceType.OpeningStock)
            throw new ValidationException("Document is not an opening stock balance.");

        var obLines = await context.OpeningBalanceLines.AsNoTracking()
            .Where(l => l.DocumentId == openingBalanceDocumentId)
            .OrderBy(l => l.LineNumber)
            .ToListAsync(cancellationToken);
        if (obLines.Count == 0)
            throw new ValidationException("Opening balance has no stock lines.");

        var movementIds = new List<Guid>();
        foreach (var warehouseGroup in obLines.GroupBy(l => l.WarehouseId ?? Guid.Empty))
        {
            if (warehouseGroup.Key == Guid.Empty)
                throw new ValidationException("Warehouse is required on every opening stock line.");

            var warehouseId = warehouseGroup.Key;
            var stockRef = $"OB-FIN-{openingBalanceDocumentId:N}-{warehouseId:N}";
            var stockDoc = await context.OpeningStockDocuments
                .FirstOrDefaultAsync(d => d.Reference == stockRef, cancellationToken);

            if (stockDoc is null)
            {
                var stockDocId = Guid.NewGuid();
                var stockNumber = await numberingService.NextOpeningStockNumberAsync(obDoc.BranchId, cancellationToken);
                stockDoc = new OpeningStockDocumentEntity
                {
                    Id = stockDocId,
                    DocumentNumber = stockNumber,
                    WarehouseId = warehouseId,
                    OpeningDate = obDoc.OpeningDate,
                    Reference = stockRef,
                    CurrencyCode = obDoc.CurrencyCode,
                    Status = (int)InventoryDocumentStatus.Draft,
                    Notes = $"من رصيد افتتاحي {obDoc.Number}",
                    CreatedAt = DateTime.UtcNow
                };
                await context.OpeningStockDocuments.AddAsync(stockDoc, cancellationToken);

                foreach (var line in warehouseGroup)
                {
                    if (line.FabricItemId is not Guid fabricItemId || fabricItemId == Guid.Empty ||
                        line.FabricColorId is not Guid fabricColorId || fabricColorId == Guid.Empty)
                    {
                        throw new ValidationException("Fabric item and color IDs are required on every opening stock line.");
                    }
                    var qty = line.Quantity ?? 0;
                    var unitCost = line.UnitCost ?? (qty > 0 ? line.Debit / qty : 0);
                    var rolls = Math.Max(1, (int)(line.RollCount ?? 1));

                    await context.OpeningStockLines.AddAsync(new OpeningStockLineEntity
                    {
                        Id = Guid.NewGuid(),
                        DocumentId = stockDocId,
                        FabricItemId = fabricItemId,
                        FabricColorId = fabricColorId,
                        QuantityMeters = qty,
                        RollCount = rolls,
                        UnitCost = unitCost,
                        TotalValue = qty * unitCost,
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                }
            }

            movementIds.Add(await PostOpeningStockAsync(stockDoc.Id, cancellationToken));
        }

        return movementIds;
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
        var existing = context.WarehouseStocks.Local.FirstOrDefault(
            s => s.WarehouseId == warehouseId &&
                 s.FabricItemId == fabricItemId &&
                 s.FabricColorId == fabricColorId &&
                 s.ContainerId == containerId)
            ?? await context.WarehouseStocks.FirstOrDefaultAsync(
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

    private async Task<(Guid FabricItemId, Guid FabricColorId)> ResolveFabricByNameAsync(
        Guid companyId,
        string? itemName,
        string? colorName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemName))
            throw new ValidationException("Fabric item name is required.");

        var items = await context.FabricItems.AsNoTracking()
            .Where(i => i.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        var item = items.FirstOrDefault(i =>
            i.NameAr.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.NameEn.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Code.Equals(itemName, StringComparison.OrdinalIgnoreCase))
            ?? throw new ValidationException($"Fabric item not found: {itemName}");

        if (!string.IsNullOrWhiteSpace(colorName))
        {
            var colors = await context.FabricColors.AsNoTracking()
                .Where(c => c.FabricItemId == item.Id)
                .ToListAsync(cancellationToken);
            var color = colors.FirstOrDefault(c =>
                c.NameAr.Equals(colorName, StringComparison.OrdinalIgnoreCase) ||
                c.NameEn.Equals(colorName, StringComparison.OrdinalIgnoreCase) ||
                c.Code.Equals(colorName, StringComparison.OrdinalIgnoreCase));
            if (color is not null)
                return (item.Id, color.Id);
        }

        return (item.Id, await ResolveDefaultColorIdAsync(item.Id, cancellationToken));
    }

    private static decimal CalculateContainerCostPerMeter(ContainerAggregate container)
    {
        if (container.LandingCost is null || container.TotalMeters.Value <= 0)
            return 0m;
        var perMeter = container.LandingCost.TotalSharedExpenses.Amount / container.TotalMeters.Value;
        return perMeter * container.ExchangeRateToLocalCurrency;
    }

    internal static void ReconcileRollCostsToInvoice(IReadOnlyList<FabricRollEntity> rolls, decimal invoiceTotal)
    {
        if (rolls.Count == 0 || invoiceTotal <= 0)
            throw new ValidationException("Roll valuation requires rolls and a positive invoice total.");

        var rawTotal = rolls.Sum(r => r.LengthMeters * r.CostPerMeter);
        if (rawTotal <= 0)
        {
            var meters = rolls.Sum(r => r.LengthMeters);
            foreach (var roll in rolls)
                roll.CostPerMeter = invoiceTotal / meters;
        }
        else
        {
            var factor = invoiceTotal / rawTotal;
            foreach (var roll in rolls)
                roll.CostPerMeter *= factor;
        }

        var residual = invoiceTotal - rolls.Sum(r => r.LengthMeters * r.CostPerMeter);
        var last = rolls[^1];
        last.CostPerMeter += residual / last.LengthMeters;
    }

    private static string BuildMovementNumber(string containerNumber, DateTime utcNow)
    {
        var safe = new string(containerNumber.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(safe)) safe = "CONT";
        return $"IMP-{safe}-{utcNow:yyyyMMddHHmmss}";
    }
}
