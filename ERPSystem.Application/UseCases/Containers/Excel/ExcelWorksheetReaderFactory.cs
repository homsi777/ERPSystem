namespace ERPSystem.Application.UseCases.Containers.Excel;

internal static class ExcelWorksheetReaderFactory
{
    public static IWorksheetReader Open(byte[] content, string? fileName)
    {
        var format = ExcelFileFormatDetector.Detect(content, fileName);
        return format switch
        {
            ExcelFileFormat.LegacyXls => new NpoiWorksheetReader(content),
            ExcelFileFormat.OpenXml => new ClosedXmlWorksheetReader(content),
            _ => throw new UnsupportedExcelFormatException(ExcelFileFormatDetector.UnsupportedFormatMessage)
        };
    }
}

internal sealed class UnsupportedExcelFormatException(string message) : Exception(message);
