using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Domain.Entities.Inventory;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class InventoryManagementRepository(ErpDbContext context) : IInventoryManagementRepository
{
    public async Task<bool> WarehouseCodeExistsAsync(
        Guid branchId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await context.Warehouses.IgnoreQueryFilters().AsNoTracking()
            .AnyAsync(w => w.BranchId == branchId && w.Code == code && (!excludeId.HasValue || w.Id != excludeId), cancellationToken);

    public async Task AddWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        await context.Warehouses.AddAsync(new WarehouseEntity
        {
            Id = warehouse.Id,
            BranchId = warehouse.BranchId,
            Code = warehouse.Code,
            NameAr = warehouse.NameAr,
            NameEn = warehouse.NameEn,
            Description = warehouse.Description,
            City = warehouse.City,
            Address = warehouse.Address,
            Manager = warehouse.Manager,
            CostCenterId = warehouse.CostCenterId,
            Notes = warehouse.Notes,
            IsDefault = warehouse.IsDefault,
            CapacityRolls = warehouse.CapacityRolls,
            IsActive = warehouse.IsActive,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task UpdateWarehouseAsync(Warehouse warehouse, CancellationToken cancellationToken = default)
    {
        var entity = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == warehouse.Id, cancellationToken)
            ?? throw new InvalidOperationException("Warehouse not found.");
        entity.NameAr = warehouse.NameAr;
        entity.NameEn = warehouse.NameEn;
        entity.Description = warehouse.Description;
        entity.City = warehouse.City;
        entity.Address = warehouse.Address;
        entity.Manager = warehouse.Manager;
        entity.CostCenterId = warehouse.CostCenterId;
        entity.Notes = warehouse.Notes;
        entity.IsDefault = warehouse.IsDefault;
        entity.CapacityRolls = warehouse.CapacityRolls;
        entity.IsActive = warehouse.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var e = await context.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        return e is null ? null : MapWarehouse(e);
    }

    public async Task<IReadOnlyList<WarehouseListExtendedDto>> GetWarehouseListAsync(
        Guid branchId, CancellationToken cancellationToken = default)
    {
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId).OrderBy(w => w.Code).ToListAsync(cancellationToken);

        var result = new List<WarehouseListExtendedDto>();
        foreach (var w in warehouses)
        {
            var stocks = await context.WarehouseStocks.AsNoTracking()
                .Where(s => s.WarehouseId == w.Id).ToListAsync(cancellationToken);
            var rolls = await context.FabricRolls.AsNoTracking()
                .Where(r => r.WarehouseId == w.Id && r.RemainingLengthMeters > 0).ToListAsync(cancellationToken);
            var value = rolls.Sum(r => r.RemainingLengthMeters * r.CostPerMeter);

            result.Add(new WarehouseListExtendedDto
            {
                Id = w.Id, Code = w.Code, NameAr = w.NameAr, NameEn = w.NameEn,
                City = w.City, Manager = w.Manager, IsDefault = w.IsDefault, IsActive = w.IsActive,
                RollCount = stocks.Sum(s => s.RollCount),
                TotalMeters = stocks.Sum(s => s.TotalMeters),
                InventoryValue = value
            });
        }
        return result;
    }

    public async Task ArchiveWarehouseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity is not null)
        {
            entity.IsArchived = true;
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }

    public async Task AddLocationAsync(WarehouseLocation location, CancellationToken cancellationToken = default)
    {
        await context.WarehouseLocations.AddAsync(new WarehouseLocationEntity
        {
            Id = location.Id,
            WarehouseId = location.WarehouseId,
            ParentId = location.ParentId,
            LocationType = (int)location.LocationType,
            Code = location.Code,
            Name = location.Name,
            Zone = location.Zone,
            BinCode = location.BinCode,
            CapacityMeters = location.CapacityMeters,
            Status = (int)location.Status,
            Priority = location.Priority,
            Barcode = location.Barcode,
            QrCode = location.QrCode,
            IsActive = location.IsActive,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<StorageLocationDto>> GetLocationsAsync(
        Guid warehouseId, CancellationToken cancellationToken = default) =>
        await context.WarehouseLocations.AsNoTracking()
            .Where(l => l.WarehouseId == warehouseId)
            .OrderBy(l => l.Priority).ThenBy(l => l.Code)
            .Select(l => new StorageLocationDto
            {
                Id = l.Id, WarehouseId = l.WarehouseId, ParentId = l.ParentId,
                LocationType = ((StorageLocationType)l.LocationType).ToString(),
                Code = l.Code, Name = l.Name, CapacityMeters = l.CapacityMeters,
                Status = ((StorageLocationStatus)l.Status).ToString(), Priority = l.Priority
            }).ToListAsync(cancellationToken);

    public async Task AddAuditEntryAsync(InventoryAuditEntry entry, CancellationToken cancellationToken = default)
    {
        await context.InventoryAuditLogs.AddAsync(new InventoryAuditEntryEntity
        {
            Id = entry.Id, EntityId = entry.EntityId, EntityType = entry.EntityType,
            Action = entry.Action, Username = entry.Username,
            FieldName = entry.FieldName, PreviousValue = entry.PreviousValue,
            NewValue = entry.NewValue, Reason = entry.Reason,
            SourceModule = entry.SourceModule, ReferenceDocumentId = entry.ReferenceDocumentId,
            RecordedAt = entry.RecordedAt, CreatedByUserId = entry.UserId, CreatedAt = entry.RecordedAt
        }, cancellationToken);
    }

    public async Task AddTimelineEventAsync(InventoryTimelineEvent entry, CancellationToken cancellationToken = default)
    {
        await context.InventoryTimelineEvents.AddAsync(new InventoryTimelineEventEntity
        {
            Id = entry.Id, EntityId = entry.EntityId, EntityType = entry.EntityType,
            EventType = entry.EventType, Title = entry.Title, Description = entry.Description,
            Username = entry.Username, PreviousValue = entry.PreviousValue,
            NewValue = entry.NewValue, Reason = entry.Reason,
            OccurredAt = entry.OccurredAt, CreatedByUserId = entry.UserId, CreatedAt = entry.OccurredAt
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryAuditDto>> GetAuditTrailAsync(
        Guid entityId, string entityType, CancellationToken cancellationToken = default) =>
        await context.InventoryAuditLogs.AsNoTracking()
            .Where(a => a.EntityId == entityId && a.EntityType == entityType)
            .OrderByDescending(a => a.RecordedAt)
            .Select(a => new InventoryAuditDto
            {
                RecordedAt = a.RecordedAt, Action = a.Action, Username = a.Username,
                FieldName = a.FieldName, PreviousValue = a.PreviousValue,
                NewValue = a.NewValue, Reason = a.Reason
            }).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<InventoryTimelineDto>> GetTimelineAsync(
        Guid entityId, string entityType, CancellationToken cancellationToken = default) =>
        await context.InventoryTimelineEvents.AsNoTracking()
            .Where(t => t.EntityId == entityId && t.EntityType == entityType)
            .OrderByDescending(t => t.OccurredAt)
            .Select(t => new InventoryTimelineDto
            {
                OccurredAt = t.OccurredAt, EventType = t.EventType, Title = t.Title,
                Description = t.Description, Username = t.Username
            }).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<FabricStockBalanceDto>> GetFabricStockBalancesAsync(
        Guid branchId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var warehouseIds = warehouseId.HasValue
            ? [warehouseId.Value]
            : await context.Warehouses.AsNoTracking()
                .Where(w => w.BranchId == branchId).Select(w => w.Id).ToListAsync(cancellationToken);

        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => warehouseIds.Contains(s.WarehouseId) && s.TotalMeters > 0)
            .ToListAsync(cancellationToken);

        var fabricIds = stocks.Select(s => s.FabricItemId).Distinct().ToList();
        var colorIds = stocks.Select(s => s.FabricColorId).Distinct().ToList();
        var whIds = stocks.Select(s => s.WarehouseId).Distinct().ToList();
        var containerIds = stocks.Where(s => s.ContainerId != Guid.Empty).Select(s => s.ContainerId).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(w => whIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);
        var containers = containerIds.Count > 0
            ? await context.Containers.AsNoTracking()
                .Where(c => containerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken)
            : [];

        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => warehouseIds.Contains(r.WarehouseId) && r.RemainingLengthMeters > 0)
            .ToListAsync(cancellationToken);

        return stocks.Select(s =>
        {
            var itemRolls = rolls.Where(r =>
                r.WarehouseId == s.WarehouseId &&
                r.FabricItemId == s.FabricItemId &&
                r.FabricColorId == s.FabricColorId).ToList();
            var avgCost = itemRolls.Count > 0 ? itemRolls.Average(r => r.CostPerMeter) : 0m;
            return new FabricStockBalanceDto
            {
                WarehouseId = s.WarehouseId,
                WarehouseName = warehouses.GetValueOrDefault(s.WarehouseId)?.NameAr ?? "—",
                FabricItemId = s.FabricItemId,
                FabricCode = fabrics.GetValueOrDefault(s.FabricItemId)?.Code ?? "—",
                FabricName = fabrics.GetValueOrDefault(s.FabricItemId)?.NameAr ?? "—",
                FabricColorId = s.FabricColorId,
                ColorName = colors.GetValueOrDefault(s.FabricColorId)?.NameAr ?? "—",
                ContainerNumber = s.ContainerId != Guid.Empty && containers.TryGetValue(s.ContainerId, out var c)
                    ? c.ContainerNumber : "—",
                RollCount = s.RollCount,
                TotalMeters = s.TotalMeters,
                ReservedMeters = s.ReservedMeters,
                AvailableMeters = s.AvailableMeters,
                InventoryValue = s.AvailableMeters * avgCost
            };
        }).OrderByDescending(s => s.TotalMeters).ToList();
    }

    public async Task<IReadOnlyList<FabricRollListDto>> GetFabricRollsAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId)
            .OrderBy(r => r.RollNumber).ToListAsync(cancellationToken);
        if (rolls.Count == 0) return [];

        var fabricIds = rolls.Select(r => r.FabricItemId).Distinct().ToList();
        var colorIds = rolls.Select(r => r.FabricColorId).Distinct().ToList();
        var batchIds = rolls.Where(r => r.FabricBatchId.HasValue).Select(r => r.FabricBatchId!.Value).Distinct().ToList();
        var locIds = rolls.Where(r => r.StorageLocationId.HasValue).Select(r => r.StorageLocationId!.Value).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var batches = batchIds.Count > 0
            ? await context.FabricBatches.AsNoTracking()
                .Where(b => batchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken)
            : [];
        var locations = locIds.Count > 0
            ? await context.WarehouseLocations.AsNoTracking()
                .Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken)
            : [];

        return rolls.Select(r => new FabricRollListDto
        {
            Id = r.Id, RollNumber = r.RollNumber, Barcode = r.Barcode,
            FabricName = fabrics.GetValueOrDefault(r.FabricItemId)?.NameAr ?? "—",
            ColorName = colors.GetValueOrDefault(r.FabricColorId)?.NameAr ?? "—",
            LengthMeters = r.LengthMeters, RemainingLengthMeters = r.RemainingLengthMeters,
            CostPerMeter = r.CostPerMeter,
            CurrentValue = r.RemainingLengthMeters * r.CostPerMeter,
            Status = ((FabricRollStatus)r.Status).ToString(),
            BatchNumber = r.FabricBatchId.HasValue && batches.TryGetValue(r.FabricBatchId.Value, out var b) ? b.BatchNumber : null,
            LocationCode = r.StorageLocationId.HasValue && locations.TryGetValue(r.StorageLocationId.Value, out var l) ? l.Code : null
        }).ToList();
    }

    public async Task<IReadOnlyList<StockMovementListDto>> GetMovementsAsync(
        Guid branchId, Guid? warehouseId = null, CancellationToken cancellationToken = default)
    {
        var warehouseIds = warehouseId.HasValue
            ? [warehouseId.Value]
            : await context.Warehouses.AsNoTracking()
                .Where(w => w.BranchId == branchId).Select(w => w.Id).ToListAsync(cancellationToken);

        var movements = await context.StockMovements.AsNoTracking()
            .Where(m => warehouseIds.Contains(m.WarehouseId))
            .OrderByDescending(m => m.MovementDate).Take(200)
            .ToListAsync(cancellationToken);

        var whMap = await context.Warehouses.AsNoTracking()
            .Where(w => warehouseIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);
        var movementIds = movements.Select(m => m.Id).ToList();
        var lines = await context.StockMovementLines.AsNoTracking()
            .Where(l => movementIds.Contains(l.MovementId)).ToListAsync(cancellationToken);

        return movements.Select(m => new StockMovementListDto
        {
            Id = m.Id, MovementNumber = m.MovementNumber, MovementDate = m.MovementDate,
            Type = ((MovementType)m.Type).ToString(),
            WarehouseName = whMap.GetValueOrDefault(m.WarehouseId)?.NameAr ?? "—",
            Reference = m.ReferenceType.HasValue ? ((DocumentType)m.ReferenceType).ToString() : null,
            TotalMeters = lines.Where(l => l.MovementId == m.Id).Sum(l => Math.Abs(l.QuantityMeters)),
            TotalValue = lines.Where(l => l.MovementId == m.Id).Sum(l => Math.Abs(l.TotalValue)),
            Status = ((StockMovementStatus)m.Status).ToString()
        }).ToList();
    }

    public async Task<IReadOnlyList<InventoryAlertDto>> GetAlertsAsync(
        Guid branchId, bool unacknowledgedOnly = true, CancellationToken cancellationToken = default)
    {
        var q = context.InventoryAlerts.AsNoTracking().Where(a => a.BranchId == branchId);
        if (unacknowledgedOnly) q = q.Where(a => !a.IsAcknowledged);
        var alerts = await q.OrderByDescending(a => a.CreatedAt).Take(50).ToListAsync(cancellationToken);
        var whIds = alerts.Where(a => a.WarehouseId.HasValue).Select(a => a.WarehouseId!.Value).Distinct().ToList();
        var whMap = whIds.Count > 0
            ? await context.Warehouses.AsNoTracking().Where(w => whIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken)
            : [];

        return alerts.Select(a => new InventoryAlertDto
        {
            Id = a.Id,
            AlertType = ((InventoryAlertType)a.AlertType).ToString(),
            Severity = ((InventoryAlertSeverity)a.Severity).ToString(),
            Title = a.Title, Message = a.Message,
            WarehouseName = a.WarehouseId.HasValue && whMap.TryGetValue(a.WarehouseId.Value, out var w) ? w.NameAr : null,
            CreatedAt = a.CreatedAt, IsAcknowledged = a.IsAcknowledged
        }).ToList();
    }

    public async Task<InventoryDashboardDto> GetDashboardAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var warehouses = await GetWarehouseListAsync(branchId, cancellationToken);
        var stock = await GetFabricStockBalancesAsync(branchId, cancellationToken: cancellationToken);
        var alerts = await GetAlertsAsync(branchId, true, cancellationToken);
        var transfers = await GetTransfersAsync(branchId, cancellationToken);
        var stocktakes = await GetStocktakeSessionsAsync(branchId, cancellationToken);

        return new InventoryDashboardDto
        {
            TotalInventoryValue = warehouses.Sum(w => w.InventoryValue),
            WarehouseCount = warehouses.Count,
            TotalRolls = warehouses.Sum(w => w.RollCount),
            TotalMeters = warehouses.Sum(w => w.TotalMeters),
            ReservedMeters = stock.Sum(s => s.ReservedMeters),
            LowStockCount = stock.Count(s => s.AvailableMeters > 0 && s.AvailableMeters <= 50),
            PendingTransfers = transfers.Count(t => t.Status is "Draft" or "Approved" or "InTransit"),
            PendingStocktakes = stocktakes.Count(s => s.Status is "Draft" or "Counting" or "Review"),
            ActiveAlerts = alerts.Count,
            TopFabrics = stock.OrderByDescending(s => s.TotalMeters).Take(10).ToList(),
            RecentAlerts = alerts.Take(5).ToList()
        };
    }

    public async Task<InventoryOperationsCenterDto> GetOperationsCenterAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(w => w.Id == warehouseId).ToListAsync(cancellationToken);
        var w = warehouses.FirstOrDefault();
        if (w is null) throw new InvalidOperationException("Warehouse not found.");

        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId).ToListAsync(cancellationToken);
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId && r.RemainingLengthMeters > 0).ToListAsync(cancellationToken);
        var value = rolls.Sum(r => r.RemainingLengthMeters * r.CostPerMeter);

        var whDto = new WarehouseListExtendedDto
        {
            Id = w.Id, Code = w.Code, NameAr = w.NameAr, NameEn = w.NameEn,
            City = w.City, Manager = w.Manager, IsDefault = w.IsDefault, IsActive = w.IsActive,
            RollCount = stocks.Sum(s => s.RollCount), TotalMeters = stocks.Sum(s => s.TotalMeters),
            InventoryValue = value
        };

        var branchId = w.BranchId;
        var transfers = await GetTransfersAsync(branchId, cancellationToken);
        var stocktakes = await GetStocktakeSessionsAsync(branchId, cancellationToken);

        return new InventoryOperationsCenterDto
        {
            Warehouse = whDto,
            Stock = await GetFabricStockBalancesAsync(branchId, warehouseId, cancellationToken),
            Rolls = await GetFabricRollsAsync(warehouseId, cancellationToken),
            Locations = await GetLocationsAsync(warehouseId, cancellationToken),
            RecentMovements = await GetMovementsAsync(branchId, warehouseId, cancellationToken),
            Alerts = await GetAlertsAsync(branchId, true, cancellationToken),
            RecentAudit = await GetAuditTrailAsync(warehouseId, "Warehouse", cancellationToken),
            Timeline = await GetTimelineAsync(warehouseId, "Warehouse", cancellationToken),
            PendingTransfers = transfers.Count(t => t.Status is "Draft" or "Approved"),
            PendingStocktakes = stocktakes.Count(s => s.Status is "Draft" or "Counting"),
            InventoryValue = value
        };
    }

    public async Task<Guid> CreateTransferAsync(
        StockTransfer transfer,
        IReadOnlyList<(Guid FabricItemId, Guid FabricColorId, decimal Meters, int Rolls, Guid? RollId)> lines,
        CancellationToken cancellationToken = default)
    {
        await context.StockTransfers.AddAsync(new StockTransferDocumentEntity
        {
            Id = transfer.Id, Number = transfer.Number,
            FromWarehouseId = transfer.FromWarehouseId, ToWarehouseId = transfer.ToWarehouseId,
            FromLocationId = transfer.FromLocationId, ToLocationId = transfer.ToLocationId,
            Status = (int)transfer.Status, Date = transfer.Date, Notes = transfer.Notes,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        foreach (var line in lines)
        {
            await context.StockTransferLines.AddAsync(new StockTransferLineEntity
            {
                Id = Guid.NewGuid(), TransferId = transfer.Id,
                FabricItemId = line.FabricItemId, FabricColorId = line.FabricColorId,
                FabricRollId = line.RollId, QuantityMeters = line.Meters, RollCount = line.Rolls,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        return transfer.Id;
    }

    public async Task<StockTransferDetailDto?> GetTransferDetailAsync(
        Guid transferId, CancellationToken cancellationToken = default)
    {
        var t = await context.StockTransfers.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == transferId, cancellationToken);
        if (t is null) return null;

        var whMap = await context.Warehouses.AsNoTracking()
            .Where(w => w.Id == t.FromWarehouseId || w.Id == t.ToWarehouseId)
            .ToDictionaryAsync(w => w.Id, cancellationToken);

        var rawLines = await context.StockTransferLines.AsNoTracking()
            .Where(l => l.TransferId == transferId).ToListAsync(cancellationToken);
        if (rawLines.Count == 0)
        {
            return new StockTransferDetailDto
            {
                Id = t.Id, Number = t.Number,
                FromWarehouseId = t.FromWarehouseId, ToWarehouseId = t.ToWarehouseId,
                FromWarehouse = whMap.GetValueOrDefault(t.FromWarehouseId)?.NameAr ?? "—",
                ToWarehouse = whMap.GetValueOrDefault(t.ToWarehouseId)?.NameAr ?? "—",
                Status = ((InventoryDocumentStatus)t.Status).ToString(),
                Notes = t.Notes, Date = t.Date
            };
        }

        var fabricIds = rawLines.Select(l => l.FabricItemId).Distinct().ToList();
        var colorIds = rawLines.Select(l => l.FabricColorId).Distinct().ToList();
        var rollIds = rawLines.Where(l => l.FabricRollId.HasValue).Select(l => l.FabricRollId!.Value).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var rolls = rollIds.Count > 0
            ? await context.FabricRolls.AsNoTracking()
                .Where(r => rollIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken)
            : new Dictionary<Guid, FabricRollEntity>();

        var batchIds = rolls.Values.Where(r => r.FabricBatchId.HasValue).Select(r => r.FabricBatchId!.Value).Distinct().ToList();
        var locIds = rolls.Values.Where(r => r.StorageLocationId.HasValue).Select(r => r.StorageLocationId!.Value).Distinct().ToList();
        var batches = batchIds.Count > 0
            ? await context.FabricBatches.AsNoTracking().Where(b => batchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken)
            : [];
        var locations = locIds.Count > 0
            ? await context.WarehouseLocations.AsNoTracking().Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken)
            : [];

        var lines = rawLines.Select(l =>
        {
            rolls.TryGetValue(l.FabricRollId ?? Guid.Empty, out var roll);
            var unitCost = roll?.CostPerMeter ?? 0m;
            return new StockTransferLineDetailDto
            {
                Id = l.Id,
                FabricItemId = l.FabricItemId,
                FabricColorId = l.FabricColorId,
                FabricRollId = l.FabricRollId,
                FabricName = fabrics.GetValueOrDefault(l.FabricItemId)?.NameAr ?? "—",
                ColorName = colors.GetValueOrDefault(l.FabricColorId)?.NameAr ?? "—",
                RollNumber = roll?.RollNumber ?? 0,
                BatchNumber = roll?.FabricBatchId.HasValue == true && batches.TryGetValue(roll.FabricBatchId.Value, out var b) ? b.BatchNumber : null,
                LocationCode = roll?.StorageLocationId.HasValue == true && locations.TryGetValue(roll.StorageLocationId.Value, out var loc) ? loc.Code : null,
                QuantityMeters = l.QuantityMeters,
                RollCount = l.RollCount,
                UnitValue = l.QuantityMeters * unitCost
            };
        }).ToList();

        return new StockTransferDetailDto
        {
            Id = t.Id, Number = t.Number,
            FromWarehouseId = t.FromWarehouseId, ToWarehouseId = t.ToWarehouseId,
            FromWarehouse = whMap.GetValueOrDefault(t.FromWarehouseId)?.NameAr ?? "—",
            ToWarehouse = whMap.GetValueOrDefault(t.ToWarehouseId)?.NameAr ?? "—",
            Status = ((InventoryDocumentStatus)t.Status).ToString(),
            Notes = t.Notes, Date = t.Date,
            Lines = lines,
            TotalMeters = lines.Sum(x => x.QuantityMeters),
            TotalRolls = lines.Sum(x => x.RollCount),
            TotalValue = lines.Sum(x => x.UnitValue)
        };
    }

    public async Task ApproveTransferAsync(Guid transferId, Guid userId, CancellationToken cancellationToken = default)
    {
        var t = await context.StockTransfers.FirstAsync(x => x.Id == transferId, cancellationToken);
        t.Status = (int)InventoryDocumentStatus.Approved;
        t.ApprovedByUserId = userId;
        t.ApprovedAt = DateTime.UtcNow;
    }

    public async Task<IReadOnlyList<WarehouseTransferRollDto>> GetTransferableRollsAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var available = (int)FabricRollStatus.Available;
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId &&
                        r.Status == available &&
                        r.RemainingLengthMeters > 0)
            .OrderBy(r => r.RollNumber)
            .ToListAsync(cancellationToken);
        if (rolls.Count == 0) return [];

        var fabricIds = rolls.Select(r => r.FabricItemId).Distinct().ToList();
        var colorIds = rolls.Select(r => r.FabricColorId).Distinct().ToList();
        var batchIds = rolls.Where(r => r.FabricBatchId.HasValue).Select(r => r.FabricBatchId!.Value).Distinct().ToList();
        var locIds = rolls.Where(r => r.StorageLocationId.HasValue).Select(r => r.StorageLocationId!.Value).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken);
        var batches = batchIds.Count > 0
            ? await context.FabricBatches.AsNoTracking().Where(b => batchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken)
            : [];
        var locations = locIds.Count > 0
            ? await context.WarehouseLocations.AsNoTracking().Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken)
            : [];

        return rolls.Select(r => new WarehouseTransferRollDto
        {
            Id = r.Id,
            FabricItemId = r.FabricItemId,
            FabricColorId = r.FabricColorId,
            FabricBatchId = r.FabricBatchId,
            RollNumber = r.RollNumber,
            Barcode = r.Barcode,
            FabricName = fabrics.GetValueOrDefault(r.FabricItemId)?.NameAr ?? "—",
            ColorName = colors.GetValueOrDefault(r.FabricColorId)?.NameAr ?? "—",
            BatchNumber = r.FabricBatchId.HasValue && batches.TryGetValue(r.FabricBatchId.Value, out var b) ? b.BatchNumber : null,
            LocationCode = r.StorageLocationId.HasValue && locations.TryGetValue(r.StorageLocationId.Value, out var l) ? l.Code : null,
            RemainingLengthMeters = r.RemainingLengthMeters,
            CostPerMeter = r.CostPerMeter,
            CurrentValue = r.RemainingLengthMeters * r.CostPerMeter,
            Status = ((FabricRollStatus)r.Status).ToString()
        }).ToList();
    }

    public async Task<bool> ValidateRollTransferAsync(
        Guid rollId, Guid fromWarehouseId, decimal meters, CancellationToken cancellationToken = default)
    {
        var roll = await context.FabricRolls.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == rollId, cancellationToken);
        if (roll is null) return false;
        if (roll.WarehouseId != fromWarehouseId) return false;
        if (roll.Status != (int)FabricRollStatus.Available) return false;
        if (meters <= 0 || meters > roll.RemainingLengthMeters) return false;
        return true;
    }

    public async Task SeedStocktakeLinesAsync(
        Guid sessionId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var existing = await context.StocktakeLines.AnyAsync(l => l.SessionId == sessionId, cancellationToken);
        if (existing) return;

        var rolls = await GetTransferableRollsAsync(warehouseId, cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var roll in rolls)
        {
            await context.StocktakeLines.AddAsync(new StocktakeLineEntity
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                FabricItemId = roll.FabricItemId,
                FabricColorId = roll.FabricColorId,
                FabricRollId = roll.Id,
                SystemMeters = roll.RemainingLengthMeters,
                CountedMeters = roll.RemainingLengthMeters,
                DifferenceMeters = 0,
                CreatedAt = now
            }, cancellationToken);
        }
    }

    public async Task<StocktakeDetailDto?> GetStocktakeDetailAsync(
        Guid sessionId, CancellationToken cancellationToken = default)
    {
        var s = await context.StocktakeSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (s is null) return null;

        var wh = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == s.WarehouseId, cancellationToken);

        var rawLines = await context.StocktakeLines.AsNoTracking()
            .Where(l => l.SessionId == sessionId).ToListAsync(cancellationToken);

        var fabricIds = rawLines.Select(l => l.FabricItemId).Distinct().ToList();
        var colorIds = rawLines.Select(l => l.FabricColorId).Distinct().ToList();
        var rollIds = rawLines.Where(l => l.FabricRollId.HasValue).Select(l => l.FabricRollId!.Value).Distinct().ToList();

        var fabrics = fabricIds.Count > 0
            ? await context.FabricItems.AsNoTracking().Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken)
            : [];
        var colors = colorIds.Count > 0
            ? await context.FabricColors.AsNoTracking().Where(c => colorIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken)
            : [];
        var rolls = rollIds.Count > 0
            ? await context.FabricRolls.AsNoTracking().Where(r => rollIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, cancellationToken)
            : [];

        var batchIds = rolls.Values.Where(r => r.FabricBatchId.HasValue).Select(r => r.FabricBatchId!.Value).Distinct().ToList();
        var locIds = rolls.Values.Where(r => r.StorageLocationId.HasValue).Select(r => r.StorageLocationId!.Value).Distinct().ToList();
        var batches = batchIds.Count > 0
            ? await context.FabricBatches.AsNoTracking().Where(b => batchIds.Contains(b.Id)).ToDictionaryAsync(b => b.Id, cancellationToken)
            : [];
        var locations = locIds.Count > 0
            ? await context.WarehouseLocations.AsNoTracking().Where(l => locIds.Contains(l.Id)).ToDictionaryAsync(l => l.Id, cancellationToken)
            : [];

        var lines = rawLines.Select(l =>
        {
            rolls.TryGetValue(l.FabricRollId ?? Guid.Empty, out var roll);
            return new StocktakeLineDto
            {
                Id = l.Id,
                FabricRollId = l.FabricRollId,
                RollNumber = roll?.RollNumber ?? 0,
                FabricName = fabrics.GetValueOrDefault(l.FabricItemId)?.NameAr ?? "—",
                ColorName = colors.GetValueOrDefault(l.FabricColorId)?.NameAr ?? "—",
                BatchNumber = roll?.FabricBatchId.HasValue == true && batches.TryGetValue(roll.FabricBatchId.Value, out var b) ? b.BatchNumber : null,
                LocationCode = roll?.StorageLocationId.HasValue == true && locations.TryGetValue(roll.StorageLocationId.Value, out var loc) ? loc.Code : null,
                SystemMeters = l.SystemMeters,
                CountedMeters = l.CountedMeters,
                DifferenceMeters = l.DifferenceMeters
            };
        }).OrderBy(l => l.RollNumber).ToList();

        return new StocktakeDetailDto
        {
            Id = s.Id,
            SessionNumber = s.SessionNumber,
            WarehouseId = s.WarehouseId,
            WarehouseName = wh?.NameAr ?? "—",
            Responsible = s.Responsible,
            Status = ((InventoryDocumentStatus)s.Status).ToString(),
            Notes = s.Notes,
            Date = s.Date,
            Lines = lines,
            TotalSystemMeters = lines.Sum(x => x.SystemMeters),
            TotalCountedMeters = lines.Sum(x => x.CountedMeters),
            TotalVarianceMeters = lines.Sum(x => x.DifferenceMeters),
            LinesWithVariance = lines.Count(x => x.DifferenceMeters != 0)
        };
    }

    public async Task UpdateStocktakeLineCountsAsync(
        Guid sessionId,
        IReadOnlyList<(Guid LineId, decimal CountedMeters)> lines,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.StocktakeLines
            .Where(l => l.SessionId == sessionId)
            .ToListAsync(cancellationToken);

        foreach (var (lineId, counted) in lines)
        {
            var line = entities.FirstOrDefault(l => l.Id == lineId);
            if (line is null) continue;
            line.CountedMeters = counted;
            line.DifferenceMeters = counted - line.SystemMeters;
        }
    }

    public async Task<Guid> CreateStocktakeAsync(StocktakeSession session, CancellationToken cancellationToken = default)
    {
        await context.StocktakeSessions.AddAsync(new StocktakeSessionEntity
        {
            Id = session.Id, SessionNumber = session.SessionNumber,
            WarehouseId = session.WarehouseId, LocationId = session.LocationId,
            Responsible = session.Responsible, Status = (int)session.Status,
            Notes = session.Notes, Date = session.Date, CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        return session.Id;
    }

    public async Task AddStocktakeLineAsync(
        Guid sessionId, Guid fabricItemId, Guid fabricColorId,
        decimal systemMeters, decimal countedMeters, Guid? rollId = null,
        CancellationToken cancellationToken = default)
    {
        await context.StocktakeLines.AddAsync(new StocktakeLineEntity
        {
            Id = Guid.NewGuid(), SessionId = sessionId,
            FabricItemId = fabricItemId, FabricColorId = fabricColorId,
            FabricRollId = rollId, SystemMeters = systemMeters,
            CountedMeters = countedMeters, DifferenceMeters = countedMeters - systemMeters,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task<Guid> CreateOpeningStockAsync(
        OpeningStockDocument doc,
        IReadOnlyList<(Guid FabricItemId, Guid FabricColorId, decimal Meters, int Rolls, decimal UnitCost)> lines,
        CancellationToken cancellationToken = default)
    {
        await context.OpeningStockDocuments.AddAsync(new OpeningStockDocumentEntity
        {
            Id = doc.Id, DocumentNumber = doc.DocumentNumber,
            WarehouseId = doc.WarehouseId, OpeningDate = doc.OpeningDate,
            Reference = doc.Reference, CurrencyCode = doc.CurrencyCode,
            Status = (int)doc.Status, Notes = doc.Notes, CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        foreach (var line in lines)
        {
            await context.OpeningStockLines.AddAsync(new OpeningStockLineEntity
            {
                Id = Guid.NewGuid(), DocumentId = doc.Id,
                FabricItemId = line.FabricItemId, FabricColorId = line.FabricColorId,
                QuantityMeters = line.Meters, RollCount = line.Rolls,
                UnitCost = line.UnitCost, TotalValue = line.Meters * line.UnitCost,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);
        }
        return doc.Id;
    }

    public async Task<IReadOnlyList<StockTransferListDto>> GetTransfersAsync(
        Guid branchId, CancellationToken cancellationToken = default)
    {
        var whIds = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId).Select(w => w.Id).ToListAsync(cancellationToken);
        var transfers = await context.StockTransfers.AsNoTracking()
            .Where(t => whIds.Contains(t.FromWarehouseId) || whIds.Contains(t.ToWarehouseId))
            .OrderByDescending(t => t.Date).ToListAsync(cancellationToken);
        var allWhIds = transfers.SelectMany(t => new[] { t.FromWarehouseId, t.ToWarehouseId }).Distinct().ToList();
        var whMap = await context.Warehouses.AsNoTracking()
            .Where(w => allWhIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);

        return transfers.Select(t => new StockTransferListDto
        {
            Id = t.Id, Number = t.Number,
            FromWarehouse = whMap.GetValueOrDefault(t.FromWarehouseId)?.NameAr ?? "—",
            ToWarehouse = whMap.GetValueOrDefault(t.ToWarehouseId)?.NameAr ?? "—",
            Status = ((InventoryDocumentStatus)t.Status).ToString(), Date = t.Date
        }).ToList();
    }

    public async Task<IReadOnlyList<StocktakeListDto>> GetStocktakeSessionsAsync(
        Guid branchId, CancellationToken cancellationToken = default)
    {
        var whIds = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId).Select(w => w.Id).ToListAsync(cancellationToken);
        var sessions = await context.StocktakeSessions.AsNoTracking()
            .Where(s => whIds.Contains(s.WarehouseId))
            .OrderByDescending(s => s.Date).ToListAsync(cancellationToken);
        var whMap = await context.Warehouses.AsNoTracking()
            .Where(w => whIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);

        return sessions.Select(s => new StocktakeListDto
        {
            Id = s.Id, SessionNumber = s.SessionNumber,
            WarehouseName = whMap.GetValueOrDefault(s.WarehouseId)?.NameAr ?? "—",
            Responsible = s.Responsible,
            Status = ((InventoryDocumentStatus)s.Status).ToString(), Date = s.Date
        }).ToList();
    }

    public async Task<IReadOnlyList<OpeningStockListDto>> GetOpeningStockDocumentsAsync(
        Guid branchId, CancellationToken cancellationToken = default)
    {
        var whIds = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId).Select(w => w.Id).ToListAsync(cancellationToken);
        var docs = await context.OpeningStockDocuments.AsNoTracking()
            .Where(d => whIds.Contains(d.WarehouseId))
            .OrderByDescending(d => d.OpeningDate).ToListAsync(cancellationToken);
        var whMap = await context.Warehouses.AsNoTracking()
            .Where(w => whIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);
        var docIds = docs.Select(d => d.Id).ToList();
        var lines = await context.OpeningStockLines.AsNoTracking()
            .Where(l => docIds.Contains(l.DocumentId)).ToListAsync(cancellationToken);

        return docs.Select(d => new OpeningStockListDto
        {
            Id = d.Id, DocumentNumber = d.DocumentNumber,
            WarehouseName = whMap.GetValueOrDefault(d.WarehouseId)?.NameAr ?? "—",
            OpeningDate = d.OpeningDate,
            Status = ((InventoryDocumentStatus)d.Status).ToString(),
            TotalValue = lines.Where(l => l.DocumentId == d.Id).Sum(l => l.TotalValue)
        }).ToList();
    }

    public async Task<bool> WarehouseHasStockAsync(Guid warehouseId, CancellationToken cancellationToken = default) =>
        await context.WarehouseStocks.AsNoTracking()
            .AnyAsync(s => s.WarehouseId == warehouseId && s.TotalMeters > 0, cancellationToken)
        || await context.FabricRolls.AsNoTracking()
            .AnyAsync(r => r.WarehouseId == warehouseId && r.RemainingLengthMeters > 0, cancellationToken);

    public async Task<WarehouseDetailDto?> GetWarehouseDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var w = await context.Warehouses.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (w is null) return null;

        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.WarehouseId == id).ToListAsync(cancellationToken);
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == id && r.RemainingLengthMeters > 0).ToListAsync(cancellationToken);
        var value = rolls.Sum(r => r.RemainingLengthMeters * r.CostPerMeter);

        var lastMove = await context.StockMovements.AsNoTracking()
            .Where(m => m.WarehouseId == id || m.SourceWarehouseId == id || m.DestinationWarehouseId == id)
            .OrderByDescending(m => m.MovementDate).Select(m => m.MovementNumber).FirstOrDefaultAsync(cancellationToken);

        var lastStocktake = await context.StocktakeSessions.AsNoTracking()
            .Where(s => s.WarehouseId == id)
            .OrderByDescending(s => s.Date).Select(s => s.SessionNumber).FirstOrDefaultAsync(cancellationToken);

        var timeline = await GetTimelineAsync(id, "Warehouse", cancellationToken);
        var audit = await GetAuditTrailAsync(id, "Warehouse", cancellationToken);

        return new WarehouseDetailDto
        {
            Id = w.Id, Code = w.Code, NameAr = w.NameAr, NameEn = w.NameEn,
            Description = w.Description, City = w.City, Address = w.Address,
            Manager = w.Manager, CostCenterId = w.CostCenterId, Notes = w.Notes,
            IsDefault = w.IsDefault, IsActive = w.IsActive, IsArchived = w.IsArchived,
            CapacityRolls = w.CapacityRolls,
            RollCount = stocks.Sum(s => s.RollCount),
            TotalMeters = stocks.Sum(s => s.TotalMeters),
            InventoryValue = value,
            CreatedAt = w.CreatedAt, UpdatedAt = w.UpdatedAt,
            LastMovement = lastMove, LastStocktake = lastStocktake,
            RecentTimeline = timeline.Take(5).ToList(),
            RecentAudit = audit.Take(5).ToList()
        };
    }

    private static Warehouse MapWarehouse(WarehouseEntity e) =>
        Warehouse.FromPersistence(e.Id, e.BranchId, e.Code, e.NameAr, e.City,
            e.NameEn, e.Description, e.Address, e.Manager, e.CostCenterId,
            e.Notes, e.IsDefault, e.CapacityRolls, e.IsActive);
}
