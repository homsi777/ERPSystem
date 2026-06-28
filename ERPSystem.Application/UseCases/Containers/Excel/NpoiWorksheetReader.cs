using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace ERPSystem.Application.UseCases.Containers.Excel;

internal sealed class NpoiWorksheetReader : IWorksheetReader
{
    private readonly HSSFWorkbook _workbook;
    private readonly ISheet _sheet;
    private readonly DataFormatter _formatter = new();

    public NpoiWorksheetReader(byte[] content)
    {
        _workbook = new HSSFWorkbook(new MemoryStream(content));
        if (_workbook.NumberOfSheets == 0)
            throw new InvalidOperationException("Workbook has no worksheets.");
        _sheet = _workbook.GetSheetAt(0);
    }

    public int FirstRowUsed
    {
        get
        {
            var first = _sheet.FirstRowNum;
            var last = _sheet.LastRowNum;
            for (var i = first; i <= last; i++)
            {
                if (_sheet.GetRow(i) is not null)
                    return i + 1;
            }
            return 0;
        }
    }

    public int LastRowUsed => _sheet.LastRowNum >= 0 ? _sheet.LastRowNum + 1 : 0;

    public int GetLastColumnUsed(int rowNumber)
    {
        var row = _sheet.GetRow(rowNumber - 1);
        if (row is null)
            return 0;

        var lastCell = row.LastCellNum;
        return lastCell > 0 ? lastCell : 0;
    }

    public string GetCellString(int rowNumber, int columnNumber)
    {
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

        var first = Math.Max(0, (int)row.FirstCellNum);
        var last = row.LastCellNum;
        for (var col = first; col < last; col++)
        {
            var cell = row.GetCell(col);
            if (cell is null)
                continue;

            var text = _formatter.FormatCellValue(cell).Trim();
            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    public void Dispose() => _workbook.Close();
}
