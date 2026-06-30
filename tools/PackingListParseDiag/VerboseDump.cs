using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;
using System.Text;

static class VerboseDump
{
    public static void Run(string filePath, int maxCol = 20)
    {
        var bytes = File.ReadAllBytes(filePath);
        using var sheet = ExcelWorksheetReaderFactory.Open(bytes, filePath);
        Console.WriteLine($"=== {Path.GetFileName(filePath)} rows {sheet.FirstRowUsed}-{sheet.LastRowUsed} ===\n");

        for (var row = sheet.FirstRowUsed; row <= sheet.LastRowUsed; row++)
        {
            var lastCol = Math.Min(sheet.GetLastColumnUsed(row), maxCol);
            var cells = new List<string>();
            for (var col = 1; col <= lastCol; col++)
            {
                var t = sheet.GetCellString(row, col);
                if (!string.IsNullOrWhiteSpace(t))
                    cells.Add($"C{col}=[{t}]");
            }

            var flags = new List<string>();
            if (PackingListExcelParser.DetectColumnBlocks(sheet, row).Count > 0)
                flags.Add("HDR");
            if (TryGrand(sheet, row, out var gm, out var gr))
                flags.Add($"GRAND({gm}/{gr})");

            var line = cells.Count == 0 ? "(empty)" : string.Join(" ", cells);
            Console.WriteLine($"R{row,3} {string.Join(" ", flags),-12} {line}");
        }

        using var sheet2 = ExcelWorksheetReaderFactory.Open(bytes, filePath);
        var parsed = PackingListExcelParser.Parse(sheet2);
        Console.WriteLine($"\nParsed: groups={parsed.Groups.Count} rolls={parsed.Groups.Sum(g => g.Rolls.Count)} validRolls={parsed.Groups.Sum(g => g.Rolls.Count(r => r.IsValid))}");
        Console.WriteLine($"Declared grand: meters={parsed.DeclaredGrandMeters} rolls={parsed.DeclaredGrandRolls}");
        foreach (var g in parsed.Groups.Take(5))
            Console.WriteLine($"  G{g.GroupIndex} {g.FabricCode}/{g.Color} decl={g.DeclaredTotalMeters}/{g.DeclaredTotalRolls} rolls={g.Rolls.Count}");
        if (parsed.Groups.Count > 5)
            Console.WriteLine($"  ... +{parsed.Groups.Count - 5} more groups");
    }

    private static bool TryGrand(IWorksheetReader sheet, int row, out decimal m, out int r)
    {
        m = 0; r = 0;
        foreach (var text in sheet.GetUsedCellStrings(row))
        {
            var match = System.Text.RegularExpressions.Regex.Match(text, @"TOTAL\s*[:=]?\s*([\d,.]+)\s*MTS\s*/\s*(\d+)\s*ROLLS?", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success && decimal.TryParse(match.Groups[1].Value.Replace(",", ""), out m) && int.TryParse(match.Groups[2].Value, out r))
                return true;
        }
        return false;
    }
}
