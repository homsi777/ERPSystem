namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Records a single subpage/tab navigation so duplicate/repeated navigation initialization can be detected.
/// </summary>
public sealed class NavigationMetric
{
    public required string FromScreen { get; init; }
    public required string ToScreen { get; init; }
    public required string CorrelationId { get; init; }
    public DateTime OccurredUtc { get; init; } = DateTime.UtcNow;
    public double DurationMs { get; set; }
    public bool NewViewInstanceCreated { get; init; }
    public bool NewViewModelInstanceCreated { get; init; }

    /// <summary>
    /// True when this exact (ToScreen) navigation was already recorded within the
    /// <see cref="DuplicateWindow"/> — a strong signal of double-load (Loaded event + navigation
    /// event + command all firing the same load).
    /// </summary>
    public bool LooksDuplicate { get; set; }

    public static readonly TimeSpan DuplicateWindow = TimeSpan.FromMilliseconds(750);
}
