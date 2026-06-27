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

    public async Task<IReadOnlyList<SupplierAggregate>> GetListAsync(
        Guid companyId,
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = context.Suppliers.AsNoTracking().Where(s => s.CompanyId == companyId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Code.Contains(term) || s.Name.Contains(term));
        }

        var entities = await query.OrderBy(s => s.Code).ToListAsync(cancellationToken);
        return entities.Select(SupplierMapper.ToAggregate).ToList();
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
