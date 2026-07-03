using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
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
    }

    public Task UpdateAsync(WarehouseAggregate aggregate, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}

internal sealed class FabricCatalogRepository(ErpDbContext context) : IFabricCatalogRepository
{
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
        var query = context.FabricItems.AsNoTracking().Where(i => i.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i => i.Code.Contains(term) || i.NameAr.Contains(term));
        }

        var entities = await query.OrderBy(i => i.Code).ToListAsync(cancellationToken);
        return entities.Select(ToFabricItem).ToList();
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

    public async Task<IReadOnlyList<FabricCategory>> GetCategoriesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.FabricCategories.AsNoTracking()
            .Where(c => c.CompanyId == companyId).OrderBy(c => c.Code).ToListAsync(cancellationToken);
        return entities.Select(e =>
        {
            var cat = DomainHydrator.Create<FabricCategory>();
            DomainHydrator.Set(cat, nameof(FabricCategory.Id), e.Id);
            DomainHydrator.Set(cat, nameof(FabricCategory.Code), e.Code);
            DomainHydrator.Set(cat, nameof(FabricCategory.NameAr), e.NameAr);
            return cat;
        }).ToList();
    }

    private static FabricItem ToFabricItem(Persistence.Models.Catalog.FabricItemEntity entity)
    {
        var item = DomainHydrator.Create<FabricItem>();
        DomainHydrator.Set(item, nameof(FabricItem.Id), entity.Id);
        DomainHydrator.Set(item, nameof(FabricItem.Code), entity.Code);
        DomainHydrator.Set(item, nameof(FabricItem.NameAr), entity.NameAr);
        DomainHydrator.Set(item, nameof(FabricItem.NameEn), entity.NameEn);
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
        return color;
    }
}

internal sealed class PurchaseInvoiceRepository(ErpDbContext context) : IPurchaseInvoiceRepository
{
    public async Task<PurchaseInvoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var header = await context.PurchaseInvoices.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (header is null) return null;
        var items = await context.PurchaseInvoiceItems.AsNoTracking()
            .Where(i => i.PurchaseInvoiceId == id).ToListAsync(cancellationToken);
        return PurchaseMapper.ToDomain(header, items);
    }

    public async Task<IReadOnlyList<PurchaseInvoice>> GetListAsync(
        Guid companyId,
        PurchaseInvoiceStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.PurchaseInvoices.AsNoTracking().Where(p => p.CompanyId == companyId);
        if (status.HasValue)
            query = query.Where(p => p.Status == (int)status.Value);

        var headers = await query.ToListAsync(cancellationToken);
        var list = new List<PurchaseInvoice>();
        foreach (var header in headers)
        {
            var items = await context.PurchaseInvoiceItems.AsNoTracking()
                .Where(i => i.PurchaseInvoiceId == header.Id).ToListAsync(cancellationToken);
            list.Add(PurchaseMapper.ToDomain(header, items));
        }
        return list;
    }

    public async Task AddAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default) =>
        await context.PurchaseInvoices.AddAsync(new PurchaseInvoiceEntity
        {
            Id = invoice.Id,
            CompanyId = Guid.Empty,
            InvoiceNumber = invoice.InvoiceNumber,
            SupplierId = invoice.SupplierId,
            InvoiceDate = invoice.InvoiceDate,
            TotalAmount = invoice.TotalAmount.Amount,
            Remaining = invoice.Remaining.Amount,
            Status = (int)invoice.Status
        }, cancellationToken);

    public Task UpdateAsync(PurchaseInvoice invoice, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
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

    public async Task AddAsync(Cashbox cashbox, CancellationToken cancellationToken = default) =>
        await context.Cashboxes.AddAsync(new CashboxEntity
        {
            Id = cashbox.Id,
            BranchId = cashbox.BranchId,
            Code = cashbox.Code,
            Name = cashbox.Name,
            Balance = cashbox.Balance.Amount,
            Currency = cashbox.Currency,
            IsActive = cashbox.IsActive
        }, cancellationToken);

    public async Task UpdateAsync(Cashbox cashbox, CancellationToken cancellationToken = default)
    {
        var entity = await context.Cashboxes.FirstOrDefaultAsync(c => c.Id == cashbox.Id, cancellationToken)
            ?? throw new InvalidOperationException("Cashbox not found.");
        entity.Balance = cashbox.Balance.Amount;
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
