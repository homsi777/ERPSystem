using ERPSystem.Api.Mapping;
using ERPSystem.Api.Services;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.UseCases.Finance;

namespace ERPSystem.Api.Endpoints;

/// <summary>
/// Read/print surface for payment vouchers (سند صرف). Vouchers are currently created only from
/// the desktop app; this exposes the same stored data for printing from the web client.
/// </summary>
public static class PaymentVoucherEndpoints
{
    public static IEndpointRouteBuilder MapPaymentVoucherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payment-vouchers")
            .WithTags("payment-vouchers")
            .RequireAuthorization();

        group.MapGet("{id:guid}/pdf", GetPaymentVoucherPdfAsync)
            .WithName("GetPaymentVoucherPdf");

        return app;
    }

    private static async Task<IResult> GetPaymentVoucherPdfAsync(
        Guid id,
        GetPaymentVoucherPrintHandler handler,
        PaymentVoucherPdfService pdfService,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPaymentVoucherPrintQuery { VoucherId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result, voucher =>
        {
            var bytes = pdfService.Generate(voucher);
            var fileName = $"سند صرف - {voucher.VoucherNumber} - {voucher.VoucherDate:yyyy-MM-dd}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        });
    }
}
