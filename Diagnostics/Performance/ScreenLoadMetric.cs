namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// One completed measurement of a WPF screen/page load. No customer, financial, or otherwise
/// sensitive business data is ever stored here — only names of screens/view-models, counters and
/// durations, so this is safe to log and keep in memory or on disk.
/// </summary>
public sealed class ScreenLoadMetric
{
    public string? SessionId { get; set; }
    public required string Screen { get; init; }
    public string? ViewModelName { get; init; }
    public required string CorrelationId { get; init; }
    public DateTime NavigationStartUtc { get; init; }
    public int ThreadId { get; init; }

    public double? ViewConstructionMs { get; set; }
    public double? ViewModelConstructionMs { get; set; }
    public double? DataLoadMs { get; set; }
    public double? DbWaitMs { get; set; }
    public double? MappingMs { get; set; }
    public double? ItemsSourceAssignmentMs { get; set; }
    public double? DataGridBindingMs { get; set; }
    public double? RenderingMs { get; set; }
    public double TotalMs { get; set; }

    public int QueryCount { get; set; }
    public int ServiceCallCount { get; set; }
    public int RowsReturned { get; set; }
    public bool Cancelled { get; set; }

    public PerfSeverity Severity => PerformanceThresholds.Classify(TotalMs);

    public string MainCauseHint()
    {
        if (Cancelled) return "Cancelled";
        if (QueryCount > 10) return "N+1 (high query count)";
        if (DbWaitMs is > 0 && TotalMs > 0 && DbWaitMs / TotalMs > 0.85) return "Sequential Calls / Slow Query";
        if (DataLoadMs is > 0 && TotalMs > 0 && DataLoadMs / TotalMs > 0.9) return "Unbounded Loading / Over-fetching";
        if (DataGridBindingMs is > PerformanceThresholds.HighMs) return "Heavy DataGrid";
        if (RowsReturned > 500) return "Unbounded Loading";
        return TotalMs >= PerformanceThresholds.ScreenLoadTargetMs ? "Slow Query" : "Within budget";
    }
}
