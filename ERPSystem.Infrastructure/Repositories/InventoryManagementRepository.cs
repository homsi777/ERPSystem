using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Domain.Entities.Inventory;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
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
        if (warehouses.Count == 0)
            return [];

        var warehouseIds = warehouses.Select(w => w.Id).ToList();
        var stockTotals = await context.WarehouseStocks.AsNoTracking()
            .Where(s => warehouseIds.Contains(s.WarehouseId))
            .GroupBy(s => s.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                RollCount = g.Sum(s => s.RollCount),
                TotalMeters = g.Sum(s => s.TotalMeters)
            })
            .ToDictionaryAsync(x => x.WarehouseId, cancellationToken);

        var rollValues = await context.FabricRolls.AsNoTracking()
            .Where(r => warehouseIds.Contains(r.WarehouseId) && r.RemainingLengthMeters > 0 &&
                        r.Status == (int)FabricRollStatus.Available)
            .GroupBy(r => r.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                RollCount = g.Count(),
                InventoryValue = g.Sum(r => r.RemainingLengthMeters * r.CostPerMeter)
            })
            .ToDictionaryAsync(x => x.WarehouseId, cancellationToken);

        return warehouses.Select(w =>
        {
            stockTotals.TryGetValue(w.Id, out var stock);
            rollValues.TryGetValue(w.Id, out var value);
            return new WarehouseListExtendedDto
            {
                Id = w.Id, Code = w.Code, NameAr = w.NameAr, NameEn = w.NameEn,
                City = w.City, Manager = w.Manager, IsDefault = w.IsDefault, IsActive = w.IsActive,
                RollCount = stock?.RollCount ?? value?.RollCount ?? 0,
                TotalMeters = stock?.TotalMeters ?? 0m,
                InventoryValue = value?.InventoryValue ?? 0m
            };
        }).ToList();
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
        Guid branchId, Guid? warehouseId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var warehouseIds = warehouseId.HasValue
            ? [warehouseId.Value]
            : await context.Warehouses.AsNoTracking()
                .Where(w => w.BranchId == branchId)
                .Select(w => w.Id)
                .ToListAsync(cancellationToken);

        if (warehouseIds.Count == 0)
            return [];

        var term = search?.Trim();
        var hasSearch = !string.IsNullOrWhiteSpace(term);

        var rollCosts = context.FabricRolls.AsNoTracking()
            .Where(r => warehouseIds.Contains(r.WarehouseId) && r.RemainingLengthMeters > 0 &&
                        r.Status == (int)FabricRollStatus.Available)
            .GroupBy(r => new { r.WarehouseId, r.FabricItemId, r.FabricColorId, r.ContainerId })
            .Select(g => new
            {
                g.Key.WarehouseId,
                g.Key.FabricItemId,
                g.Key.FabricColorId,
                g.Key.ContainerId,
                InventoryValue = g.Sum(r => r.RemainingLengthMeters * r.CostPerMeter)
            });

        var balances =
            from s in context.WarehouseStocks.AsNoTracking()
            join w in context.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id
            join f in context.FabricItems.AsNoTracking() on s.FabricItemId equals f.Id
            join c in context.FabricColors.AsNoTracking() on s.FabricColorId equals c.Id
            join container in context.Containers.AsNoTracking() on s.ContainerId equals container.Id into containerJoin
            from container in containerJoin.DefaultIfEmpty()
            join rc in rollCosts
                on new { s.WarehouseId, s.FabricItemId, s.FabricColorId, s.ContainerId }
                equals new { rc.WarehouseId, rc.FabricItemId, rc.FabricColorId, rc.ContainerId }
                into rollCostJoin
            from rc in rollCostJoin.DefaultIfEmpty()
            where warehouseIds.Contains(s.WarehouseId) && s.TotalMeters > 0
                  && (!hasSearch
                      || f.Code.Contains(term!)
                      || f.NameAr.Contains(term!)
                      || c.NameAr.Contains(term!)
                      || (container != null && container.ContainerNumber.Contains(term!)))
            orderby f.NameAr, c.NameAr, container != null ? container.ContainerNumber : "", s.TotalMeters descending
            select new FabricStockBalanceDto
            {
                WarehouseId = s.WarehouseId,
                WarehouseName = w.NameAr,
                FabricItemId = s.FabricItemId,
                FabricCode = f.Code,
                FabricName = f.NameAr,
                FabricColorId = s.FabricColorId,
                ColorName = c.NameAr,
                ContainerId = s.ContainerId,
                ContainerNumber = container == null ? "-" : container.ContainerNumber,
                RollCount = s.RollCount,
                TotalMeters = s.TotalMeters,
                ReservedMeters = s.ReservedMeters,
                AvailableMeters = s.AvailableMeters,
                InventoryValue = rc == null ? 0m : rc.InventoryValue
            };

        return await balances.ToListAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<FabricStockBalanceDto>> GetFabricStockBalancesSlowAsync(
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
            .Where(r => warehouseIds.Contains(r.WarehouseId) && r.RemainingLengthMeters > 0 &&
                        r.Status == (int)FabricRollStatus.Available)
            .ToListAsync(cancellationToken);

        return stocks.Select(s =>
        {
            var itemRolls = rolls.Where(r =>
                r.WarehouseId == s.WarehouseId &&
                r.FabricItemId == s.FabricItemId &&
                r.FabricColorId == s.FabricColorId).ToList();
            var inventoryValue = itemRolls.Sum(r => r.RemainingLengthMeters * r.CostPerMeter);
            return new FabricStockBalanceDto
            {
                WarehouseId = s.WarehouseId,
                WarehouseName = warehouses.GetValueOrDefault(s.WarehouseId)?.NameAr ?? "—",
                FabricItemId = s.FabricItemId,
                FabricCode = fabrics.GetValueOrDefault(s.FabricItemId)?.Code ?? "—",
                FabricName = fabrics.GetValueOrDefault(s.FabricItemId)?.NameAr ?? "—",
                FabricColorId = s.FabricColorId,
                ColorName = colors.GetValueOrDefault(s.FabricColorId)?.NameAr ?? "—",
                ContainerId = s.ContainerId,
                ContainerNumber = s.ContainerId != Guid.Empty && containers.TryGetValue(s.ContainerId, out var c)
                    ? c.ContainerNumber : "—",
                RollCount = s.RollCount,
                TotalMeters = s.TotalMeters,
                ReservedMeters = s.ReservedMeters,
                AvailableMeters = s.AvailableMeters,
                InventoryValue = inventoryValue
            };
        }).OrderByDescending(s => s.TotalMeters).ToList();
    }

    public async Task<IReadOnlyList<FabricRollListDto>> GetFabricRollsAsync(
        Guid warehouseId, CancellationToken cancellationToken = default)
    {
        var page = await GetFabricRollsPageAsync(warehouseId, pageNumber: 1, pageSize: 200, cancellationToken: cancellationToken);
        return page.Items.ToList();
    }

    public async Task<PaginatedFabricRollDto> GetFabricRollsPageAsync(
        Guid warehouseId,
        int pageNumber = 1,
        int pageSize = 50,
        int? status = null,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 10, 500);

        var query = context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId && r.RemainingLengthMeters > 0);

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = int.TryParse(term, out var rollNumber)
                ? query.Where(r =>
                    r.RollNumber == rollNumber ||
                    (r.Barcode != null && r.Barcode.Contains(term)) ||
                    (r.LotCode != null && r.LotCode.Contains(term)))
                : query.Where(r =>
                    (r.Barcode != null && r.Barcode.Contains(term)) ||
                    (r.LotCode != null && r.LotCode.Contains(term)));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var skip = (pageNumber - 1) * pageSize;

        var rows = await (
            from r in query
            join f in context.FabricItems.AsNoTracking() on r.FabricItemId equals f.Id
            join c in context.FabricColors.AsNoTracking() on r.FabricColorId equals c.Id
            join b in context.FabricBatches.AsNoTracking() on r.FabricBatchId equals (Guid?)b.Id into batchJoin
            from b in batchJoin.DefaultIfEmpty()
            join l in context.WarehouseLocations.AsNoTracking() on r.StorageLocationId equals (Guid?)l.Id into locationJoin
            from l in locationJoin.DefaultIfEmpty()
            orderby r.RollNumber
            select new
            {
                r.Id,
                r.RollNumber,
                r.Barcode,
                FabricName = f.NameAr,
                ColorName = c.NameAr,
                r.LengthMeters,
                r.RemainingLengthMeters,
                r.CostPerMeter,
                r.Status,
                BatchNumber = b == null ? null : b.BatchNumber,
                LocationCode = l == null ? null : l.Code,
                r.LotCode
            })
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PaginatedFabricRollDto
        {
            Items = rows.Select(r => new FabricRollListDto
            {
                Id = r.Id,
                RollNumber = r.RollNumber,
                Barcode = r.Barcode,
                FabricName = r.FabricName,
                ColorName = r.ColorName,
                LengthMeters = r.LengthMeters,
                RemainingLengthMeters = r.RemainingLengthMeters,
                CostPerMeter = r.CostPerMeter,
                CurrentValue = r.RemainingLengthMeters * r.CostPerMeter,
                Status = ((FabricRollStatus)r.Status).ToString(),
                BatchNumber = r.BatchNumber,
                LocationCode = r.LocationCode,
                LotCode = r.LotCode
            }).ToList(),
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    private async Task<IReadOnlyList<FabricRollListDto>> GetFabricRollsSlowAsync(
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

    public async Task<IReadOnlyList<FabricRollListDto>> GetFabricRollsByStockAsync(
        Guid warehouseId, Guid containerId, Guid fabricItemId, Guid fabricColorId,
        CancellationToken cancellationToken = default)
    {
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId &&
                        r.ContainerId == containerId &&
                        r.FabricItemId == fabricItemId &&
                        r.FabricColorId == fabricColorId &&
                        r.RemainingLengthMeters > 0)
            .OrderBy(r => r.RollNumber).ToListAsync(cancellationToken);
        if (rolls.Count == 0) return [];

        var fabric = await context.FabricItems.AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == fabricItemId, cancellationToken);
        var color = await context.FabricColors.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == fabricColorId, cancellationToken);
        var batchIds = rolls.Where(r => r.FabricBatchId.HasValue).Select(r => r.FabricBatchId!.Value).Distinct().ToList();
        var locIds = rolls.Where(r => r.StorageLocationId.HasValue).Select(r => r.StorageLocationId!.Value).Distinct().ToList();

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
            FabricName = fabric?.NameAr ?? "—",
            ColorName = color?.NameAr ?? "—",
            LengthMeters = r.LengthMeters, RemainingLengthMeters = r.RemainingLengthMeters,
            CostPerMeter = r.CostPerMeter,
            CurrentValue = r.RemainingLengthMeters * r.CostPerMeter,
            Status = ((FabricRollStatus)r.Status).ToString(),
            BatchNumber = r.FabricBatchId.HasValue && batches.TryGetValue(r.FabricBatchId.Value, out var b) ? b.BatchNumber : null,
            LocationCode = r.StorageLocationId.HasValue && locations.TryGetValue(r.StorageLocationId.Value, out var l) ? l.Code : null,
            LotCode = r.LotCode
        }).ToList();
    }

    public async Task<IReadOnlyList<FabricRollSalesReservationDto>> GetFabricRollSalesReservationsAsync(
        IReadOnlyList<Guid> rollIds,
        Guid? excludeSalesInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        var ids = rollIds.Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0)
            return [];

        var activeStatuses = new[]
        {
            (int)SalesInvoiceStatus.Detailed,
            (int)SalesInvoiceStatus.ReadyForApproval,
            (int)SalesInvoiceStatus.Approved,
            (int)SalesInvoiceStatus.Printed
        };

        var query =
            from detail in context.SalesInvoiceRollDetails.AsNoTracking()
            join invoice in context.SalesInvoices.AsNoTracking() on detail.SalesInvoiceId equals invoice.Id
            where detail.FabricRollId.HasValue &&
                  ids.Contains(detail.FabricRollId.Value) &&
                  activeStatuses.Contains(invoice.Status)
            select new { detail.FabricRollId, invoice.Id, invoice.InvoiceNumber, invoice.Status };

        if (excludeSalesInvoiceId.HasValue)
            query = query.Where(x => x.Id != excludeSalesInvoiceId.Value);

        return await query
            .GroupBy(x => x.FabricRollId!.Value)
            .Select(g => g
                .OrderByDescending(x => x.Status)
                .Select(x => new FabricRollSalesReservationDto
                {
                    FabricRollId = g.Key,
                    SalesInvoiceId = x.Id,
                    SalesInvoiceNumber = x.InvoiceNumber,
                    SalesInvoiceStatus = x.Status
                })
                .First())
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DetailingCandidateRollDto>> GetDetailingCandidateRollsAsync(
        Guid warehouseId,
        Guid containerId,
        Guid fabricItemId,
        Guid fabricColorId,
        Guid? excludeSalesInvoiceId = null,
        CancellationToken cancellationToken = default)
    {
        // Same prefetch filter as InventoryEngine.ResolveDetailingEntriesAsync (container +
        // warehouse + fabric + color + status Reserved/Available) so a roll shown here is
        // guaranteed to be accepted by the resolver at final completion.
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r =>
                r.WarehouseId == warehouseId &&
                r.ContainerId == containerId &&
                r.FabricItemId == fabricItemId &&
                r.FabricColorId == fabricColorId &&
                (r.Status == (int)FabricRollStatus.Reserved || r.Status == (int)FabricRollStatus.Available))
            .OrderBy(r => r.RollNumber)
            .ToListAsync(cancellationToken);

        if (rolls.Count == 0)
            return [];

        var rollIds = rolls.Select(r => r.Id).ToList();
        var reservations = await GetFabricRollSalesReservationsAsync(rollIds, excludeSalesInvoiceId, cancellationToken);
        var reservationByRoll = reservations.ToDictionary(r => r.FabricRollId);

        return rolls.Select(r =>
        {
            reservationByRoll.TryGetValue(r.Id, out var reservation);
            return new DetailingCandidateRollDto
            {
                FabricRollId = r.Id,
                RollNumber = r.RollNumber,
                RemainingLengthMeters = r.RemainingLengthMeters,
                Status = ((FabricRollStatus)r.Status).ToString(),
                ReservedInSalesInvoiceId = reservation?.SalesInvoiceId,
                ReservedInSalesInvoiceNumber = reservation?.SalesInvoiceNumber,
                ReservedInSalesInvoiceStatus = reservation?.SalesInvoiceStatus
            };
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
        var w = await context.Warehouses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == warehouseId, cancellationToken)
            ?? throw new InvalidOperationException("Warehouse not found.");

        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId).ToListAsync(cancellationToken);
        var value = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId && r.RemainingLengthMeters > 0)
            .SumAsync(r => r.RemainingLengthMeters * r.CostPerMeter, cancellationToken);

        var whDto = new WarehouseListExtendedDto
        {
            Id = w.Id, Code = w.Code, NameAr = w.NameAr, NameEn = w.NameEn,
            City = w.City, Manager = w.Manager, IsDefault = w.IsDefault, IsActive = w.IsActive,
            RollCount = stocks.Sum(s => s.RollCount), TotalMeters = stocks.Sum(s => s.TotalMeters),
            InventoryValue = value
        };

        var branchId = w.BranchId;
        var stockBalances = await GetFabricStockBalancesAsync(branchId, warehouseId, cancellationToken: cancellationToken);
        var locations = await GetLocationsAsync(warehouseId, cancellationToken);
        var movements = await GetMovementsAsync(branchId, warehouseId, cancellationToken);
        var audit = await GetAuditTrailAsync(warehouseId, "Warehouse", cancellationToken);
        var timeline = await GetTimelineAsync(warehouseId, "Warehouse", cancellationToken);

        var transfers = await context.StockTransfers.AsNoTracking()
            .Where(t => t.FromWarehouseId == warehouseId || t.ToWarehouseId == warehouseId)
            .OrderByDescending(t => t.Date).Take(20).ToListAsync(cancellationToken);
        var stocktakes = await context.StocktakeSessions.AsNoTracking()
            .Where(s => s.WarehouseId == warehouseId)
            .OrderByDescending(s => s.Date).Take(20).ToListAsync(cancellationToken);
        var openingDocs = await context.OpeningStockDocuments.AsNoTracking()
            .Where(d => d.WarehouseId == warehouseId)
            .OrderByDescending(d => d.OpeningDate).Take(10).ToListAsync(cancellationToken);

        var pendingTransfers = transfers.Count(t => t.Status is (int)InventoryDocumentStatus.Draft or (int)InventoryDocumentStatus.Approved);
        var pendingStocktakes = stocktakes.Count(s => s.Status is (int)InventoryDocumentStatus.Draft or (int)InventoryDocumentStatus.Counting or (int)InventoryDocumentStatus.Review);

        string? costCenterName = null;
        if (w.CostCenterId.HasValue)
        {
            costCenterName = await context.CostCenters.AsNoTracking()
                .Where(c => c.Id == w.CostCenterId.Value)
                .Select(c => c.Name)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var executive = await BuildExecutiveDashboardAsync(
            warehouseId, w.NameAr, branchId, value, stockBalances,
            transfers, stocktakes, openingDocs, audit, timeline, pendingTransfers, pendingStocktakes,
            cancellationToken);

        var branchAlerts = await GetAlertsAsync(branchId, true, cancellationToken);
        var whAlerts = branchAlerts
            .Where(a => a.WarehouseName == w.NameAr || a.WarehouseName is null)
            .ToList();

        return new InventoryOperationsCenterDto
        {
            Warehouse = whDto,
            CostCenterName = costCenterName,
            Executive = executive,
            Stock = stockBalances,
            Rolls = await GetFabricRollsAsync(warehouseId, cancellationToken),
            Locations = locations,
            RecentMovements = movements,
            Alerts = whAlerts,
            RecentAudit = audit,
            Timeline = timeline,
            PendingTransfers = pendingTransfers,
            PendingStocktakes = pendingStocktakes,
            InventoryValue = value
        };
    }

    private async Task<WarehouseExecutiveDashboardDto> BuildExecutiveDashboardAsync(
        Guid warehouseId,
        string warehouseName,
        Guid branchId,
        decimal inventoryValue,
        IReadOnlyList<FabricStockBalanceDto> stockBalances,
        IReadOnlyList<StockTransferDocumentEntity> transfers,
        IReadOnlyList<StocktakeSessionEntity> stocktakes,
        IReadOnlyList<OpeningStockDocumentEntity> openingDocs,
        IReadOnlyList<InventoryAuditDto> audit,
        IReadOnlyList<InventoryTimelineDto> timeline,
        int pendingTransfers,
        int pendingStocktakes,
        CancellationToken cancellationToken)
    {
        var since30 = DateTime.UtcNow.Date.AddDays(-29);
        var fabricIds = stockBalances.Select(r => r.FabricItemId).Distinct().ToList();
        var fabrics = fabricIds.Count > 0
            ? await context.FabricItems.AsNoTracking()
                .Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, cancellationToken)
            : [];
        var categoryIds = fabrics.Values.Select(f => f.CategoryId).Distinct().ToList();
        var categories = categoryIds.Count > 0
            ? await context.FabricCategories.AsNoTracking()
                .Where(c => categoryIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, cancellationToken)
            : [];

        var valueByFabric = stockBalances
            .GroupBy(r => r.FabricItemId)
            .Select(g =>
            {
                var v = g.Sum(r => r.InventoryValue);
                return new { FabricId = g.Key, Value = v, Name = fabrics.GetValueOrDefault(g.Key)?.NameAr ?? "—" };
            })
            .OrderByDescending(x => x.Value).Take(8).ToList();
        var totalVal = valueByFabric.Sum(x => x.Value);
        var fabricSlices = valueByFabric.Select(x => new WarehouseValueSliceDto
        {
            Label = x.Name,
            Value = x.Value,
            Percent = totalVal > 0 ? Math.Round(x.Value / totalVal * 100, 1) : 0
        }).ToList();

        var valueByCategory = stockBalances
            .GroupBy(r => fabrics.GetValueOrDefault(r.FabricItemId)?.CategoryId ?? Guid.Empty)
            .Select(g =>
            {
                var v = g.Sum(r => r.InventoryValue);
                var catName = g.Key != Guid.Empty && categories.TryGetValue(g.Key, out var c) ? c.NameAr : "غير مصنف";
                return new { Label = catName, Value = v };
            })
            .OrderByDescending(x => x.Value).Take(6).ToList();
        var catTotal = valueByCategory.Sum(x => x.Value);
        var categorySlices = valueByCategory.Select(x => new WarehouseValueSliceDto
        {
            Label = x.Label,
            Value = x.Value,
            Percent = catTotal > 0 ? Math.Round(x.Value / catTotal * 100, 1) : 0
        }).ToList();

        var damagedStatus = (int)FabricRollStatus.Wasted;
        var blockedQuality = (int)InventoryQualityStatus.Damaged;
        var damagedMeters = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId && r.QualityStatus == blockedQuality)
            .SumAsync(r => r.RemainingLengthMeters, cancellationToken);
        var blockedMeters = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == warehouseId &&
                        r.Status != (int)FabricRollStatus.Available &&
                        r.Status != damagedStatus)
            .SumAsync(r => r.RemainingLengthMeters, cancellationToken);
        var quantities = new WarehouseQuantityMetricsDto
        {
            TotalRolls = stockBalances.Sum(s => s.RollCount),
            TotalMeters = stockBalances.Sum(s => s.TotalMeters),
            AvailableMeters = stockBalances.Sum(s => s.AvailableMeters),
            ReservedMeters = stockBalances.Sum(s => s.ReservedMeters),
            DamagedMeters = damagedMeters,
            BlockedMeters = blockedMeters
        };

        var whIds = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId).Select(w => w.Id).ToListAsync(cancellationToken);
        var whMap = await context.Warehouses.AsNoTracking()
            .Where(w => whIds.Contains(w.Id)).ToDictionaryAsync(w => w.Id, cancellationToken);

        var recentMovementsRaw = await context.StockMovements.AsNoTracking()
            .Where(m => m.WarehouseId == warehouseId ||
                        m.SourceWarehouseId == warehouseId ||
                        m.DestinationWarehouseId == warehouseId)
            .OrderByDescending(m => m.MovementDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        var movementIds = recentMovementsRaw.Select(m => m.Id).ToList();
        var movementLines = movementIds.Count > 0
            ? await context.StockMovementLines.AsNoTracking()
                .Where(l => movementIds.Contains(l.MovementId)).ToListAsync(cancellationToken)
            : [];
        var userIds = recentMovementsRaw.Where(m => m.UserId.HasValue).Select(m => m.UserId!.Value).Distinct().ToList();
        var users = userIds.Count > 0
            ? await context.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, cancellationToken)
            : [];

        var movementCards = recentMovementsRaw.Select(m =>
        {
            var lines = movementLines.Where(l => l.MovementId == m.Id).ToList();
            var type = (MovementType)m.Type;
            var from = m.SourceWarehouseId.HasValue
                ? whMap.GetValueOrDefault(m.SourceWarehouseId.Value)?.NameAr ?? "—"
                : type is MovementType.Sale or MovementType.Transfer ? warehouseName : "—";
            var to = m.DestinationWarehouseId.HasValue
                ? whMap.GetValueOrDefault(m.DestinationWarehouseId.Value)?.NameAr ?? "—"
                : type is MovementType.Import or MovementType.Purchase or MovementType.OpeningBalance
                    ? warehouseName : "—";
            if (type is MovementType.Import or MovementType.Purchase or MovementType.OpeningBalance or MovementType.Stocktake)
                from = type switch
                {
                    MovementType.Import => "استيراد الصين",
                    MovementType.Purchase => "فاتورة شراء",
                    MovementType.OpeningBalance => "أول المدة",
                    MovementType.Stocktake => "جرد",
                    _ => from
                };

            return new WarehouseMovementCardDto
            {
                Id = m.Id,
                MovementNumber = m.MovementNumber,
                Type = type.ToString(),
                TypeIcon = MovementIcon(type),
                FromLabel = from,
                ToLabel = to,
                QuantityMeters = lines.Sum(l => Math.Abs(l.QuantityMeters)),
                TotalValue = lines.Sum(l => Math.Abs(l.TotalValue)),
                Timestamp = m.MovementDate,
                Username = m.UserId.HasValue && users.TryGetValue(m.UserId.Value, out var u)
                    ? (string.IsNullOrWhiteSpace(u.FullNameAr) ? u.Username : u.FullNameAr) : "—",
                ReferenceType = m.ReferenceType.HasValue ? ((DocumentType)m.ReferenceType).ToString() : null,
                ReferenceId = m.ReferenceId,
                ReferenceNumber = m.MovementNumber
            };
        }).ToList();

        var last30Movements = await context.StockMovements.AsNoTracking()
            .Where(m => m.MovementDate >= since30 &&
                        (m.WarehouseId == warehouseId ||
                         m.SourceWarehouseId == warehouseId ||
                         m.DestinationWarehouseId == warehouseId))
            .ToListAsync(cancellationToken);
        var last30Ids = last30Movements.Select(m => m.Id).ToList();
        var last30Lines = last30Ids.Count > 0
            ? await context.StockMovementLines.AsNoTracking()
                .Where(l => last30Ids.Contains(l.MovementId)).ToListAsync(cancellationToken)
            : [];

        var incomingTypes = new HashSet<MovementType>
        {
            MovementType.Import, MovementType.Purchase, MovementType.OpeningBalance, MovementType.SaleReturn
        };
        var outgoingTypes = new HashSet<MovementType> { MovementType.Sale, MovementType.Consumption, MovementType.Damage };

        decimal ClassifyIncoming(StockMovementEntity m)
        {
            var lines = last30Lines.Where(l => l.MovementId == m.Id).ToList();
            if (m.DestinationWarehouseId == warehouseId) return lines.Sum(l => Math.Abs(l.QuantityMeters));
            if (m.WarehouseId == warehouseId && m.SourceWarehouseId.HasValue && (MovementType)m.Type == MovementType.Transfer)
                return lines.Sum(l => Math.Abs(l.QuantityMeters));
            if (m.WarehouseId == warehouseId && incomingTypes.Contains((MovementType)m.Type))
                return lines.Sum(l => Math.Abs(l.QuantityMeters));
            if (m.WarehouseId == warehouseId && (MovementType)m.Type == MovementType.Stocktake)
                return lines.Where(l => l.QuantityMeters > 0).Sum(l => l.QuantityMeters);
            return 0;
        }

        decimal ClassifyOutgoing(StockMovementEntity m)
        {
            var lines = last30Lines.Where(l => l.MovementId == m.Id).ToList();
            if (m.SourceWarehouseId == warehouseId) return lines.Sum(l => Math.Abs(l.QuantityMeters));
            if (m.WarehouseId == warehouseId && m.DestinationWarehouseId.HasValue && (MovementType)m.Type == MovementType.Transfer)
                return lines.Sum(l => Math.Abs(l.QuantityMeters));
            if (m.WarehouseId == warehouseId && outgoingTypes.Contains((MovementType)m.Type))
                return lines.Sum(l => Math.Abs(l.QuantityMeters));
            if (m.WarehouseId == warehouseId && (MovementType)m.Type == MovementType.Stocktake)
                return lines.Where(l => l.QuantityMeters < 0).Sum(l => Math.Abs(l.QuantityMeters));
            return 0;
        }

        var dailyActivity = Enumerable.Range(0, 30)
            .Select(i => since30.AddDays(i))
            .Select(day =>
            {
                var dayMovements = last30Movements
                    .Where(m => m.MovementDate.Date == day.Date).ToList();
                var incoming = dayMovements.Sum(ClassifyIncoming);
                var outgoing = dayMovements.Sum(ClassifyOutgoing);
                return new WarehouseDailyActivityDto
                {
                    Date = day,
                    IncomingMeters = incoming,
                    OutgoingMeters = outgoing,
                    NetMeters = incoming - outgoing
                };
            }).ToList();

        var net30 = dailyActivity.Sum(d => d.NetMeters);
        var trendPct = inventoryValue > 0 && net30 != 0
            ? Math.Round(net30 / Math.Max(inventoryValue, 1) * 100, 1)
            : 0m;

        var sparkline = new List<decimal>();
        var running = inventoryValue;
        for (var i = dailyActivity.Count - 1; i >= 0; i--)
        {
            sparkline.Add(Math.Max(0, running));
            running -= dailyActivity[i].NetMeters;
        }
        sparkline.Reverse();

        var topFabrics = last30Lines
            .Join(last30Movements, l => l.MovementId, m => m.Id, (l, m) => new { l, m })
            .GroupBy(x => x.l.FabricItemId)
            .Select(g => new WarehouseTopFabricDto
            {
                FabricName = fabrics.GetValueOrDefault(g.Key)?.NameAr ?? "—",
                MetersMoved = g.Sum(x => Math.Abs(x.l.QuantityMeters)),
                MovementCount = g.Select(x => x.m.Id).Distinct().Count()
            })
            .OrderByDescending(x => x.MetersMoved)
            .Take(5)
            .ToList();

        var alerts = new List<WarehouseAlertCardDto>();
        foreach (var s in stockBalances.Where(x => x.AvailableMeters > 0 && x.AvailableMeters <= 50))
        {
            alerts.Add(new WarehouseAlertCardDto
            {
                AlertType = "LowStock",
                Severity = "Warning",
                Title = "مخزون منخفض",
                Message = $"{s.FabricName} / {s.ColorName}: {s.AvailableMeters:N1} م متاح",
                NavigationTarget = "Stock"
            });
        }
        foreach (var s in stockBalances.Where(x => x.AvailableMeters < 0 || x.TotalMeters < 0))
        {
            alerts.Add(new WarehouseAlertCardDto
            {
                AlertType = "NegativeStock",
                Severity = "Critical",
                Title = "مخزون سالب",
                Message = $"{s.FabricName}: {s.AvailableMeters:N1} م",
                NavigationTarget = "Stock"
            });
        }
        if (pendingTransfers > 0)
        {
            alerts.Add(new WarehouseAlertCardDto
            {
                AlertType = "PendingTransfer",
                Severity = "Info",
                Title = "مناقلات معلقة",
                Message = $"{pendingTransfers} مناقلة بانتظار الإكمال",
                NavigationTarget = "Transfers"
            });
        }
        if (pendingStocktakes > 0)
        {
            alerts.Add(new WarehouseAlertCardDto
            {
                AlertType = "PendingStocktake",
                Severity = "Info",
                Title = "جرد معلق",
                Message = $"{pendingStocktakes} جلسة جرد نشطة",
                NavigationTarget = "Stocktake"
            });
        }

        var dbAlerts = await context.InventoryAlerts.AsNoTracking()
            .Where(a => a.BranchId == branchId && !a.IsAcknowledged &&
                        (a.WarehouseId == warehouseId || a.WarehouseId == null))
            .OrderByDescending(a => a.CreatedAt).Take(5).ToListAsync(cancellationToken);
        foreach (var a in dbAlerts)
        {
            alerts.Add(new WarehouseAlertCardDto
            {
                AlertType = ((InventoryAlertType)a.AlertType).ToString(),
                Severity = ((InventoryAlertSeverity)a.Severity).ToString(),
                Title = a.Title,
                Message = a.Message,
                DocumentId = a.FabricRollId
            });
        }

        var documents = new List<WarehouseDocumentCardDto>();
        foreach (var t in transfers.Take(5))
        {
            documents.Add(new WarehouseDocumentCardDto
            {
                DocumentType = "Transfer",
                Id = t.Id,
                Number = t.Number,
                Status = ((InventoryDocumentStatus)t.Status).ToString(),
                Date = t.Date,
                NavigationTarget = "TransferWizard"
            });
        }
        foreach (var s in stocktakes.Take(3))
        {
            documents.Add(new WarehouseDocumentCardDto
            {
                DocumentType = "Stocktake",
                Id = s.Id,
                Number = s.SessionNumber,
                Status = ((InventoryDocumentStatus)s.Status).ToString(),
                Date = s.Date,
                NavigationTarget = "StocktakeWizard"
            });
        }
        foreach (var d in openingDocs.Take(3))
        {
            documents.Add(new WarehouseDocumentCardDto
            {
                DocumentType = "OpeningStock",
                Id = d.Id,
                Number = d.DocumentNumber,
                Status = ((InventoryDocumentStatus)d.Status).ToString(),
                Date = d.OpeningDate,
                NavigationTarget = "OpeningStockForm"
            });
        }
        var recentDocuments = documents.OrderByDescending(d => d.Date).Take(5).ToList();

        WarehouseUserActivityDto? lastActivity = null;
        var lastAudit = audit.FirstOrDefault();
        var lastTimeline = timeline.FirstOrDefault();
        if (lastAudit is not null && (lastTimeline is null || lastAudit.RecordedAt >= lastTimeline.OccurredAt))
        {
            lastActivity = new WarehouseUserActivityDto
            {
                Username = lastAudit.Username,
                ActionType = lastAudit.Action,
                Timestamp = lastAudit.RecordedAt
            };
        }
        else if (lastTimeline is not null)
        {
            lastActivity = new WarehouseUserActivityDto
            {
                Username = lastTimeline.Username,
                ActionType = lastTimeline.Title,
                Timestamp = lastTimeline.OccurredAt
            };
        }

        return new WarehouseExecutiveDashboardDto
        {
            TotalInventoryValue = inventoryValue,
            ValueTrendPercent30d = trendPct,
            ValueByFabric = fabricSlices,
            ValueByCategory = categorySlices,
            ValueSparkline30d = sparkline,
            Quantities = quantities,
            RecentMovements = movementCards.Take(5).ToList(),
            LastTransaction = movementCards.FirstOrDefault(),
            TopMovingFabrics = topFabrics,
            Alerts = alerts.Take(8).ToList(),
            RecentDocuments = recentDocuments,
            LastUserActivity = lastActivity,
            Activity30Days = dailyActivity
        };
    }

    private static string MovementIcon(MovementType type) => type switch
    {
        MovementType.Import => "\uE898",
        MovementType.Purchase => "\uE7BF",
        MovementType.Sale => "\uE9F9",
        MovementType.Transfer => "\uE8AB",
        MovementType.OpeningBalance => "\uE8C8",
        MovementType.Stocktake => "\uE787",
        MovementType.Adjustment or MovementType.Correction => "\uE70F",
        _ => "\uE8CB"
    };

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
        var value = await context.FabricRolls.AsNoTracking()
            .Where(r => r.WarehouseId == id && r.RemainingLengthMeters > 0)
            .SumAsync(r => r.RemainingLengthMeters * r.CostPerMeter, cancellationToken);

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

    public async Task<IReadOnlyList<FabricSearchProfileDto>> GetFabricSearchProfilesAsync(
        Guid branchId,
        string search,
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default)
    {
        var stock = await GetFabricStockBalancesAsync(branchId, warehouseId, search, cancellationToken);
        if (stock.Count == 0)
            return [];

        var fabricIds = stock.Select(s => s.FabricItemId).Distinct().ToList();
        var containerIds = stock.Where(s => s.ContainerId != Guid.Empty).Select(s => s.ContainerId).Distinct().ToList();

        var fabrics = await (
            from f in context.FabricItems.AsNoTracking()
            join c in context.FabricCategories.AsNoTracking() on f.CategoryId equals c.Id into catJoin
            from c in catJoin.DefaultIfEmpty()
            where fabricIds.Contains(f.Id)
            select new { f.Id, f.Code, f.NameAr, CategoryName = c == null ? "—" : c.NameAr }
        ).ToDictionaryAsync(x => x.Id, cancellationToken);

        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => fabricIds.Contains(r.FabricItemId) && r.RemainingLengthMeters > 0)
            .Select(r => new
            {
                r.FabricItemId,
                r.FabricColorId,
                r.ContainerId,
                r.WarehouseId,
                r.CostPerMeter,
                r.SalePricePerMeter,
                r.RemainingLengthMeters
            })
            .ToListAsync(cancellationToken);

        var containers = containerIds.Count == 0
            ? []
            : await context.Containers.AsNoTracking()
                .Where(c => containerIds.Contains(c.Id))
                .ToListAsync(cancellationToken);

        var supplierIds = containers.Select(c => c.SupplierId).Distinct().ToList();
        var suppliers = supplierIds.Count == 0
            ? new Dictionary<Guid, SupplierEntity>()
            : await context.Suppliers.AsNoTracking()
                .Where(s => supplierIds.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, cancellationToken);

        var typeLines = containerIds.Count == 0
            ? []
            : await context.ContainerFabricTypeLines.AsNoTracking()
                .Where(l => containerIds.Contains(l.ContainerId) && l.FabricItemId.HasValue && fabricIds.Contains(l.FabricItemId.Value))
                .ToListAsync(cancellationToken);

        var movementEvents = await (
            from line in context.StockMovementLines.AsNoTracking()
            join movement in context.StockMovements.AsNoTracking() on line.MovementId equals movement.Id
            join wh in context.Warehouses.AsNoTracking() on movement.WarehouseId equals wh.Id
            where fabricIds.Contains(line.FabricItemId) && wh.BranchId == branchId
            orderby movement.MovementDate descending
            select new
            {
                line.FabricItemId,
                movement.MovementDate,
                movement.MovementNumber,
                movement.Type,
                WarehouseName = wh.NameAr,
                line.QuantityMeters,
                line.TotalValue
            })
            .Take(400)
            .ToListAsync(cancellationToken);

        return fabricIds
            .Select(fabricId =>
            {
                var fabricStock = stock.Where(s => s.FabricItemId == fabricId).ToList();
                var fabricRolls = rolls.Where(r => r.FabricItemId == fabricId).ToList();
                var fabricMeta = fabrics.GetValueOrDefault(fabricId);

                var salePrices = fabricRolls
                    .Where(r => r.SalePricePerMeter.HasValue && r.SalePricePerMeter.Value > 0)
                    .Select(r => r.SalePricePerMeter!.Value)
                    .Concat(typeLines
                        .Where(l => l.FabricItemId == fabricId && l.SalePricePerMeterUsd > 0)
                        .Select(l => l.SalePricePerMeterUsd))
                    .ToList();

                var costs = fabricRolls.Where(r => r.CostPerMeter > 0).Select(r => r.CostPerMeter).ToList();

                var colors = fabricStock
                    .GroupBy(s => new { s.FabricColorId, s.ColorName })
                    .Select(g =>
                    {
                        var colorRolls = fabricRolls.Where(r => r.FabricColorId == g.Key.FabricColorId).ToList();
                        var colorSale = colorRolls
                            .Where(r => r.SalePricePerMeter.HasValue && r.SalePricePerMeter.Value > 0)
                            .Select(r => r.SalePricePerMeter!.Value)
                            .ToList();
                        var colorCost = colorRolls.Where(r => r.CostPerMeter > 0).Select(r => r.CostPerMeter).ToList();
                        return new FabricSearchColorBreakdownDto
                        {
                            FabricColorId = g.Key.FabricColorId,
                            ColorName = g.Key.ColorName,
                            RollCount = g.Sum(x => x.RollCount),
                            TotalMeters = g.Sum(x => x.TotalMeters),
                            AvailableMeters = g.Sum(x => x.AvailableMeters),
                            ReservedMeters = g.Sum(x => x.ReservedMeters),
                            InventoryValue = g.Sum(x => x.InventoryValue),
                            AvgSalePricePerMeter = colorSale.Count > 0 ? colorSale.Average() : null,
                            AvgCostPerMeter = colorCost.Count > 0 ? colorCost.Average() : null,
                            ContainerCount = g.Select(x => x.ContainerId).Distinct().Count()
                        };
                    })
                    .OrderByDescending(c => c.TotalMeters)
                    .ToList();

                var locations = fabricStock
                    .OrderBy(s => s.WarehouseName)
                    .ThenBy(s => s.ContainerNumber)
                    .ThenBy(s => s.ColorName)
                    .Select(s =>
                    {
                        var locRolls = fabricRolls.Where(r =>
                            r.WarehouseId == s.WarehouseId &&
                            r.ContainerId == s.ContainerId &&
                            r.FabricColorId == s.FabricColorId).ToList();
                        var locSale = locRolls
                            .Where(r => r.SalePricePerMeter.HasValue && r.SalePricePerMeter.Value > 0)
                            .Select(r => r.SalePricePerMeter!.Value)
                            .ToList();
                        var locCost = locRolls.Where(r => r.CostPerMeter > 0).Select(r => r.CostPerMeter).ToList();
                        return new FabricSearchLocationDetailDto
                        {
                            WarehouseId = s.WarehouseId,
                            WarehouseName = s.WarehouseName,
                            ContainerId = s.ContainerId,
                            ContainerNumber = s.ContainerNumber,
                            FabricColorId = s.FabricColorId,
                            ColorName = s.ColorName,
                            RollCount = s.RollCount,
                            TotalMeters = s.TotalMeters,
                            AvailableMeters = s.AvailableMeters,
                            ReservedMeters = s.ReservedMeters,
                            InventoryValue = s.InventoryValue,
                            AvgCostPerMeter = locCost.Count > 0 ? locCost.Average() : null,
                            AvgSalePricePerMeter = locSale.Count > 0 ? locSale.Average() : null
                        };
                    })
                    .ToList();

                var fabricContainerIds = fabricStock.Where(s => s.ContainerId != Guid.Empty).Select(s => s.ContainerId).Distinct().ToList();
                var containerJourney = fabricContainerIds
                    .Select(cid =>
                    {
                        var container = containers.FirstOrDefault(c => c.Id == cid);
                        if (container is null)
                            return null;

                        var line = typeLines
                            .Where(l => l.ContainerId == cid && l.FabricItemId == fabricId)
                            .OrderByDescending(l => l.SalePricePerMeterUsd)
                            .FirstOrDefault();
                        var legStock = fabricStock.Where(s => s.ContainerId == cid).ToList();
                        suppliers.TryGetValue(container.SupplierId, out var supplier);

                        return new FabricSearchContainerLegDto
                        {
                            ContainerId = cid,
                            ContainerNumber = container.ContainerNumber,
                            StatusLabel = MapContainerStatusLabel(container.Status),
                            SupplierName = supplier?.NameAr ?? supplier?.Name,
                            ShipmentDate = container.ShipmentDate,
                            ArrivalDate = container.ArrivalDate,
                            ApprovedAt = container.ApprovedAt,
                            RollCount = legStock.Sum(s => s.RollCount),
                            TotalMeters = legStock.Sum(s => s.TotalMeters),
                            LandedCostPerMeter = line?.LandedCostPerMeterUsd > 0 ? line.LandedCostPerMeterUsd : null,
                            SalePricePerMeter = line?.SalePricePerMeterUsd > 0 ? line.SalePricePerMeterUsd : null,
                            Warehouses = legStock.Select(s => s.WarehouseName).Distinct().OrderBy(x => x).ToList()
                        };
                    })
                    .Where(x => x is not null)
                    .Cast<FabricSearchContainerLegDto>()
                    .OrderByDescending(x => x.TotalMeters)
                    .ToList();

                var timeline = new List<FabricSearchJourneyEventDto>();
                foreach (var leg in containerJourney)
                {
                    if (leg.ShipmentDate is { } shipped)
                    {
                        timeline.Add(new FabricSearchJourneyEventDto
                        {
                            OccurredAt = shipped,
                            Category = "China",
                            Title = $"شحن حاوية {leg.ContainerNumber}",
                            Description = $"المورد: {leg.SupplierName ?? "—"} • {leg.StatusLabel}"
                        });
                    }
                    if (leg.ArrivalDate.HasValue)
                    {
                        timeline.Add(new FabricSearchJourneyEventDto
                        {
                            OccurredAt = leg.ArrivalDate.Value,
                            Category = "China",
                            Title = $"وصول حاوية {leg.ContainerNumber}",
                            Description = $"الحالة: {leg.StatusLabel}"
                        });
                    }
                    if (leg.ApprovedAt.HasValue)
                    {
                        timeline.Add(new FabricSearchJourneyEventDto
                        {
                            OccurredAt = leg.ApprovedAt.Value,
                            Category = "China",
                            Title = $"اعتماد حاوية {leg.ContainerNumber}",
                            Description = leg.SalePricePerMeter.HasValue
                                ? $"سعر البيع: ${leg.SalePricePerMeter.Value:N2}/م"
                                : "تم اعتماد التكلفة والتسعير"
                        });
                    }
                }

                foreach (var evt in movementEvents.Where(e => e.FabricItemId == fabricId).Take(25))
                {
                    timeline.Add(new FabricSearchJourneyEventDto
                    {
                        OccurredAt = evt.MovementDate,
                        Category = "Inventory",
                        Title = MapMovementTitle(evt.Type),
                        Description = $"{evt.WarehouseName} • {Math.Abs(evt.QuantityMeters):N0} م • {evt.MovementNumber}"
                    });
                }

                timeline = timeline
                    .OrderByDescending(t => t.OccurredAt)
                    .Take(30)
                    .ToList();

                return new FabricSearchProfileDto
                {
                    FabricItemId = fabricId,
                    FabricCode = fabricMeta?.Code ?? fabricStock[0].FabricCode,
                    FabricName = fabricMeta?.NameAr ?? fabricStock[0].FabricName,
                    CategoryName = fabricMeta?.CategoryName ?? "—",
                    TotalRolls = fabricStock.Sum(s => s.RollCount),
                    TotalMeters = fabricStock.Sum(s => s.TotalMeters),
                    AvailableMeters = fabricStock.Sum(s => s.AvailableMeters),
                    ReservedMeters = fabricStock.Sum(s => s.ReservedMeters),
                    InventoryValue = fabricStock.Sum(s => s.InventoryValue),
                    AvgCostPerMeter = costs.Count > 0 ? costs.Average() : null,
                    AvgSalePricePerMeter = salePrices.Count > 0 ? salePrices.Average() : null,
                    MinSalePricePerMeter = salePrices.Count > 0 ? salePrices.Min() : null,
                    MaxSalePricePerMeter = salePrices.Count > 0 ? salePrices.Max() : null,
                    WarehouseCount = fabricStock.Select(s => s.WarehouseId).Distinct().Count(),
                    ContainerCount = fabricContainerIds.Count,
                    ColorCount = colors.Count,
                    Colors = colors,
                    Locations = locations,
                    ContainerJourney = containerJourney,
                    JourneyTimeline = timeline
                };
            })
            .OrderByDescending(p => p.TotalRolls)
            .ToList();
    }

    private static string MapContainerStatusLabel(int status) => ((ChinaContainerStatus)status) switch
    {
        ChinaContainerStatus.Draft => "مسودة",
        ChinaContainerStatus.InTransit => "قيد الشحن",
        ChinaContainerStatus.Arrived => "وصلت",
        ChinaContainerStatus.UnderReview => "قيد المراجعة",
        ChinaContainerStatus.LandingCostReviewed => "روجّعت التكلفة",
        ChinaContainerStatus.Approved => "معتمدة",
        ChinaContainerStatus.InWarehouse => "في المستودع",
        ChinaContainerStatus.Closed => "مغلقة",
        ChinaContainerStatus.Archived => "مؤرشفة",
        ChinaContainerStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static string MapMovementTitle(int type) => ((MovementType)type) switch
    {
        MovementType.Import => "استيراد للمخزون",
        MovementType.Purchase => "شراء",
        MovementType.Sale => "بيع",
        MovementType.SaleReturn => "مرتجع بيع",
        MovementType.Transfer => "مناقلة مخزون",
        MovementType.OpeningBalance => "رصيد افتتاحي",
        MovementType.Stocktake => "جرد",
        MovementType.Adjustment => "تسوية مخزون",
        _ => "حركة مخزون"
    };

    private static Warehouse MapWarehouse(WarehouseEntity e) =>
        Warehouse.FromPersistence(e.Id, e.BranchId, e.Code, e.NameAr, e.City,
            e.NameEn, e.Description, e.Address, e.Manager, e.CostCenterId,
            e.Notes, e.IsDefault, e.CapacityRolls, e.IsActive);
}
