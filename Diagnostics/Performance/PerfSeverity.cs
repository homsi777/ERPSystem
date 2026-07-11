namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Severity bucket for a single timed operation, per the thresholds mandated by the
/// WPF Performance Rescue task: &lt;100ms = OK, &gt;=100ms = Warning, &gt;=500ms = High, &gt;=1000ms = Critical.
/// </summary>
public enum PerfSeverity
{
    Ok = 0,
    Warning = 1,
    High = 2,
    Critical = 3
}

public static class PerformanceThresholds
{
    public const double WarningMs = 100;
    public const double HighMs = 500;
    public const double CriticalMs = 1000;

    /// <summary>Target budget for a full screen open (navigation start to first page of data rendered).</summary>
    public const double ScreenLoadTargetMs = 1500;

    public static PerfSeverity Classify(double milliseconds) => milliseconds switch
    {
        >= CriticalMs => PerfSeverity.Critical,
        >= HighMs => PerfSeverity.High,
        >= WarningMs => PerfSeverity.Warning,
        _ => PerfSeverity.Ok
    };
}
