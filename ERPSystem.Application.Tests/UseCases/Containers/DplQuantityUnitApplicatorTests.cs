using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Tests.UseCases.Containers;

public sealed class DplQuantityUnitApplicatorTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    [Fact]
    public void Apply_yard_unit_preserves_114C_meter_totals()
    {
        var parse = Parse114C();
        if (parse is null)
            return;

        var applied = DplQuantityUnitApplicator.Apply(parse, DplQuantityUnit.Yards);
        Assert.Equal(DplQuantityUnit.Yards, applied.SelectedQuantityUnit);
        Assert.True(PackingListExcelParser.MetersApproximatelyEqual(
            50256m,
            applied.GrandTotal.ParsedTotalMeters));

        foreach (var group in applied.Groups)
        {
            Assert.True(PackingListExcelParser.MetersApproximatelyEqual(25128m, group.ParsedTotalMeters),
                $"Group {group.FabricCode}/{group.Color} meters mismatch");
        }
    }

    [Fact]
    public void Apply_meter_unit_on_yard_file_fails_invoice_cross_validation()
    {
        var parse = Parse114C();
        if (parse is null)
            return;

        var wrong = DplQuantityUnitApplicator.Apply(parse, DplQuantityUnit.Meters);
        var session = Build114CSession(wrong);
        if (session is null)
            return;

        var results = DplInvoicePlGroupValidator.Validate(session);
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.False(r.Passed));
    }

    [Fact]
    public void Cross_validation_passes_for_114C_yard_selection()
    {
        var parse = Parse114C();
        if (parse is null)
            return;

        var applied = DplQuantityUnitApplicator.Apply(parse, DplQuantityUnit.Yards);
        var session = Build114CSession(applied);
        if (session is null)
            return;

        var results = DplInvoicePlGroupValidator.Validate(session);
        Assert.Equal(2, results.Count);
        Assert.All(results, r => Assert.True(r.Passed));
    }

    [Fact]
    public void Conversion_factor_is_exactly_0_9144()
    {
        Assert.Equal(0.9144m, DplQuantityConverter.YardsToMetersFactor);
        Assert.Equal(109.728m, DplQuantityConverter.ToMeters(120m, DplQuantityUnit.Yards));
    }

    private static ContainerExcelParseResultDto? Parse114C()
    {
        var path = Path.Combine(RepoRoot, "DPL-(114C).xls");
        if (!File.Exists(path))
            return null;

        using var sheet = ExcelWorksheetReaderFactory.Open(File.ReadAllBytes(path), path);
        var parsed = PackingListExcelParser.Parse(sheet);
        return new ContainerExcelParseResultDto
        {
            FileName = Path.GetFileName(path),
            DetectedQuantityUnit = parsed.DetectedQuantityUnit,
            Groups = parsed.Groups.Select(g =>
            {
                var parsedMeters = g.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters);
                var parsedRolls = g.Rolls.Count(r => r.IsValid);
                return new PackingListGroupDto
                {
                    GroupIndex = g.GroupIndex,
                    FabricCode = g.FabricCode,
                    Color = g.Color,
                    DeclaredTotalMeters = g.DeclaredTotalMeters,
                    DeclaredTotalRolls = g.DeclaredTotalRolls,
                    ParsedTotalMeters = parsedMeters,
                    ParsedTotalRolls = parsedRolls,
                    MetersMatch = PackingListExcelParser.MetersApproximatelyEqual(g.DeclaredTotalMeters, parsedMeters),
                    RollsMatch = PackingListExcelParser.RollsApproximatelyEqual(g.DeclaredTotalRolls, parsedRolls),
                    FabricResolved = true,
                    ColorResolved = true,
                    Rolls = g.Rolls
                };
            }).ToList(),
            GrandTotal = new PackingListGrandTotalDto
            {
                DeclaredTotalMeters = parsed.DeclaredGrandMeters,
                DeclaredTotalRolls = parsed.DeclaredGrandRolls,
                ParsedTotalMeters = parsed.Groups.Sum(x => x.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters)),
                ParsedTotalRolls = parsed.Groups.Sum(x => x.Rolls.Count(r => r.IsValid)),
                MetersMatch = true,
                RollsMatch = true
            }
        };
    }

    private static ChinaImportMultiFileSessionDto? Build114CSession(ContainerExcelParseResultDto dpl)
    {
        var invoicePath = Path.Combine(RepoRoot, "INVOICE-(114C).xlsx");
        var plPath = Path.Combine(RepoRoot, "PL-(114C).xlsx");
        if (!File.Exists(invoicePath) || !File.Exists(plPath))
            return null;

        using var invSheet = ExcelWorksheetReaderFactory.Open(File.ReadAllBytes(invoicePath), invoicePath);
        var invoice = ChinaInvoiceExcelParser.Parse(invSheet, invoicePath);
        using var plSheet = ExcelWorksheetReaderFactory.Open(File.ReadAllBytes(plPath), plPath);
        var pl = ChinaPackingListSummaryParser.Parse(plSheet, plPath);

        return ChinaImportCrossFileMatcher.BuildSession(dpl, invoice, pl);
    }
}
