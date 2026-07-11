using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

/// <summary>
/// Loads sales-invoice list enrichment lookups in parallel, each on its own DbContext instance.
/// </summary>
internal sealed class SalesInvoiceListLookupLoader(IDbContextFactory<ErpDbContext> dbContextFactory)
    : ISalesInvoiceListLookupLoader
{
    public async Task<(
        IReadOnlyDictionary<Guid, string> CustomerNames,
        IReadOnlyDictionary<Guid, string> WarehouseNames,
        IReadOnlyDictionary<Guid, string> ContainerNumbers)> LoadAsync(
        Guid companyId,
        IEnumerable<Guid> customerIds,
        IEnumerable<Guid> warehouseIds,
        IEnumerable<Guid> containerIds,
        CancellationToken cancellationToken = default)
    {
        var customerTask = LoadCustomersAsync(companyId, customerIds, cancellationToken);
        var warehouseTask = LoadWarehousesAsync(warehouseIds, cancellationToken);
        var containerTask = LoadContainersAsync(companyId, containerIds, cancellationToken);
        await Task.WhenAll(customerTask, warehouseTask, containerTask);
        return (await customerTask, await warehouseTask, await containerTask);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadCustomersAsync(
        Guid companyId,
        IEnumerable<Guid> customerIds,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await new CustomerRepository(context).GetNameLookupAsync(companyId, customerIds, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadWarehousesAsync(
        IEnumerable<Guid> warehouseIds,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await new WarehouseRepository(context).GetNameLookupAsync(warehouseIds, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadContainersAsync(
        Guid companyId,
        IEnumerable<Guid> containerIds,
        CancellationToken cancellationToken)
    {
        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        return await new ChinaContainerRepository(context).GetNumberLookupAsync(companyId, containerIds, cancellationToken);
    }
}
