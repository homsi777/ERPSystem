using System.Text.RegularExpressions;

namespace ERPSystem.Application.Common;

public static partial class PackingListCatalogNormalizer
{
  public static string NormalizeFabricCode(string? code) =>
      string.IsNullOrWhiteSpace(code) ? "" : code.Trim();

  public static string NormalizeColor(string? color) =>
      string.IsNullOrWhiteSpace(color) ? "DEFAULT" : CollapseWhitespace(color);

  public static string CollapseWhitespace(string value) =>
      WhitespaceCollapse().Replace(value.Trim(), " ");

  [GeneratedRegex(@"\s+")]
  private static partial Regex WhitespaceCollapse();
}
