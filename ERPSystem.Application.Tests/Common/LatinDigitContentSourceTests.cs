using ERPSystem.Application.Common;

namespace ERPSystem.Application.Tests.Common;

/// <summary>
/// Verifies Eastern digits are not produced at formatting/source layer (API/Application).
/// </summary>
public sealed class LatinDigitContentSourceTests
{
    private const string EasternSample = "فاتورة ١٢٣٤ — ٥٦٧٨.٩٠";

    [Fact]
    public void LatinDigits_Normalize_converts_api_like_eastern_strings()
    {
        var normalized = LatinDigits.Normalize(EasternSample);
        Assert.DoesNotContain('١', normalized);
        Assert.Contains("1234", normalized);
        Assert.Contains("5678.90", normalized);
    }

    [Fact]
    public void LatinDigits_Format_never_emits_eastern_digits_for_decimals()
    {
        var rendered = LatinDigits.Format(1234567.89m, "N2");
        Assert.False(rendered.Any(c => c is >= '\u0660' and <= '\u0669'));
        Assert.Contains("1,234,567.89", rendered);
    }

    [Fact]
    public void LatinDigits_Format_never_emits_eastern_digits_for_dates()
    {
        var rendered = LatinDigits.Format(new DateTime(2026, 7, 15), "yyyy/MM/dd");
        Assert.False(rendered.Any(c => c is >= '\u0660' and <= '\u0669'));
        Assert.Equal("2026/07/15", rendered);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(99)]
    [InlineData(1234567890.1234)]
    public void LatinDigits_Format_numeric_types_use_western_digits(decimal value)
    {
        var rendered = LatinDigits.Format(value, "N4");
        Assert.False(rendered.Any(c => c is >= '\u0660' and <= '\u0669'));
    }
}
