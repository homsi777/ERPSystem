using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.UseCases.Queries;

namespace ERPSystem.Api.Endpoints;

public static class DashboardEndpoints
{
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/dashboard")
            .WithTags("dashboard")
            .RequireAuthorization();

        group.MapGet("/summary", GetSummaryAsync)
            .WithName("GetDashboardSummary");

        return app;
    }

    private static async Task<IResult> GetSummaryAsync(
        ICurrentBranchService branchService,
        GetDashboardSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId || branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(
            new GetDashboardSummaryQuery { CompanyId = companyId, BranchId = branchId },
            cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }
}
