using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Expenses;
using ERPSystem.Application.Queries.Expenses;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Expenses;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class ExpenseEndpoints
{
    public static IEndpointRouteBuilder MapExpenseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/expenses")
            .WithTags("expenses")
            .RequireAuthorization();

        group.MapGet("", GetExpenseListAsync).WithName("GetExpenseList");
        group.MapGet("entries", GetExpenseEntriesAsync).WithName("GetExpenseEntries");
        group.MapGet("dashboard/summary", GetExpenseDashboardAsync).WithName("GetExpenseDashboard");
        group.MapGet("reports", GetExpenseReportAsync).WithName("GetExpenseReport");
        group.MapGet("categories", GetExpenseCategoriesAsync).WithName("GetExpenseCategories");
        group.MapGet("cost-centers", GetCostCentersAsync).WithName("GetExpenseCostCenters");
        group.MapGet("{expenseId:guid}/operations-center", GetExpenseOperationsCenterAsync).WithName("GetExpenseOperationsCenter");
        group.MapGet("{expenseId:guid}/audit", GetExpenseAuditTrailAsync).WithName("GetExpenseAuditTrail");
        group.MapGet("{expenseId:guid}/timeline", GetExpenseTimelineAsync).WithName("GetExpenseTimeline");
        group.MapGet("{expenseId:guid}", GetExpenseDetailsAsync).WithName("GetExpenseDetails");
        group.MapPost("", CreateExpenseAsync).WithName("CreateExpense");
        group.MapPut("{expenseId:guid}", UpdateExpenseAsync).WithName("UpdateExpense");
        group.MapPost("{expenseId:guid}/approve", ApproveExpenseAsync).WithName("ApproveExpense");
        group.MapPost("{expenseId:guid}/reject", RejectExpenseAsync).WithName("RejectExpense");
        group.MapPost("{expenseId:guid}/pay", PayExpenseAsync).WithName("PayExpense");
        group.MapPost("{expenseId:guid}/duplicate", DuplicateExpenseAsync).WithName("DuplicateExpense");
        group.MapPost("{expenseId:guid}/close", CloseExpenseAsync).WithName("CloseExpense");
        group.MapPost("{expenseId:guid}/cancel", CancelExpenseAsync).WithName("CancelExpense");
        group.MapPost("{expenseId:guid}/archive", ArchiveExpenseAsync).WithName("ArchiveExpense");
        group.MapDelete("{expenseId:guid}", DeleteExpenseAsync).WithName("DeleteExpense");

        return app;
    }

    private static async Task<IResult> GetExpenseListAsync(
        [FromQuery] string? search,
        [FromQuery] ExpenseStatus? status,
        [FromQuery] ExpenseCategoryKind? categoryKind,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool? includeArchived,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ICurrentBranchService branchService,
        GetExpenseListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetExpenseListQuery
        {
            CompanyId = companyId,
            Filter = new ExpenseListFilter
            {
                Search = search,
                Status = status,
                CategoryKind = categoryKind,
                FromDate = from,
                ToDate = to,
                IncludeArchived = includeArchived ?? false
            },
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 50
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseEntriesAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? expenseId,
        [FromQuery] Guid? cashboxId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        ICurrentBranchService branchService,
        GetExpenseEntriesHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetExpenseEntriesQuery
        {
            CompanyId = companyId,
            Filter = new ExpenseEntryListFilter
            {
                Search = search,
                ExpenseId = expenseId,
                CashboxId = cashboxId,
                FromDate = from,
                ToDate = to
            },
            Page = page > 0 ? page : 1,
            PageSize = pageSize > 0 ? pageSize : 100
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseReportAsync(
        [FromQuery] string reportType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ExpenseCategoryKind? categoryKind,
        [FromQuery] Guid? costCenterId,
        ICurrentBranchService branchService,
        GetExpenseReportHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetExpenseReportQuery
        {
            CompanyId = companyId,
            ReportType = string.IsNullOrWhiteSpace(reportType) ? "Detailed" : reportType,
            FromDate = from,
            ToDate = to,
            CategoryKind = categoryKind,
            CostCenterId = costCenterId
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseDashboardAsync(
        ICurrentBranchService branchService,
        GetExpenseDashboardHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetExpenseDashboardQuery { CompanyId = companyId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseCategoriesAsync(
        ICurrentBranchService branchService,
        GetExpenseCategoriesHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetExpenseCategoriesQuery { CompanyId = companyId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetCostCentersAsync(
        ICurrentBranchService branchService,
        GetCostCentersHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new GetCostCentersQuery { CompanyId = companyId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseOperationsCenterAsync(
        Guid expenseId,
        GetExpenseOperationsCenterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExpenseOperationsCenterQuery { ExpenseId = expenseId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseAuditTrailAsync(
        Guid expenseId,
        GetExpenseAuditTrailHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExpenseAuditTrailQuery { ExpenseId = expenseId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseTimelineAsync(
        Guid expenseId,
        GetExpenseTimelineHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExpenseTimelineQuery { ExpenseId = expenseId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetExpenseDetailsAsync(
        Guid expenseId,
        GetExpenseDetailsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetExpenseDetailsQuery { ExpenseId = expenseId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CreateExpenseAsync(
        [FromBody] CreateExpenseRequest request,
        ICurrentBranchService branchService,
        ICommandHandler<CreateExpenseCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId || branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new CreateExpenseCommand
        {
            CompanyId = companyId,
            BranchId = branchId,
            Name = request.Name,
            CategoryId = request.CategoryId,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OriginalCurrency = string.IsNullOrWhiteSpace(request.OriginalCurrency) ? "USD" : request.OriginalCurrency,
            OriginalAmount = request.OriginalAmount,
            ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
            BaseCurrency = string.IsNullOrWhiteSpace(request.BaseCurrency) ? "USD" : request.BaseCurrency,
            PaymentMethod = request.PaymentMethod,
            PayeeName = request.PayeeName,
            SupplierId = request.SupplierId,
            CostCenterId = request.CostCenterId,
            Department = request.Department,
            Notes = request.Notes,
            SubmitForApproval = request.SubmitForApproval
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> UpdateExpenseAsync(
        Guid expenseId,
        [FromBody] CreateExpenseRequest request,
        ICommandHandler<UpdateExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateExpenseCommand
        {
            ExpenseId = expenseId,
            Name = request.Name,
            CategoryId = request.CategoryId,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            OriginalCurrency = string.IsNullOrWhiteSpace(request.OriginalCurrency) ? "USD" : request.OriginalCurrency,
            OriginalAmount = request.OriginalAmount,
            ExchangeRate = request.ExchangeRate <= 0 ? 1m : request.ExchangeRate,
            BaseCurrency = string.IsNullOrWhiteSpace(request.BaseCurrency) ? "USD" : request.BaseCurrency,
            PaymentMethod = request.PaymentMethod,
            PayeeName = request.PayeeName,
            SupplierId = request.SupplierId,
            CostCenterId = request.CostCenterId,
            Department = request.Department,
            Notes = request.Notes
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ApproveExpenseAsync(
        Guid expenseId,
        [FromBody] ExpenseReasonRequest? request,
        ICommandHandler<ApproveExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ApproveExpenseCommand { ExpenseId = expenseId, Reason = request?.Reason }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> RejectExpenseAsync(
        Guid expenseId,
        [FromBody] ExpenseReasonRequest? request,
        ICommandHandler<RejectExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RejectExpenseCommand { ExpenseId = expenseId, Reason = request?.Reason }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> PayExpenseAsync(
        Guid expenseId,
        [FromBody] PayExpenseRequest request,
        ICommandHandler<RecordExpensePaymentCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency;
        var amountOriginal = request.AmountOriginal > 0 ? request.AmountOriginal : request.Amount;
        var exchangeRate = request.ExchangeRateSnapshot <= 0 ? 1m : request.ExchangeRateSnapshot;
        var amountBase = request.AmountBase > 0
            ? request.AmountBase
            : string.Equals(currency, "USD", StringComparison.OrdinalIgnoreCase)
                ? amountOriginal
                : Math.Round(amountOriginal / exchangeRate, 4);

        var result = await handler.HandleAsync(new RecordExpensePaymentCommand
        {
            ExpenseId = expenseId,
            PaymentDate = request.PaymentDate == default ? DateTime.UtcNow : request.PaymentDate,
            AmountOriginal = amountOriginal,
            AmountBase = amountBase,
            Currency = currency,
            ExchangeRateSnapshot = exchangeRate,
            PaymentMethod = request.PaymentMethod,
            FundingSource = request.FundingSource,
            ReferenceNumber = request.ReferenceNumber,
            Notes = request.Notes,
            CashboxId = request.CashboxId
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> DuplicateExpenseAsync(
        Guid expenseId,
        ICurrentBranchService branchService,
        ICommandHandler<DuplicateExpenseCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (branchService.BranchId is not Guid branchId)
        {
            return Results.Json(
                new ApiErrorResponse("Unauthorized", "Authentication required.", []),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await handler.HandleAsync(new DuplicateExpenseCommand
        {
            ExpenseId = expenseId,
            BranchId = branchId
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CloseExpenseAsync(
        Guid expenseId,
        [FromBody] ExpenseReasonRequest? request,
        ICommandHandler<CloseExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CloseExpenseCommand { ExpenseId = expenseId, Reason = request?.Reason }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CancelExpenseAsync(
        Guid expenseId,
        [FromBody] ExpenseReasonRequest? request,
        ICommandHandler<CancelExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CancelExpenseCommand { ExpenseId = expenseId, Reason = request?.Reason }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ArchiveExpenseAsync(
        Guid expenseId,
        [FromBody] ExpenseReasonRequest? request,
        ICommandHandler<ArchiveExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ArchiveExpenseCommand { ExpenseId = expenseId, Reason = request?.Reason }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> DeleteExpenseAsync(
        Guid expenseId,
        ICommandHandler<DeleteExpenseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteExpenseCommand { ExpenseId = expenseId }, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private sealed record CreateExpenseRequest(
        string Name,
        Guid CategoryId,
        string? Description,
        DateTime StartDate,
        DateTime? EndDate,
        string? OriginalCurrency,
        decimal OriginalAmount,
        decimal ExchangeRate,
        string? BaseCurrency,
        ExpensePaymentMethod PaymentMethod,
        string? PayeeName,
        Guid? SupplierId,
        Guid? CostCenterId,
        string? Department,
        string? Notes,
        bool SubmitForApproval);

    private sealed record ExpenseReasonRequest(string? Reason);

    private sealed record PayExpenseRequest(
        DateTime PaymentDate,
        decimal Amount,
        decimal AmountOriginal,
        decimal AmountBase,
        decimal ExchangeRateSnapshot,
        string? Currency,
        ExpensePaymentMethod PaymentMethod,
        ExpenseFundingSource FundingSource,
        string? ReferenceNumber,
        string? Notes,
        Guid? CashboxId);
}
