using System.Diagnostics;

namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Timing scope for one screen/page load. Create via <see cref="IWpfPerformanceProfiler.BeginScreenLoad"/>
/// inside a <c>using</c> statement (or <c>using var</c>) that spans from navigation start to the moment
/// the first page of data is visible on screen. Call the Mark*/Measure* helpers for the sub-phases you
/// can identify; any phase you don't call is simply reported as null/not-measured — this is intentionally
/// tolerant since most screens in this codebase are code-behind "page controls" that combine
/// View+ViewModel, so not every phase applies to every screen.
/// </summary>
public sealed class WpfPerformanceScope : IDisposable
{
    private readonly Stopwatch _total = Stopwatch.StartNew();
    private readonly Action<ScreenLoadMetric> _onComplete;
    private readonly ScreenLoadMetric _metric;
    private readonly IDisposable _queryScopeToken;
    private readonly QueryCounterState _queryState;
    private bool _disposed;

    internal WpfPerformanceScope(
        string screenName,
        string? viewModelName,
        string correlationId,
        Action<ScreenLoadMetric> onComplete)
    {
        _onComplete = onComplete;
        _metric = new ScreenLoadMetric
        {
            Screen = screenName,
            ViewModelName = viewModelName,
            CorrelationId = correlationId,
            ThreadId = Environment.CurrentManagedThreadId,
            NavigationStartUtc = DateTime.UtcNow
        };
        _queryScopeToken = EfQueryTelemetry.BeginScope(out _queryState);
    }

    public string CorrelationId => _metric.CorrelationId;

    public void MarkViewConstructed() => _metric.ViewConstructionMs = _total.Elapsed.TotalMilliseconds;

    public void MarkViewModelConstructed() => _metric.ViewModelConstructionMs = _total.Elapsed.TotalMilliseconds;

    /// <summary>Wrap the awaited data-load call: <c>using (scope.MeasureDataLoad()) { ... await ... }</c>.</summary>
    public IDisposable MeasureDataLoad() => new PhaseTimer(ms => _metric.DataLoadMs = ms);

    public IDisposable MeasureMapping() => new PhaseTimer(ms => _metric.MappingMs = ms);

    public IDisposable MeasureItemsSourceAssignment() => new PhaseTimer(ms => _metric.ItemsSourceAssignmentMs = ms);

    public IDisposable MeasureDataGridBinding() => new PhaseTimer(ms => _metric.DataGridBindingMs = ms);

    public IDisposable MeasureRendering() => new PhaseTimer(ms => _metric.RenderingMs = ms);

    public void IncrementServiceCalls(int by = 1) => _metric.ServiceCallCount += by;

    public void SetRowsReturned(int rows) => _metric.RowsReturned = rows;

    public void MarkCancelled() => _metric.Cancelled = true;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _metric.TotalMs = _total.Elapsed.TotalMilliseconds;
        _metric.QueryCount = _queryState.QueryCount;
        _metric.DbWaitMs = _queryState.TotalDbMilliseconds;
        _queryScopeToken.Dispose();

        _onComplete(_metric);
    }

    private sealed class PhaseTimer(Action<double> onComplete) : IDisposable
    {
        private readonly Stopwatch _sw = Stopwatch.StartNew();
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            onComplete(_sw.Elapsed.TotalMilliseconds);
        }
    }
}
