using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;

static class SafetyCheck
{
    public static int Run(string filePath)
    {
        var bytes = File.ReadAllBytes(filePath);

        using (var fullSheet = ExcelWorksheetReaderFactory.Open(bytes, filePath))
        {
            var full = PackingListExcelParser.Parse(fullSheet);
            var fullRolls = full.Groups.Sum(g => g.Rolls.Count(r => r.IsValid));
            Console.WriteLine($"Full parse: declared={full.DeclaredGrandRolls} parsed={fullRolls}");
            if (full.DeclaredGrandRolls != fullRolls)
            {
                Console.WriteLine("FAIL: full file should match declared grand total.");
                return 1;
            }
        }

        using var cappedInner = ExcelWorksheetReaderFactory.Open(bytes, filePath);
        using var cappedSheet = new RowCappedWorksheetReader(cappedInner, maxRow: 24);
        var truncated = PackingListExcelParser.Parse(cappedSheet);
        var truncatedRolls = truncated.Groups.Sum(g => g.Rolls.Count(r => r.IsValid));

        using var grandSheet = ExcelWorksheetReaderFactory.Open(bytes, filePath);
        var declaredGrand = PackingListExcelParser.Parse(grandSheet).DeclaredGrandRolls;

        Console.WriteLine($"Truncated parse (rows 1-24): declared={declaredGrand} parsed={truncatedRolls}");

        if (declaredGrand is null)
        {
            Console.WriteLine("FAIL: could not read declared grand total from source file.");
            return 1;
        }

        if (truncatedRolls >= declaredGrand)
        {
            Console.WriteLine("FAIL: truncated parse should read fewer rolls than declared.");
            return 1;
        }

        var wouldBlock = !PackingListExcelParser.RollsApproximatelyEqual(declaredGrand.Value, truncatedRolls);
        if (!wouldBlock)
        {
            Console.WriteLine("FAIL: safety check should block truncated import.");
            return 1;
        }

        Console.WriteLine(
            $"PASS: import would block — تحذير: تم تحليل {truncatedRolls} توب فقط من أصل {declaredGrand.Value} المعلن في الملف. لا يمكن المتابعة.");
        return 0;
    }
}
