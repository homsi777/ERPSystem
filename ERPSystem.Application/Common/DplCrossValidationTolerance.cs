using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Application.Common;

/// <summary>
/// Tolerance for DPL converted totals vs Invoice/PL meter totals (per fabric/color group).
/// </summary>
public static class DplCrossValidationTolerance
{
    /// <summary>
    /// Absolute floor (0.5 m): per-roll conversion uses 4-decimal rounding; hundreds of rolls
    /// can accumulate sub-meter drift (e.g. 229 × 120 yd → 25,127.712 m vs 25,128 m declared ≈ 0.29 m).
    /// Relative component (0.1%): scales with group size without opening a wide gap.
    /// Wrong unit selection produces ~9% discrepancy (yard treated as meter or vice versa) — far above this.
    /// </summary>
    public const decimal AbsoluteFloorMeters = 0.5m;
    public const decimal RelativeFraction = 0.001m;

    public static bool WithinTolerance(decimal expectedMeters, decimal calculatedMeters)
    {
        if (expectedMeters <= 0)
            return calculatedMeters <= 0;

        var diff = Math.Abs(expectedMeters - calculatedMeters);
        var allowed = Math.Max(AbsoluteFloorMeters, expectedMeters * RelativeFraction);
        return diff <= allowed;
    }

    public static string FormatTolerance(decimal expectedMeters)
    {
        var allowed = Math.Max(AbsoluteFloorMeters, expectedMeters * RelativeFraction);
        return $"{allowed:N3} م (max(0.5 م، 0.1% × {expectedMeters:N2} م))";
    }
}
