namespace ERPSystem.Web.Client.Models.Inventory;

public sealed class FabricStockBalanceModel
{
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = "";
    public Guid FabricItemId { get; init; }
    public string FabricCode { get; init; } = "";
    public string FabricName { get; init; } = "";
    public Guid FabricColorId { get; init; }
    public string ColorName { get; init; } = "";
    public Guid ContainerId { get; init; }
    public string ContainerNumber { get; init; } = "";
    public int RollCount { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal ReservedMeters { get; init; }
    public decimal AvailableMeters { get; init; }
    public decimal InventoryValue { get; init; }
}
