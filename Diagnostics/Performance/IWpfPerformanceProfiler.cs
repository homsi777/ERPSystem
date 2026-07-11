namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Central, in-process performance instrumentation for the WPF desktop app. Purely observational —
/// it never changes control flow, business results, or financial/accounting/inventory data. Safe to
/// keep registered in every build; overhead per screen load is a handful of Stopwatch reads.
/// </summary>
public interface IWpfPerformanceProfiler
{
    /// <summary>Unique id for this desktop session — stamped on every JSONL line.</summary>
    string SessionId { get; }

    /// <summary>Per-session JSONL log written alongside the daily aggregate log.</summary>
    string? SessionLogFilePath { get; }

    /// <summary>
    /// Begins timing a screen/page load. Dispose the returned scope when the first page of data has
    /// been rendered to the user (not when a background prefetch finishes).
    /// </summary>
    WpfPerformanceScope BeginScreenLoad(string screenName, string? viewModelName = null, string? correlationId = null);

    void RecordNavigation(NavigationMetric metric);

    void RecordUiThreadOperation(string screenName, string operation, double durationMs, string? correlationId = null);

    IReadOnlyList<ScreenLoadMetric> GetRecentScreenLoads(int count = 100);

    IReadOnlyList<NavigationMetric> GetRecentNavigations(int count = 100);

    IReadOnlyList<UiThreadMetric> GetRecentUiThreadOperations(int count = 200);

    event EventHandler<ScreenLoadMetric>? ScreenLoadRecorded;
}
