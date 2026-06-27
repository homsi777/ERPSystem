using ERPSystem.Domain.Entities.Inventory;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.Services;

public static class StockMovementValidator
{
    public static void EnsureCanPost(StockMovement movement)
    {
        if (movement.Status != StockMovementStatus.Draft)
            throw new InventoryException("Only draft stock movements can be posted.");
    }

    public static bool IsValidTransfer(Guid fromWarehouseId, Guid toWarehouseId) =>
        fromWarehouseId != Guid.Empty &&
        toWarehouseId != Guid.Empty &&
        fromWarehouseId != toWarehouseId;
}
