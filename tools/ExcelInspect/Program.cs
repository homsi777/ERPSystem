using ERPSystem.Application.UseCases.Containers.Excel;
var files = new[] { "INVOICE-(126C).xlsx", "PL-(126C).xlsx" };
foreach (var f in files) {
  Console.WriteLine("=== " + f + " ===");
  using var r = ExcelWorksheetReaderFactory.Open(File.ReadAllBytes(f), f);
  for (int row = 1; row <= Math.Min(r.LastRowUsed, 35); row++) {
    var cells = new List<string>();
    for (int col = 1; col <= Math.Min(r.LastColumnUsed, 12); col++) {
      var t = r.GetCellString(row, col);
      if (!string.IsNullOrWhiteSpace(t)) cells.Add($"[{col}]{t}");
    }
    if (cells.Count > 0) Console.WriteLine($"R{row}: {string.Join(" | ", cells)}");
  }
}
