namespace ERPSystem.Application.Abstractions.Services;

public interface ISalesInvoiceListLookupLoader
{
    Task<(
        IReadOnlyDictionary<Guid, string> CustomerNames,
        IReadOnlyDictionary<Guid, string> WarehouseNames,
        IReadOnlyDictionary<Guid, string> ContainerNumbers)> LoadAsync(
        Guid companyId,
        IEnumerable<Guid> customerIds,
        IEnumerable<Guid> warehouseIds,
        IEnumerable<Guid> containerIds,
        CancellationToken cancellationToken = default);
}
