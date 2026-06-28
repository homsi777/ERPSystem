namespace ERPSystem.Application.UseCases.Containers;

internal static class PackingListImportLogger
{
    private static readonly object Lock = new();
    private static readonly string LogPath = Path.Combine(
        Path.GetTempPath(),
        "erp-china-import.log");

    public static void Stage(string stage, string? detail = null)
    {
        var line = $"{DateTime.UtcNow:O}\t{stage}\t{detail ?? ""}";
        lock (Lock)
        {
            try
            {
                File.AppendAllText(LogPath, line + Environment.NewLine);
            }
            catch
            {
                // Logging must never break import.
            }
        }
    }

    public static string LogFilePath => LogPath;
}
