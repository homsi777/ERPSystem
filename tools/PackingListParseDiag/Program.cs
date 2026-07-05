using System.Diagnostics;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;

const int timeoutSeconds = 10;
var dumpMode = args.Contains("--dump");
var safetyTest = args.Contains("--safety-test");
var multi126c = args.Contains("--multi126c");

if (multi126c)
{
    var root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
    var invoice = Path.Combine(root, "INVOICE-(126C).xlsx");
    var pl = Path.Combine(root, "PL-(126C).xlsx");
    var dpl = Path.Combine(root, "DPL-(126C).xls");
    if (!File.Exists(dpl))
        dpl = Path.Combine(root, "DPL.xls");
    foreach (var f in new[] { invoice, pl, dpl })
    {
        if (!File.Exists(f))
        {
            Console.WriteLine($"Missing: {f}");
            return 1;
        }
    }

    using var invSheet = ExcelWorksheetReaderFactory.Open(await File.ReadAllBytesAsync(invoice), invoice);
    var inv = ChinaInvoiceExcelParser.Parse(invSheet, Path.GetFileName(invoice));
    using var plSheet = ExcelWorksheetReaderFactory.Open(await File.ReadAllBytesAsync(pl), pl);
    var plDto = ChinaPackingListSummaryParser.Parse(plSheet, Path.GetFileName(pl));

    Console.WriteLine($"Invoice lines: {inv.Lines.Count}, freight={inv.SeaFreightUsd:N2}, insurance={inv.InsuranceUsd:N2}, total={inv.GrandTotalUsd:N2}");
    Console.WriteLine($"PL lines: {plDto.Lines.Count}, weight={plDto.TotalNetWeightKg:N0} kg");

    using var dplSheet = ExcelWorksheetReaderFactory.Open(await File.ReadAllBytesAsync(dpl), dpl);
    var dplRaw = PackingListExcelParser.Parse(dplSheet);
    var dplGroups = dplRaw.Groups.Select((g, i) => new ERPSystem.Application.DTOs.Containers.PackingListGroupDto
    {
        GroupIndex = i + 1,
        FabricCode = g.FabricCode,
        Color = g.Color,
        ParsedTotalMeters = g.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters),
        ParsedTotalRolls = g.Rolls.Count(r => r.IsValid),
        Rolls = g.Rolls.Select(r => new ERPSystem.Application.DTOs.Containers.PackingListRollDto
        {
            RollNumber = r.RollNumber,
            QuantityMeters = r.QuantityMeters,
            LotCode = r.LotCode,
            IsValid = r.IsValid,
            InvalidReason = r.InvalidReason
        }).ToList()
    }).ToList();

    Console.WriteLine($"DPL groups: {dplGroups.Count}, rolls={dplGroups.Sum(g => g.ParsedTotalRolls)}");

    var rollDetail = new ContainerExcelParseResultDto
    {
        FileName = Path.GetFileName(dpl),
        Groups = dplGroups
    };

    var before = ChinaImportCrossFileMatcher.BuildSession(rollDetail, inv, plDto);
    Console.WriteLine($"BEFORE linking: {before.TypeLines.Count} rows, unmatched DPL={before.UnmatchedDplGroups.Count}");
    foreach (var u in before.UnmatchedDplGroups)
        Console.WriteLine($"  ? {u.DisplayLabel} -> suggest: {u.SuggestedInvoiceDescription ?? "—"} (score {u.SuggestionScore})");

    var sessionLinks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    foreach (var u in before.UnmatchedDplGroups)
    {
        if (!string.IsNullOrWhiteSpace(u.SuggestedInvoiceMatchKey))
            sessionLinks[u.DplMatchKey] = u.SuggestedInvoiceMatchKey;
    }

    var after = ChinaImportCrossFileMatcher.BuildSession(rollDetail, inv, plDto, new ChinaImportMatchContext
    {
        SessionDplToInvoiceKeys = sessionLinks
    });

    Console.WriteLine($"AFTER linking: {after.TypeLines.Count} rows, unmatched DPL={after.UnmatchedDplGroups.Count}");
    var fullyMatched = after.TypeLines.Count(t => t.HasInvoice && t.HasPackingSummary && t.HasDpl && t.MismatchWarnings.Count == 0);
    Console.WriteLine($"Fully matched (✅): {fullyMatched}/{inv.Lines.Count}");
    foreach (var t in after.TypeLines)
        Console.WriteLine($"  [{t.LineIndex}] {t.TypeDisplayName} | {t.MatchStatusDisplay} | DPL={t.HasDpl}");

    return fullyMatched == inv.Lines.Count && after.UnmatchedDplGroups.Count == 0 ? 0 : 2;
}

var filePath = args.FirstOrDefault(a => !a.StartsWith('-'))
    ?? @"C:\Users\Homsi\Desktop\ALamal-AB\COLOMBIA.xls";

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    return 1;
}

if (safetyTest)
{
    return SafetyCheck.Run(filePath);
}

if (dumpMode)
{
    VerboseDump.Run(filePath);
    return 0;
}

var bytes = await File.ReadAllBytesAsync(filePath);
Console.WriteLine($"File: {filePath}");
Console.WriteLine($"Size: {bytes.Length:N0} bytes");
Console.WriteLine($"Format: {ExcelFileFormatDetector.Detect(bytes, filePath)}");
Console.WriteLine($"Log:  {PackingListImportLogger.LogFilePath}");

using (var sheet = ExcelWorksheetReaderFactory.Open(bytes, filePath))
{
    Console.WriteLine($"FirstRowUsed: {sheet.FirstRowUsed}");
    Console.WriteLine($"LastRowUsed:  {sheet.LastRowUsed}");
    Console.WriteLine($"Row span:     {sheet.LastRowUsed - sheet.FirstRowUsed + 1:N0}");
}

Console.WriteLine($"Starting parse with {timeoutSeconds}s timeout...");
var parseTask = Task.Run(() =>
{
    using var sheet = ExcelWorksheetReaderFactory.Open(bytes, filePath);
    var sw = Stopwatch.StartNew();
    var result = PackingListExcelParser.Parse(sheet);
    sw.Stop();
    return (result, sw.Elapsed);
});

if (await Task.WhenAny(parseTask, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds))) != parseTask)
{
    Console.WriteLine($"TIMEOUT after {timeoutSeconds}s — parse did not complete.");
    return 2;
}

var (output, elapsed) = await parseTask;
Console.WriteLine($"Parse completed in {elapsed.TotalMilliseconds:N0} ms");
Console.WriteLine($"Groups: {output.Groups.Count}");
Console.WriteLine($"Rolls:  {output.Groups.Sum(g => g.Rolls.Count)}");
Console.WriteLine($"Valid:  {output.Groups.Sum(g => g.Rolls.Count(r => r.IsValid))}");
Console.WriteLine($"Grand:  {output.DeclaredGrandMeters} m / {output.DeclaredGrandRolls} rolls");
return 0;
