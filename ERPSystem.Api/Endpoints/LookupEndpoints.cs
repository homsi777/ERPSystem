using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Queries.Suppliers;
using ERPSystem.Application.Queries.Warehouses;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Application.UseCases.Queries;

namespace ERPSystem.Api.Endpoints;

public static class LookupEndpoints
{
    public static IEndpointRouteBuilder MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/lookups")
            .WithTags("lookups")
            .RequireAuthorization();

        group.MapGet("/suppliers", GetSuppliersAsync)
            .WithName("GetSupplierLookups");

        group.MapGet("/warehouses", GetWarehousesAsync)
            .WithName("GetWarehouseLookups");

        group.MapGet("/cashboxes", GetCashboxesAsync)
            .WithName("GetCashboxLookups");

        return app;
    }

    private static async Task<IResult> GetSuppliersAsync(
        ICurrentBranchService branchService,
        GetSupplierListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetSupplierListQuery
        {
            CompanyId = companyId,
            Page = 1,
            PageSize = 500
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result, suppliers => Results.Ok(
            suppliers.Items
                .Where(s => s.IsActive)
                .Select(s => new LookupItemResponse(s.Id, string.IsNullOrWhiteSpace(s.NameAr) ? s.NameEn : s.NameAr))
                .OrderBy(s => s.Name)
                .ToList()));
    }

    private static async Task<IResult> GetWarehousesAsync(
        ICurrentBranchService branchService,
        GetWarehouseListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetWarehouseListQuery { BranchId = branchId }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result, warehouses => Results.Ok(
            warehouses
                .Where(w => w.IsActive)
                .Select(w => new LookupItemResponse(w.Id, w.NameAr))
                .OrderBy(w => w.Name)
                .ToList()));
    }

    private static async Task<IResult> GetCashboxesAsync(
        ICurrentBranchService branchService,
        GetCashboxListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetCashboxListQuery { BranchId = branchId }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result, cashboxes => Results.Ok(
            cashboxes
                .Where(c => c.IsActive)
                .Select(c => new LookupItemResponse(c.Id, c.Name))
                .OrderBy(c => c.Name)
                .ToList()));
    }

    private sealed record LookupItemResponse(Guid Id, string Name);
}
