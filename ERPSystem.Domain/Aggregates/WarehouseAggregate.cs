using ERPSystem.Domain.Common;
using ERPSystem.Domain.Entities.Inventory;

namespace ERPSystem.Domain.Aggregates;

public sealed class WarehouseAggregate : AggregateRoot
{
    public Warehouse Warehouse { get; private set; } = null!;

    private readonly List<WarehouseLocation> _locations = [];
    private readonly List<WarehouseStockBalance> _balances = [];

    public IReadOnlyList<WarehouseLocation> Locations => _locations.AsReadOnly();
    public IReadOnlyList<WarehouseStockBalance> Balances => _balances.AsReadOnly();

    private WarehouseAggregate() { }

    public static WarehouseAggregate Create(Warehouse warehouse) => new()
    {
        Id = warehouse.Id,
        Warehouse = warehouse
    };

    public void AddLocation(WarehouseLocation location) => _locations.Add(location);

    public void AddOrUpdateBalance(WarehouseStockBalance balance)
    {
        var existing = _balances.FirstOrDefault(b =>
            b.WarehouseId == balance.WarehouseId &&
            b.FabricItemId == balance.FabricItemId &&
            b.FabricColorId == balance.FabricColorId &&
            b.ContainerId == balance.ContainerId);

        if (existing is null)
            _balances.Add(balance);
    }

    public WarehouseStockBalance? FindBalance(Guid fabricItemId, Guid fabricColorId, Guid containerId) =>
        _balances.FirstOrDefault(b =>
            b.FabricItemId == fabricItemId &&
            b.FabricColorId == fabricColorId &&
            b.ContainerId == containerId);
}
