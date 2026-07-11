namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// A single timed operation that ran on (or blocked) the UI thread — control creation, data
/// binding, CollectionView.Refresh, sorting, filtering, ObservableCollection bulk-add, etc.
/// </summary>
public sealed class UiThreadMetric
{
    public required string Screen { get; init; }
    public required string Operation { get; init; }
    public string? CorrelationId { get; init; }
    public double DurationMs { get; init; }
    public int ThreadId { get; init; }
    public bool WasUiThread { get; init; }
    public DateTime OccurredUtc { get; init; } = DateTime.UtcNow;

    public PerfSeverity Severity => PerformanceThresholds.Classify(DurationMs);
}
