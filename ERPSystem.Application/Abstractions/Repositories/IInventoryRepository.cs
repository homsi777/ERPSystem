using ERPSystem.Application.DTOs.Inventory;

namespace ERPSystem.Application.Abstractions.Repositories;

public interface IInventoryRepository
{
    Task<bool> IsStockPostedForContainerAsync(Guid containerId, CancellationToken cancellationToken = default);
    Task<ContainerInventoryMetricsDto?> GetContainerMetricsAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FabricRollInventoryDto>> GetAvailableRollsForContainerAsync(
        Guid containerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetWarehousesWithContainerStockAsync(
        Guid containerId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetSellableContainerIdsAsync(CancellationToken cancellationToken = default);
    Task<int> CountLowStockItemsAsync(Guid branchId, decimal thresholdMeters = 50m, CancellationToken cancellationToken = default);
}
