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
    Task<IReadOnlyList<Guid>> GetSellableContainerIdsAsync(
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Containers that currently have available fabric rolls — used by sales invoice picking
    /// without requiring China-import general-manager permissions.
    /// When <paramref name="warehouseId"/> is set, only containers with stock in that warehouse.
    /// </summary>
    Task<IReadOnlyList<SellableContainerDto>> GetSellableContainersAsync(
        Guid? warehouseId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountLowStockItemsAsync(Guid branchId, decimal thresholdMeters = 50m, CancellationToken cancellationToken = default);

    /// <summary>Returns the cost-per-meter for the given fabric roll ids.</summary>
    Task<IReadOnlyDictionary<Guid, decimal>> GetRollCostsAsync(
        IReadOnlyCollection<Guid> rollIds,
        CancellationToken cancellationToken = default);
}
