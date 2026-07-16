using ERPSystem.Api.Mapping;
using ERPSystem.Api.Services;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class PaymentVoucherEndpoints
{
    public static IEndpointRouteBuilder MapPaymentVoucherEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/payment-vouchers")
            .WithTags("payment-vouchers")
            .RequireAuthorization();

        group.MapGet("", GetListAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);
        group.MapGet("/supplier/{supplierId:guid}", GetBySupplierAsync);
        group.MapPost("", CreateAsync);
        group.MapPost("/{id:guid}/approve", ApproveAsync);
        group.MapPost("/{id:guid}/post", PostAsync);
        group.MapGet("/{id:guid}/pdf", GetPaymentVoucherPdfAsync);
        return app;
    }

    private static async Task<IResult> GetListAsync(
        VoucherStatus? status,
        Guid? supplierId,
        ICurrentBranchService current,
        GetPaymentVoucherListHandler handler,
        CancellationToken cancellationToken)
    {
        if (current.CompanyId is not Guid companyId) return Results.Unauthorized();
        var result = await handler.HandleAsync(new GetPaymentVoucherListQuery
        {
            CompanyId = companyId, Status = status, SupplierId = supplierId
        }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static Task<IResult> GetBySupplierAsync(
        Guid supplierId,
        ICurrentBranchService current,
        GetPaymentVoucherListHandler handler,
        CancellationToken cancellationToken) =>
        GetListAsync(null, supplierId, current, handler, cancellationToken);

    private static async Task<IResult> GetByIdAsync(
        Guid id,
        GetPaymentVoucherDetailsHandler handler,
        CancellationToken cancellationToken) =>
        ApplicationResultHttpMapper.ToHttpResult(await handler.HandleAsync(
            new GetPaymentVoucherDetailsQuery { VoucherId = id }, cancellationToken));

    private static async Task<IResult> CreateAsync(
        [FromBody] CreatePaymentVoucherRequest request,
        ICurrentBranchService current,
        ICommandHandler<CreatePaymentVoucherCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (current.CompanyId is not Guid companyId || current.BranchId is not Guid branchId)
            return Results.Unauthorized();
        var result = await handler.HandleAsync(new CreatePaymentVoucherCommand
        {
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = request.SupplierId,
            CashboxId = request.CashboxId,
            BankAccountId = request.BankAccountId,
            PaymentMethodId = request.PaymentMethodId,
            PurchaseInvoiceId = request.PurchaseInvoiceId,
            Amount = request.Amount,
            Currency = request.Currency ?? "USD",
            Reference = request.Reference
        }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ApproveAsync(
        Guid id,
        ICommandHandler<ApprovePaymentVoucherCommand, ApplicationResult> handler,
        CancellationToken cancellationToken) =>
        ApplicationResultHttpMapper.ToHttpResult(await handler.HandleAsync(
            new ApprovePaymentVoucherCommand { VoucherId = id }, cancellationToken));

    private static async Task<IResult> PostAsync(
        Guid id,
        [FromBody] PostPaymentVoucherRequest? request,
        ICommandHandler<PostPaymentVoucherCommand, ApplicationResult> handler,
        CancellationToken cancellationToken) =>
        ApplicationResultHttpMapper.ToHttpResult(await handler.HandleAsync(
            new PostPaymentVoucherCommand { VoucherId = id, PurchaseInvoiceId = request?.PurchaseInvoiceId }, cancellationToken));

    private static async Task<IResult> GetPaymentVoucherPdfAsync(
        Guid id,
        GetPaymentVoucherPrintHandler handler,
        PaymentVoucherPdfService pdfService,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetPaymentVoucherPrintQuery { VoucherId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result, voucher => Results.File(
            pdfService.Generate(voucher), "application/pdf", $"payment-{voucher.VoucherNumber}-{voucher.VoucherDate:yyyy-MM-dd}.pdf"));
    }

    private sealed record CreatePaymentVoucherRequest(
        Guid SupplierId,
        Guid? CashboxId,
        Guid? BankAccountId,
        Guid PaymentMethodId,
        Guid? PurchaseInvoiceId,
        decimal Amount,
        string? Currency,
        string? Reference);

    private sealed record PostPaymentVoucherRequest(Guid? PurchaseInvoiceId);
}
