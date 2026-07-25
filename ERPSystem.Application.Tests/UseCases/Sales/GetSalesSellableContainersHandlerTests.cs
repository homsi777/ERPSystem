using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.UseCases.Sales;

namespace ERPSystem.Application.Tests.UseCases.Sales;

public sealed class GetSalesSellableContainersHandlerTests
{
    [Fact]
    public async Task HandleAsync_FiltersContainersByTheSelectedWarehouse()
    {
        var warehouseId = Guid.Parse("37aefd71-41a3-4783-bf9e-831c97d1ead3");
        var repository = new FakeInventoryRepository(
        [
            new SellableContainerDto
            {
                Id = Guid.Parse("c5e43e56-2a44-4d8a-bd85-4903bc0594b6"),
                ContainerNumber = "092"
            }
        ]);
        var handler = new GetSalesSellableContainersHandler(repository);

        var result = await handler.HandleAsync(new GetSalesSellableContainersQuery
        {
            WarehouseId = warehouseId
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(warehouseId, repository.RequestedWarehouseId);
        Assert.Single(result.Value!);
        Assert.Equal("092", result.Value![0].ContainerNumber);
    }

    [Fact]
    public async Task HandleAsync_RejectsAnEmptyWarehouseId()
    {
        var repository = new FakeInventoryRepository([]);
        var handler = new GetSalesSellableContainersHandler(repository);

        var result = await handler.HandleAsync(new GetSalesSellableContainersQuery());

        Assert.False(result.IsSuccess);
        Assert.Null(repository.RequestedWarehouseId);
    }

    private sealed class FakeInventoryRepository(
        IReadOnlyList<SellableContainerDto> containers) : IInventoryRepository
    {
        public Guid? RequestedWarehouseId { get; private set; }

        public Task<IReadOnlyList<SellableContainerDto>> GetSellableContainersAsync(
            Guid? warehouseId = null,
            CancellationToken cancellationToken = default)
        {
            RequestedWarehouseId = warehouseId;
            return Task.FromResult(containers);
        }

        public Task<bool> IsStockPostedForContainerAsync(
            Guid containerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<ContainerInventoryMetricsDto?> GetContainerMetricsAsync(
            Guid containerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ContainerInventoryMetricsDto?>(null);

        public Task<IReadOnlyList<FabricRollInventoryDto>> GetAvailableRollsForContainerAsync(
            Guid containerId,
            Guid warehouseId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FabricRollInventoryDto>>([]);

        public Task<IReadOnlyList<Guid>> GetWarehousesWithContainerStockAsync(
            Guid containerId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<IReadOnlyList<Guid>> GetSellableContainerIdsAsync(
            Guid? warehouseId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Guid>>([]);

        public Task<int> CountLowStockItemsAsync(
            Guid branchId,
            decimal thresholdMeters = 50,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<IReadOnlyDictionary<Guid, decimal>> GetRollCostsAsync(
            IReadOnlyCollection<Guid> rollIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, decimal>>(
                new Dictionary<Guid, decimal>());
    }
}
