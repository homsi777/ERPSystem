using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Inventory;
using ERPSystem.Application.UseCases.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/inventory")
            .WithTags("inventory")
            .RequireAuthorization();

        group.MapGet("/stock", GetFabricStockAsync)
            .WithName("GetFabricStockBalances");

        return app;
    }

    private static async Task<IResult> GetFabricStockAsync(
        [FromQuery] Guid? warehouseId,
        ICurrentBranchService branchService,
        GetFabricStockBalancesHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(
            new GetFabricStockBalancesQuery(branchId, warehouseId),
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }
}
