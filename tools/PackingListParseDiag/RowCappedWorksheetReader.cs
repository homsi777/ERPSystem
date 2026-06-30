using ERPSystem.Application.UseCases.Containers.Excel;

internal sealed class RowCappedWorksheetReader(IWorksheetReader inner, int maxRow) : IWorksheetReader
{
    public int FirstRowUsed => inner.FirstRowUsed;

    public int LastRowUsed => Math.Min(inner.LastRowUsed, maxRow);

    public string GetCellString(int row, int col) => inner.GetCellString(row, col);

    public IEnumerable<string> GetUsedCellStrings(int row) => inner.GetUsedCellStrings(row);

    public int GetLastColumnUsed(int row) => inner.GetLastColumnUsed(row);

    public void Dispose() => inner.Dispose();
}
