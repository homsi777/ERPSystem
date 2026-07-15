using System.Globalization;
using System.Text.RegularExpressions;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers.Excel;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Containers;

internal sealed class PackingListExcelParser
{
    private const decimal MetersToleranceAbsolute = 0.05m;

    private static readonly Regex GrandTotalMtsFirstRegex = new(
        @"TOTAL\s*[:=]+\s*([\d,.]+)\s*MTS\s*/\s*(\d+)\s*ROLLS?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GrandTotalCompactMtsRegex = new(
        @"TOTAL\s*[:=]*\s*([\d,.]+)\s*MTS\s*/\s*(\d+)\s*ROLLS?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GrandTotalRollsFirstRegex = new(
        @"(\d+)\s*ROL{1,2}S?\s*/\s*([\d,.]+)\s*M(?:TS)?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GrandTotalYdsWithMtsRegex = new(
        @"TOTAL\s*[:=]*\s*([\d,.]+)\s*YDS?\s*\(\s*([\d,.]+)\s*MTS\s*\)\s*/\s*(\d+)\s*ROLLS?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex GrandTotalYdsOnlyRegex = new(
        @"TOTAL\s*[:=]*\s*([\d,.]+)\s*YDS?\s*/\s*(\d+)\s*ROLLS?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    internal sealed class ParseOutput
    {
        public string? SupplierNameFromFile { get; set; }
        public DplQuantityUnit DetectedQuantityUnit { get; set; } = DplQuantityUnit.Meters;
        public decimal? DeclaredGrandMeters { get; set; }
        public decimal? DeclaredGrandYards { get; set; }
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

    public static ParseOutput Parse(IWorksheetReader sheet, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        PackingListImportLogger.Stage("parse-start");
        var output = new ParseOutput();
        var firstRow = sheet.FirstRowUsed;
        var lastRow = sheet.LastRowUsed;
        if (firstRow <= 0 || lastRow <= 0)
        {
            PackingListImportLogger.Stage("parse-empty");
            return output;
        }

        PackingListImportLogger.Stage("parse-row-walk", $"rows={firstRow}-{lastRow}");

        ScanDeclaredGrandTotals(sheet, firstRow, lastRow, output);

        var sequence = 0;
        PackingListGroupBuilder? currentGroup = null;
        IReadOnlyList<int>? columnBlocks = null;
        DplQuantityUnit? fileQuantityUnit = output.DetectedQuantityUnit;
        var gridOnlyMode = false;

        for (var rowNum = firstRow; rowNum <= lastRow; rowNum++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (output.SupplierNameFromFile is null && rowNum <= firstRow + 5)
            {
                var candidate = sheet.GetCellString(rowNum, 1);
                if (!string.IsNullOrWhiteSpace(candidate) &&
                    !candidate.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase) &&
                    !IsColumnHeaderRow(sheet, rowNum) &&
                    !TryParseGrandTotalRow(sheet, rowNum, out _, out _, out _) &&
                    !TryParseGroupHeaderRow(sheet, rowNum, out _, out _, out _, out _))
                {
                    output.SupplierNameFromFile = candidate;
                }
            }

            if (IsColumnHeaderRow(sheet, rowNum))
            {
                columnBlocks = DetectColumnBlocks(sheet, rowNum);
                fileQuantityUnit ??= DetectQuantityUnitFromHeaderRow(sheet, rowNum);
                output.DetectedQuantityUnit = fileQuantityUnit ?? output.DetectedQuantityUnit;
                PackingListImportLogger.Stage(
                    "column-blocks",
                    $"row={rowNum} blocks={columnBlocks.Count} unit={output.DetectedQuantityUnit}");
                if (currentGroup is null)
                {
                    currentGroup = CreateGridGroup(output, sheet, firstRow);
                    gridOnlyMode = true;
                }

                continue;
            }

            if (IsGroupTotalRow(sheet, rowNum))
            {
                columnBlocks = null;
                PackingListImportLogger.Stage("section-total", $"row={rowNum}");
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
                gridOnlyMode = false;
                PackingListImportLogger.Stage("group-header", $"#{currentGroup.GroupIndex} {fabricCode}/{color}");
                continue;
            }

            if (currentGroup is null || columnBlocks is null || columnBlocks.Count == 0)
                continue;

            var quantityUnit = fileQuantityUnit ?? output.DetectedQuantityUnit;

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

                if (!TryParseDecimal(qtyText, out var nativeQuantity) || nativeQuantity <= 0)
                {
                    reasons.Add(quantityUnit == DplQuantityUnit.Yards
                        ? "الكمية باليارد غير صالحة"
                        : "الكمية بالمتر غير صالحة");
                }

                var quantityMeters = nativeQuantity > 0
                    ? DplQuantityConverter.ToMeters(nativeQuantity, quantityUnit)
                    : 0m;

                sequence++;
                currentGroup.Rolls.Add(new PackingListRollDto
                {
                    SequenceNumber = sequence,
                    GroupIndex = currentGroup.GroupIndex,
                    RollNumber = rollNumber,
                    QuantityNative = nativeQuantity,
                    QuantityUnit = quantityUnit,
                    QuantityMeters = quantityMeters,
                    LotCode = lotText.Trim(),
                    IsValid = reasons.Count == 0,
                    InvalidReason = reasons.Count == 0 ? null : string.Join("؛ ", reasons)
                });
            }
        }

        if (gridOnlyMode && output.Groups.Count == 1 && output.Groups[0].Rolls.Count > 0)
        {
            var gridGroup = output.Groups[0];
            gridGroup.DeclaredTotalRolls = gridGroup.Rolls.Count(r => r.IsValid);
            gridGroup.DeclaredTotalMeters = gridGroup.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters);
        }

