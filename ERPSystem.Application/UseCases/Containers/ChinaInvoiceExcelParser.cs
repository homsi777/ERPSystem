using System.Globalization;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

internal static class ChinaInvoiceExcelParser
{
    public static ChinaInvoiceParseResultDto Parse(IWorksheetReader sheet, string fileName)
    {
        var lines = new List<ChinaInvoiceLineDto>();
        decimal seaFreight = 0m;
        decimal insurance = 0m;
        decimal grandTotal = 0m;
        decimal declaredMeters = 0m;
        int declaredRolls = 0;

        var firstRow = sheet.FirstRowUsed;
        var lastRow = sheet.LastRowUsed;
        if (firstRow <= 0 || lastRow <= 0)
        {
            return new ChinaInvoiceParseResultDto { FileName = fileName };
        }

        var dataStartRow = FindDataStartRow(sheet, firstRow, lastRow);
        var lineIndex = 0;

        for (var row = dataStartRow; row <= lastRow; row++)
        {
            var description = FindDescriptionCell(sheet, row);
            if (string.IsNullOrWhiteSpace(description))
                continue;

            if (ChinaImportTypeNameNormalizer.RowContainsAny(description, "SEA FREIGHT"))
            {
                seaFreight = ReadAmount(sheet, row);
                continue;
            }

            if (ChinaImportTypeNameNormalizer.RowContainsAny(description, "INSURANCE"))
            {
                insurance = ReadAmount(sheet, row);
                continue;
            }

            if (ChinaImportTypeNameNormalizer.RowContainsAny(description, "TOTAL:"))
            {
                ReadTotalsRow(sheet, row, ref declaredMeters, ref declaredRolls);
                grandTotal = ReadAmount(sheet, row);
                continue;
            }

            if (!ChinaImportTypeNameNormalizer.IsDataRowLabel(description))
                continue;

            if (!TryParseLineRow(sheet, row, description, ++lineIndex, out var line))
                continue;

            lines.Add(line);
        }

        var lineSum = lines.Sum(l => l.LineAmountUsd) + seaFreight + insurance;
        var amountsMatch = grandTotal <= 0 ||
                           Math.Abs(lineSum - grandTotal) <= Math.Max(1m, grandTotal * 0.01m);

        return new ChinaInvoiceParseResultDto
        {
            FileName = fileName,
            Lines = lines,
            SeaFreightUsd = seaFreight,
            InsuranceUsd = insurance,
            GrandTotalUsd = grandTotal,
            DeclaredTotalMeters = declaredMeters,
            DeclaredTotalRolls = declaredRolls,
            LineAmountsMatchTotal = amountsMatch,
            TotalValidationWarning = amountsMatch || grandTotal <= 0
                ? null
                : $"مجموع البنود ({lineSum:N2}) يختلف عن الإجمالي ({grandTotal:N2}) — تحذير فقط."
        };
    }

    private static int FindDataStartRow(IWorksheetReader sheet, int firstRow, int lastRow)
    {
        for (var row = firstRow; row <= Math.Min(lastRow, firstRow + 30); row++)
        {
            for (var col = 1; col <= sheet.GetLastColumnUsed(row); col++)
            {
                var text = sheet.GetCellString(row, col);
                if (ChinaImportTypeNameNormalizer.RowContainsAny(text,
                        "Quantities and Descriptions", "品  名", "Descriptions"))
                    return row + 2;
            }
        }

        return firstRow + 20;
    }

    private static string FindDescriptionCell(IWorksheetReader sheet, int row)
    {
        for (var col = 1; col <= Math.Min(4, sheet.GetLastColumnUsed(row)); col++)
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
        out ChinaInvoiceLineDto line)
    {
        line = null!;
        var lastCol = sheet.GetLastColumnUsed(row);

        decimal meters = 0m;
        int rolls = 0;
        decimal unitPrice = 0m;
        decimal amount = 0m;

        for (var col = 1; col <= lastCol; col++)
        {
            var text = sheet.GetCellString(row, col);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var upper = text.ToUpperInvariant();
            if (upper is "MTS" or "M" && col > 1)
            {
                var prev = sheet.GetCellString(row, col - 1);
                ChinaImportTypeNameNormalizer.TryParseDecimal(prev, out meters);
            }
            else if (upper is "ROLLS" or "ROLL" && col > 1)
            {
                var prev = sheet.GetCellString(row, col - 1);
                ChinaImportTypeNameNormalizer.TryParseInt(prev, out rolls);
            }
            else if (upper is "/M" && col > 1)
            {
                var prev = sheet.GetCellString(row, col - 1);
                ChinaImportTypeNameNormalizer.TryParseDecimal(prev, out unitPrice);
            }
        }

        amount = ReadAmount(sheet, row);
        if (meters <= 0 && rolls <= 0 && amount <= 0)
            return false;

        if (unitPrice <= 0 && meters > 0 && amount > 0)
            unitPrice = Math.Round(amount / meters, 4);

        line = new ChinaInvoiceLineDto
        {
            LineIndex = lineIndex,
            Description = description,
            MatchKey = ChinaImportTypeNameNormalizer.BuildDescriptionMatchKey(description),
            LengthMeters = meters,
            RollCount = rolls,
            UnitPriceUsd = unitPrice,
            LineAmountUsd = amount
        };
        return true;
    }

    private static void ReadTotalsRow(IWorksheetReader sheet, int row, ref decimal meters, ref int rolls)
    {
        var lastCol = sheet.GetLastColumnUsed(row);
        for (var col = 1; col <= lastCol; col++)
        {
            var text = sheet.GetCellString(row, col);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var upper = text.ToUpperInvariant();
            if (upper is "MTS" or "M" && col > 1)
            {
                ChinaImportTypeNameNormalizer.TryParseDecimal(sheet.GetCellString(row, col - 1), out meters);
            }
            else if (upper is "ROLLS" or "ROLL" && col > 1)
            {
                ChinaImportTypeNameNormalizer.TryParseInt(sheet.GetCellString(row, col - 1), out rolls);
            }
        }
    }

    private static decimal ReadAmount(IWorksheetReader sheet, int row)
    {
        var lastCol = sheet.GetLastColumnUsed(row);
        for (var col = lastCol; col >= 1; col--)
        {
            var text = sheet.GetCellString(row, col);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (text.Equals("USD", StringComparison.OrdinalIgnoreCase))
                continue;

            if (ChinaImportTypeNameNormalizer.TryParseDecimal(text, out var value) && value > 0)
                return value;
        }

        return 0m;
    }
}
