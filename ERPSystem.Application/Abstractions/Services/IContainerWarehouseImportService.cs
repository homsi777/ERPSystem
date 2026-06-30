using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Application.Abstractions.Services;

public interface IContainerWarehouseImportService
{
    Task PostContainerStockAsync(
        Guid warehouseId,
        ContainerAggregate container,
        CancellationToken cancellationToken = default);
}
