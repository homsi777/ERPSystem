using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Catalog;
using ERPSystem.Application.DTOs.Identity;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Catalog;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Entities.Identity;
using ERPSystem.Domain.Entities.Purchasing;
using ERPSystem.Domain.Entities.System;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.Finance;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using ERPSystem.Infrastructure.Persistence.Models.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class WarehouseRepository(ErpDbContext context) : IWarehouseRepository
{
    public async Task<WarehouseAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var warehouse = await context.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (warehouse is null) return null;

        var locations = await context.WarehouseLocations.AsNoTracking()
            .Where(l => l.WarehouseId == id).ToListAsync(cancellationToken);
        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.WarehouseId == id).ToListAsync(cancellationToken);
        return WarehouseMapper.ToAggregate(warehouse, locations, stocks);
    }

    public async Task<IReadOnlyList<WarehouseAggregate>> GetListAsync(
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        var warehouses = await context.Warehouses.AsNoTracking()
            .Where(w => w.BranchId == branchId).OrderBy(w => w.Code).ToListAsync(cancellationToken);

        var list = new List<WarehouseAggregate>();
        foreach (var w in warehouses)
            list.Add(await GetByIdAsync(w.Id, cancellationToken) ?? throw new InvalidOperationException());
        return list;
    }

    public async Task AddAsync(WarehouseAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await context.Warehouses.AddAsync(new()
        {
            Id = aggregate.Warehouse.Id,
            BranchId = aggregate.Warehouse.BranchId,
            Code = aggregate.Warehouse.Code,
            NameAr = aggregate.Warehouse.NameAr,
            City = aggregate.Warehouse.City,
            CapacityRolls = aggregate.Warehouse.CapacityRolls,
            IsActive = aggregate.Warehouse.IsActive
        }, cancellationToken);
        await cache.RemoveByPrefixAsync(FabricCatalogCachePrefix, cancellationToken);
    }

    public Task UpdateAsync(WarehouseAggregate aggregate, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FabricCatalogRepository(ErpDbContext context, ICacheService cache) : IFabricCatalogRepository
{
    private const string FabricCatalogCachePrefix = "lookup:fabric-catalog:";

    public async Task<FabricItem?> GetItemByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.FabricItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return entity is null ? null : ToFabricItem(entity);
    }

    public async Task<FabricColor?> GetColorByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.FabricColors.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : ToFabricColor(entity);
    }

    public async Task<IReadOnlyList<FabricItem>> GetItemsAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{FabricCatalogCachePrefix}items:{companyId}:all";
        if (string.IsNullOrWhiteSpace(search))
        {
            var cached = await cache.GetAsync<IReadOnlyList<FabricItem>>(cacheKey, cancellationToken);
            if (cached is not null)
                return cached;
        }

        var query = context.FabricItems.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Code.Contains(term) || i.NameAr.Contains(term));
        }

        var entities = await query.OrderBy(i => i.Code).ToListAsync(cancellationToken);
        var items = entities.Select(ToFabricItem).ToList();
        if (string.IsNullOrWhiteSpace(search))
            await cache.SetAsync(cacheKey, items, TimeSpan.FromMinutes(30), cancellationToken);
        return items;
    }

    public async Task<FabricItem?> GetItemByCodeAsync(
        Guid companyId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized = code.Trim();
        var entity = await context.FabricItems.AsNoTracking()
            .FirstOrDefaultAsync(
                i => i.CompanyId == companyId &&
                     i.Code.ToLower() == normalized.ToLower() &&
                     i.IsActive,
                cancellationToken);
        return entity is null ? null : ToFabricItem(entity);
    }

    public async Task<FabricColor?> GetColorForItemAsync(
        Guid fabricItemId,
        string colorCodeOrName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(colorCodeOrName))
            return null;

        var normalized = PackingListCatalogNormalizer.CollapseWhitespace(colorCodeOrName);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => c.FabricItemId == fabricItemId && c.IsActive)
            .ToListAsync(cancellationToken);

        var entity = colors.FirstOrDefault(c =>
            PackingListCatalogNormalizer.CollapseWhitespace(c.Code)
                .Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            PackingListCatalogNormalizer.CollapseWhitespace(c.NameAr)
                .Equals(normalized, StringComparison.OrdinalIgnoreCase) ||
            (c.NameEn != null && PackingListCatalogNormalizer.CollapseWhitespace(c.NameEn)
                .Equals(normalized, StringComparison.OrdinalIgnoreCase)));

        return entity is null ? null : ToFabricColor(entity);
    }

    public async Task<IReadOnlyList<FabricColor>> GetColorsForItemAsync(
        Guid fabricItemId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{FabricCatalogCachePrefix}colors:{fabricItemId}";
        var cached = await cache.GetAsync<IReadOnlyList<FabricColor>>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        var entities = await context.FabricColors.AsNoTracking()
            .Where(c => c.FabricItemId == fabricItemId && c.IsActive)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
        var colors = entities.Select(ToFabricColor).ToList();
        await cache.SetAsync(cacheKey, colors, TimeSpan.FromMinutes(30), cancellationToken);
        return colors;
    }

    public async Task<FabricCategory?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.FabricCategories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : ToFabricCategory(entity);
    }

    public async Task<IReadOnlyList<FabricCategoryListDto>> GetCategoryListAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        var categories = await context.FabricCategories.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .OrderBy(c => c.Code)
            .ToListAsync(cancellationToken);
        var itemCounts = await context.FabricItems.AsNoTracking()
            .Where(i => i.CompanyId == companyId)
            .GroupBy(i => i.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.CategoryId, x => x.Count, cancellationToken);

        return categories.Select(c => new FabricCategoryListDto
        {
            Id = c.Id,
            Code = c.Code,
            NameAr = c.NameAr,
            NameEn = c.NameEn,
            ItemCount = itemCounts.GetValueOrDefault(c.Id),
            IsActive = c.IsActive
        }).ToList();
    }

    public async Task<IReadOnlyList<FabricItemListDto>> GetItemListAsync(
        Guid companyId, Guid? categoryId = null, string? search = null, CancellationToken cancellationToken = default)
    {
        var query = context.FabricItems.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (categoryId.HasValue)
            query = query.Where(i => i.CategoryId == categoryId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Code.Contains(term) || i.NameAr.Contains(term));
        }

        var items = await query.OrderBy(i => i.Code).ToListAsync(cancellationToken);
        var categoryMap = await context.FabricCategories.AsNoTracking()
            .Where(c => c.CompanyId == companyId)
            .ToDictionaryAsync(c => c.Id, cancellationToken);
        var colorCounts = await context.FabricColors.AsNoTracking()
            .Where(c => items.Select(i => i.Id).Contains(c.FabricItemId))
            .GroupBy(c => c.FabricItemId)
            .Select(g => new { ItemId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ItemId, x => x.Count, cancellationToken);

        return items.Select(i => new FabricItemListDto
        {
            Id = i.Id,
            CategoryId = i.CategoryId,
            CategoryName = categoryMap.GetValueOrDefault(i.CategoryId)?.NameAr ?? "—",
            Code = i.Code,
            NameAr = i.NameAr,
            NameEn = i.NameEn,
            ColorCount = colorCounts.GetValueOrDefault(i.Id),
            IsActive = i.IsActive
        }).ToList();
    }

    public async Task<IReadOnlyList<FabricColorListDto>> GetColorListAsync(
        Guid fabricItemId, CancellationToken cancellationToken = default) =>
        await context.FabricColors.AsNoTracking()
            .Where(c => c.FabricItemId == fabricItemId)
            .OrderBy(c => c.Code)
            .Select(c => new FabricColorListDto
            {
                Id = c.Id,
                FabricItemId = c.FabricItemId,
                Code = c.Code,
                NameAr = c.NameAr,
                NameEn = c.NameEn,
                IsActive = c.IsActive
            }).ToListAsync(cancellationToken);

    public async Task<bool> CategoryCodeExistsAsync(
        Guid companyId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await context.FabricCategories.AsNoTracking()
            .AnyAsync(c => c.CompanyId == companyId && c.Code == code &&
                           (!excludeId.HasValue || c.Id != excludeId), cancellationToken);

    public async Task<bool> ItemCodeExistsAsync(
        Guid companyId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        await context.FabricItems.AsNoTracking()
            .AnyAsync(i => i.CompanyId == companyId && i.Code == code &&
                           (!excludeId.HasValue || i.Id != excludeId), cancellationToken);

    public async Task AddCategoryAsync(FabricCategory category, Guid companyId, CancellationToken cancellationToken = default)
    {
        await context.FabricCategories.AddAsync(new Persistence.Models.Catalog.FabricCategoryEntity
        {
            Id = category.Id,
            CompanyId = companyId,
            Code = category.Code,
            NameAr = category.NameAr,
            NameEn = category.NameEn,
            IsActive = category.IsActive,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task UpdateCategoryAsync(FabricCategory category, CancellationToken cancellationToken = default)
    {
        var entity = await context.FabricCategories.FirstOrDefaultAsync(c => c.Id == category.Id, cancellationToken)
            ?? throw new InvalidOperationException("Category not found.");
        entity.NameAr = category.NameAr;
        entity.NameEn = category.NameEn;
        entity.IsActive = category.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await cache.RemoveByPrefixAsync(FabricCatalogCachePrefix, cancellationToken);
    }

    public async Task AddItemAsync(FabricItem item, Guid companyId, CancellationToken cancellationToken = default)
    {
        await context.FabricItems.AddAsync(new Persistence.Models.Catalog.FabricItemEntity
        {
            Id = item.Id,
            CompanyId = companyId,
            CategoryId = item.CategoryId,
            Code = item.Code,
            NameAr = item.NameAr,
            NameEn = item.NameEn,
            DefaultUnit = item.DefaultUnit,
            IsActive = item.IsActive,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await cache.RemoveByPrefixAsync(FabricCatalogCachePrefix, cancellationToken);
    }

    public async Task UpdateItemAsync(FabricItem item, CancellationToken cancellationToken = default)
    {
        var entity = await context.FabricItems.FirstOrDefaultAsync(i => i.Id == item.Id, cancellationToken)
            ?? throw new InvalidOperationException("Fabric item not found.");
        entity.CategoryId = item.CategoryId;
        entity.NameAr = item.NameAr;
        entity.NameEn = item.NameEn;
        entity.DefaultUnit = item.DefaultUnit;
        entity.IsActive = item.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await cache.RemoveByPrefixAsync(FabricCatalogCachePrefix, cancellationToken);
    }

    public async Task AddColorAsync(FabricColor color, CancellationToken cancellationToken = default)
    {
        await context.FabricColors.AddAsync(new Persistence.Models.Catalog.FabricColorEntity
        {
            Id = color.Id,
            FabricItemId = color.FabricItemId,
            Code = color.ColorCode,
            NameAr = color.NameAr,
            NameEn = color.NameEn,
            IsActive = color.IsActive,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
        await cache.RemoveByPrefixAsync(FabricCatalogCachePrefix, cancellationToken);
    }

    public async Task UpdateColorAsync(FabricColor color, CancellationToken cancellationToken = default)
    {
        var entity = await context.FabricColors.FirstOrDefaultAsync(c => c.Id == color.Id, cancellationToken)
            ?? throw new InvalidOperationException("Color not found.");
        entity.NameAr = color.NameAr;
        entity.NameEn = color.NameEn;
        entity.IsActive = color.IsActive;
        entity.UpdatedAt = DateTime.UtcNow;
        await cache.RemoveByPrefixAsync(FabricCatalogCachePrefix, cancellationToken);
    }

    public async Task<IReadOnlyList<ImportedFabricClassificationDto>> GetImportedClassificationsAsync(
        Guid companyId, Guid? containerId = null, CancellationToken cancellationToken = default)
    {
        var containerQuery = context.Containers.AsNoTracking()
            .Where(c => c.CompanyId == companyId);
        if (containerId.HasValue)
            containerQuery = containerQuery.Where(c => c.Id == containerId.Value);

        var containers = await containerQuery
            .Select(c => new { c.Id, c.ContainerNumber })
            .ToListAsync(cancellationToken);
        if (containers.Count == 0)
            return [];

        var containerIds = containers.Select(c => c.Id).ToList();
        var containerMap = containers.ToDictionary(c => c.Id, c => c.ContainerNumber);

        var itemGroups = await context.ContainerItems.AsNoTracking()
            .Where(i => containerIds.Contains(i.ContainerId))
            .GroupBy(i => new { i.ContainerId, i.FabricItemId, i.FabricColorId })
            .Select(g => new
            {
                g.Key.ContainerId,
                g.Key.FabricItemId,
                g.Key.FabricColorId,
                RollCount = g.Sum(x => x.RollCount),
                LengthMeters = g.Sum(x => x.LengthMeters)
            })
            .ToListAsync(cancellationToken);

        if (itemGroups.Count == 0)
            return [];

        var fabricIds = itemGroups.Select(g => g.FabricItemId).Distinct().ToList();
        var colorIds = itemGroups.Select(g => g.FabricColorId).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, cancellationToken);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var typeLines = await context.ContainerFabricTypeLines.AsNoTracking()
            .Where(t => containerIds.Contains(t.ContainerId) && t.FabricItemId != null && t.FabricColorId != null)
            .Select(t => new { t.ContainerId, t.FabricItemId, t.FabricColorId, t.TypeDisplayName })
            .ToListAsync(cancellationToken);

        var typeMap = typeLines.ToDictionary(
            t => (t.ContainerId, t.FabricItemId!.Value, t.FabricColorId!.Value),
            t => t.TypeDisplayName);

        return itemGroups
            .Where(g => fabrics.ContainsKey(g.FabricItemId) && colors.ContainsKey(g.FabricColorId))
            .Select(g =>
            {
                var fabric = fabrics[g.FabricItemId];
                var color = colors[g.FabricColorId];
                typeMap.TryGetValue((g.ContainerId, g.FabricItemId, g.FabricColorId), out var typeName);
                return new ImportedFabricClassificationDto
                {
                    ContainerId = g.ContainerId,
                    ContainerNumber = containerMap.GetValueOrDefault(g.ContainerId, "—"),
                    FabricItemId = g.FabricItemId,
                    CategoryId = fabric.CategoryId,
                    FabricCode = fabric.Code,
                    NameAr = fabric.NameAr,
                    NameEn = fabric.NameEn,
                    FabricColorId = g.FabricColorId,
                    ColorCode = color.Code,
                    ColorNameAr = color.NameAr,
                    TypeDisplayName = typeName,
                    RollCount = g.RollCount,
                    LengthMeters = g.LengthMeters
                };
            })
            .OrderBy(r => r.ContainerNumber)
            .ThenBy(r => r.FabricCode)
            .ThenBy(r => r.ColorCode)
            .ToList();
    }

    public async Task<IReadOnlyList<ImportedFabricContainerFilterDto>> GetImportedFabricContainerFiltersAsync(
        Guid companyId, CancellationToken cancellationToken = default)
    {
        var distinctPairs = await (
            from c in context.Containers.AsNoTracking()
            where c.CompanyId == companyId
            join i in context.ContainerItems.AsNoTracking() on c.Id equals i.ContainerId
            select new { c.Id, c.ContainerNumber, i.FabricItemId, i.FabricColorId })
            .Distinct()
            .ToListAsync(cancellationToken);

        return distinctPairs
            .GroupBy(x => new { x.Id, x.ContainerNumber })
            .Select(g => new ImportedFabricContainerFilterDto
            {
                Id = g.Key.Id,
                ContainerNumber = g.Key.ContainerNumber,
                FabricTypeCount = g.Count()
            })
            .OrderByDescending(x => x.ContainerNumber)
            .ToList();
    }

    public async Task<IReadOnlyList<FabricCategory>> GetCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.FabricCategories.AsNoTracking()
            .Where(c => c.CompanyId == companyId).OrderBy(c => c.Code).ToListAsync(cancellationToken);
        return entities.Select(e => ToFabricCategory(e)).ToList();
    }

    private static FabricCategory ToFabricCategory(Persistence.Models.Catalog.FabricCategoryEntity entity)
    {
        var cat = DomainHydrator.Create<FabricCategory>();
        DomainHydrator.Set(cat, nameof(FabricCategory.Id), entity.Id);
        DomainHydrator.Set(cat, nameof(FabricCategory.Code), entity.Code);
        DomainHydrator.Set(cat, nameof(FabricCategory.NameAr), entity.NameAr);
        DomainHydrator.Set(cat, nameof(FabricCategory.NameEn), entity.NameEn);
        DomainHydrator.Set(cat, nameof(FabricCategory.IsActive), entity.IsActive);
        return cat;
    }

    private static FabricItem ToFabricItem(Persistence.Models.Catalog.FabricItemEntity entity)
    {
        var item = DomainHydrator.Create<FabricItem>();
        DomainHydrator.Set(item, nameof(FabricItem.Id), entity.Id);
        DomainHydrator.Set(item, nameof(FabricItem.CategoryId), entity.CategoryId);
        DomainHydrator.Set(item, nameof(FabricItem.Code), entity.Code);
        DomainHydrator.Set(item, nameof(FabricItem.NameAr), entity.NameAr);
        DomainHydrator.Set(item, nameof(FabricItem.NameEn), entity.NameEn);
        DomainHydrator.Set(item, nameof(FabricItem.DefaultUnit), entity.DefaultUnit);
        DomainHydrator.Set(item, nameof(FabricItem.IsActive), entity.IsActive);
        return item;
    }

    private static FabricColor ToFabricColor(Persistence.Models.Catalog.FabricColorEntity entity)
    {
        var color = DomainHydrator.Create<FabricColor>();
        DomainHydrator.Set(color, nameof(FabricColor.Id), entity.Id);
        DomainHydrator.Set(color, nameof(FabricColor.FabricItemId), entity.FabricItemId);
        DomainHydrator.Set(color, nameof(FabricColor.ColorCode), entity.Code);
        DomainHydrator.Set(color, nameof(FabricColor.NameAr), entity.NameAr);
        DomainHydrator.Set(color, nameof(FabricColor.NameEn), entity.NameEn);
        DomainHydrator.Set(color, nameof(FabricColor.IsActive), entity.IsActive);
        return color;
    }
}

internal sealed class ReceiptVoucherRepository(ErpDbContext context) : IReceiptVoucherRepository
{
    public async Task<ReceiptVoucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.ReceiptVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return entity is null ? null : FinanceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<ReceiptVoucher>> GetListAsync(
        Guid companyId,
        VoucherStatus? status = null,
        Guid? customerId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.ReceiptVouchers.AsNoTracking().Where(v => v.CompanyId == companyId);
        if (status.HasValue) query = query.Where(v => v.Status == (int)status.Value);
        if (customerId.HasValue) query = query.Where(v => v.CustomerId == customerId.Value);
        var entities = await query.OrderByDescending(v => v.VoucherDate).ToListAsync(cancellationToken);
        return entities.Select(FinanceMapper.ToDomain).ToList();
    }

    public async Task AddAsync(ReceiptVoucher voucher, CancellationToken cancellationToken = default) =>
        await context.ReceiptVouchers.AddAsync(new ReceiptVoucherEntity
        {
            Id = voucher.Id,
            VoucherNumber = voucher.VoucherNumber,
            CustomerId = voucher.CustomerId,
            CashboxId = voucher.CashboxId,
            Amount = voucher.Amount.Amount,
            VoucherDate = voucher.VoucherDate,
            Status = (int)voucher.Status,
            PostedAt = voucher.PostedAt,
            CancelledAt = voucher.CancelledAt,
            CancelReason = voucher.CancelReason
        }, cancellationToken);

    public async Task UpdateAsync(ReceiptVoucher voucher, CancellationToken cancellationToken = default)
    {
        var entity = await context.ReceiptVouchers.FirstOrDefaultAsync(v => v.Id == voucher.Id, cancellationToken)
            ?? throw new InvalidOperationException("Receipt voucher not found.");
        entity.Status = (int)voucher.Status;
        entity.PostedAt = voucher.PostedAt;
        entity.CancelledAt = voucher.CancelledAt;
        entity.CancelReason = voucher.CancelReason;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}

internal sealed class PaymentVoucherRepository(ErpDbContext context) : IPaymentVoucherRepository
{
    public async Task<PaymentVoucher?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.PaymentVouchers.AsNoTracking().FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
        return entity is null ? null : FinanceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<PaymentVoucher>> GetListAsync(
        Guid companyId,
        VoucherStatus? status = null,
        Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.PaymentVouchers.AsNoTracking().Where(v => v.CompanyId == companyId);
        if (status.HasValue) query = query.Where(v => v.Status == (int)status.Value);
        if (supplierId.HasValue) query = query.Where(v => v.SupplierId == supplierId.Value);
        var entities = await query.OrderByDescending(v => v.VoucherDate).ToListAsync(cancellationToken);
        return entities.Select(FinanceMapper.ToDomain).ToList();
    }

    public async Task AddAsync(PaymentVoucher voucher, CancellationToken cancellationToken = default) =>
        await context.PaymentVouchers.AddAsync(new PaymentVoucherEntity
        {
            Id = voucher.Id,
            VoucherNumber = voucher.VoucherNumber,
            SupplierId = voucher.SupplierId,
            CashboxId = voucher.CashboxId,
            Amount = voucher.Amount.Amount,
            VoucherDate = voucher.VoucherDate,
            Status = (int)voucher.Status
        }, cancellationToken);

    public async Task UpdateAsync(PaymentVoucher voucher, CancellationToken cancellationToken = default)
    {
        var entity = await context.PaymentVouchers.FirstOrDefaultAsync(v => v.Id == voucher.Id, cancellationToken)
            ?? throw new InvalidOperationException("Payment voucher not found.");
        entity.Status = (int)voucher.Status;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}

internal sealed class CashboxRepository(ErpDbContext context) : ICashboxRepository
{
    public async Task<Cashbox?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Cashboxes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : FinanceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Cashbox>> GetListAsync(Guid branchId, CancellationToken cancellationToken = default)
    {
        var entities = await context.Cashboxes.AsNoTracking()
            .Where(c => c.BranchId == branchId).ToListAsync(cancellationToken);
        return entities.Select(FinanceMapper.ToDomain).ToList();
    }

    public async Task<bool> ExistsByCodeAsync(
        Guid branchId, string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = context.Cashboxes.AsNoTracking()
            .Where(c => c.BranchId == branchId && c.Code == code);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<(decimal Receipts, decimal Payments)> GetTodayTotalsAsync(
        Guid cashboxId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);
        var posted = (int)VoucherStatus.Posted;

        var receipts = await context.ReceiptVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == posted &&
                        v.PostedAt >= today && v.PostedAt < tomorrow)
            .SumAsync(v => v.Amount, cancellationToken);

        var payments = await context.PaymentVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == posted &&
                        v.PostedAt >= today && v.PostedAt < tomorrow)
            .SumAsync(v => v.Amount, cancellationToken);

        return (receipts, payments);
    }

    public async Task<IReadOnlyList<(DateTime Date, string Type, string Number, string Description, decimal Amount, bool IsInbound)>> GetMovementsAsync(
        Guid cashboxId,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var posted = (int)VoucherStatus.Posted;
        var from = fromDate?.Date ?? DateTime.MinValue;
        var to = toDate?.Date.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
        var rows = new List<(DateTime, string, string, string, decimal, bool)>();

        var receipts = await context.ReceiptVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == posted &&
                        v.PostedAt >= from && v.PostedAt <= to)
            .OrderByDescending(v => v.PostedAt)
            .Select(v => new { v.PostedAt, v.VoucherNumber, v.Amount })
            .ToListAsync(cancellationToken);
        rows.AddRange(receipts.Select(r =>
            (r.PostedAt ?? DateTime.UtcNow, "سند قبض", r.VoucherNumber, "تحصيل من عميل", r.Amount, true)));

        var payments = await context.PaymentVouchers.AsNoTracking()
            .Where(v => v.CashboxId == cashboxId && v.Status == posted &&
                        v.PostedAt >= from && v.PostedAt <= to)
            .OrderByDescending(v => v.PostedAt)
            .Select(v => new { v.PostedAt, v.VoucherNumber, v.Amount })
            .ToListAsync(cancellationToken);
        rows.AddRange(payments.Select(p =>
            (p.PostedAt ?? DateTime.UtcNow, "سند صرف", p.VoucherNumber, "صرف لمورد", p.Amount, false)));

        var transfersOut = await context.CashboxTransfers.AsNoTracking()
            .Where(t => t.FromCashboxId == cashboxId && t.Status == posted &&
                        t.TransferDate >= from && t.TransferDate <= to)
            .OrderByDescending(t => t.TransferDate)
            .Select(t => new { t.TransferDate, t.TransferNumber, t.Amount })
            .ToListAsync(cancellationToken);
        rows.AddRange(transfersOut.Select(t =>
            (t.TransferDate, "تحويل صادر", t.TransferNumber, "تحويل إلى صندوق آخر", t.Amount, false)));

        var transfersIn = await context.CashboxTransfers.AsNoTracking()
            .Where(t => t.ToCashboxId == cashboxId && t.Status == posted &&
                        t.TransferDate >= from && t.TransferDate <= to)
            .OrderByDescending(t => t.TransferDate)
            .Select(t => new { t.TransferDate, t.TransferNumber, t.Amount })
            .ToListAsync(cancellationToken);
        rows.AddRange(transfersIn.Select(t =>
            (t.TransferDate, "تحويل وارد", t.TransferNumber, "تحويل من صندوق آخر", t.Amount, true)));

        return rows.OrderByDescending(r => r.Item1).ToList();
    }

    public async Task AddAsync(Cashbox cashbox, CancellationToken cancellationToken = default) =>
        await context.Cashboxes.AddAsync(new CashboxEntity
        {
            Id = cashbox.Id,
            BranchId = cashbox.BranchId,
            Code = cashbox.Code,
            Name = cashbox.Name,
            Balance = cashbox.Balance.Amount,
            Currency = cashbox.Currency,
            IsActive = cashbox.IsActive,
            AccountId = cashbox.AccountId
        }, cancellationToken);

    public async Task UpdateAsync(Cashbox cashbox, CancellationToken cancellationToken = default)
    {
        var entity = await context.Cashboxes.FirstOrDefaultAsync(c => c.Id == cashbox.Id, cancellationToken)
            ?? throw new InvalidOperationException("Cashbox not found.");
        entity.Code = cashbox.Code;
        entity.Name = cashbox.Name;
        entity.Currency = cashbox.Currency;
        entity.Balance = cashbox.Balance.Amount;
        entity.IsActive = cashbox.IsActive;
        entity.AccountId = cashbox.AccountId;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}

internal sealed class CashboxTransferRepository(ErpDbContext context) : ICashboxTransferRepository
{
    public async Task<CashboxTransfer?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.CashboxTransfers.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return entity is null ? null : FinanceMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<CashboxTransfer>> GetListAsync(
        Guid branchId,
        VoucherStatus? status = null,
        Guid? cashboxId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.CashboxTransfers.AsNoTracking().Where(t => t.BranchId == branchId);
        if (status.HasValue)
            query = query.Where(t => t.Status == (int)status.Value);
        if (cashboxId.HasValue)
        {
            var id = cashboxId.Value;
            query = query.Where(t => t.FromCashboxId == id || t.ToCashboxId == id);
        }

        var entities = await query.OrderByDescending(t => t.TransferDate).ToListAsync(cancellationToken);
        return entities.Select(FinanceMapper.ToDomain).ToList();
    }

    public async Task AddAsync(
        CashboxTransfer transfer,
        Guid companyId,
        Guid branchId,
        CancellationToken cancellationToken = default) =>
        await context.CashboxTransfers.AddAsync(
            FinanceMapper.ToEntity(transfer, companyId, branchId), cancellationToken);

    public async Task UpdateAsync(CashboxTransfer transfer, CancellationToken cancellationToken = default)
    {
        var entity = await context.CashboxTransfers.FirstOrDefaultAsync(t => t.Id == transfer.Id, cancellationToken)
            ?? throw new InvalidOperationException("Cashbox transfer not found.");
        entity.Status = (int)transfer.Status;
        entity.PostedAt = transfer.Status == VoucherStatus.Posted ? DateTime.UtcNow : entity.PostedAt;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}

internal sealed class JournalEntryRepository(ErpDbContext context) : IJournalEntryRepository
{
    public async Task<AccountingAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await LoadAsync(id, null, cancellationToken);

    public async Task<AccountingAggregate?> GetByNumberAsync(string entryNumber, CancellationToken cancellationToken = default) =>
        await LoadAsync(null, entryNumber, cancellationToken);

    public async Task<IReadOnlyList<AccountingAggregate>> GetListAsync(
        Guid companyId,
        JournalEntryStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.JournalEntries.AsNoTracking().Where(j => j.CompanyId == companyId);
        if (status.HasValue) query = query.Where(j => j.Status == (int)status.Value);
        var headers = await query.OrderByDescending(j => j.EntryDate).ToListAsync(cancellationToken);
        var list = new List<AccountingAggregate>();
        foreach (var header in headers)
            list.Add(await LoadFromHeaderAsync(header, cancellationToken));
        return list;
    }

    public async Task AddAsync(AccountingAggregate entry, Guid companyId, Guid branchId, CancellationToken cancellationToken = default)
    {
        await context.JournalEntries.AddAsync(new JournalEntryEntity
        {
            Id = entry.Id,
            CompanyId = companyId,
            BranchId = branchId,
            EntryNumber = entry.EntryNumber,
            EntryDate = UtcDateTimeNormalizer.ToUtc(entry.EntryDate),
            Description = entry.Description,
            Status = (int)entry.Status,
            SourceType = entry.SourceType.HasValue ? (int)entry.SourceType.Value : null,
            SourceId = entry.SourceId,
            CreatedByUserId = entry.CreatedByUserId,
            PostedAt = entry.PostedAt.HasValue ? UtcDateTimeNormalizer.ToUtc(entry.PostedAt.Value) : null,
            PostedByUserId = entry.PostedByUserId,
            ReversalOfEntryId = entry.ReversalOfEntryId,
            CancelledAt = entry.CancelledAt.HasValue ? UtcDateTimeNormalizer.ToUtc(entry.CancelledAt.Value) : null,
            JournalBookId = entry.JournalBookId
        }, cancellationToken);

        await context.JournalEntryLines.AddRangeAsync(entry.Lines.Select(l => new JournalEntryLineEntity
        {
            Id = l.Id,
            JournalEntryId = entry.Id,
            AccountId = l.AccountId,
            Debit = l.Debit.Amount,
            Credit = l.Credit.Amount,
            Narrative = l.Narrative,
            PartyId = l.PartyId
        }), cancellationToken);
    }

    public async Task<(IReadOnlyList<JournalEntryListRow> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        JournalEntryListFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.JournalEntries.AsNoTracking().Where(j => j.CompanyId == companyId);

        if (filter.Status.HasValue)
            query = query.Where(j => j.Status == (int)filter.Status.Value);
        if (filter.FromDate.HasValue)
            query = query.Where(j => j.EntryDate >= UtcDateTimeNormalizer.ToUtc(filter.FromDate.Value));
        if (filter.ToDate.HasValue)
        {
            var endUtc = UtcDateTimeNormalizer.ToUtc(filter.ToDate.Value.Date.AddDays(1).AddTicks(-1));
            query = query.Where(j => j.EntryDate <= endUtc);
        }
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(j =>
                j.EntryNumber.Contains(term) ||
                j.Description.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var headers = await query
            .OrderByDescending(j => j.EntryDate)
            .ThenByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        if (headers.Count == 0)
            return ([], total);

        var ids = headers.Select(h => h.Id).ToList();
        var lineStats = await context.JournalEntryLines.AsNoTracking()
            .Where(l => ids.Contains(l.JournalEntryId))
            .GroupBy(l => l.JournalEntryId)
            .Select(g => new
            {
                JournalEntryId = g.Key,
                DebitTotal = g.Sum(x => x.Debit),
                CreditTotal = g.Sum(x => x.Credit),
                LineCount = g.Count()
            })
            .ToDictionaryAsync(x => x.JournalEntryId, cancellationToken);

        var rows = headers.Select(h =>
        {
            lineStats.TryGetValue(h.Id, out var stats);
            return new JournalEntryListRow
            {
                Id = h.Id,
                EntryNumber = h.EntryNumber,
                EntryDate = h.EntryDate,
                Description = h.Description,
                Status = (JournalEntryStatus)h.Status,
                DebitTotal = stats?.DebitTotal ?? 0m,
                CreditTotal = stats?.CreditTotal ?? 0m,
                LineCount = stats?.LineCount ?? 0,
                SourceType = h.SourceType.HasValue ? (DocumentType)h.SourceType.Value : null
            };
        }).ToList();

        return (rows, total);
    }

    public async Task<IReadOnlyList<JournalEntryListRow>> GetBySourceIdAsync(
        Guid sourceId,
        CancellationToken cancellationToken = default)
    {
        var headers = await context.JournalEntries.AsNoTracking()
            .Where(j => j.SourceId == sourceId)
            .OrderByDescending(j => j.EntryDate)
            .ToListAsync(cancellationToken);
        if (headers.Count == 0)
            return [];

        var ids = headers.Select(h => h.Id).ToList();
        var lineStats = await context.JournalEntryLines.AsNoTracking()
            .Where(l => ids.Contains(l.JournalEntryId))
            .GroupBy(l => l.JournalEntryId)
            .Select(g => new
            {
                JournalEntryId = g.Key,
                DebitTotal = g.Sum(x => x.Debit),
                CreditTotal = g.Sum(x => x.Credit),
                LineCount = g.Count()
            })
            .ToDictionaryAsync(x => x.JournalEntryId, cancellationToken);

        return headers.Select(h =>
        {
            lineStats.TryGetValue(h.Id, out var stats);
            return new JournalEntryListRow
            {
                Id = h.Id,
                EntryNumber = h.EntryNumber,
                EntryDate = h.EntryDate,
                Description = h.Description,
                Status = (JournalEntryStatus)h.Status,
                DebitTotal = stats?.DebitTotal ?? 0m,
                CreditTotal = stats?.CreditTotal ?? 0m,
                LineCount = stats?.LineCount ?? 0,
                SourceType = h.SourceType.HasValue ? (DocumentType)h.SourceType.Value : null
            };
        }).ToList();
    }

    public async Task UpdateAsync(AccountingAggregate entry, CancellationToken cancellationToken = default)
    {
        var header = await context.JournalEntries.FirstOrDefaultAsync(j => j.Id == entry.Id, cancellationToken)
            ?? throw new InvalidOperationException("Journal entry not found.");
        header.Status = (int)entry.Status;
        header.PostedAt = entry.PostedAt;
        header.PostedByUserId = entry.PostedByUserId;
        header.CancelledAt = entry.CancelledAt;
        header.UpdatedAt = DateTime.UtcNow;
    }

    private async Task<AccountingAggregate?> LoadAsync(Guid? id, string? number, CancellationToken ct)
    {
        var header = id.HasValue
            ? await context.JournalEntries.AsNoTracking().FirstOrDefaultAsync(j => j.Id == id.Value, ct)
            : await context.JournalEntries.AsNoTracking().FirstOrDefaultAsync(j => j.EntryNumber == number, ct);
        return header is null ? null : await LoadFromHeaderAsync(header, ct);
    }

    private async Task<AccountingAggregate> LoadFromHeaderAsync(JournalEntryEntity header, CancellationToken ct)
    {
        var lines = await context.JournalEntryLines.AsNoTracking()
            .Where(l => l.JournalEntryId == header.Id).ToListAsync(ct);
        return AccountingMapper.ToAggregate(header, lines);
    }
}

internal sealed class AuditLogRepository(ErpDbContext context) : IAuditLogRepository
{
    public async Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default) =>
        await context.AuditLogs.AddAsync(new Persistence.Models.Audit.AuditLogEntity
        {
            Id = auditLog.Id,
            OccurredAt = auditLog.OccurredAt,
            UserId = auditLog.UserId,
            Action = auditLog.Action,
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId,
            OldValuesJson = auditLog.OldValuesJson,
            NewValuesJson = auditLog.NewValuesJson,
            BranchId = auditLog.BranchId
        }, cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.AuditLogs.AsNoTracking()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .OrderByDescending(a => a.OccurredAt)
            .ToListAsync(cancellationToken);

        return entities.Select(e => AuditLog.Record(
            e.UserId, e.Action, e.EntityType, e.EntityId, e.OldValuesJson, e.NewValuesJson, e.BranchId)).ToList();
    }

    public async Task<IReadOnlyList<AuditActivityItem>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await context.AuditLogs.AsNoTracking()
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
            .Select(a => new AuditActivityItem(a.OccurredAt, a.UserId, a.Action, a.EntityType, a.EntityId))
            .ToListAsync(cancellationToken);
    }
}

internal sealed class UserRepository(ErpDbContext context) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
        return entity is null ? null : ToUser(entity);
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var entity = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        return entity is null ? null : ToUser(entity);
    }

    public async Task<UserCredentialDto?> GetCredentialByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var entity = await context.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (entity is null)
            return null;

        return new UserCredentialDto
        {
            UserId = entity.Id,
            Username = entity.Username,
            FullNameAr = entity.FullNameAr,
            PasswordHash = entity.PasswordHash,
            IsActive = entity.IsActive
        };
    }

    public async Task<IReadOnlyList<Role>> GetRolesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        var roles = await context.Roles.AsNoTracking()
            .Where(r => roleIds.Contains(r.Id)).ToListAsync(cancellationToken);
        return roles.Select(r =>
        {
            var role = Role.Create(r.Name, r.Description, r.IsSystem);
            DomainHydrator.Set(role, nameof(Role.Id), r.Id);
            return role;
        }).ToList();
    }

    public async Task<IReadOnlyList<string>> GetPermissionCodesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var roleIds = await context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        var permissionIds = await context.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId)).Select(rp => rp.PermissionId).ToListAsync(cancellationToken);
        return await context.Permissions.AsNoTracking()
            .Where(p => permissionIds.Contains(p.Id))
            .Select(p => p.Code)
            .Distinct()
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasPermissionAsync(Guid userId, string permissionCode, CancellationToken cancellationToken = default)
    {
        var roleIds = await context.UserRoles.AsNoTracking()
            .Where(ur => ur.UserId == userId).Select(ur => ur.RoleId).ToListAsync(cancellationToken);
        var permissionIds = await context.RolePermissions.AsNoTracking()
            .Where(rp => roleIds.Contains(rp.RoleId)).Select(rp => rp.PermissionId).ToListAsync(cancellationToken);
        return await context.Permissions.AsNoTracking()
            .AnyAsync(p => permissionIds.Contains(p.Id) && p.Code == permissionCode, cancellationToken);
    }

    private static User ToUser(UserEntity entity)
    {
        var user = DomainHydrator.Create<User>();
        DomainHydrator.Set(user, nameof(User.Id), entity.Id);
        DomainHydrator.Set(user, nameof(User.Username), entity.Username);
        DomainHydrator.Set(user, nameof(User.FullNameAr), entity.FullNameAr);
        DomainHydrator.Set(user, nameof(User.FullNameEn), entity.FullNameEn);
        DomainHydrator.Set(user, nameof(User.IsActive), entity.IsActive);
        return user;
    }
}
