using System.Diagnostics;

namespace ERPSystem.Application.Diagnostics;

/// <summary>
/// Optional startup sub-phase timing (ms only) for Migrate/Seed breakdown reports.
/// </summary>
public static class StartupPhaseRecorder
{
    private static readonly List<StartupPhaseTiming> Timings = [];
    private static readonly object Lock = new();

    public static async Task RunAsync(string phase, Func<Task> action)
    {
        var sw = Stopwatch.StartNew();
        await action();
        lock (Lock)
        {
            Timings.Add(new StartupPhaseTiming(phase, sw.Elapsed.TotalMilliseconds));
        }
    }

    public static IReadOnlyList<StartupPhaseTiming> GetTimings()
    {
        lock (Lock)
        {
            return Timings.ToList();
        }
    }

    public sealed record StartupPhaseTiming(string Phase, double TotalMs);
}
