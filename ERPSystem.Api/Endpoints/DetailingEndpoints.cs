using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class DetailingEndpoints
{
    public static IEndpointRouteBuilder MapDetailingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/detailing")
            .WithTags("detailing")
            .RequireAuthorization();

        group.MapGet("/queue", GetQueueAsync)
            .WithName("GetWarehouseDetailingQueue");

        group.MapGet("/{invoiceId:guid}", GetDetailingAsync)
            .WithName("GetWarehouseDetailing");

        group.MapPost("/{invoiceId:guid}/complete", CompleteDetailingAsync)
            .WithName("CompleteWarehouseDetailing");

        return app;
    }

    private static async Task<IResult> GetQueueAsync(
        [FromQuery] Guid? warehouseId,
        GetWarehouseDetailingQueueHandler handler,
        CancellationToken cancellationToken)
    {
        if (warehouseId is not Guid resolvedWarehouseId || resolvedWarehouseId == Guid.Empty)
        {
            return Results.Json(
                new ApiErrorResponse("ValidationFailed", "Warehouse is required.", []),
                statusCode: StatusCodes.Status400BadRequest);
        }

        var result = await handler.HandleAsync(
            new GetWarehouseDetailingQueueQuery { WarehouseId = resolvedWarehouseId },
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetDetailingAsync(
        Guid invoiceId,
        GetSalesInvoiceOperationsCenterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new GetSalesInvoiceOperationsCenterQuery { InvoiceId = invoiceId },
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(
            result,
            onSuccess: value => value.Detailing is WarehouseDetailingDto detailing
                ? Results.Ok(detailing)
                : Results.NotFound(new ApiErrorResponse("NotFound", "Invoice is not awaiting detailing.", [])));
    }

    private static async Task<IResult> CompleteDetailingAsync(
        Guid invoiceId,
        [FromBody] CompleteWarehouseDetailingRequest request,
        ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CompleteWarehouseDetailingCommand
            {
                InvoiceId = invoiceId,
                RollEntries = request.RollEntries
                    .Select(entry => new RollLengthEntryCommand
                    {
                        RollDetailId = entry.RollDetailId,
                        LengthMeters = entry.LengthMeters
                    })
                    .ToList()
            },
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private sealed record CompleteWarehouseDetailingRequest(IReadOnlyList<RollLengthEntryRequest> RollEntries);

    private sealed record RollLengthEntryRequest(Guid RollDetailId, decimal LengthMeters);
}
