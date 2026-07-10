using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Queries.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Application.UseCases.Sales;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/sales")
            .WithTags("sales")
            .RequireAuthorization();

        group.MapGet("invoices", GetInvoiceListAsync).WithName("GetSalesInvoiceList");
        group.MapGet("invoices/{invoiceId:guid}", GetInvoiceDetailsAsync).WithName("GetSalesInvoiceDetails");
        group.MapPost("invoices", CreateInvoiceAsync).WithName("CreateSalesInvoice");
        group.MapPost("invoices/{invoiceId:guid}/send-to-warehouse", SendToWarehouseAsync).WithName("SendSalesInvoiceToWarehouse");
        group.MapPost("invoices/{invoiceId:guid}/approve", ApproveInvoiceAsync).WithName("ApproveSalesInvoice");
        group.MapPost("invoices/{invoiceId:guid}/cancel", CancelInvoiceAsync).WithName("CancelSalesInvoice");
        group.MapGet("invoices/{invoiceId:guid}/below-cost", GetBelowCostAsync).WithName("GetSalesInvoiceBelowCost");
        group.MapGet("warehouse-stock", GetWarehouseStockAsync).WithName("GetSalesWarehouseStock");

        return app;
    }

    private static async Task<IResult> GetInvoiceListAsync(
        [FromQuery] SalesInvoiceStatus? status,
        [FromQuery] Guid? customerId,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ICurrentBranchService branchService,
        GetSalesInvoiceListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetSalesInvoiceListQuery
        {
            CompanyId = companyId,
            BranchId = branchService.BranchId,
            Status = status,
            CustomerId = customerId,
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 50
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetInvoiceDetailsAsync(
        Guid invoiceId,
        GetSalesInvoiceOperationsCenterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesInvoiceOperationsCenterQuery { InvoiceId = invoiceId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CreateInvoiceAsync(
        [FromBody] CreateSalesInvoiceRequest request,
        ICurrentBranchService branchService,
        ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId || branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new CreateSalesInvoiceDraftCommand
        {
            CompanyId = companyId,
            BranchId = branchId,
            InvoiceNumber = request.InvoiceNumber,
            CustomerId = request.CustomerId,
            WarehouseId = request.WarehouseId,
            ChinaContainerId = request.ChinaContainerId,
            PaymentType = request.PaymentType,
            DiscountAmount = request.DiscountAmount,
            PartialPaymentAmount = request.PartialPaymentAmount,
            Lines = request.Lines.Select(l => new SalesInvoiceLineCommand
            {
                LineNumber = l.LineNumber,
                ChinaContainerId = l.ChinaContainerId,
                FabricItemId = l.FabricItemId,
                FabricColorId = l.FabricColorId,
                RollCount = l.RollCount,
                UnitPrice = l.UnitPrice,
                OriginalUnitPrice = l.OriginalUnitPrice == 0 ? l.UnitPrice : l.OriginalUnitPrice,
                DiscountReason = l.DiscountReason,
                Notes = l.Notes
            }).ToList()
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> SendToWarehouseAsync(
        Guid invoiceId,
        ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new SendSalesInvoiceToWarehouseCommand { InvoiceId = invoiceId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ApproveInvoiceAsync(
        Guid invoiceId,
        ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ApproveSalesInvoiceCommand { InvoiceId = invoiceId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CancelInvoiceAsync(
        Guid invoiceId,
        [FromBody] CancelSalesInvoiceRequest request,
        ICommandHandler<CancelSalesInvoiceCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CancelSalesInvoiceCommand
        {
            InvoiceId = invoiceId,
            Reason = request.Reason ?? ""
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetBelowCostAsync(
        Guid invoiceId,
        CheckSalesInvoiceBelowCostHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CheckSalesInvoiceBelowCostQuery { InvoiceId = invoiceId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetWarehouseStockAsync(
        [FromQuery] Guid containerId,
        [FromQuery] Guid warehouseId,
        GetSalesWarehouseStockHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetSalesWarehouseStockQuery
        {
            ContainerId = containerId,
            WarehouseId = warehouseId
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private sealed record CreateSalesInvoiceRequest(
        Guid CustomerId,
        Guid WarehouseId,
        Guid ChinaContainerId,
        PaymentType PaymentType,
        decimal DiscountAmount,
        decimal? PartialPaymentAmount,
        string? InvoiceNumber,
        IReadOnlyList<SalesInvoiceLineRequest> Lines);

    private sealed record SalesInvoiceLineRequest(
        int LineNumber,
        Guid ChinaContainerId,
        Guid FabricItemId,
        Guid FabricColorId,
        int RollCount,
        decimal UnitPrice,
        decimal OriginalUnitPrice,
        string? DiscountReason,
        string? Notes);

    private sealed record CancelSalesInvoiceRequest(string? Reason);
}
