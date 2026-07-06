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

        group.MapGet("/warehouses", GetWarehouseListAsync)
            .WithName("GetInventoryWarehouseList");

        group.MapGet("/dashboard", GetDashboardAsync)
            .WithName("GetInventoryDashboard");

        group.MapGet("/movements", GetMovementsAsync)
            .WithName("GetInventoryMovements");

        group.MapGet("/alerts", GetAlertsAsync)
            .WithName("GetInventoryAlerts");

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

    private static async Task<IResult> GetWarehouseListAsync(
        ICurrentBranchService branchService,
        GetInventoryWarehouseListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetInventoryWarehouseListQuery(branchId), cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetDashboardAsync(
        ICurrentBranchService branchService,
        GetInventoryDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetInventoryDashboardQuery(branchId), cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetMovementsAsync(
        [FromQuery] Guid? warehouseId,
        ICurrentBranchService branchService,
        GetInventoryMovementsHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetInventoryMovementsQuery(branchId, warehouseId), cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetAlertsAsync(
        [FromQuery] bool? unacknowledgedOnly,
        ICurrentBranchService branchService,
        GetInventoryAlertsHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(
            new GetInventoryAlertsQuery(branchId, unacknowledgedOnly ?? true),
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }
}
