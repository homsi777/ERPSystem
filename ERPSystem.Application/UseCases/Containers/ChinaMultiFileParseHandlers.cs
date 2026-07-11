using ERPSystem.Application.Abstractions;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

public sealed class ParseChinaInvoiceExcelHandler
    : IQueryHandler<ParseChinaInvoiceExcelQuery, ApplicationResult<ChinaInvoiceParseResultDto>>
{
    public async Task<ApplicationResult<ChinaInvoiceParseResultDto>> HandleAsync(
        ParseChinaInvoiceExcelQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FileContent.Length == 0)
            return await Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.ValidationFailed(
                nameof(query.FileContent), "ملف الفاتورة فارغ."));

        var format = ExcelFileFormatDetector.Detect(query.FileContent, query.FileName);
        if (format == ExcelFileFormat.Unsupported)
        {
            return await Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.ValidationFailed(
                "File", ExcelFileFormatDetector.UnsupportedFormatMessage));
        }

        try
        {
            var dto = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var sheet = ExcelWorksheetReaderFactory.Open(query.FileContent, query.FileName);
                return ChinaInvoiceExcelParser.Parse(sheet, query.FileName);
            }, cancellationToken);

            if (dto.Lines.Count == 0)
            {
                return await Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.ValidationFailed(
                    "File", "لم يتم العثور على بنود أقمشة في ملف الفاتورة."));
            }

            return await Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.Success(dto));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.Failure(ex.Message));
        }
    }
}

public sealed class ParseChinaPackingSummaryExcelHandler
    : IQueryHandler<ParseChinaPackingSummaryExcelQuery, ApplicationResult<ChinaPackingSummaryParseResultDto>>
{
    public async Task<ApplicationResult<ChinaPackingSummaryParseResultDto>> HandleAsync(
        ParseChinaPackingSummaryExcelQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FileContent.Length == 0)
            return await Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.ValidationFailed(
                nameof(query.FileContent), "ملف PL فارغ."));

        var format = ExcelFileFormatDetector.Detect(query.FileContent, query.FileName);
        if (format == ExcelFileFormat.Unsupported)
        {
            return await Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.ValidationFailed(
                "File", ExcelFileFormatDetector.UnsupportedFormatMessage));
        }

        try
        {
            var dto = await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var sheet = ExcelWorksheetReaderFactory.Open(query.FileContent, query.FileName);
                return ChinaPackingListSummaryParser.Parse(sheet, query.FileName);
            }, cancellationToken);

            if (dto.Lines.Count == 0)
            {
                return await Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.ValidationFailed(
                    "File", "لم يتم العثور على بنود أقمشة في ملف PL."));
            }

            return await Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.Success(dto));
        }
        catch (Exception ex)
        {
            return await Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.Failure(ex.Message));
        }
    }
}
