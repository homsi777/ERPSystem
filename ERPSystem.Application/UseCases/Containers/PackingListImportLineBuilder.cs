using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Application.UseCases.Containers;

public static class PackingListImportLineBuilder
{
    public static IReadOnlyList<ImportContainerLineCommand> BuildLines(ContainerExcelParseResultDto parse)
    {
        var lines = new List<ImportContainerLineCommand>();
        var lineNumber = 0;
        var fileUnit = parse.DetectedQuantityUnit;

        foreach (var group in parse.Groups.Where(g => g.FabricResolved && g.ColorResolved))
        {
            var validRolls = group.Rolls.Where(r => r.IsValid).ToList();
            if (validRolls.Count > 0)
            {
                foreach (var roll in validRolls)
                {
                    lineNumber++;
                    lines.Add(new ImportContainerLineCommand
                    {
                        LineNumber = lineNumber,
                        FabricItemId = group.FabricItemId!.Value,
                        FabricColorId = group.FabricColorId!.Value,
                        RollCount = 1,
                        LengthMeters = roll.QuantityMeters,
                        DplQuantityNative = roll.QuantityNative,
                        DplQuantityUnit = roll.QuantityUnit,
                        LotCode = string.IsNullOrWhiteSpace(roll.LotCode) ? null : roll.LotCode,
                        SupplierRollNumber = roll.RollNumber > 0 ? roll.RollNumber : null
                    });
                }
                continue;
            }

            if (group.ParsedTotalMeters <= 0 && group.ParsedTotalRolls <= 0)
                continue;

            lineNumber++;
            lines.Add(new ImportContainerLineCommand
            {
                LineNumber = lineNumber,
                FabricItemId = group.FabricItemId!.Value,
                FabricColorId = group.FabricColorId!.Value,
                RollCount = group.ParsedTotalRolls > 0 ? group.ParsedTotalRolls : 1,
                LengthMeters = group.ParsedTotalMeters,
                DplQuantityNative = group.ParsedTotalMeters,
                DplQuantityUnit = fileUnit
            });
        }

        return lines;
    }
}
