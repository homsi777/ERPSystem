using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class FinanceEndpoints
{
    public static IEndpointRouteBuilder MapFinanceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/finance")
            .WithTags("finance")
            .RequireAuthorization();

        group.MapGet("/cashboxes", GetCashboxesAsync);
        group.MapGet("/payment-methods", GetPaymentMethodsAsync);
        group.MapGet("/bank-accounts", GetBankAccountsAsync);
        group.MapGet("/cashboxes/reconciliation", GetCashboxReconciliationAsync);
        group.MapPost("/receipts/{id:guid}/approve", ApproveReceiptAsync);
        group.MapPost("/receipts/{id:guid}/reverse", ReverseReceiptAsync);

        return app;
    }

    private static async Task<IResult> GetCashboxesAsync(
        ICurrentBranchService branchService,
        GetCashboxListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
            return Results.Unauthorized();
        var list = await handler.HandleAsync(new GetCashboxListQuery { BranchId = branchId }, cancellationToken);
        return Results.Ok(list);
    }

    private static async Task<IResult> GetPaymentMethodsAsync(
        ICurrentBranchService branchService,
        IPaymentMethodRepository repo,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
            return Results.Unauthorized();
        var methods = await repo.GetActiveForCompanyAsync(companyId, cancellationToken);
        var dtos = methods.Select(m => new PaymentMethodDto
        {
            Id = m.Id,
            Code = m.Code,
            Name = m.Name,
            Kind = m.Kind,
            RequiresCashbox = m.RequiresCashbox,
            RequiresBankAccount = m.RequiresBankAccount,
            RequiresReference = m.RequiresReference
        });
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetBankAccountsAsync(
        ICurrentBranchService branchService,
        IBankAccountRepository repo,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
            return Results.Unauthorized();
        var banks = await repo.GetActiveForCompanyAsync(companyId, cancellationToken);
        var dtos = banks.Select(b => new BankAccountListDto
        {
            Id = b.Id,
            Code = b.Code,
            Name = b.Name,
            BankName = b.BankName,
            GlAccountId = b.GlAccountId,
            Currency = b.Currency,
            IsActive = b.IsActive
        });
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetCashboxReconciliationAsync(
        ICurrentBranchService branchService,
        ICashboxReconciliationService service,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
            return Results.Unauthorized();
        var rows = await service.GetReconciliationAsync(companyId, cancellationToken);
        return Results.Ok(rows);
    }

    private static async Task<IResult> ApproveReceiptAsync(
        Guid id,
        ICommandHandler<ApproveReceiptVoucherCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ApproveReceiptVoucherCommand { VoucherId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ReverseReceiptAsync(
        Guid id,
        [FromBody] ReverseReceiptRequest request,
        ICommandHandler<ReverseReceiptVoucherCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReverseReceiptVoucherCommand
        {
            ReceiptVoucherId = id,
            Reason = request.Reason,
            ReversalDate = request.ReversalDate ?? DateTime.UtcNow,
            IdempotencyKey = request.IdempotencyKey
        }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private sealed record ReverseReceiptRequest(string Reason, DateTime? ReversalDate, string? IdempotencyKey);
}
