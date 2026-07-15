using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Containers;

/// <summary>
/// Re-applies a user-selected DPL length unit to an entire parse result.
/// QuantityNative always stays the raw cell value; QuantityMeters is recomputed.
/// </summary>
public static class DplQuantityUnitApplicator
{
    public static ContainerExcelParseResultDto Apply(
        ContainerExcelParseResultDto source,
        DplQuantityUnit selectedUnit)
    {
        var groups = source.Groups.Select(g => ApplyGroup(g, selectedUnit)).ToList();
        var totalParsedMeters = groups.Sum(g => g.ParsedTotalMeters);
        var totalParsedRolls = groups.Sum(g => g.ParsedTotalRolls);

        var grand = source.GrandTotal;
        var metersMatch = grand.DeclaredTotalMeters is null ||
                          PackingListExcelParser.MetersApproximatelyEqual(grand.DeclaredTotalMeters.Value, totalParsedMeters);
        var rollsMatch = grand.DeclaredTotalRolls is null ||
                         PackingListExcelParser.RollsApproximatelyEqual(grand.DeclaredTotalRolls.Value, totalParsedRolls);

        var grandSummary = grand.DeclaredTotalMeters.HasValue && grand.DeclaredTotalRolls.HasValue
            ? $"المعلن: {grand.DeclaredTotalMeters:N2} م / {grand.DeclaredTotalRolls} توب — المحلّل: {totalParsedMeters:N2} م / {totalParsedRolls} توب (وحدة DPL: {DplQuantityConverter.UnitLabelArabic(selectedUnit)})"
            : $"المحلّل: {totalParsedMeters:N2} م / {totalParsedRolls} توب — وحدة DPL: {DplQuantityConverter.UnitLabelArabic(selectedUnit)}";

        return new ContainerExcelParseResultDto
        {
            FileName = source.FileName,
            SupplierNameFromFile = source.SupplierNameFromFile,
            DetectedQuantityUnit = source.DetectedQuantityUnit,
            SelectedQuantityUnit = selectedUnit,
            HasUnresolvedGroups = source.HasUnresolvedGroups,
            Groups = groups,
            GrandTotal = new PackingListGrandTotalDto
            {
                DeclaredTotalMeters = grand.DeclaredTotalMeters,
                DeclaredTotalRolls = grand.DeclaredTotalRolls,
                ParsedTotalMeters = totalParsedMeters,
                ParsedTotalRolls = totalParsedRolls,
                MetersMatch = metersMatch,
                RollsMatch = rollsMatch,
                SummaryText = grandSummary
            }
        };
    }

    public static DplQuantityUnit ResolveEffectiveUnit(ContainerExcelParseResultDto parse) =>
        parse.SelectedQuantityUnit ?? parse.DetectedQuantityUnit;

    private static PackingListGroupDto ApplyGroup(PackingListGroupDto group, DplQuantityUnit selectedUnit)
    {
        var rolls = group.Rolls.Select(r => ApplyRoll(r, selectedUnit)).ToList();
        var parsedMeters = rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters);
        var parsedRolls = rolls.Count(r => r.IsValid);

        return new PackingListGroupDto
        {
            GroupIndex = group.GroupIndex,
            FabricCode = group.FabricCode,
            Color = group.Color,
            DeclaredTotalMeters = group.DeclaredTotalMeters,
            DeclaredTotalRolls = group.DeclaredTotalRolls,
            ParsedTotalMeters = parsedMeters,
            ParsedTotalRolls = parsedRolls,
            MetersMatch = group.DeclaredTotalMeters <= 0 ||
                          PackingListExcelParser.MetersApproximatelyEqual(group.DeclaredTotalMeters, parsedMeters),
            RollsMatch = group.DeclaredTotalRolls <= 0 ||
                         PackingListExcelParser.RollsApproximatelyEqual(group.DeclaredTotalRolls, parsedRolls),
            FabricResolved = group.FabricResolved,
            ColorResolved = group.ColorResolved,
            FabricItemId = group.FabricItemId,
            FabricColorId = group.FabricColorId,
            ResolutionError = group.ResolutionError,
            Rolls = rolls,
            ResolutionIssues = group.ResolutionIssues
        };
    }

    private static PackingListRollDto ApplyRoll(PackingListRollDto roll, DplQuantityUnit selectedUnit) =>
        new()
        {
            SequenceNumber = roll.SequenceNumber,
            GroupIndex = roll.GroupIndex,
            RollNumber = roll.RollNumber,
            QuantityNative = roll.QuantityNative,
            QuantityUnit = selectedUnit,
            QuantityMeters = DplQuantityConverter.ToMeters(roll.QuantityNative, selectedUnit),
            LotCode = roll.LotCode,
            IsValid = roll.IsValid,
            InvalidReason = roll.InvalidReason
        };
}
