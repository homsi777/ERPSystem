using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Aggregates;

namespace ERPSystem.Infrastructure.Services;

internal sealed class ContainerWarehouseImportService(IInventoryEngine engine) : IContainerWarehouseImportService
{
    public Task PostContainerStockAsync(
        Guid warehouseId,
        ContainerAggregate container,
        CancellationToken cancellationToken = default) =>
        engine.PostContainerImportAsync(warehouseId, container, cancellationToken);
}
