using System.Collections.Concurrent;
using System.Reflection;

namespace ERPSystem.DocumentEngine.Services;

/// <summary>
/// Reads the embedded design-system assets (CSS, fonts, svg). Assets are cached
/// after first read. The combined stylesheet is assembled in a deterministic
/// cascade order so overrides (print, mobile) always win.
/// </summary>
public sealed class AssetProvider
{
    private static readonly Assembly EngineAssembly = typeof(AssetProvider).Assembly;
    private const string ResourceRoot = "ERPSystem.DocumentEngine.Assets.";

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    /// <summary>
    /// Stylesheet cascade order. Later files may override earlier ones.
    /// print.css MUST be last so print/pdf rules take precedence.
    /// </summary>
    private static readonly string[] StyleCascade =
    {
        "css/variables.css",
        "fonts/fonts.css",
        "css/base.css",
        "css/layout.css",
        "css/components.css",
        "css/tables.css",
        "css/forms.css",
        "css/cards.css",
        "css/badges.css",
        "css/timeline.css",
        "css/charts.css",
        "icons/icons.css",
        "css/utilities.css",
        "css/desktop.css",
        "css/mobile.css",
        "css/print.css"
    };

    /// <summary>Returns the full combined design-system stylesheet.</summary>
    public string GetCombinedStyles()
    {
        return Cache.GetOrAdd("__combined__", _ =>
        {
            var sb = new System.Text.StringBuilder(64 * 1024);
            foreach (var path in StyleCascade)
            {
                var css = TryReadText(path);
                if (css is null)
                {
                    continue;
                }

                sb.Append("/* ==== ").Append(path).Append(" ==== */\n");
                sb.Append(css).Append('\n');
            }

            return sb.ToString();
        });
    }

    /// <summary>Reads a single embedded text asset by its relative path (e.g. "css/base.css").</summary>
    public string? TryReadText(string relativePath)
    {
        var resourceName = ToResourceName(relativePath);
        return Cache.GetOrAdd(resourceName, name =>
        {
            using var stream = EngineAssembly.GetManifestResourceStream(name);
            if (stream is null)
            {
                return NullMarker;
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }) is { } value && !ReferenceEquals(value, NullMarker) ? value : null;
    }

    /// <summary>Lists every embedded asset resource name (diagnostics).</summary>
    public IReadOnlyList<string> ListResources() =>
        EngineAssembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(ResourceRoot, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

    private static readonly string NullMarker = "\u0000__missing__";

    private static string ToResourceName(string relativePath)
    {
        var normalized = relativePath.Replace('\\', '/').TrimStart('/');
        return ResourceRoot + normalized.Replace('/', '.');
    }
}
