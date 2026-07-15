namespace ERPSystem.Application.DTOs.Containers;

/// <summary>
/// Cross-validation of converted DPL group totals against Invoice/PL meter totals.
/// </summary>
public sealed class DplGroupCrossValidationResult
{
    public string GroupKey { get; init; } = "";
    public string DisplayName { get; init; } = "";
    public decimal ExpectedMeters { get; init; }
    public string ExpectedSource { get; init; } = "";
    public decimal CalculatedMeters { get; init; }
    public decimal DifferenceMeters { get; init; }
    public bool Passed { get; init; }
    public bool UserConfirmed { get; init; }
    public string MessageArabic { get; init; } = "";
}
