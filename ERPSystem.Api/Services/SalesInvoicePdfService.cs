using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Sales;

namespace ERPSystem.Api.Services;

/// <summary>API adapter over the shared sales invoice PDF generator.</summary>
public sealed class SalesInvoicePdfService
{
    private readonly SalesInvoicePdfGenerator _generator;

    public SalesInvoicePdfService(IWebHostEnvironment environment) =>
        _generator = SalesInvoicePdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(SalesInvoiceOperationsCenterDto operations) =>
        _generator.Generate(operations);
}
