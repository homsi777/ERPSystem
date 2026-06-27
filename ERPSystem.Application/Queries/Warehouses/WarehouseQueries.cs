namespace ERPSystem.Application.Queries.Warehouses;

public sealed class GetWarehouseListQuery
{
    public Guid BranchId { get; init; }
}

public sealed class GetWarehouseOperationsCenterQuery
{
    public Guid WarehouseId { get; init; }
}
