using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.UseCases.Accounting;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class AccountingEndpoints
{
    public static IEndpointRouteBuilder MapAccountingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/accounting")
            .WithTags("accounting")
            .RequireAuthorization();

        group.MapGet("journal-entries", GetJournalEntryListAsync).WithName("GetJournalEntryList");
        group.MapGet("journal-entries/{entryId:guid}", GetJournalEntryDetailsAsync).WithName("GetJournalEntryDetails");
        group.MapGet("accounts", GetAccountListAsync).WithName("GetAccountList");
        group.MapGet("accounts/{accountId:guid}/ledger", GetAccountLedgerAsync).WithName("GetAccountLedger");
        group.MapGet("reports/trial-balance", GetTrialBalanceAsync).WithName("GetTrialBalance");

        return app;
    }

    private static async Task<IResult> GetJournalEntryListAsync(
        [FromQuery] string? search,
        [FromQuery] JournalEntryStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ICurrentBranchService branchService,
        GetJournalEntryListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetJournalEntryListQuery
        {
            CompanyId = companyId,
            Filter = new JournalEntryListFilter
            {
                Search = search,
                Status = status,
                FromDate = from,
                ToDate = to
            },
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 50
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetJournalEntryDetailsAsync(
        Guid entryId,
        GetJournalEntryDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetJournalEntryDetailsQuery { EntryId = entryId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetAccountListAsync(
        [FromQuery] string? search,
        [FromQuery] GlAccountType? accountType,
        [FromQuery] bool? activeOnly,
        ICurrentBranchService branchService,
        GetAccountListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetAccountListQuery
        {
            CompanyId = companyId,
            Search = search,
            AccountType = accountType,
            ActiveOnly = activeOnly ?? true
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetAccountLedgerAsync(
        Guid accountId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        GetAccountLedgerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetAccountLedgerQuery
        {
            AccountId = accountId,
            FromDate = from ?? DateTime.Today.AddMonths(-3),
            ToDate = to ?? DateTime.Today
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetTrialBalanceAsync(
        [FromQuery] DateTime? asOfDate,
        ICurrentBranchService branchService,
        GetTrialBalanceHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetTrialBalanceQuery
        {
            CompanyId = companyId,
            AsOfDate = asOfDate ?? DateTime.Today
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }
}
