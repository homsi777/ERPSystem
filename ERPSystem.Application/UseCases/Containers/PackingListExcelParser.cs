using System.Globalization;
using System.Text.RegularExpressions;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

internal sealed class PackingListExcelParser
{
    private const decimal MetersToleranceAbsolute = 0.05m;
    private static readonly Regex GrandTotalRegex = new(
        @"TOTAL\s*[:=]?\s*([\d,.]+)\s*MTS\s*/\s*(\d+)\s*ROLLS?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal sealed class ParseOutput
    {
        public string? SupplierNameFromFile { get; set; }
        public decimal? DeclaredGrandMeters { get; set; }
        public int? DeclaredGrandRolls { get; set; }
        public List<PackingListGroupBuilder> Groups { get; init; } = [];
    }

    internal sealed class PackingListGroupBuilder
    {
        public int GroupIndex { get; init; }
        public string FabricCode { get; set; } = "";
        public string Color { get; set; } = "";
        public decimal DeclaredTotalMeters { get; set; }
        public int DeclaredTotalRolls { get; set; }
        public List<PackingListRollDto> Rolls { get; } = [];
    }

    public static ParseOutput Parse(IWorksheetReader sheet)
    {
        var output = new ParseOutput();
        var firstRow = sheet.FirstRowUsed;
        var lastRow = sheet.LastRowUsed;
        if (firstRow <= 0 || lastRow <= 0)
            return output;

        var sequence = 0;
        PackingListGroupBuilder? currentGroup = null;
        IReadOnlyList<int>? columnBlocks = null;

        for (var rowNum = firstRow; rowNum <= lastRow; rowNum++)
        {
            if (TryParseGrandTotalRow(sheet, rowNum, out var grandMeters, out var grandRolls))
            {
                output.DeclaredGrandMeters ??= grandMeters;
                output.DeclaredGrandRolls ??= grandRolls;
                continue;
            }

            if (output.SupplierNameFromFile is null && rowNum <= firstRow + 5)
            {
                var candidate = sheet.GetCellString(rowNum, 1);
                if (!string.IsNullOrWhiteSpace(candidate) &&
                    !candidate.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase) &&
                    !IsColumnHeaderRow(sheet, rowNum))
                {
                    output.SupplierNameFromFile = candidate;
                }
            }

            if (IsColumnHeaderRow(sheet, rowNum))
            {
                columnBlocks = DetectColumnBlocks(sheet, rowNum);
                continue;
            }

            if (IsGroupTotalRow(sheet, rowNum))
            {
                columnBlocks = null;
                continue;
            }

            if (TryParseGroupHeaderRow(sheet, rowNum, out var fabricCode, out var color, out var declaredMeters, out var declaredRolls))
            {
                currentGroup = new PackingListGroupBuilder
                {
                    GroupIndex = output.Groups.Count + 1,
                    FabricCode = fabricCode,
                    Color = color,
                    DeclaredTotalMeters = declaredMeters,
                    DeclaredTotalRolls = declaredRolls
                };
                output.Groups.Add(currentGroup);
                columnBlocks = null;
                continue;
            }

            if (currentGroup is null || columnBlocks is null || columnBlocks.Count == 0)
                continue;

            foreach (var blockStart in columnBlocks)
            {
                var rollText = sheet.GetCellString(rowNum, blockStart);
                var qtyText = sheet.GetCellString(rowNum, blockStart + 1);
                var lotText = sheet.GetCellString(rowNum, blockStart + 2);

                if (string.IsNullOrWhiteSpace(rollText) &&
                    string.IsNullOrWhiteSpace(qtyText) &&
                    string.IsNullOrWhiteSpace(lotText))
                    continue;

                if (rollText.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase))
                    continue;

                var reasons = new List<string>();
                var rollNumber = 0;
                if (!TryParseInt(rollText, out rollNumber) || rollNumber <= 0)
                    reasons.Add("رقم التوب غير صالح");

                if (!TryParseDecimal(qtyText, out var quantity) || quantity <= 0)
                    reasons.Add("الكمية بالمتر غير صالحة");

                sequence++;
                currentGroup.Rolls.Add(new PackingListRollDto
                {
                    SequenceNumber = sequence,
                    GroupIndex = currentGroup.GroupIndex,
                    RollNumber = rollNumber,
                    QuantityMeters = quantity,
                    LotCode = lotText.Trim(),
                    IsValid = reasons.Count == 0,
                    InvalidReason = reasons.Count == 0 ? null : string.Join("؛ ", reasons)
                });
            }
        }

