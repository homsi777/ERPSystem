using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace ERPSystem.Application.UseCases.Containers.Excel;

internal sealed class NpoiWorksheetReader : IWorksheetReader
{
    private const int MaxRowCap = 20_000;
    private const int MaxColumnCap = 512;

    private readonly HSSFWorkbook _workbook;
    private readonly ISheet _sheet;
    private readonly DataFormatter _formatter = new();
    private readonly int _firstRowUsed;
    private readonly int _lastRowUsed;

    public NpoiWorksheetReader(byte[] content)
    {
        _workbook = new HSSFWorkbook(new MemoryStream(content));
        if (_workbook.NumberOfSheets == 0)
            throw new InvalidOperationException("Workbook has no worksheets.");
        _sheet = _workbook.GetSheetAt(0);

        (_firstRowUsed, _lastRowUsed) = ComputeUsedRowRange(_sheet);
        PackingListImportLogger.Stage(
            "npoi-open",
            $"FirstRow={_firstRowUsed} LastRow={_lastRowUsed} SheetLastRowNum={_sheet.LastRowNum} PhysicalRows={_sheet.PhysicalNumberOfRows}");
    }

    public int FirstRowUsed => _firstRowUsed;

    public int LastRowUsed => _lastRowUsed;

    public int GetLastColumnUsed(int rowNumber)
    {
        var row = _sheet.GetRow(rowNumber - 1);
        if (row is null)
            return 0;

        var maxCol = 0;
        foreach (ICell cell in row)
        {
            if (cell is null)
                continue;
            maxCol = Math.Max(maxCol, cell.ColumnIndex + 1);
        }

        return Math.Min(maxCol, MaxColumnCap);
    }

    public string GetCellString(int rowNumber, int columnNumber)
    {
        if (columnNumber <= 0 || columnNumber > MaxColumnCap)
            return "";

        var row = _sheet.GetRow(rowNumber - 1);
        if (row is null)
            return "";

        var cell = row.GetCell(columnNumber - 1);
        if (cell is null)
            return "";

        return _formatter.FormatCellValue(cell).Trim();
    }

    public IEnumerable<string> GetUsedCellStrings(int rowNumber)
    {
        var row = _sheet.GetRow(rowNumber - 1);
        if (row is null)
            yield break;

        foreach (ICell cell in row)
        {
            if (cell is null)
                continue;

            var text = _formatter.FormatCellValue(cell).Trim();
            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    public void Dispose() => _workbook.Close();

    /// <summary>
    /// NPOI's <see cref="ISheet.LastRowNum"/> reflects the spreadsheet's allocated/dimension
    /// range, which can be inflated far beyond physical rows (e.g. 65535). Iterating 1..LastRowNum
    /// then scans empty rows but still calls GetLastColumnUsed using LastCellNum — on sparse .xls
    /// files that can mean tens of millions of cell reads and a UI freeze. Physical-row bounds
    /// match actual content.
    /// </summary>
    private static (int First, int Last) ComputeUsedRowRange(ISheet sheet)
    {
        var first = 0;
        var last = 0;
        var found = false;

        foreach (IRow row in sheet)
        {
            if (row is null || row.PhysicalNumberOfCells == 0)
                continue;

            var rowNumber = row.RowNum + 1;
            if (!found)
            {
                first = rowNumber;
                found = true;
            }
            last = rowNumber;
        }

        if (!found)
            return (0, 0);

        if (last - first + 1 > MaxRowCap)
            last = first + MaxRowCap - 1;

        return (first, last);
    }
}
