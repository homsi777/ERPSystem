using System.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Counts EF Core DB round-trips (query count + cumulative wait time) for the currently executing
/// screen-load scope, without touching <c>ErpDbContext</c> or Infrastructure DI registration at all.
/// It listens to the standard .NET <see cref="DiagnosticListener"/> feed that EF Core already
/// publishes ("Microsoft.EntityFrameworkCore.Database.Command.CommandExecuted") and correlates
/// commands to the ambient <see cref="WpfPerformanceScope"/> via <see cref="AsyncLocal{T}"/>, which
/// flows correctly across await boundaries (including through <c>IServiceScopeFactory.CreateScope()</c>
/// and nested command/query handler calls) as long as the calling code stays on the normal async
/// call chain (no <c>Task.Run(...).Result</c> / disconnected threads).
///
/// No SQL text or parameter values are ever captured — only a count and a duration per command.
/// </summary>
public static class EfQueryTelemetry
{
    private static readonly AsyncLocal<QueryCounterState?> Ambient = new();
    private static readonly object StartLock = new();
    private static bool _started;

    public static void EnsureStarted()
    {
        if (_started) return;
        lock (StartLock)
        {
            if (_started) return;
            DiagnosticListener.AllListeners.Subscribe(new ListenerObserver());
            _started = true;
        }
    }

    /// <summary>Begins a new ambient counting scope; dispose the result to restore the previous one.</summary>
    public static IDisposable BeginScope(out QueryCounterState state)
    {
        state = new QueryCounterState();
        var previous = Ambient.Value;
        Ambient.Value = state;
        return new PopToken(previous);
    }

    private static void OnCommandExecuted(TimeSpan duration)
    {
        var state = Ambient.Value;
        if (state is null)
            return;

        state.RecordCommand(duration);
    }

    private sealed class PopToken(QueryCounterState? previous) : IDisposable
    {
        public void Dispose() => Ambient.Value = previous;
    }

    private sealed class ListenerObserver : IObserver<DiagnosticListener>
    {
        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.EntityFrameworkCore")
                listener.Subscribe(new CommandObserver());
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }

    private sealed class CommandObserver : IObserver<KeyValuePair<string, object?>>
    {
        public void OnNext(KeyValuePair<string, object?> value)
        {
            if (value.Key != RelationalEventId.CommandExecuted.Name)
                return;

            if (value.Value is CommandExecutedEventData data)
                OnCommandExecuted(data.Duration);
        }

        public void OnError(Exception error) { }
        public void OnCompleted() { }
    }
}

/// <summary>Mutable counter for the currently-active screen-load scope. No SQL/parameter data stored.</summary>
public sealed class QueryCounterState
{
    private int _queryCount;
    private double _totalMs;
    private readonly object _lock = new();

    public int QueryCount => _queryCount;
    public double TotalDbMilliseconds => _totalMs;

    internal void RecordCommand(TimeSpan duration)
    {
        lock (_lock)
        {
            _queryCount++;
            _totalMs += duration.TotalMilliseconds;
        }
    }
}
