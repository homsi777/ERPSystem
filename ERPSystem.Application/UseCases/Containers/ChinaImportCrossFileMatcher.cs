using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;

namespace ERPSystem.Application.UseCases.Containers;

public static class ChinaImportCrossFileMatcher
{
    private const decimal LengthToleranceRatio = 0.02m;
    private const int RollTolerance = 0;

    public static ChinaImportMultiFileSessionDto BuildSession(
        ContainerExcelParseResultDto? rollDetail,
        ChinaInvoiceParseResultDto? invoice,
        ChinaPackingSummaryParseResultDto? packingSummary,
        ChinaImportMatchContext? matchContext = null)
    {
        var dplGroups = rollDetail?.Groups ?? [];
        var invoiceLines = invoice?.Lines ?? [];
        var plLines = packingSummary?.Lines ?? [];
        matchContext ??= new ChinaImportMatchContext();

        if (invoiceLines.Count > 0 || plLines.Count > 0)
        {
            var merged = BuildMergedSession(dplGroups, invoice, packingSummary, invoiceLines, plLines, matchContext);
            return FinalizeSession(merged, rollDetail);
        }

        return BuildDplOnlySession(rollDetail, invoice, packingSummary, dplGroups);
    }

    private static ChinaImportMultiFileSessionDto BuildMergedSession(
        IReadOnlyList<PackingListGroupDto> dplGroups,
        ChinaInvoiceParseResultDto? invoice,
        ChinaPackingSummaryParseResultDto? packingSummary,
        IReadOnlyList<ChinaInvoiceLineDto> invoiceLines,
        IReadOnlyList<ChinaPackingSummaryLineDto> plLines,
        ChinaImportMatchContext matchContext)
    {
        var canonicalKeys = invoiceLines.Count > 0
            ? invoiceLines.Select(l => l.MatchKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            : plLines.Select(l => l.MatchKey).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var buckets = canonicalKeys.ToDictionary(
            k => k,
            _ => new MatchBucket(),
            StringComparer.OrdinalIgnoreCase);

        foreach (var inv in invoiceLines)
        {
            if (!buckets.TryGetValue(inv.MatchKey, out var bucket))
                continue;
            bucket.Invoice = inv;
        }

        foreach (var pl in plLines)
        {
            if (!buckets.TryGetValue(pl.MatchKey, out var bucket))
            {
                bucket = new MatchBucket();
                buckets[pl.MatchKey] = bucket;
            }
            bucket.PackingSummary = pl;
        }

        var assignedInvoiceKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var unmatchedDpl = new List<ChinaImportUnmatchedDplGroupDto>();

        foreach (var dpl in dplGroups)
        {
            var dplKey = ChinaImportTypeNameNormalizer.BuildDplMatchKey(dpl.FabricCode, dpl.Color);
            var canonicalKey = ResolveCanonicalKey(dpl, dplKey, canonicalKeys, matchContext);

            if (canonicalKey is not null && buckets.TryGetValue(canonicalKey, out var bucket))
            {
                bucket.Dpl = dpl;
                bucket.LinkedViaAlias = !ChinaImportTypeNameNormalizer.KeysMatch(dplKey, canonicalKey);
                assignedInvoiceKeys.Add(canonicalKey);
            }
            else
            {
                var (suggestedKey, suggestedDesc, score) = invoiceLines.Count > 0
                    ? ChinaImportDplLinkSuggester.SuggestInvoiceKey(dpl, invoiceLines, assignedInvoiceKeys)
                    : ChinaImportDplLinkSuggester.SuggestPlKey(dpl, plLines, assignedInvoiceKeys);

                unmatchedDpl.Add(new ChinaImportUnmatchedDplGroupDto
                {
                    DplMatchKey = dplKey,
                    GroupIndex = dpl.GroupIndex,
                    FabricCode = dpl.FabricCode,
                    Color = dpl.Color,
                    FabricItemId = dpl.FabricItemId,
                    FabricColorId = dpl.FabricColorId,
                    RollCount = dpl.ParsedTotalRolls,
                    LengthMeters = dpl.ParsedTotalMeters,
                    SuggestedInvoiceMatchKey = suggestedKey,
                    SuggestedInvoiceDescription = suggestedDesc,
                    SuggestionScore = score
                });
            }
        }

        var linkOptions = BuildLinkOptions(invoiceLines, plLines);
        var typeLines = new List<ChinaImportTypeLineDto>();
        var lineIndex = 0;

        foreach (var key in canonicalKeys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
        {
            if (!buckets.TryGetValue(key, out var bucket))
                continue;

            typeLines.Add(BuildTypeLine(++lineIndex, key, bucket));
        }

        return new ChinaImportMultiFileSessionDto
        {
            RollDetail = null, // set by caller via wrapper
            Invoice = invoice,
            PackingSummary = packingSummary,
            TypeLines = typeLines,
            UnmatchedDplGroups = unmatchedDpl,
            InvoiceLinkOptions = linkOptions
        };
    }

    private static ChinaImportMultiFileSessionDto FinalizeSession(
        ChinaImportMultiFileSessionDto draft,
        ContainerExcelParseResultDto? rollDetail) =>
        new()
        {
            RollDetail = rollDetail,
            Invoice = draft.Invoice,
            PackingSummary = draft.PackingSummary,
            TypeLines = draft.TypeLines,
            UnmatchedDplGroups = draft.UnmatchedDplGroups,
            InvoiceLinkOptions = draft.InvoiceLinkOptions
        };

    private static ChinaImportMultiFileSessionDto BuildDplOnlySession(
        ContainerExcelParseResultDto? rollDetail,
        ChinaInvoiceParseResultDto? invoice,
        ChinaPackingSummaryParseResultDto? packingSummary,
        IReadOnlyList<PackingListGroupDto> dplGroups)
    {
        var typeLines = new List<ChinaImportTypeLineDto>();
        var lineIndex = 0;

        foreach (var dpl in dplGroups)
        {
            var key = ChinaImportTypeNameNormalizer.BuildDplMatchKey(dpl.FabricCode, dpl.Color);
            typeLines.Add(new ChinaImportTypeLineDto
            {
                LineIndex = ++lineIndex,
                TypeDisplayName = $"{dpl.FabricCode} / {dpl.Color}",
                MatchKey = key,
                DplMatchKey = key,
                FabricCode = dpl.FabricCode,
                Color = dpl.Color,
                FabricItemId = dpl.FabricItemId,
                FabricColorId = dpl.FabricColorId,
                HasDpl = true,
                LengthMeters = dpl.ParsedTotalMeters,
                RollCount = dpl.ParsedTotalRolls
            });
        }

        return new ChinaImportMultiFileSessionDto
        {
            RollDetail = rollDetail,
            Invoice = invoice,
            PackingSummary = packingSummary,
            TypeLines = typeLines
        };
    }

    private static string? ResolveCanonicalKey(
        PackingListGroupDto dpl,
        string dplKey,
        IReadOnlyList<string> canonicalKeys,
        ChinaImportMatchContext matchContext)
    {
        foreach (var key in canonicalKeys)
        {
            if (ChinaImportTypeNameNormalizer.KeysMatch(dplKey, key))
                return key;
        }

        if (matchContext.SessionDplToInvoiceKeys.TryGetValue(dplKey, out var sessionKey) &&
            canonicalKeys.Any(k => ChinaImportTypeNameNormalizer.KeysMatch(k, sessionKey)))
        {
            return canonicalKeys.First(k => ChinaImportTypeNameNormalizer.KeysMatch(k, sessionKey));
        }

        foreach (var alias in matchContext.PersistedAliases)
        {
            if (!AliasMatchesDpl(alias, dpl, dplKey))
                continue;

            var matched = canonicalKeys.FirstOrDefault(k =>
                ChinaImportTypeNameNormalizer.KeysMatch(k, alias.InvoiceDescriptionMatchKey));
            if (matched is not null)
                return matched;
        }

        return null;
    }

    private static bool AliasMatchesDpl(FabricTypeAliasDto alias, PackingListGroupDto dpl, string dplKey)
    {
        if (!string.IsNullOrWhiteSpace(alias.DplMatchKey) &&
            ChinaImportTypeNameNormalizer.KeysMatch(alias.DplMatchKey, dplKey))
            return true;

        if (dpl.FabricItemId.HasValue && dpl.FabricColorId.HasValue &&
            alias.FabricItemId == dpl.FabricItemId.Value &&
            alias.FabricColorId == dpl.FabricColorId.Value)
            return true;

        return false;
    }

    private static ChinaImportTypeLineDto BuildTypeLine(int lineIndex, string key, MatchBucket bucket)
    {
        var inv = bucket.Invoice;
        var pl = bucket.PackingSummary;
        var dpl = bucket.Dpl;

        var missing = new List<string>();
        if (inv is null) missing.Add("الفاتورة");
        if (pl is null) missing.Add("PL");
        if (dpl is null) missing.Add("DPL");

        var warnings = BuildWarnings(inv, pl, dpl);

        var displayName = inv?.Description ??
                          pl?.Description ??
                          (dpl is not null ? $"{dpl.FabricCode} / {dpl.Color}" : key);

        return new ChinaImportTypeLineDto
        {
            LineIndex = lineIndex,
            TypeDisplayName = displayName,
            MatchKey = key,
            DplMatchKey = dpl is not null
                ? ChinaImportTypeNameNormalizer.BuildDplMatchKey(dpl.FabricCode, dpl.Color)
                : null,
            FabricCode = dpl?.FabricCode,
            Color = dpl?.Color,
            FabricItemId = dpl?.FabricItemId,
            FabricColorId = dpl?.FabricColorId,
            HasInvoice = inv is not null,
            HasPackingSummary = pl is not null,
            HasDpl = dpl is not null,
            LinkedViaAlias = bucket.LinkedViaAlias,
            MissingSources = missing,
            MismatchWarnings = warnings,
            LengthMeters = inv?.LengthMeters ?? pl?.LengthMeters ?? dpl?.ParsedTotalMeters ?? 0m,
            RollCount = inv?.RollCount ?? pl?.RollCount ?? dpl?.ParsedTotalRolls ?? 0,
            NetWeightKg = pl?.NetWeightKg ?? 0m,
            Cbm = pl?.Cbm ?? 0m,
            ChinaUnitPriceUsd = inv?.UnitPriceUsd ?? 0m,
            InvoiceLineAmountUsd = inv?.LineAmountUsd ?? 0m
        };
    }

    private static List<string> BuildWarnings(
        ChinaInvoiceLineDto? inv,
        ChinaPackingSummaryLineDto? pl,
        PackingListGroupDto? dpl)
    {
        var warnings = new List<string>();

        if (inv is not null && pl is not null)
        {
            if (!MetersClose(inv.LengthMeters, pl.LengthMeters))
                warnings.Add($"الأمتار: فاتورة {inv.LengthMeters:N0} ≠ PL {pl.LengthMeters:N0}");
            if (inv.RollCount > 0 && pl.RollCount > 0 && Math.Abs(inv.RollCount - pl.RollCount) > RollTolerance)
                warnings.Add($"الأثواب: فاتورة {inv.RollCount} ≠ PL {pl.RollCount}");
        }

        if (dpl is not null && inv is not null)
        {
            if (!MetersClose(dpl.ParsedTotalMeters, inv.LengthMeters))
                warnings.Add($"الأمتار: DPL {dpl.ParsedTotalMeters:N0} ≠ فاتورة {inv.LengthMeters:N0}");
            if (dpl.ParsedTotalRolls > 0 && inv.RollCount > 0 &&
                Math.Abs(dpl.ParsedTotalRolls - inv.RollCount) > RollTolerance)
                warnings.Add($"الأثواب: DPL {dpl.ParsedTotalRolls} ≠ فاتورة {inv.RollCount}");
        }

        if (dpl is not null && pl is not null && pl.LengthMeters > 0)
        {
            if (!MetersClose(dpl.ParsedTotalMeters, pl.LengthMeters))
                warnings.Add($"الأمتار: DPL {dpl.ParsedTotalMeters:N0} ≠ PL {pl.LengthMeters:N0}");
        }

        return warnings;
    }

    private static IReadOnlyList<ChinaImportInvoiceLinkOptionDto> BuildLinkOptions(
        IReadOnlyList<ChinaInvoiceLineDto> invoiceLines,
        IReadOnlyList<ChinaPackingSummaryLineDto> plLines)
    {
        if (invoiceLines.Count > 0)
        {
            return invoiceLines.Select(l => new ChinaImportInvoiceLinkOptionDto
            {
                MatchKey = l.MatchKey,
                Description = l.Description,
                RollCount = l.RollCount,
                LengthMeters = l.LengthMeters
            }).ToList();
        }

        return plLines.Select(l => new ChinaImportInvoiceLinkOptionDto
        {
            MatchKey = l.MatchKey,
            Description = l.Description,
            RollCount = l.RollCount,
            LengthMeters = l.LengthMeters
        }).ToList();
    }

    private static bool MetersClose(decimal a, decimal b)
    {
        if (a <= 0 || b <= 0)
            return true;
        var diff = Math.Abs(a - b);
        return diff <= Math.Max(1m, Math.Max(a, b) * LengthToleranceRatio);
    }

    private sealed class MatchBucket
    {
        public ChinaInvoiceLineDto? Invoice { get; set; }
        public ChinaPackingSummaryLineDto? PackingSummary { get; set; }
        public PackingListGroupDto? Dpl { get; set; }
        public bool LinkedViaAlias { get; set; }
    }
}
