using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Application.UseCases.Containers;

/// <summary>
/// Validates summed DPL meter totals (after unit conversion) against Invoice/PL per fabric/color group.
/// </summary>
public static class DplInvoicePlGroupValidator
{
    public static IReadOnlyList<DplGroupCrossValidationResult> Validate(
        ChinaImportMultiFileSessionDto session,
        IReadOnlySet<string>? userConfirmedGroupKeys = null)
    {
        userConfirmedGroupKeys ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var results = new List<DplGroupCrossValidationResult>();

        foreach (var line in session.TypeLines.Where(t => t.HasDpl))
        {
            var invLine = session.Invoice?.Lines
                .FirstOrDefault(l => ChinaImportTypeNameNormalizer.KeysMatch(l.MatchKey, line.MatchKey));
            var plLine = session.PackingSummary?.Lines
                .FirstOrDefault(l => ChinaImportTypeNameNormalizer.KeysMatch(l.MatchKey, line.MatchKey));

            var expectedMeters = invLine?.LengthMeters ?? plLine?.LengthMeters ?? 0m;
            var expectedSource = invLine is not null ? "فاتورة" : "PL";

            if (expectedMeters <= 0)
                continue;

            var dplGroup = session.RollDetail?.Groups.FirstOrDefault(g =>
                ChinaImportTypeNameNormalizer.KeysMatch(
                    ChinaImportTypeNameNormalizer.BuildDplMatchKey(g.FabricCode, g.Color),
                    line.DplMatchKey ?? line.MatchKey));

            var calculatedMeters = dplGroup?.ParsedTotalMeters ?? line.LengthMeters;
            var groupKey = line.DplMatchKey ?? line.MatchKey;
            var passed = DplCrossValidationTolerance.WithinTolerance(expectedMeters, calculatedMeters);
            var userConfirmed = userConfirmedGroupKeys.Contains(groupKey);
            var diff = Math.Abs(expectedMeters - calculatedMeters);

            results.Add(new DplGroupCrossValidationResult
            {
                GroupKey = groupKey,
                DisplayName = line.TypeDisplayName,
                ExpectedMeters = expectedMeters,
                ExpectedSource = expectedSource,
                CalculatedMeters = calculatedMeters,
                DifferenceMeters = diff,
                Passed = passed,
                UserConfirmed = userConfirmed,
                MessageArabic = passed
                    ? $"✅ {line.TypeDisplayName}: DPL {calculatedMeters:N2} م يطابق {expectedSource} {expectedMeters:N2} م (فرق {diff:N3} م)"
                    : $"⚠️ {line.TypeDisplayName}: DPL {calculatedMeters:N2} م ≠ {expectedSource} {expectedMeters:N2} م (فرق {diff:N2} م — الحد المسموح {DplCrossValidationTolerance.FormatTolerance(expectedMeters)})"
            });
        }

        return results;
    }

    public static bool AllPassedOrConfirmed(IReadOnlyList<DplGroupCrossValidationResult> results) =>
        results.Count == 0 || results.All(r => r.Passed || r.UserConfirmed);

    public static bool HasInvoiceOrPlReference(ChinaImportMultiFileSessionDto session) =>
        session.Invoice?.Lines.Count > 0 || session.PackingSummary?.Lines.Count > 0;
}
