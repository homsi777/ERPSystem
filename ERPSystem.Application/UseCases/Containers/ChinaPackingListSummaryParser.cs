using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

internal static class ChinaPackingListSummaryParser
{
    public static ChinaPackingSummaryParseResultDto Parse(IWorksheetReader sheet, string fileName)
    {
        var lines = new List<ChinaPackingSummaryLineDto>();
        decimal totalMeters = 0m;
        int totalRolls = 0;
        decimal totalCbm = 0m;
        decimal totalGross = 0m;
        decimal totalNet = 0m;

        var firstRow = sheet.FirstRowUsed;
        var lastRow = sheet.LastRowUsed;
        if (firstRow <= 0 || lastRow <= 0)
            return new ChinaPackingSummaryParseResultDto { FileName = fileName };

        var dataStartRow = FindDataStartRow(sheet, firstRow, lastRow);
        var lineIndex = 0;

        for (var row = dataStartRow; row <= lastRow; row++)
        {
            var description = FindDescriptionCell(sheet, row);
            if (string.IsNullOrWhiteSpace(description))
                continue;

            if (ChinaImportTypeNameNormalizer.RowContainsAny(description, "TOTAL:"))
            {
                ReadTotalsRow(sheet, row, ref totalRolls, ref totalMeters, ref totalCbm, ref totalGross, ref totalNet);
                continue;
            }

            if (!ChinaImportTypeNameNormalizer.IsDataRowLabel(description))
                continue;

            if (!TryParseLineRow(sheet, row, description, ++lineIndex, out var line))
                continue;

            lines.Add(line);
        }

        if (totalMeters <= 0)
            totalMeters = lines.Sum(l => l.LengthMeters);
        if (totalRolls <= 0)
            totalRolls = lines.Sum(l => l.RollCount);
        if (totalCbm <= 0)
            totalCbm = lines.Sum(l => l.Cbm);
        if (totalGross <= 0)
            totalGross = lines.Sum(l => l.GrossWeightKg);
        if (totalNet <= 0)
            totalNet = lines.Sum(l => l.NetWeightKg);

        return new ChinaPackingSummaryParseResultDto
        {
            FileName = fileName,
            Lines = lines,
            DeclaredTotalMeters = totalMeters,
            DeclaredTotalRolls = totalRolls,
            TotalCbm = totalCbm,
            TotalGrossWeightKg = totalGross,
            TotalNetWeightKg = totalNet
        };
    }

    private static int FindDataStartRow(IWorksheetReader sheet, int firstRow, int lastRow)
    {
        for (var row = firstRow; row <= Math.Min(lastRow, firstRow + 25); row++)
        {
            for (var col = 1; col <= sheet.GetLastColumnUsed(row); col++)
            {
                var text = sheet.GetCellString(row, col);
                if (ChinaImportTypeNameNormalizer.RowContainsAny(text,
                        "Article and Descriptions", "PACKAGE", "QUANTITY"))
                    return row + 2;
            }
        }

        return firstRow + 15;
    }

    private static string FindDescriptionCell(IWorksheetReader sheet, int row)
    {
        for (var col = 1; col <= 3; col++)
        {
            var text = sheet.GetCellString(row, col)?.Trim();
            if (!string.IsNullOrWhiteSpace(text) &&
                !text.Equals("N/M", StringComparison.OrdinalIgnoreCase))
                return text;
        }

        return sheet.GetCellString(row, 2)?.Trim() ?? "";
    }

    private static bool TryParseLineRow(
        IWorksheetReader sheet,
        int row,
        string description,
        int lineIndex,
        out ChinaPackingSummaryLineDto line)
    {
        line = null!;
        var lastCol = sheet.GetLastColumnUsed(row);

        int rolls = 0;
        decimal meters = 0m;
        decimal cbm = 0m;
        decimal gross = 0m;
        decimal net = 0m;

        for (var col = 1; col <= lastCol; col++)
        {
            var text = sheet.GetCellString(row, col);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var upper = text.ToUpperInvariant();
            if (upper is "ROLLS" or "ROLL" && col > 1)
                ChinaImportTypeNameNormalizer.TryParseInt(sheet.GetCellString(row, col - 1), out rolls);
            else if (upper is "MTS" or "M" && col > 1)
                ChinaImportTypeNameNormalizer.TryParseDecimal(sheet.GetCellString(row, col - 1), out meters);
            else if (upper is "CBM" && col > 1)
                ChinaImportTypeNameNormalizer.TryParseDecimal(sheet.GetCellString(row, col - 1), out cbm);
            else if (upper is "KGS" or "KG")
            {
                var prev = sheet.GetCellString(row, col - 1);
                if (gross <= 0 && ChinaImportTypeNameNormalizer.TryParseDecimal(prev, out gross))
                    continue;
                if (net <= 0 && ChinaImportTypeNameNormalizer.TryParseDecimal(prev, out net))
                    continue;
            }
        }

        if (rolls <= 0 && meters <= 0)
            return false;

        line = new ChinaPackingSummaryLineDto
        {
            LineIndex = lineIndex,
            Description = description,
            MatchKey = ChinaImportTypeNameNormalizer.BuildDescriptionMatchKey(description),
            RollCount = rolls,
            LengthMeters = meters,
            Cbm = cbm,
            GrossWeightKg = gross,
            NetWeightKg = net > 0 ? net : gross
        };
        return true;
    }

    private static void ReadTotalsRow(
        IWorksheetReader sheet,
        int row,
        ref int rolls,
        ref decimal meters,
        ref decimal cbm,
        ref decimal gross,
        ref decimal net)
    {
        var lastCol = sheet.GetLastColumnUsed(row);
        for (var col = 1; col <= lastCol; col++)
        {
            var text = sheet.GetCellString(row, col);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var upper = text.ToUpperInvariant();
            if (upper is "ROLLS" or "ROLL" && col > 1)
                ChinaImportTypeNameNormalizer.TryParseInt(sheet.GetCellString(row, col - 1), out rolls);
            else if (upper is "MTS" or "M" && col > 1)
                ChinaImportTypeNameNormalizer.TryParseDecimal(sheet.GetCellString(row, col - 1), out meters);
            else if (upper is "CBM" && col > 1)
                ChinaImportTypeNameNormalizer.TryParseDecimal(sheet.GetCellString(row, col - 1), out cbm);
            else if (upper is "KGS" or "KG")
            {
                var prev = sheet.GetCellString(row, col - 1);
                if (gross <= 0)
                    ChinaImportTypeNameNormalizer.TryParseDecimal(prev, out gross);
                else if (net <= 0)
                    ChinaImportTypeNameNormalizer.TryParseDecimal(prev, out net);
            }
        }
    }
}
