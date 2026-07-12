using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Expenses;

namespace ERPSystem.Api.Services;

public sealed class ExpenseReportPdfService
{
    private readonly ExpenseReportPdfGenerator _generator;

    public ExpenseReportPdfService(IWebHostEnvironment environment) =>
        _generator = ExpenseReportPdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(ExpenseReportDto report) => _generator.Generate(report);
}
