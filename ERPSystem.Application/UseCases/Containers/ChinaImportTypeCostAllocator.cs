using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Domain.Entities.ChinaImport;

namespace ERPSystem.Application.UseCases.Containers;

public static class ChinaImportTypeCostAllocator
{
    public static IReadOnlyList<ContainerFabricTypeLine> BuildTypeLines(
        IReadOnlyList<ChinaImportTypeLineDto> sourceLines,
        bool usesWeightedAllocation)
    {
        return sourceLines.Select(s => ContainerFabricTypeLine.Create(
            s.LineIndex,
            s.TypeDisplayName,
            s.MatchKey,
            s.FabricItemId,
            s.FabricColorId,
            s.LengthMeters,
            s.RollCount,
            s.NetWeightKg,
            s.Cbm,
            s.ChinaUnitPriceUsd,
            s.InvoiceLineAmountUsd,
            s.HasInvoice,
            s.HasPackingSummary,
            s.HasDpl,
            s.MismatchWarnings.Count > 0 ? string.Join("؛ ", s.MismatchWarnings) : null,
            usesWeightedAllocation)).ToList();
    }

    public static void ApplyWeightedAllocation(
        IReadOnlyList<ContainerFabricTypeLine> typeLines,
        decimal totalSharedExpensesUsd)
    {
        var totalWeight = typeLines.Sum(l => l.NetWeightKg);
        if (totalWeight <= 0)
        {
            ApplyFlatFallback(typeLines, totalSharedExpensesUsd);
            return;
        }

        foreach (var line in typeLines)
        {
            var share = totalSharedExpensesUsd * (line.NetWeightKg / totalWeight);
            var landed = line.ChinaUnitPriceUsd +
                         (line.LengthMeters > 0 ? share / line.LengthMeters : 0m);
            line.ApplyCostAllocation(share, landed);
        }
    }

    public static void ApplyFlatFallback(
        IReadOnlyList<ContainerFabricTypeLine> typeLines,
        decimal totalSharedExpensesUsd)
    {
        var totalMeters = typeLines.Sum(l => l.LengthMeters);
        var expensePerMeter = totalMeters > 0 ? totalSharedExpensesUsd / totalMeters : 0m;

        foreach (var line in typeLines)
        {
            var share = expensePerMeter * line.LengthMeters;
            var landed = line.ChinaUnitPriceUsd + expensePerMeter;
            line.ApplyCostAllocation(share, landed);
        }
    }

    public static IReadOnlyList<ChinaImportTypeLineDto> WithCalculatedCosts(
        IReadOnlyList<ChinaImportTypeLineDto> sourceLines,
        decimal totalSharedExpensesUsd,
        bool usesWeightedAllocation)
    {
        var domainLines = BuildTypeLines(sourceLines, usesWeightedAllocation);
        if (usesWeightedAllocation)
            ApplyWeightedAllocation(domainLines, totalSharedExpensesUsd);
        else
            ApplyFlatFallback(domainLines, totalSharedExpensesUsd);

        return sourceLines.Select((s, i) =>
        {
            var d = domainLines[i];
            return new ChinaImportTypeLineDto
            {
                LineIndex = s.LineIndex,
                TypeDisplayName = s.TypeDisplayName,
                MatchKey = s.MatchKey,
                FabricCode = s.FabricCode,
                Color = s.Color,
                FabricItemId = s.FabricItemId,
                FabricColorId = s.FabricColorId,
                HasInvoice = s.HasInvoice,
                HasPackingSummary = s.HasPackingSummary,
                HasDpl = s.HasDpl,
                MissingSources = s.MissingSources,
                MismatchWarnings = s.MismatchWarnings,
                LengthMeters = s.LengthMeters,
                RollCount = s.RollCount,
                NetWeightKg = s.NetWeightKg,
                Cbm = s.Cbm,
                ChinaUnitPriceUsd = s.ChinaUnitPriceUsd,
                InvoiceLineAmountUsd = s.InvoiceLineAmountUsd,
                ExpenseShareUsd = d.ExpenseShareUsd,
                LandedCostPerMeterUsd = d.LandedCostPerMeterUsd,
                MarginPerMeterUsd = d.MarginPerMeterUsd,
                SalePricePerMeterUsd = d.SalePricePerMeterUsd
            };
        }).ToList();
    }
}
