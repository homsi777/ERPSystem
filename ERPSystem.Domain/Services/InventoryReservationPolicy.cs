using ERPSystem.Domain.Entities.Inventory;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Services;

public static class InventoryReservationPolicy
{
    public static void Reserve(
        WarehouseStockBalance balance,
        LengthInMeters requestedMeters,
        LengthInMeters lowStockThreshold)
    {
        if (requestedMeters.Value <= 0)
            throw new InventoryException("Reservation amount must be greater than zero.");

        balance.Reserve(requestedMeters);

        if (balance.AvailableMeters.Value <= lowStockThreshold.Value)
        {
            // Low stock signal — handled by application layer via WarehouseStockLow event
        }
    }

    public static bool CanReserve(WarehouseStockBalance balance, LengthInMeters requestedMeters) =>
        requestedMeters.Value > 0 && requestedMeters.Value <= balance.AvailableMeters.Value;
}
