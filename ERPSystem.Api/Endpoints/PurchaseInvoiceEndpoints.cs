using ERPSystem.Api.Mapping;
using ERPSystem.Application.Queries.Purchases;
using ERPSystem.Application.UseCases.Queries;

namespace ERPSystem.Api.Endpoints;

public static class PurchaseInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapPurchaseInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/v1/purchase-invoices")
            .WithTags("purchase-invoices")
            .RequireAuthorization()
            .MapGet("/{id:guid}", GetOperationsAsync);
        return app;
    }

    private static async Task<IResult> GetOperationsAsync(
        Guid id,
        GetPurchaseInvoiceOperationsCenterHandler handler,
        CancellationToken cancellationToken) =>
        ApplicationResultHttpMapper.ToHttpResult(await handler.HandleAsync(
            new GetPurchaseInvoiceOperationsCenterQuery { InvoiceId = id }, cancellationToken));
}
