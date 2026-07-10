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

        group.MapGet("/fabric-search-profiles", GetFabricSearchProfilesAsync)
            .WithName("GetFabricSearchProfiles");

        group.MapGet("/warehouses", GetWarehouseListAsync)
            .WithName("GetInventoryWarehouseList");

        group.MapGet("/warehouses/{warehouseId:guid}/rolls", GetWarehouseRollsAsync)
            .WithName("GetInventoryWarehouseRolls");

        group.MapGet("/rolls-by-stock", GetFabricRollsByStockAsync)
            .WithName("GetInventoryFabricRollsByStock");

        group.MapGet("/roll-sales-reservations", GetRollSalesReservationsAsync)
            .WithName("GetInventoryRollSalesReservations");

        group.MapGet("/detailing-candidate-rolls", GetDetailingCandidateRollsAsync)
            .WithName("GetInventoryDetailingCandidateRolls");

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
        [FromQuery] string? search,
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
            new GetFabricStockBalancesQuery(branchId, warehouseId, search),
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetFabricSearchProfilesAsync(
        [FromQuery] string search,
        [FromQuery] Guid? warehouseId,
        ICurrentBranchService branchService,
        GetFabricSearchProfilesHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(
            new GetFabricSearchProfilesQuery(branchId, search, warehouseId),
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

    private static async Task<IResult> GetWarehouseRollsAsync(
        Guid warehouseId,
        [FromQuery] int? pageNumber,
        [FromQuery] int? pageSize,
        [FromQuery] int? status,
        [FromQuery] string? search,
        GetFabricRollsPageHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetFabricRollsPageQuery(
                warehouseId,
                Math.Max(1, pageNumber ?? 1),
                Math.Clamp(pageSize ?? 50, 10, 500),
                status,
                search),
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetFabricRollsByStockAsync(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid containerId,
        [FromQuery] Guid fabricItemId,
        [FromQuery] Guid fabricColorId,
        GetFabricRollsByStockHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetFabricRollsByStockQuery(warehouseId, containerId, fabricItemId, fabricColorId),
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetRollSalesReservationsAsync(
        [FromQuery] Guid[] rollIds,
        [FromQuery] Guid? excludeSalesInvoiceId,
        GetFabricRollSalesReservationsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetFabricRollSalesReservationsQuery(rollIds, excludeSalesInvoiceId),
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetDetailingCandidateRollsAsync(
        [FromQuery] Guid warehouseId,
        [FromQuery] Guid containerId,
        [FromQuery] Guid fabricItemId,
        [FromQuery] Guid fabricColorId,
        [FromQuery] Guid? excludeSalesInvoiceId,
        GetDetailingCandidateRollsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetDetailingCandidateRollsQuery(warehouseId, containerId, fabricItemId, fabricColorId, excludeSalesInvoiceId),
            cancellationToken);

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
