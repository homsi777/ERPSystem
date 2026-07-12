using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Finance;

namespace ERPSystem.Api.Services;

public sealed class ReceiptVoucherPdfService
{
    private readonly ReceiptVoucherPdfGenerator _generator;

    public ReceiptVoucherPdfService(IWebHostEnvironment environment) =>
        _generator = ReceiptVoucherPdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(ReceiptVoucherPrintDto voucher) => _generator.Generate(voucher);
}
