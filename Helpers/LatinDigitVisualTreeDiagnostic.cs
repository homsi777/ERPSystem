using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace ERPSystem.Helpers;

public sealed record LatinDigitFinding(
    string ElementType,
    string TreePath,
    string EffectiveLanguage,
    string? TextSample,
    bool HasEasternDigitsInContent,
    bool HasArabicEffectiveLanguage);

/// <summary>
/// Walks a WPF visual tree and reports elements at risk of Eastern Arabic-Indic digit display.
/// </summary>
public static class LatinDigitVisualTreeDiagnostic
{
    public static bool ContainsEasternDigits(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return false;

        foreach (var ch in text)
        {
            if (ch is >= '\u0660' and <= '\u0669' or >= '\u06F0' and <= '\u06F9')
                return true;
        }

        return false;
    }

    public static IReadOnlyList<LatinDigitFinding> Scan(DependencyObject root, int maxFindings = 5000)
    {
        var results = new List<LatinDigitFinding>();
        ScanRecursive(root, "", results, maxFindings);
        return results;
    }

    public static IReadOnlyList<LatinDigitFinding> ScanBrokenOnly(DependencyObject root, int maxFindings = 5000) =>
        Scan(root, maxFindings)
            .Where(f => f.HasEasternDigitsInContent || f.HasArabicEffectiveLanguage)
            .ToList();

    public static string BuildReport(string screenName, DependencyObject root, IReadOnlyList<LatinDigitFinding>? brokenOnly = null)
    {
        var all = Scan(root);
        brokenOnly ??= all.Where(f => f.HasEasternDigitsInContent || f.HasArabicEffectiveLanguage).ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"# Latin digit audit — {screenName}");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Elements scanned: {all.Count}");
        sb.AppendLine($"Broken elements: {brokenOnly.Count}");
        sb.AppendLine();

        if (brokenOnly.Count == 0)
        {
            sb.AppendLine("PASS — no Eastern digits in content and no Arabic effective Language.");
            return sb.ToString();
        }

        sb.AppendLine("FAIL — details:");
        foreach (var f in brokenOnly.Take(200))
        {
            sb.AppendLine($"- {f.ElementType} @ {f.TreePath}");
            sb.AppendLine($"  Language={f.EffectiveLanguage}, EasternInContent={f.HasEasternDigitsInContent}, ArabicLanguage={f.HasArabicEffectiveLanguage}");
            if (!string.IsNullOrWhiteSpace(f.TextSample))
                sb.AppendLine($"  Text sample: {Truncate(f.TextSample, 120)}");
        }

        if (brokenOnly.Count > 200)
            sb.AppendLine($"... and {brokenOnly.Count - 200} more.");

        return sb.ToString();
    }

    public static void WriteReportToFile(string screenName, DependencyObject root, string filePath)
    {
        var broken = ScanBrokenOnly(root);
        File.WriteAllText(filePath, BuildReport(screenName, root, broken), Encoding.UTF8);
    }

    private static void ScanRecursive(
        DependencyObject node,
        string path,
        List<LatinDigitFinding> results,
        int maxFindings)
    {
        if (results.Count >= maxFindings)
            return;

        var typeName = node.GetType().Name;
        var currentPath = string.IsNullOrEmpty(path) ? typeName : $"{path}/{typeName}";

        string? text = ExtractText(node);
        var language = node is FrameworkElement fe
            ? fe.Language?.ToString() ?? "(null)"
            : "(n/a)";
        var arabicLanguage = node is FrameworkElement fe2 &&
                             fe2.Language is not null &&
                             fe2.Language.ToString().StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var eastern = ContainsEasternDigits(text);

        if (text is not null || node is FrameworkElement)
        {
            results.Add(new LatinDigitFinding(
                typeName,
                currentPath,
                language,
                text is null ? null : Truncate(text, 80),
                eastern,
                arabicLanguage));
        }

        var childCount = VisualTreeHelper.GetChildrenCount(node);
        for (var i = 0; i < childCount; i++)
            ScanRecursive(VisualTreeHelper.GetChild(node, i), $"{currentPath}[{i}]", results, maxFindings);
    }

    private static string? ExtractText(DependencyObject node) => node switch
    {
        TextBlock tb => tb.Text,
        Label lbl when lbl.Content is string s => s,
        Button btn when btn.Content is string s => s,
        DatePickerTextBox dpt => dpt.Text,
        TextBox box => box.Text,
        Run run => run.Text,
        AccessText access => access.Text,
        ContentPresenter cp when cp.Content is string s => s,
        _ => null
    };

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
