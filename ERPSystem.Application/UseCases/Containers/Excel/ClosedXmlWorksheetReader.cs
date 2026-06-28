using System.Globalization;
using ClosedXML.Excel;

namespace ERPSystem.Application.UseCases.Containers.Excel;

internal sealed class ClosedXmlWorksheetReader : IWorksheetReader
{
    private readonly XLWorkbook _workbook;
    private readonly IXLWorksheet _worksheet;

    public ClosedXmlWorksheetReader(byte[] content)
    {
        _workbook = new XLWorkbook(new MemoryStream(content));
        _worksheet = _workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("Workbook has no worksheets.");
    }

    public int FirstRowUsed => _worksheet.FirstRowUsed()?.RowNumber() ?? 0;

    public int LastRowUsed => _worksheet.LastRowUsed()?.RowNumber() ?? 0;

    public int GetLastColumnUsed(int rowNumber)
    {
        var row = _worksheet.Row(rowNumber);
        return row.LastCellUsed()?.Address.ColumnNumber ?? 0;
    }

    public string GetCellString(int rowNumber, int columnNumber)
    {
        var cell = _worksheet.Row(rowNumber).Cell(columnNumber);
        if (cell.IsEmpty())
            return "";

        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
            XLDataType.DateTime => cell.GetDateTime().ToString(CultureInfo.InvariantCulture),
            _ => cell.GetString().Trim()
        };
    }

    public IEnumerable<string> GetUsedCellStrings(int rowNumber)
    {
        foreach (var cell in _worksheet.Row(rowNumber).CellsUsed())
        {
            var text = cell.DataType switch
            {
                XLDataType.Number => cell.GetDouble().ToString(CultureInfo.InvariantCulture),
                XLDataType.DateTime => cell.GetDateTime().ToString(CultureInfo.InvariantCulture),
                _ => cell.GetString().Trim()
            };

            if (!string.IsNullOrWhiteSpace(text))
                yield return text;
        }
    }

    public void Dispose() => _workbook.Dispose();
}
