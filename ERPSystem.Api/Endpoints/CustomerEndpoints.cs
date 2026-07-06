using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customers")
            .WithTags("customers")
            .RequireAuthorization();

        group.MapGet("", GetCustomerListAsync)
            .WithName("GetCustomerList");

        group.MapPost("", CreateCustomerAsync)
            .WithName("CreateCustomer");

        group.MapGet("{id:guid}", GetCustomerDetailsAsync)
            .WithName("GetCustomerDetails");

        group.MapPut("{id:guid}", UpdateCustomerAsync)
            .WithName("UpdateCustomer");

        group.MapPost("{id:guid}/deactivate", DeactivateCustomerAsync)
            .WithName("DeactivateCustomer");

        group.MapPost("{id:guid}/opening-balance", PostCustomerOpeningBalanceAsync)
            .WithName("PostCustomerOpeningBalance");

        group.MapGet("{id:guid}/sales-details", GetCustomerSalesDetailsAsync)
            .WithName("GetCustomerSalesDetails");

        group.MapGet("{id:guid}/statement", GetCustomerStatementAsync)
            .WithName("GetCustomerStatement");

        group.MapGet("{id:guid}/ledger", GetCustomerAccountLedgerAsync)
            .WithName("GetCustomerAccountLedger");

        group.MapPost("{id:guid}/reconcile", ReconcileCustomerAccountAsync)
            .WithName("ReconcileCustomerAccount");

        return app;
    }

    private static async Task<IResult> CreateCustomerAsync(
        [FromBody] CreateCustomerRequest request,
        ICurrentBranchService branchService,
        ICommandHandler<CreateCustomerCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new CreateCustomerCommand
        {
            CompanyId = companyId,
            Code = request.Code,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            Type = request.Type,
            CreditLimit = request.CreditLimit,
            CreditLimitEnabled = request.CreditLimitEnabled
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> UpdateCustomerAsync(
        Guid id,
        [FromBody] UpdateCustomerRequest request,
        ICommandHandler<UpdateCustomerCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateCustomerCommand
        {
            CustomerId = id,
            NameAr = request.NameAr,
            NameEn = request.NameEn,
            CreditLimit = request.CreditLimit,
            CreditLimitEnabled = request.CreditLimitEnabled,
            PaymentTermsDays = request.PaymentTermsDays
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> DeactivateCustomerAsync(
        Guid id,
        ICommandHandler<DeactivateCustomerCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeactivateCustomerCommand { CustomerId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> PostCustomerOpeningBalanceAsync(
        Guid id,
        [FromBody] PostCustomerOpeningBalanceRequest request,
        ICommandHandler<PostCustomerOpeningBalanceCommand, ApplicationResult<Application.DTOs.Customers.CustomerOpeningBalanceResultDto>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PostCustomerOpeningBalanceCommand
        {
            CustomerId = id,
            Amount = request.Amount,
            PostingDate = request.PostingDate,
            ReferenceNote = request.ReferenceNote
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetCustomerAccountLedgerAsync(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        GetCustomerAccountLedgerHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerAccountLedgerQuery
        {
            CustomerId = id,
            FromDate = from,
            ToDate = to
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ReconcileCustomerAccountAsync(
        Guid id,
        [FromBody] ReconcileCustomerAccountRequest request,
        ICommandHandler<ReconcileCustomerAccountCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ReconcileCustomerAccountCommand
        {
            CustomerId = id,
            ReconciliationDate = request.ReconciliationDate,
            DocumentId = request.DocumentId,
            BalanceAtReconciliation = request.BalanceAtReconciliation
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private sealed record CreateCustomerRequest(
        string Code,
        string NameAr,
        string NameEn,
        CustomerType Type,
        decimal CreditLimit,
        bool CreditLimitEnabled);

    private sealed record UpdateCustomerRequest(
        string NameAr,
        string NameEn,
        decimal CreditLimit,
        bool CreditLimitEnabled,
        int PaymentTermsDays);

    private sealed record PostCustomerOpeningBalanceRequest(
        decimal Amount,
        DateTime PostingDate,
        string? ReferenceNote);

    private sealed record ReconcileCustomerAccountRequest(
        DateTime ReconciliationDate,
        Guid DocumentId,
        decimal BalanceAtReconciliation);

    private static async Task<IResult> GetCustomerListAsync(
        [FromQuery] string? search,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ICurrentBranchService branchService,
        GetCustomerListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetCustomerListQuery
        {
            CompanyId = companyId,
            Search = search,
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 50
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetCustomerDetailsAsync(
        Guid id,
        GetCustomerDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerDetailsQuery { CustomerId = id }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetCustomerSalesDetailsAsync(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        GetCustomerSalesDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerSalesDetailsQuery
        {
            CustomerId = id,
            FromDate = from,
            ToDate = to
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetCustomerStatementAsync(
        Guid id,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        GetCustomerStatementHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerStatementQuery
        {
            CustomerId = id,
            FromDate = from,
            ToDate = to
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }
}
