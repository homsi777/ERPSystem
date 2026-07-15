using ERPSystem.Application.Common;

namespace ERPSystem.Application.Tests;

public class LatinDigitsTests
{
    [Fact]
    public void Normalize_replaces_eastern_arabic_indic_digits()
    {
        Assert.Equal("2026/07/15", LatinDigits.Normalize("٢٠٢٦/٠٧/١٥"));
        Assert.Equal("1234567890", LatinDigits.Normalize("١٢٣٤٥٦٧٨٩٠"));
    }

    [Fact]
    public void Format_uses_western_digits_for_numbers_and_dates()
    {
        Assert.Equal("1,234.50", LatinDigits.Format(1234.5m, "N2"));
        Assert.Equal("15/07/2026", LatinDigits.Format(new DateTime(2026, 7, 15), "dd/MM/yyyy"));
    }

    [Fact]
    public void FormatText_interpolates_with_western_digits()
    {
        Assert.Equal("عرض 12 من 458 سجل", LatinDigits.FormatText("عرض {0} من {1} سجل", 12, 458));
    }
}
