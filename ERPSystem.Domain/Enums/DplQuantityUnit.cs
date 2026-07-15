namespace ERPSystem.Domain.Enums;

/// <summary>
/// Unit used for roll quantities in a China DPL (detailed packing list) file.
/// DPL is the source of truth for per-roll lengths at sale time.
/// </summary>
public enum DplQuantityUnit
{
    Meters = 0,
    Yards = 1
}
