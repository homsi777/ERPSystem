using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Mapping;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class CustomerRepository(ErpDbContext context) : ICustomerRepository
{
    public async Task<CustomerAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return entity is null ? null : CustomerMapper.ToAggregate(entity);
    }

    public async Task<IReadOnlyList<CustomerAggregate>> GetListAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking().Where(c => c.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c => c.Code.Contains(term) || c.NameAr.Contains(term) || c.NameEn.Contains(term));
        }

        var entities = await query.OrderBy(c => c.Code).ToListAsync(cancellationToken);
        return entities.Select(CustomerMapper.ToAggregate).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNameLookupAsync(
        Guid companyId,
        IEnumerable<Guid>? customerIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking().Where(c => c.CompanyId == companyId);
        if (customerIds is not null)
        {
            var ids = customerIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, string>();
            query = query.Where(c => ids.Contains(c.Id));
        }

        return await query.ToDictionaryAsync(c => c.Id, c => c.NameAr, cancellationToken);
    }

    public async Task<(IReadOnlyList<CustomerAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Customers.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(c =>
                c.Code.Contains(term) ||
                c.NameAr.Contains(term) ||
                c.NameEn.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderBy(c => c.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(CustomerMapper.ToAggregate).ToList(), totalCount);
    }

    public async Task<IReadOnlyList<CustomerAggregate>> GetWithPositiveBalanceAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var entities = await context.Customers.AsNoTracking()
            .Where(c => c.CompanyId == companyId && c.Balance > 0)
            .OrderByDescending(c => c.Balance)
            .ToListAsync(cancellationToken);

        return entities.Select(CustomerMapper.ToAggregate).ToList();
    }

    public async Task<(string CustomerName, string? CustomerPhone, string? WarehouseName, decimal CustomerBalance)?> GetInvoicePartyDisplayAsync(
        Guid customerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        var row = await context.Customers.AsNoTracking()
            .Where(c => c.Id == customerId)
            .Select(c => new
            {
                c.NameAr,
                c.Phone,
                c.Balance,
                WarehouseName = warehouseId != Guid.Empty
                    ? context.Warehouses.AsNoTracking()
                        .Where(w => w.Id == warehouseId)
                        .Select(w => w.NameAr)
                        .FirstOrDefault()
                    : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : (row.NameAr, row.Phone?.ToString(), row.WarehouseName, row.Balance);
    }

    public async Task AddAsync(CustomerAggregate aggregate, CancellationToken cancellationToken = default)
    {
        await context.Customers.AddAsync(CustomerMapper.ToEntity(aggregate), cancellationToken);
    }

    public async Task UpdateAsync(CustomerAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var entity = await context.Customers.FirstOrDefaultAsync(c => c.Id == aggregate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Customer not found.");
        CustomerMapper.UpdateEntity(entity, aggregate);
    }
}

internal sealed class SupplierRepository(ErpDbContext context) : ISupplierRepository
{
    public async Task<SupplierAggregate?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return entity is null ? null : SupplierMapper.ToAggregate(entity);
    }

    public async Task<bool> ExistsByCodeAsync(
        Guid companyId,
        string code,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.Code == code);
        if (excludeId.HasValue)
            query = query.Where(s => s.Id != excludeId.Value);
        return await query.AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierAggregate>> GetListAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.AsNoTracking().Where(s => s.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Code.Contains(term) ||
                s.Name.Contains(term) ||
                s.NameAr.Contains(term) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.Country != null && s.Country.Contains(term)));
        }

        var entities = await query.OrderBy(s => s.Code).ToListAsync(cancellationToken);
        return entities.Select(SupplierMapper.ToAggregate).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, string>> GetNameLookupAsync(
        Guid companyId,
        IEnumerable<Guid>? supplierIds = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.AsNoTracking().Where(s => s.CompanyId == companyId);
        if (supplierIds is not null)
        {
            var ids = supplierIds.Distinct().ToList();
            if (ids.Count == 0)
                return new Dictionary<Guid, string>();
            query = query.Where(s => ids.Contains(s.Id));
        }

        return await query.ToDictionaryAsync(s => s.Id, s => s.Name, cancellationToken);
    }

    public async Task<(IReadOnlyList<SupplierAggregate> Items, int TotalCount)> GetPagedAsync(
        Guid companyId,
        string? search,
        string? country,
        int? paymentTermsDays,
        bool? hasBalance,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s =>
                s.Code.Contains(term) ||
                s.NameAr.Contains(term) ||
                s.NameEn.Contains(term) ||
                (s.Phone != null && s.Phone.Contains(term)) ||
                (s.Country != null && s.Country.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(country) && country != "الكل")
            query = query.Where(s => s.Country == country);

        if (paymentTermsDays.HasValue)
            query = query.Where(s => s.PaymentTermsDays == paymentTermsDays.Value);

        if (hasBalance == true)
            query = query.Where(s => s.Balance > 0);
        else if (hasBalance == false)
            query = query.Where(s => s.Balance <= 0);

        var totalCount = await query.CountAsync(cancellationToken);
        var entities = await query
            .OrderBy(s => s.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (entities.Select(SupplierMapper.ToAggregate).ToList(), totalCount);
    }

    public async Task AddAsync(SupplierAggregate aggregate, CancellationToken cancellationToken = default) =>
        await context.Suppliers.AddAsync(SupplierMapper.ToEntity(aggregate), cancellationToken);

    public async Task UpdateAsync(SupplierAggregate aggregate, CancellationToken cancellationToken = default)
    {
        var entity = await context.Suppliers.FirstOrDefaultAsync(s => s.Id == aggregate.Id, cancellationToken)
            ?? throw new InvalidOperationException("Supplier not found.");
        SupplierMapper.UpdateEntity(entity, aggregate);
    }
}