        PackingListImportLogger.Stage(
            "parse-complete",
            $"groups={output.Groups.Count} rolls={output.Groups.Sum(g => g.Rolls.Count)} valid={output.Groups.Sum(g => g.Rolls.Count(r => r.IsValid))} unit={output.DetectedQuantityUnit}");
        return output;
    }

    /// <summary>
    /// Each roll-column header starts a 3-column block (Roll | Quantity | Lot).
    /// Supports supplier variants: ROLL NUMBER, ROLL NO, ROLL#, QUANTITY(M), QUANTITY(YDS), etc.
    /// </summary>
    public static IReadOnlyList<int> DetectColumnBlocks(IWorksheetReader sheet, int rowNumber)
    {
        var blocks = new List<int>();
        var lastCol = sheet.GetLastColumnUsed(rowNumber);
        for (var col = 1; col <= lastCol; col++)
        {
            if (IsRollBlockHeader(NormalizeHeader(sheet.GetCellString(rowNumber, col))))
                blocks.Add(col);
        }

        return blocks;
    }

    private static bool IsRollBlockHeader(string normalizedHeader) =>
        normalizedHeader is "ROLL NUMBER" or "ROLL NO" or "ROLL#" or "ROLL NO#";

    public static bool MetersApproximatelyEqual(decimal declared, decimal parsed)
    {
        if (declared <= 0)
            return parsed <= 0;
        var delta = Math.Abs(declared - parsed);
        return delta <= Math.Max(MetersToleranceAbsolute, declared * 0.001m);
    }

    public static bool RollsApproximatelyEqual(int declared, int parsed) =>
        declared <= 0 || declared == parsed;

    private static DplQuantityUnit? DetectQuantityUnitFromHeaderRow(IWorksheetReader sheet, int rowNumber)
    {
        var lastCol = sheet.GetLastColumnUsed(rowNumber);
        DplQuantityUnit? detected = null;
        for (var col = 1; col <= lastCol; col++)
        {
            var unit = DetectUnitFromQuantityHeader(NormalizeHeader(sheet.GetCellString(rowNumber, col)));
            if (unit is null)
                continue;

            if (detected is null)
                detected = unit;
            else if (detected != unit)
                return null;
        }

        return detected;
    }

    internal static DplQuantityUnit? DetectUnitFromQuantityHeader(string normalizedHeader)
    {
        if (string.IsNullOrWhiteSpace(normalizedHeader))
            return null;

        if (normalizedHeader.Contains("YDS", StringComparison.Ordinal) ||
            normalizedHeader.Contains("YARD", StringComparison.Ordinal))
            return DplQuantityUnit.Yards;

        if (normalizedHeader.Contains("(M)", StringComparison.Ordinal) ||
            normalizedHeader.Contains("MTS", StringComparison.Ordinal) ||
            normalizedHeader.Contains("MTR", StringComparison.Ordinal) ||
            normalizedHeader.Contains("METER", StringComparison.Ordinal))
            return DplQuantityUnit.Meters;

        return null;
    }

    private static void ScanDeclaredGrandTotals(IWorksheetReader sheet, int firstRow, int lastRow, ParseOutput output)
    {
        for (var rowNum = firstRow; rowNum <= lastRow; rowNum++)
        {
            if (!TryParseGrandTotalRow(sheet, rowNum, out var grandMeters, out var grandYards, out var grandRolls))
                continue;

            output.DeclaredGrandMeters ??= grandMeters;
            output.DeclaredGrandYards ??= grandYards;
            output.DeclaredGrandRolls ??= grandRolls;

            if (grandYards > 0 && output.DetectedQuantityUnit == DplQuantityUnit.Meters)
                output.DetectedQuantityUnit = DplQuantityUnit.Yards;

            PackingListImportLogger.Stage(
                "grand-total-scan",
                $"row={rowNum} meters={grandMeters} yards={grandYards} rolls={grandRolls}");
        }
    }

    private static PackingListGroupBuilder CreateGridGroup(ParseOutput output, IWorksheetReader sheet, int firstRow)
    {
        var label = sheet.GetCellString(firstRow, 1).Trim();
        if (string.IsNullOrWhiteSpace(label))
            label = "PACKINGLIST";

        var group = new PackingListGroupBuilder
        {
            GroupIndex = output.Groups.Count + 1,
            FabricCode = label,
            Color = ""
        };
        output.Groups.Add(group);
        PackingListImportLogger.Stage("grid-group", $"#{group.GroupIndex} label={label}");
        return group;
    }

    private static bool TryParseGrandTotalRow(
        IWorksheetReader sheet,
        int rowNumber,
        out decimal meters,
        out decimal yards,
        out int rolls)
    {
        meters = 0;
        yards = 0;
        rolls = 0;

        foreach (var text in sheet.GetUsedCellStrings(rowNumber))
        {
            var ydsWithMts = GrandTotalYdsWithMtsRegex.Match(text);
            if (ydsWithMts.Success &&
                TryParseDecimal(ydsWithMts.Groups[1].Value, out yards) &&
                TryParseDecimal(ydsWithMts.Groups[2].Value, out meters) &&
                TryParseInt(ydsWithMts.Groups[3].Value, out rolls))
                return true;

            var ydsOnly = GrandTotalYdsOnlyRegex.Match(text);
            if (ydsOnly.Success &&
                TryParseDecimal(ydsOnly.Groups[1].Value, out yards) &&
                TryParseInt(ydsOnly.Groups[2].Value, out rolls))
            {
                meters = DplQuantityConverter.ToMeters(yards, DplQuantityUnit.Yards);
                return true;
            }

            var match = GrandTotalMtsFirstRegex.Match(text);
            if (match.Success &&
                TryParseDecimal(match.Groups[1].Value, out meters) &&
                TryParseInt(match.Groups[2].Value, out rolls))
                return true;

            match = GrandTotalCompactMtsRegex.Match(text);
            if (match.Success &&
                TryParseDecimal(match.Groups[1].Value, out meters) &&
                TryParseInt(match.Groups[2].Value, out rolls))
                return true;

            match = GrandTotalRollsFirstRegex.Match(text);
            if (match.Success &&
                TryParseInt(match.Groups[1].Value, out rolls) &&
                TryParseDecimal(match.Groups[2].Value, out meters))
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

        if (string.IsNullOrWhiteSpace(fabricCode))
            return false;

        if (string.IsNullOrWhiteSpace(color))
        {
            if (!TrySplitCombinedFabricColor(fabricCode, out var splitCode, out var splitColor))
                return false;

            fabricCode = splitCode;
            color = splitColor;
        }

        if (fabricCode.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase) ||
            IsRollBlockHeader(NormalizeHeader(fabricCode)))
            return false;

        if (TryParseInt(fabricCode, out _) && TryParseDecimal(color, out _))
            return false;

        var lastCol = sheet.GetLastColumnUsed(rowNumber);
        var hasMtsMarker = false;
        var hasYdsMarker = false;
        var hasRollsMarker = false;
        for (var col = 2; col <= lastCol; col++)
        {
            var header = NormalizeHeader(sheet.GetCellString(rowNumber, col));
            if (header is "MTS" or "M")
            {
                hasMtsMarker = true;
                TryParseDecimal(sheet.GetCellString(rowNumber, col - 1), out declaredMeters);
            }
            else if (header is "YDS" or "YD" or "YARD" or "YARDS")
            {
                hasYdsMarker = true;
                if (TryParseDecimal(sheet.GetCellString(rowNumber, col - 1), out var yards))
                    declaredMeters = DplQuantityConverter.ToMeters(yards, DplQuantityUnit.Yards);
            }
            else if (header is "ROLLS" or "ROLL" or "ROL")
            {
                hasRollsMarker = true;
                TryParseInt(sheet.GetCellString(rowNumber, col - 1), out declaredRolls);
            }
        }

        if (!hasMtsMarker && !hasYdsMarker && !hasRollsMarker)
            return false;

        return declaredMeters > 0 || declaredRolls > 0;
    }

    internal static bool TrySplitCombinedFabricColor(string combined, out string fabricCode, out string color)
    {
        fabricCode = "";
        color = "";
        combined = combined.Trim();
        var match = Regex.Match(combined, @"^(\S+)\s+(.+)$");
        if (!match.Success)
            return false;

        fabricCode = match.Groups[1].Value.Trim();
        color = match.Groups[2].Value.Trim();
        return !string.IsNullOrWhiteSpace(fabricCode) && !string.IsNullOrWhiteSpace(color);
    }

    private static bool IsColumnHeaderRow(IWorksheetReader sheet, int rowNumber) =>
        DetectColumnBlocks(sheet, rowNumber).Count > 0;

    private static bool IsGroupTotalRow(IWorksheetReader sheet, int rowNumber)
    {
        var first = sheet.GetCellString(rowNumber, 1).Trim();
        if (string.IsNullOrWhiteSpace(first))
            return false;

        if (first.Equals("TTY", StringComparison.OrdinalIgnoreCase))
            return true;

        if (!first.StartsWith("TOTAL", StringComparison.OrdinalIgnoreCase))
            return false;

        if (TryParseGrandTotalRow(sheet, rowNumber, out _, out _, out _))
            return false;

        return first.StartsWith("TOTAL:", StringComparison.OrdinalIgnoreCase) ||
               (first.Contains(':') &&
                !GrandTotalMtsFirstRegex.IsMatch(first) &&
                !GrandTotalCompactMtsRegex.IsMatch(first) &&
                !GrandTotalYdsWithMtsRegex.IsMatch(first) &&
                !GrandTotalYdsOnlyRegex.IsMatch(first));
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
