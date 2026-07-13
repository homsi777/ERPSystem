using ERPSystem.Infrastructure.Persistence.Models.Catalog;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Infrastructure.Services;

public static class LegacyOpeningBalanceRollLengthPolicy
{
    /// <summary>
    /// Replaces the provisional even-distribution length exactly once for a tagged legacy roll.
    /// Returns true only when the first real length was applied. No accounting or valuation service
    /// is called here; normal sales consumption remains responsible for later stock movement.
    /// </summary>
    public static bool TryApplyFirstRealLength(FabricRollEntity roll, decimal enteredLengthMeters)
    {
        if (!roll.IsLegacyOpeningBalance || roll.LegacyLengthConfirmed || enteredLengthMeters <= 0)
            return false;

        roll.LengthMeters = enteredLengthMeters;
        roll.RemainingLengthMeters = enteredLengthMeters;
        roll.LegacyLengthConfirmed = true;
        return true;
    }

    public static decimal ResolveAndValidateSaleLength(FabricRollEntity roll, decimal enteredLengthMeters)
    {
        TryApplyFirstRealLength(roll, enteredLengthMeters);
        var meters = enteredLengthMeters > 0 ? enteredLengthMeters : roll.RemainingLengthMeters;
        if (meters > roll.RemainingLengthMeters)
            throw new InventoryException(
                $"Entered length ({meters:N2}) exceeds remaining length ({roll.RemainingLengthMeters:N2}).");

        return meters;
    }
}
