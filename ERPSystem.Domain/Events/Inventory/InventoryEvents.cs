using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Events.Inventory;

public sealed record InventoryReserved(
    Guid WarehouseId,
    Guid FabricItemId,
    Guid FabricColorId,
    decimal Meters) : DomainEvent;

public sealed record InventoryDeducted(
    Guid WarehouseId,
    Guid FabricItemId,
    Guid FabricColorId,
    decimal Meters) : DomainEvent;

public sealed record WarehouseStockLow(
    Guid WarehouseId,
    Guid FabricItemId,
    Guid FabricColorId,
    decimal AvailableMeters) : DomainEvent;