        return output;
    }

    /// <summary>
    /// Scans every used cell on the column-header row; each cell whose normalized text
    /// equals "ROLL NUMBER" starts a 3-column block (Roll | Quantity(M) | Lot).
    /// Block count is therefore the number of such headers found — never hardcoded.
    /// </summary>
    public static IReadOnlyList<int> DetectColumnBlocks(IWorksheetReader sheet, int rowNumber)
    {
        var blocks = new List<int>();
        var lastCol = sheet.GetLastColumnUsed(rowNumber);
        for (var col = 1; col <= lastCol; col++)
        {
            if (NormalizeHeader(sheet.GetCellString(rowNumber, col)) == "ROLL NUMBER")
                blocks.Add(col);
        }
        return blocks;
    }

    public static bool MetersApproximatelyEqual(decimal declared, decimal parsed)
    {
        if (declared <= 0)
            return parsed <= 0;
        var delta = Math.Abs(declared - parsed);
        return delta <= Math.Max(MetersToleranceAbsolute, declared * 0.001m);
    }

    private static bool TryParseGrandTotalRow(IWorksheetReader sheet, int rowNumber, out decimal meters, out int rolls)
    {
        meters = 0;
        rolls = 0;
        foreach (var text in sheet.GetUsedCellStrings(rowNumber))
        {
            var match = GrandTotalRegex.Match(text);
            if (!match.Success)
                continue;

            if (TryParseDecimal(match.Groups[1].Value, out meters) &&
                TryParseInt(match.Groups[2].Value, out rolls))
                return true;
        }
        return false;
    }

    private static bool TryParseGroupHeaderRow(
        IWorksheetReader sheet,
        int rowNumber,
        out string fabricCode,
        out string color,
        out decimal declaredMeters,
        out int declaredRolls)
    {
        fabricCode = "";
        color = "";
        declaredMeters = 0;
        declaredRolls = 0;

        if (IsColumnHeaderRow(sheet, rowNumber) || IsGroupTotalRow(sheet, rowNumber))
            return false;

        fabricCode = sheet.GetCellString(rowNumber, 1).Trim();
        color = sheet.GetCellString(rowNumber, 2).Trim();

        if (string.IsNullOrWhiteSpace(fabricCode) || string.IsNullOrWhiteSpace(color))
            return false;

        if (fabricCode.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase) ||
            NormalizeHeader(fabricCode) == "ROLL NUMBER")
            return false;

        var lastCol = sheet.GetLastColumnUsed(rowNumber);
        for (var col = 2; col <= lastCol; col++)
        {
            if (NormalizeHeader(sheet.GetCellString(rowNumber, col)) == "MTS")
            {
                TryParseDecimal(sheet.GetCellString(rowNumber, col - 1), out declaredMeters);
                break;
            }
        }

        for (var col = lastCol; col >= 1; col--)
        {
            var text = sheet.GetCellString(rowNumber, col);
            if (TryParseInt(text, out var rolls) && rolls > 0)
            {
                declaredRolls = rolls;
                break;
            }
        }

        return declaredMeters > 0 || declaredRolls > 0;
    }

    private static bool IsColumnHeaderRow(IWorksheetReader sheet, int rowNumber) =>
        DetectColumnBlocks(sheet, rowNumber).Count > 0;

    private static bool IsGroupTotalRow(IWorksheetReader sheet, int rowNumber)
    {
        var first = sheet.GetCellString(rowNumber, 1);
        if (string.IsNullOrWhiteSpace(first))
            return false;

        return first.StartsWith("TOTAL:", StringComparison.OrdinalIgnoreCase) ||
               (first.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase) &&
                first.Contains(':') &&
                !GrandTotalRegex.IsMatch(first));
    }

    private static string NormalizeHeader(string value) =>
        Regex.Replace(value.Trim(), @"\s+", " ").ToUpperInvariant();

    private static bool TryParseInt(string value, out int result)
    {
        value = value.Replace(",", "").Trim();
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out result) ||
               int.TryParse(value, out result);
    }

    private static bool TryParseDecimal(string value, out decimal result)
    {
        value = value.Replace(",", "").Trim();
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result) ||
               decimal.TryParse(value, out result);
    }
}
