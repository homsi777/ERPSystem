namespace ERPSystem.Application.UseCases.Containers.Excel;

/// <summary>
/// Format-agnostic read-only access to the first worksheet (1-based row/column indices).
/// </summary>
internal interface IWorksheetReader : IDisposable
{
    int FirstRowUsed { get; }
    int LastRowUsed { get; }
    int GetLastColumnUsed(int rowNumber);
    string GetCellString(int rowNumber, int columnNumber);
    IEnumerable<string> GetUsedCellStrings(int rowNumber);
}
