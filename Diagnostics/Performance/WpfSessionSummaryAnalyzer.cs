using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Reads one session JSONL log and writes a human-readable Arabic summary for Nabil's manual review.
/// Purely observational — never touches business data.
/// </summary>
public static class WpfSessionSummaryAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static string? TryWriteSummary(string sessionLogFilePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sessionLogFilePath) || !File.Exists(sessionLogFilePath))
                return null;

            var metrics = ReadMetrics(sessionLogFilePath);
            if (metrics.Count == 0)
                return null;

            var outputPath = BuildSummaryPath(sessionLogFilePath);
            var markdown = BuildMarkdown(metrics, sessionLogFilePath);
            File.WriteAllText(outputPath, markdown, Encoding.UTF8);
            return outputPath;
        }
        catch
        {
            return null;
        }
    }

    public static IReadOnlyList<ScreenLoadMetric> ReadMetrics(string sessionLogFilePath)
    {
        var list = new List<ScreenLoadMetric>();
        foreach (var line in File.ReadLines(sessionLogFilePath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var metric = JsonSerializer.Deserialize<ScreenLoadMetric>(line, JsonOptions);
                if (metric is not null && !string.IsNullOrWhiteSpace(metric.Screen))
                    list.Add(metric);
            }
            catch
            {
                // Skip malformed lines — best effort only.
            }
        }

        return list;
    }

    private static string BuildSummaryPath(string sessionLogFilePath)
    {
        var dir = Path.GetDirectoryName(sessionLogFilePath)
                  ?? Path.Combine(
                      Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                      "ERPSystem", "perf-logs");
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        return Path.Combine(dir, $"session-summary-{stamp}.md");
    }

    private static string BuildMarkdown(IReadOnlyList<ScreenLoadMetric> metrics, string sessionLogFilePath)
    {
        var ordered = metrics
            .OrderBy(m => m.NavigationStartUtc)
            .ThenBy(m => m.CorrelationId, StringComparer.Ordinal)
            .ToList();

        var byScreen = ordered
            .GroupBy(m => m.Screen, StringComparer.Ordinal)
            .Select(g => new ScreenAggregate(
                g.Key,
                g.Count(),
                g.Sum(x => x.TotalMs),
                g.Sum(x => x.QueryCount),
                g.Max(x => x.TotalMs),
                g.Max(x => x.Severity)))
            .OrderByDescending(x => x.TotalMs)
            .ToList();

        var slowest = ordered.MaxBy(m => m.TotalMs)!;
        var thresholdHits = ordered
            .Where(m => m.Severity >= PerfSeverity.Warning)
            .OrderByDescending(m => m.TotalMs)
            .ToList();

        var sessionStart = ordered.Min(m => m.NavigationStartUtc).ToLocalTime();
        var sessionEnd = ordered.Max(m => m.NavigationStartUtc).ToLocalTime();
        var totalScreens = ordered.Count;
        var totalMs = ordered.Sum(m => m.TotalMs);
        var totalQueries = ordered.Sum(m => m.QueryCount);

        var sb = new StringBuilder();
        sb.AppendLine("# ملخص جلسة الأداء — الأمل.AB");
        sb.AppendLine();
        sb.AppendLine("## ملخص الجلسة");
        sb.AppendLine($"- **وقت البداية:** {sessionStart:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **وقت آخر شاشة:** {sessionEnd:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"- **عدد تحميلات الشاشات:** {totalScreens}");
        sb.AppendLine($"- **إجمالي وقت التحميل:** {FormatMs(totalMs)}");
        sb.AppendLine($"- **إجمالي استعلامات قاعدة البيانات:** {totalQueries}");
        sb.AppendLine($"- **ملف السجل الخام:** `{sessionLogFilePath}`");
        sb.AppendLine();
        sb.AppendLine("## عتبات الأداء");
        sb.AppendLine("- **تحذير (Warning):** ≥ 100 ms");
        sb.AppendLine("- **مرتفع (High):** ≥ 500 ms");
        sb.AppendLine("- **حرج (Critical):** ≥ 1000 ms");
        sb.AppendLine();
        sb.AppendLine("## الشاشات المفتوحة (بالترتيب)");
        sb.AppendLine("| # | الشاشة | الوقت | الاستعلامات | الشدة |");
        sb.AppendLine("|---:|---|---:|---:|---|");
        for (var i = 0; i < ordered.Count; i++)
        {
            var m = ordered[i];
            sb.AppendLine($"| {i + 1} | {Escape(m.Screen)} | {FormatMs(m.TotalMs)} | {m.QueryCount} | {SeverityLabel(m.Severity)} |");
        }

        sb.AppendLine();
        sb.AppendLine("## إجمالي الوقت لكل شاشة (مجمّع)");
        sb.AppendLine("| الشاشة | مرات الفتح | إجمالي الوقت | إجمالي الاستعلامات | أبطأ مرة |");
        sb.AppendLine("|---|---:|---:|---:|---:|");
        foreach (var row in byScreen)
        {
            sb.AppendLine(
                $"| {Escape(row.Screen)} | {row.OpenCount} | {FormatMs(row.TotalMs)} | {row.TotalQueries} | {FormatMs(row.MaxMs)} |");
        }

        sb.AppendLine();
        sb.AppendLine("## الشاشات الأبطأ (حسب أبطأ مرة واحدة)");
        sb.AppendLine("| الشاشة | أبطأ مرة | إجمالي الوقت | مرات الفتح |");
        sb.AppendLine("|---|---:|---:|---:|");
        foreach (var row in byScreen.OrderByDescending(x => x.MaxMs).Take(10))
        {
            sb.AppendLine(
                $"| {Escape(row.Screen)} | {FormatMs(row.MaxMs)} | {FormatMs(row.TotalMs)} | {row.OpenCount} |");
        }

        sb.AppendLine();
        sb.AppendLine("## أبطأ شاشة في الجلسة");
        sb.AppendLine($"- **الشاشة:** {slowest.Screen}");
        sb.AppendLine($"- **الوقت:** {FormatMs(slowest.TotalMs)} ({SeverityLabel(slowest.Severity)})");
        sb.AppendLine($"- **الاستعلامات:** {slowest.QueryCount}");
        sb.AppendLine($"- **السبب المحتمل:** {slowest.MainCauseHint()}");
        sb.AppendLine($"- **معرّف التتبع:** `{slowest.CorrelationId}`");

        sb.AppendLine();
        sb.AppendLine("## تنبيهات الأداء (≥ 100 ms)");
        if (thresholdHits.Count == 0)
        {
            sb.AppendLine("- لا توجد شاشات تجاوزت عتبة التحذير — أداء جيد في هذه الجلسة.");
        }
        else
        {
            sb.AppendLine("| الشاشة | الوقت | الشدة | الاستعلامات | السبب المحتمل |");
            sb.AppendLine("|---|---:|---|---:|---|");
            foreach (var m in thresholdHits)
            {
                sb.AppendLine(
                    $"| {Escape(m.Screen)} | {FormatMs(m.TotalMs)} | {SeverityLabel(m.Severity)} | {m.QueryCount} | {Escape(m.MainCauseHint())} |");
            }
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine("*تم إنشاء هذا الملخص تلقائياً عند إغلاق التطبيق.*");
        return sb.ToString();
    }

    private static string FormatMs(double ms) => $"{Math.Round(ms, 1)} ms";

    private static string SeverityLabel(PerfSeverity severity) => severity switch
    {
        PerfSeverity.Critical => "حرج",
        PerfSeverity.High => "مرتفع",
        PerfSeverity.Warning => "تحذير",
        _ => "جيد"
    };

    private static string Escape(string value) =>
        value.Replace("|", "\\|", StringComparison.Ordinal);

    private sealed record ScreenAggregate(
        string Screen,
        int OpenCount,
        double TotalMs,
        int TotalQueries,
        double MaxMs,
        PerfSeverity WorstSeverity);
}
