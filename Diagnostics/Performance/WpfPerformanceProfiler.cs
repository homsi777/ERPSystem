using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Default <see cref="IWpfPerformanceProfiler"/> implementation. Keeps a bounded in-memory ring
/// buffer per metric type (so the diagnostic report / a future in-app "Perf" panel can read recent
/// history) and best-effort appends a JSONL line per screen load to a local log file for offline
/// analysis. Only counters/timings/screen names are recorded — never customer, financial, or
/// inventory data.
/// </summary>
public sealed class WpfPerformanceProfiler : IWpfPerformanceProfiler
{
    private const int MaxRingSize = 500;

    private readonly ConcurrentQueue<ScreenLoadMetric> _screenLoads = new();
    private readonly ConcurrentQueue<NavigationMetric> _navigations = new();
    private readonly ConcurrentQueue<UiThreadMetric> _uiThreadOps = new();
    private readonly ILogger<WpfPerformanceProfiler> _logger;
    private readonly string? _logFilePath;
    private readonly object _fileLock = new();

    public WpfPerformanceProfiler(ILogger<WpfPerformanceProfiler> logger)
    {
        _logger = logger;
        EfQueryTelemetry.EnsureStarted();
        _logFilePath = TryResolveLogFilePath();
    }

    public event EventHandler<ScreenLoadMetric>? ScreenLoadRecorded;

    public WpfPerformanceScope BeginScreenLoad(string screenName, string? viewModelName = null, string? correlationId = null)
    {
        var id = correlationId ?? Guid.NewGuid().ToString("N")[..12];
        return new WpfPerformanceScope(screenName, viewModelName, id, Complete);
    }

    public void RecordNavigation(NavigationMetric metric)
    {
        Enqueue(_navigations, metric);
        var severity = PerformanceThresholds.Classify(metric.DurationMs);
        if (metric.LooksDuplicate)
            _logger.LogWarning(
                "[WPF-PERF] Duplicate navigation detected: {From} -> {To} ({Duration}ms) correlation={Correlation}",
                metric.FromScreen, metric.ToScreen, metric.DurationMs, metric.CorrelationId);
        else if (severity >= PerfSeverity.Warning)
            _logger.LogInformation(
                "[WPF-PERF] Navigation {From} -> {To}: {Duration}ms ({Severity}) correlation={Correlation}",
                metric.FromScreen, metric.ToScreen, metric.DurationMs, severity, metric.CorrelationId);
    }

    public void RecordUiThreadOperation(string screenName, string operation, double durationMs, string? correlationId = null)
    {
        var metric = new UiThreadMetric
        {
            Screen = screenName,
            Operation = operation,
            CorrelationId = correlationId,
            DurationMs = durationMs,
            ThreadId = Environment.CurrentManagedThreadId,
            WasUiThread = System.Windows.Application.Current?.Dispatcher.CheckAccess() ?? false
        };
        Enqueue(_uiThreadOps, metric);

        if (metric.Severity >= PerfSeverity.Warning)
            _logger.LogInformation(
                "[WPF-PERF] UI-thread op {Screen}.{Operation}: {Duration}ms ({Severity})",
                screenName, operation, durationMs, metric.Severity);
    }

    public IReadOnlyList<ScreenLoadMetric> GetRecentScreenLoads(int count = 100) =>
        _screenLoads.Reverse().Take(count).ToList();

    public IReadOnlyList<NavigationMetric> GetRecentNavigations(int count = 100) =>
        _navigations.Reverse().Take(count).ToList();

    public IReadOnlyList<UiThreadMetric> GetRecentUiThreadOperations(int count = 200) =>
        _uiThreadOps.Reverse().Take(count).ToList();

    private void Complete(ScreenLoadMetric metric)
    {
        Enqueue(_screenLoads, metric);

        var severity = metric.Severity;
        var level = severity switch
        {
            PerfSeverity.Critical => LogLevel.Error,
            PerfSeverity.High => LogLevel.Warning,
            PerfSeverity.Warning => LogLevel.Information,
            _ => LogLevel.Debug
        };

        _logger.Log(level,
            "[WPF-PERF] Screen '{Screen}' loaded in {Total}ms (target {Target}ms) — queries={Queries} dbWait={DbWait}ms rows={Rows} calls={Calls} thread={Thread} correlation={Correlation} cause={Cause}",
            metric.Screen, Math.Round(metric.TotalMs, 1), PerformanceThresholds.ScreenLoadTargetMs,
            metric.QueryCount, metric.DbWaitMs is null ? "n/a" : Math.Round(metric.DbWaitMs.Value, 1).ToString(),
            metric.RowsReturned, metric.ServiceCallCount, metric.ThreadId, metric.CorrelationId, metric.MainCauseHint());

        AppendToLogFile(metric);
        ScreenLoadRecorded?.Invoke(this, metric);
    }

    private static void Enqueue<T>(ConcurrentQueue<T> queue, T item)
    {
        queue.Enqueue(item);
        while (queue.Count > MaxRingSize && queue.TryDequeue(out _)) { }
    }

    private static string? TryResolveLogFilePath()
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ERPSystem", "perf-logs");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, $"wpf-performance-{DateTime.UtcNow:yyyyMMdd}.jsonl");
        }
        catch
        {
            return null;
        }
    }

    private void AppendToLogFile(ScreenLoadMetric metric)
    {
        if (_logFilePath is null) return;

        try
        {
            var line = JsonSerializer.Serialize(metric);
            lock (_fileLock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // Best-effort only — never let diagnostics logging break the app.
        }
    }
}
