using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

public sealed class ImportContainerExcelHandler(IFabricCatalogRepository fabricCatalogRepository)
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

        try
        {
            using var sheet = ExcelWorksheetReaderFactory.Open(query.FileContent, query.FileName);

            if (sheet.FirstRowUsed <= 0)
                return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", "ملف Excel فارغ.");

            var parsed = PackingListExcelParser.Parse(sheet);
            if (parsed.Groups.Count == 0)
                return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", "لم يتم العثور على مجموعات أقمشة في ملف Packing List.");

            var groups = new List<PackingListGroupDto>();
            var hasUnresolved = false;

            foreach (var group in parsed.Groups)
            {
                var parsedMeters = group.Rolls.Where(r => r.IsValid).Sum(r => r.QuantityMeters);
                var parsedRolls = group.Rolls.Count(r => r.IsValid);

                string? resolutionError = null;
                Guid? fabricItemId = null;
                Guid? fabricColorId = null;
                var fabricResolved = false;
                var colorResolved = false;
                var issues = new List<PackingListResolutionIssueDto>();

                var fabricItem = await fabricCatalogRepository.GetItemByCodeAsync(
                    query.CompanyId, group.FabricCode, cancellationToken);
                if (fabricItem is null)
                {
                    resolutionError = "كود القماش غير موجود";
                    hasUnresolved = true;
                    issues.Add(new PackingListResolutionIssueDto
                    {
                        GroupIndex = group.GroupIndex,
                        FabricCode = group.FabricCode,
                        Color = group.Color,
                        Reason = resolutionError
                    });
                }
                else
                {
                    fabricResolved = true;
                    fabricItemId = fabricItem.Id;
                    var fabricColor = await fabricCatalogRepository.GetColorForItemAsync(
                        fabricItem.Id, group.Color, cancellationToken);
                    if (fabricColor is null)
                    {
                        resolutionError = "اللون غير موجود";
                        hasUnresolved = true;
                        issues.Add(new PackingListResolutionIssueDto
                        {
                            GroupIndex = group.GroupIndex,
                            FabricCode = group.FabricCode,
                            Color = group.Color,
                            Reason = resolutionError
                        });
                    }
                    else
                    {
                        colorResolved = true;
                        fabricColorId = fabricColor.Id;
                    }
                }

                foreach (var roll in group.Rolls.Where(r => !r.IsValid))
                {
                    issues.Add(new PackingListResolutionIssueDto
                    {
                        GroupIndex = group.GroupIndex,
                        FabricCode = group.FabricCode,
                        Color = group.Color,
                        RollNumber = roll.RollNumber,
                        Reason = roll.InvalidReason ?? "صف غير صالح"
                    });
                }

                var metersMatch = PackingListExcelParser.MetersApproximatelyEqual(group.DeclaredTotalMeters, parsedMeters);
                var rollsMatch = group.DeclaredTotalRolls <= 0 || group.DeclaredTotalRolls == parsedRolls;

                groups.Add(new PackingListGroupDto
                {
                    GroupIndex = group.GroupIndex,
                    FabricCode = group.FabricCode,
                    Color = group.Color,
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

            var totalParsedMeters = groups.Sum(g => g.ParsedTotalMeters);
            var totalParsedRolls = groups.Sum(g => g.ParsedTotalRolls);
            var grandMetersMatch = parsed.DeclaredGrandMeters is null ||
                                   PackingListExcelParser.MetersApproximatelyEqual(parsed.DeclaredGrandMeters.Value, totalParsedMeters);
            var grandRollsMatch = parsed.DeclaredGrandRolls is null ||
                                  parsed.DeclaredGrandRolls.Value == totalParsedRolls;

            var grandSummary = parsed.DeclaredGrandMeters.HasValue && parsed.DeclaredGrandRolls.HasValue
                ? $"المعلن: {parsed.DeclaredGrandMeters:N2} م / {parsed.DeclaredGrandRolls} توب — المحلّل: {totalParsedMeters:N2} م / {totalParsedRolls} توب"
                : $"المحلّل: {totalParsedMeters:N2} م / {totalParsedRolls} توب (لا يوجد إجمالي معلن في الملف)";

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
        catch (UnsupportedExcelFormatException ex)
        {
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed("File", ex.Message);
        }
        catch (Exception)
        {
            return ApplicationResult<ContainerExcelParseResultDto>.ValidationFailed(
                "File",
                ExcelFileFormatDetector.UnreadableFileMessage);
        }
    }
}
