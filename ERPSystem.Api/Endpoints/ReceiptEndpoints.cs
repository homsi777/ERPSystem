using ERPSystem.Api.Mapping;
using ERPSystem.Api.Services;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Finance;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class ReceiptEndpoints
{
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/receipts")
            .WithTags("receipts")
            .RequireAuthorization();

        group.MapPost("", CreateReceiptAsync)
            .WithName("CreateReceiptVoucher");

        group.MapPost("{id:guid}/post", PostReceiptAsync)
            .WithName("PostReceiptVoucher");

        group.MapGet("{id:guid}/pdf", GetReceiptPdfAsync)
            .WithName("GetReceiptVoucherPdf");

        return app;
    }

    private static async Task<IResult> CreateReceiptAsync(
        [FromBody] CreateReceiptVoucherRequest request,
        ICurrentBranchService branchService,
        ICommandHandler<CreateReceiptVoucherCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId || branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new CreateReceiptVoucherCommand
        {
            CompanyId = companyId,
            BranchId = branchId,
            CustomerId = request.CustomerId,
            CashboxId = request.CashboxId,
            Amount = request.Amount,
            Allocations = request.Allocations
                .Select(a => new ReceiptInvoiceAllocationInput
                {
                    SalesInvoiceId = a.SalesInvoiceId,
                    Amount = a.Amount
                })
                .ToList()
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> PostReceiptAsync(
        Guid id,
        ICommandHandler<PostReceiptVoucherCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PostReceiptVoucherCommand { VoucherId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetReceiptPdfAsync(
        Guid id,
        GetReceiptVoucherPrintHandler handler,
        ReceiptVoucherPdfService pdfService,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetReceiptVoucherPrintQuery { VoucherId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result, voucher =>
        {
            var bytes = pdfService.Generate(voucher);
            var fileName = $"سند قبض - {voucher.VoucherNumber} - {voucher.VoucherDate:yyyy-MM-dd}.pdf";
            return Results.File(bytes, "application/pdf", fileName);
        });
    }

    private sealed record CreateReceiptVoucherRequest(
        Guid CustomerId,
        Guid CashboxId,
        decimal Amount,
        IReadOnlyList<ReceiptAllocationRequest> Allocations);

    private sealed record ReceiptAllocationRequest(Guid SalesInvoiceId, decimal Amount);
}
