using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.UseCases.Queries;
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

        group.MapGet("{id:guid}", GetCustomerDetailsAsync)
            .WithName("GetCustomerDetails");

        group.MapGet("{id:guid}/sales-details", GetCustomerSalesDetailsAsync)
            .WithName("GetCustomerSalesDetails");

        group.MapGet("{id:guid}/statement", GetCustomerStatementAsync)
            .WithName("GetCustomerStatement");

        return app;
    }

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
