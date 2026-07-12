using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Customers;

namespace ERPSystem.Api.Services;

public sealed class CustomerAccountLedgerPdfService
{
    private readonly CustomerAccountLedgerPdfGenerator _generator;

    public CustomerAccountLedgerPdfService(IWebHostEnvironment environment) =>
        _generator = CustomerAccountLedgerPdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(CustomerAccountLedgerDto ledger, DateTime? from, DateTime? to) =>
        _generator.Generate(ledger, from, to);
}
