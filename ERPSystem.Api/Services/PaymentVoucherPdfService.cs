using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Finance;

namespace ERPSystem.Api.Services;

public sealed class PaymentVoucherPdfService
{
    private readonly PaymentVoucherPdfGenerator _generator;

    public PaymentVoucherPdfService(IWebHostEnvironment environment) =>
        _generator = PaymentVoucherPdfGenerator.FromContentRoot(environment.ContentRootPath);

    public byte[] Generate(PaymentVoucherPrintDto voucher) => _generator.Generate(voucher);
}
