namespace ERPSystem.Application.UseCases.Containers.Excel;

internal enum ExcelFileFormat
{
    LegacyXls,
    OpenXml,
    Unsupported
}

internal static class ExcelFileFormatDetector
{
    private static readonly byte[] OleMagic = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public const string UnsupportedFormatMessage =
        "صيغة الملف غير مدعومة، يرجى استخدام xls أو xlsx";

    public const string UnreadableFileMessage =
        "تعذّر قراءة الملف. تأكد أن الملف سليم وبالصيغة xls أو xlsx";

    public static ExcelFileFormat Detect(ReadOnlySpan<byte> content, string? fileName)
    {
        if (content.Length >= OleMagic.Length && content[..OleMagic.Length].SequenceEqual(OleMagic))
            return ExcelFileFormat.LegacyXls;

        if (content.Length >= 2 && content[0] == 0x50 && content[1] == 0x4B)
            return ExcelFileFormat.OpenXml;

        var ext = Path.GetExtension(fileName ?? "").ToLowerInvariant();
        return ext switch
        {
            ".xls" => ExcelFileFormat.LegacyXls,
            ".xlsx" or ".xlsm" => ExcelFileFormat.OpenXml,
            _ => ExcelFileFormat.Unsupported
        };
    }
}
