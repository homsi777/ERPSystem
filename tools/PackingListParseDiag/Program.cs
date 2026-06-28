using System.Diagnostics;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;

const int timeoutSeconds = 10;
var filePath = args.Length > 0 ? args[0] : @"C:\Users\Homsi\Desktop\ALamal-AB\COLOMBIA.xls";

if (!File.Exists(filePath))
{
    Console.WriteLine($"File not found: {filePath}");
    return 1;
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
return 0;
