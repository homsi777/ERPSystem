using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Application.UseCases.Containers;

public static class ChinaImportDplLinkSuggester
{
    private const decimal LengthToleranceRatio = 0.02m;

    public static (string? MatchKey, string? Description, int Score) SuggestInvoiceKey(
        PackingListGroupDto dpl,
        IReadOnlyList<ChinaInvoiceLineDto> invoiceLines,
        IReadOnlySet<string> assignedInvoiceKeys)
    {
        var bestKey = (string?)null;
        var bestDesc = (string?)null;
        var bestScore = 0;

        foreach (var line in invoiceLines)
        {
            if (assignedInvoiceKeys.Contains(line.MatchKey))
                continue;

            var score = ScoreCandidate(dpl, line.RollCount, line.LengthMeters, line.Description, line.MatchKey);
            if (score > bestScore)
            {
                bestScore = score;
                bestKey = line.MatchKey;
                bestDesc = line.Description;
            }
        }

        return bestScore >= 50 ? (bestKey, bestDesc, bestScore) : (null, null, bestScore);
    }

    public static (string? MatchKey, string? Description, int Score) SuggestPlKey(
        PackingListGroupDto dpl,
        IReadOnlyList<ChinaPackingSummaryLineDto> plLines,
        IReadOnlySet<string> assignedKeys)
    {
        var bestKey = (string?)null;
        var bestDesc = (string?)null;
        var bestScore = 0;

        foreach (var line in plLines)
        {
            if (assignedKeys.Contains(line.MatchKey))
                continue;

            var score = ScoreCandidate(dpl, line.RollCount, line.LengthMeters, line.Description, line.MatchKey);
            if (score > bestScore)
            {
                bestScore = score;
                bestKey = line.MatchKey;
                bestDesc = line.Description;
            }
        }

        return bestScore >= 50 ? (bestKey, bestDesc, bestScore) : (null, null, bestScore);
    }

    private static int ScoreCandidate(
        PackingListGroupDto dpl,
        int candidateRolls,
        decimal candidateMeters,
        string candidateDescription,
        string candidateMatchKey)
    {
        var score = 0;
        var dplRolls = dpl.ParsedTotalRolls;
        var dplMeters = dpl.ParsedTotalMeters;

        if (candidateRolls > 0 && dplRolls > 0 && candidateRolls == dplRolls)
            score += 100;
        else if (candidateRolls > 0 && dplRolls > 0 && Math.Abs(candidateRolls - dplRolls) <= 1)
            score += 40;

        if (MetersClose(candidateMeters, dplMeters))
            score += 60;
        else if (candidateMeters > 0 && dplMeters > 0)
        {
            var ratio = Math.Abs(candidateMeters - dplMeters) / Math.Max(candidateMeters, dplMeters);
            if (ratio <= 0.05m)
                score += 30;
        }

        if (ColorAppearsInDescription(dpl.Color, candidateDescription, candidateMatchKey))
            score += 25;

        if (ChinaImportTypeNameNormalizer.KeysMatch(
                ChinaImportTypeNameNormalizer.BuildDplMatchKey(dpl.FabricCode, dpl.Color),
                candidateMatchKey))
            score += 200;

        return score;
    }

    private static bool ColorAppearsInDescription(string color, string description, string matchKey)
    {
        if (string.IsNullOrWhiteSpace(color))
            return false;

        var normColor = ChinaImportTypeNameNormalizer.NormalizeForMatch(color);
        var normDesc = ChinaImportTypeNameNormalizer.NormalizeForMatch(description);
        var normKey = ChinaImportTypeNameNormalizer.NormalizeForMatch(matchKey);

        return normDesc.Contains(normColor, StringComparison.Ordinal) ||
               normKey.Contains(normColor, StringComparison.Ordinal);
    }

    private static bool MetersClose(decimal a, decimal b)
    {
        if (a <= 0 || b <= 0)
            return false;
        var diff = Math.Abs(a - b);
        return diff <= Math.Max(1m, Math.Max(a, b) * LengthToleranceRatio);
    }
}
