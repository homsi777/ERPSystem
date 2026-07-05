using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

public sealed class ImportContainerExcelHandler(
    IFabricCatalogRepository fabricCatalogRepository,
    IUnitOfWork unitOfWork)
    : IQueryHandler<ParseContainerExcelQuery, ApplicationResult<ContainerExcelParseResultDto>>
{
    public async Task<ApplicationResult<ContainerExcelParseResultDto>> HandleAsync(
        ParseContainerExcelQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.CompanyId == Guid.Empty)
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(nameof(query.CompanyId), "Company is required.");

        if (query.FileContent.Length == 0)
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(nameof(query.FileContent), "Excel file is empty.");

        var format = ExcelFileFormatDetector.Detect(query.FileContent, query.FileName);
        if (format == ExcelFileFormat.Unsupported)
        {
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(
                "File",
                ExcelFileFormatDetector.UnsupportedFormatMessage);
        }

        PackingListImportLogger.Stage("import-start", $"{query.FileName} format={format} bytes={query.FileContent.Length}");

        try
        {
            var parsed = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                PackingListImportLogger.Stage("workbook-open-start");
                using var sheet = ExcelWorksheetReaderFactory.Open(query.FileContent, query.FileName);
                cancellationToken.ThrowIfCancellationRequested();
                if (sheet.FirstRowUsed <= 0)
                    throw new InvalidOperationException("EMPTY_SHEET");

                return PackingListExcelParser.Parse(sheet, cancellationToken);
            }, cancellationToken);

            if (parsed.Groups.Count == 0)
                return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", "لم يتم العثور على مجموعات أقمشة في ملف Packing List.");

            var totalParsedMeters = parsed.Groups.Sum(g => g.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters));
            var totalParsedRolls = parsed.Groups.Sum(g => g.Rolls.Count(r => r.IsValid));

            if (parsed.DeclaredGrandRolls.HasValue &&
                !PackingListExcelParser.RollsApproximatelyEqual(parsed.DeclaredGrandRolls.Value, totalParsedRolls))
            {
                return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(
                    "File",
                    $"تحذير: تم تحليل {totalParsedRolls} توب فقط من أصل {parsed.DeclaredGrandRolls.Value} المعلن في الملف. لا يمكن المتابعة.");
            }

            if (parsed.DeclaredGrandMeters.HasValue &&
                !PackingListExcelParser.MetersApproximatelyEqual(parsed.DeclaredGrandMeters.Value, totalParsedMeters))
            {
                return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(
                    "File",
                    $"تحذير: تم تحليل {totalParsedMeters:N2} متر فقط من أصل {parsed.DeclaredGrandMeters.Value:N2} المعلن في الملف. لا يمكن المتابعة.");
            }

            PackingListImportLogger.Stage("fabric-lookup-start", $"groups={parsed.Groups.Count}");
            var groups = new List<PackingListGroupDto>();
            var catalogBatch = new FabricCatalogImportBatch();

            foreach (var group in parsed.Groups)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var parsedMeters = group.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters);
                var parsedRolls = group.Rolls.Count(r => r.IsValid);

                string? resolutionError = null;
                Guid? fabricItemId = null;
                Guid? fabricColorId = null;
                var fabricResolved = false;
                var colorResolved = false;
                var issues = new List<PackingListResolutionIssueDto>();

                var fabricCode = PackingListCatalogNormalizer.NormalizeFabricCode(group.FabricCode);
                var colorKey = PackingListCatalogNormalizer.NormalizeColor(group.Color);

                try
                {
                    var (fabricItem, fabricColor, _) = await catalogBatch.EnsureAsync(
                        fabricCatalogRepository,
                        query.CompanyId,
                        fabricCode,
                        colorKey,
                        cancellationToken);
                    fabricResolved = true;
                    colorResolved = true;
                    fabricItemId = fabricItem.Id;
                    fabricColorId = fabricColor.Id;
                }
                catch (Exception ex)
                {
                    resolutionError = ex.Message;
                    issues.Add(new PackingListResolutionIssueDto
                    {
                        GroupIndex = group.GroupIndex,
                        FabricCode = fabricCode,
                        Color = colorKey,
                        Reason = resolutionError
                    });
                }

                foreach (var roll in group.Rolls.Where(r => !r.IsValid))
                {
                    issues.Add(new PackingListResolutionIssueDto
                    {
                        GroupIndex = group.GroupIndex,
                        FabricCode = fabricCode,
                        Color = colorKey,
                        RollNumber = roll.RollNumber,
                        Reason = roll.InvalidReason ?? "صف غير صالح"
                    });
                }

                var metersMatch = PackingListExcelParser.MetersApproximatelyEqual(group.DeclaredTotalMeters, parsedMeters);
                var rollsMatch = group.DeclaredTotalRolls <= 0 || group.DeclaredTotalRolls == parsedRolls;

                groups.Add(new PackingListGroupDto
                {
                    GroupIndex = group.GroupIndex,
                    FabricCode = fabricCode,
                    Color = colorKey,
                    DeclaredTotalMeters = group.DeclaredTotalMeters,
                    DeclaredTotalRolls = group.DeclaredTotalRolls,
                    ParsedTotalMeters = parsedMeters,
                    ParsedTotalRolls = parsedRolls,
                    MetersMatch = metersMatch,
                    RollsMatch = rollsMatch,
                    FabricResolved = fabricResolved,
                    ColorResolved = colorResolved,
                    FabricItemId = fabricItemId,
                    FabricColorId = fabricColorId,
                    ResolutionError = resolutionError,
                    Rolls = group.Rolls,
                    ResolutionIssues = issues
                });
            }
            if (catalogBatch.HasPendingChanges)
                await unitOfWork.SaveChangesAsync(cancellationToken);

            var hasUnresolved = groups.Any(g => !g.FabricResolved || !g.ColorResolved);

            var grandMetersMatch = parsed.DeclaredGrandMeters is null ||
                                   PackingListExcelParser.MetersApproximatelyEqual(parsed.DeclaredGrandMeters.Value, totalParsedMeters);
            var grandRollsMatch = parsed.DeclaredGrandRolls is null ||
                                  PackingListExcelParser.RollsApproximatelyEqual(parsed.DeclaredGrandRolls.Value, totalParsedRolls);

            var grandSummary = parsed.DeclaredGrandMeters.HasValue && parsed.DeclaredGrandRolls.HasValue
                ? $"المعلن: {parsed.DeclaredGrandMeters:N2} م / {parsed.DeclaredGrandRolls} توب — المحلّل: {totalParsedMeters:N2} م / {totalParsedRolls} توب"
                : $"المحلّل: {totalParsedMeters:N2} م / {totalParsedRolls} توب (لا يوجد إجمالي معلن في الملف)";

            PackingListImportLogger.Stage("import-complete", $"groups={groups.Count} rolls={totalParsedRolls}");

            return ApplicationResult<ContainerExcelParseResultDto>.Success(new ContainerExcelParseResultDto
            {
                FileName = query.FileName,
                SupplierNameFromFile = parsed.SupplierNameFromFile,
                HasUnresolvedGroups = hasUnresolved,
                GrandTotal = new PackingListGrandTotalDto
                {
                    DeclaredTotalMeters = parsed.DeclaredGrandMeters,
                    DeclaredTotalRolls = parsed.DeclaredGrandRolls,
                    ParsedTotalMeters = totalParsedMeters,
                    ParsedTotalRolls = totalParsedRolls,
                    MetersMatch = grandMetersMatch,
                    RollsMatch = grandRollsMatch,
                    SummaryText = grandSummary
                },
                Groups = groups
            });
        }
        catch (OperationCanceledException)
        {
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(
                "File",
                "انتهت مهلة قراءة الملف. قد يكون الملف كبيراً جداً أو تالفاً.");
        }
        catch (UnsupportedExcelFormatException ex)
        {
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message == "EMPTY_SHEET")
        {
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", "ملف Excel فارغ.");
        }
        catch (Exception ex)
        {
            PackingListImportLogger.Stage("import-failed", ex.GetType().Name + ": " + ex.Message);
            var failure = ex.ToFailureResult<ContainerExcelParseResultDto>();
            var message = !string.IsNullOrWhiteSpace(failure.ErrorMessage)
                ? failure.ErrorMessage
                : ExcelFileFormatDetector.UnreadableFileMessage;
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", message);
        }
    }
}
