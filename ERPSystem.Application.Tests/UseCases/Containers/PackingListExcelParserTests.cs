using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Tests.UseCases.Containers;

public sealed class PackingListExcelParserTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Parse_114C_yard_dpl_produces_two_groups_and_correct_meters()
    {
        var path = Path.Combine(RepoRoot, "DPL-(114C).xls");
        if (!File.Exists(path))
            return;

        using var sheet = ExcelWorksheetReaderFactory.Open(File.ReadAllBytes(path), path);
        var result = PackingListExcelParser.Parse(sheet);

        Assert.Equal(DplQuantityUnit.Yards, result.DetectedQuantityUnit);
        Assert.Equal(2, result.Groups.Count);
        Assert.Equal(458, result.Groups.Sum(g => g.Rolls.Count(r => r.IsValid)));
        Assert.True(PackingListExcelParser.MetersApproximatelyEqual(
            50256m,
            result.Groups.Sum(g => g.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters))));
        Assert.Equal(458, result.DeclaredGrandRolls);
        Assert.True(PackingListExcelParser.MetersApproximatelyEqual(50256m, result.DeclaredGrandMeters ?? 0m));

        var sampleRoll = result.Groups.SelectMany(g => g.Rolls).First(r => r.IsValid);
        Assert.Equal(120m, sampleRoll.QuantityNative);
        Assert.Equal(DplQuantityUnit.Yards, sampleRoll.QuantityUnit);
        Assert.Equal(109.728m, sampleRoll.QuantityMeters);
    }

    [Fact]
    public void Parse_126C_meter_dpl_unchanged()
    {
        var path = Path.Combine(RepoRoot, "DPL-(126C).xls");
        if (!File.Exists(path))
            return;

        using var sheet = ExcelWorksheetReaderFactory.Open(File.ReadAllBytes(path), path);
        var result = PackingListExcelParser.Parse(sheet);

        Assert.Equal(DplQuantityUnit.Meters, result.DetectedQuantityUnit);
        Assert.True(result.Groups.Count >= 1);
        Assert.True(result.Groups.Sum(g => g.Rolls.Count(r => r.IsValid)) > 0);

        var sampleRoll = result.Groups.SelectMany(g => g.Rolls).First(r => r.IsValid);
        Assert.Equal(DplQuantityUnit.Meters, sampleRoll.QuantityUnit);
        Assert.Equal(sampleRoll.QuantityNative, sampleRoll.QuantityMeters);
    }
}
