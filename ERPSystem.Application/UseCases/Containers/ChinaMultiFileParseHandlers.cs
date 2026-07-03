using ERPSystem.Application.Abstractions;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Containers.Excel;

namespace ERPSystem.Application.UseCases.Containers;

public sealed class ParseChinaInvoiceExcelHandler
    : IQueryHandler<ParseChinaInvoiceExcelQuery, ApplicationResult<ChinaInvoiceParseResultDto>>
{
    public Task<ApplicationResult<ChinaInvoiceParseResultDto>> HandleAsync(
        ParseChinaInvoiceExcelQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FileContent.Length == 0)
            return Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.ValidationFailed(
                nameof(query.FileContent), "ملف الفاتورة فارغ."));

        var format = ExcelFileFormatDetector.Detect(query.FileContent, query.FileName);
        if (format == ExcelFileFormat.Unsupported)
        {
            return Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.ValidationFailed(
                "File", ExcelFileFormatDetector.UnsupportedFormatMessage));
        }

        try
        {
            var dto = Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var sheet = ExcelWorksheetReaderFactory.Open(query.FileContent, query.FileName);
                return ChinaInvoiceExcelParser.Parse(sheet, query.FileName);
            }, cancellationToken).GetAwaiter().GetResult();

            if (dto.Lines.Count == 0)
            {
                return Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.ValidationFailed(
                    "File", "لم يتم العثور على بنود أقمشة في ملف الفاتورة."));
            }

            return Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.Success(dto));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ApplicationResult<ChinaInvoiceParseResultDto>.Failure(ex.Message));
        }
    }
}

public sealed class ParseChinaPackingSummaryExcelHandler
    : IQueryHandler<ParseChinaPackingSummaryExcelQuery, ApplicationResult<ChinaPackingSummaryParseResultDto>>
{
    public Task<ApplicationResult<ChinaPackingSummaryParseResultDto>> HandleAsync(
        ParseChinaPackingSummaryExcelQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.FileContent.Length == 0)
            return Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.ValidationFailed(
                nameof(query.FileContent), "ملف PL فارغ."));

        var format = ExcelFileFormatDetector.Detect(query.FileContent, query.FileName);
        if (format == ExcelFileFormat.Unsupported)
        {
            return Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.ValidationFailed(
                "File", ExcelFileFormatDetector.UnsupportedFormatMessage));
        }

        try
        {
            var dto = Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var sheet = ExcelWorksheetReaderFactory.Open(query.FileContent, query.FileName);
                return ChinaPackingListSummaryParser.Parse(sheet, query.FileName);
            }, cancellationToken).GetAwaiter().GetResult();

            if (dto.Lines.Count == 0)
            {
                return Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.ValidationFailed(
                    "File", "لم يتم العثور على بنود أقمشة في ملف PL."));
            }

            return Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.Success(dto));
        }
        catch (Exception ex)
        {
            return Task.FromResult(ApplicationResult<ChinaPackingSummaryParseResultDto>.Failure(ex.Message));
        }
    }
}
