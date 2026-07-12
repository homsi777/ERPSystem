using ERPSystem.Api.Mapping;
using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Common;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ERPSystem.Api.Endpoints;

public static class ContainerEndpoints
{
    public static IEndpointRouteBuilder MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/containers")
            .WithTags("containers")
            .RequireAuthorization();

        group.MapGet("", GetContainerListAsync)
            .WithName("GetChinaContainerList");

        group.MapGet("{id:guid}/operations", GetContainerOperationsAsync)
            .WithName("GetContainerOperationsCenter");

        group.MapPost("", CreateContainerAsync)
            .WithName("CreateChinaContainer");

        group.MapPost("{id:guid}/landing-cost", CalculateLandingCostAsync)
            .WithName("CalculateContainerLandingCost");

        group.MapPost("{id:guid}/sale-prices", SetSalePricesAsync)
            .WithName("SetContainerSalePrices");

        group.MapPost("{id:guid}/approve", ApproveContainerAsync)
            .WithName("ApproveContainer");

        group.MapPost("{id:guid}/move-to-warehouse", MoveToWarehouseAsync)
            .WithName("MoveContainerToWarehouse");

        group.MapPost("{id:guid}/archive", ArchiveContainerAsync)
            .WithName("ArchiveContainer");

        group.MapPost("aliases", SaveFabricTypeAliasAsync)
            .WithName("SaveContainerFabricTypeAlias");

        group.MapPost("parse/dpl", ParseDplAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .WithName("ParseContainerDpl");

        group.MapPost("parse/invoice", ParseInvoiceAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .WithName("ParseChinaInvoice");

        group.MapPost("parse/packing-summary", ParsePackingSummaryAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .WithName("ParseChinaPackingSummary");

        return app;
    }

    private static async Task<IResult> GetContainerListAsync(
        [FromQuery] ChinaContainerStatus? status,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        ICurrentBranchService branchService,
        GetChinaContainerListHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
            return Unauthorized();

        var result = await handler.HandleAsync(new GetChinaContainerListQuery
        {
            CompanyId = companyId,
            BranchId = branchService.BranchId,
            Status = status,
            Page = page is > 0 ? page.Value : 1,
            PageSize = pageSize is > 0 ? pageSize.Value : 50
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> GetContainerOperationsAsync(
        Guid id,
        GetContainerOperationsCenterHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerOperationsCenterQuery
        {
            ContainerId = id
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CreateContainerAsync(
        [FromBody] CreateChinaContainerRequest request,
        ICurrentBranchService branchService,
        IPermissionService permissions,
        ICommandHandler<CreateChinaContainerCommand, ApplicationResult<Guid>> handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId || branchService.BranchId is not Guid branchId)
            return Unauthorized();

        var permissionResult = await EnsureGeneralManagerAsync(permissions, cancellationToken);
        if (permissionResult is not null)
            return permissionResult;

        var result = await handler.HandleAsync(new CreateChinaContainerCommand
        {
            CompanyId = companyId,
            BranchId = branchId,
            SupplierId = request.SupplierId,
            ContainerNumber = request.ContainerNumber,
            ShipmentDate = request.ShipmentDate,
            ExpectedArrival = request.ExpectedArrival,
            Notes = request.Notes,
            ExchangeRateToLocalCurrency = request.ExchangeRateToLocalCurrency,
            ChinaInvoiceAmountUsd = request.ChinaInvoiceAmountUsd,
            ImportFileName = request.ImportFileName,
            Lines = request.Lines
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> CalculateLandingCostAsync(
        Guid id,
        [FromBody] CalculateLandingCostRequest request,
        IPermissionService permissions,
        ICommandHandler<CalculateLandingCostCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var permissionResult = await EnsureGeneralManagerAsync(permissions, cancellationToken);
        if (permissionResult is not null)
            return permissionResult;

        var result = await handler.HandleAsync(new CalculateLandingCostCommand
        {
            ContainerId = id,
            TotalLengthMeters = request.TotalLengthMeters,
            ContainerWeightKg = request.ContainerWeightKg,
            CustomsClearanceAmount = request.CustomsClearanceAmount,
            Shipping = request.Shipping,
            Insurance = request.Insurance,
            OtherExpense1 = request.OtherExpense1,
            OtherExpense2 = request.OtherExpense2,
            OtherExpense3 = request.OtherExpense3,
            OtherExpense4 = request.OtherExpense4,
            UsesWeightedAllocation = request.UsesWeightedAllocation,
            TypeLines = request.TypeLines,
            CustomsAmount = request.CustomsAmount,
            Clearance = request.Clearance,
            OtherExpenses = request.OtherExpenses
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> SetSalePricesAsync(
        Guid id,
        [FromBody] SetContainerSalePricesRequest request,
        IPermissionService permissions,
        ICommandHandler<SetContainerTypeSalePricesCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var permissionResult = await EnsureGeneralManagerAsync(permissions, cancellationToken);
        if (permissionResult is not null)
            return permissionResult;

        var result = await handler.HandleAsync(new SetContainerTypeSalePricesCommand
        {
            ContainerId = id,
            Lines = request.Lines
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ApproveContainerAsync(
        Guid id,
        IPermissionService permissions,
        ICommandHandler<ApproveContainerCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var permissionResult = await EnsureGeneralManagerAsync(permissions, cancellationToken);
        if (permissionResult is not null)
            return permissionResult;

        var result = await handler.HandleAsync(new ApproveContainerCommand
        {
            ContainerId = id
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> MoveToWarehouseAsync(
        Guid id,
        [FromBody] MoveContainerToWarehouseRequest request,
        IPermissionService permissions,
        ICommandHandler<MoveContainerToWarehouseCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var permissionResult = await EnsureGeneralManagerAsync(permissions, cancellationToken);
        if (permissionResult is not null)
            return permissionResult;

        var result = await handler.HandleAsync(new MoveContainerToWarehouseCommand
        {
            ContainerId = id,
            WarehouseId = request.WarehouseId
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ArchiveContainerAsync(
        Guid id,
        IPermissionService permissions,
        ICommandHandler<ArchiveContainerCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var permissionResult = await EnsureGeneralManagerAsync(permissions, cancellationToken);
        if (permissionResult is not null)
            return permissionResult;

        var result = await handler.HandleAsync(new ArchiveContainerCommand
        {
            ContainerId = id
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> SaveFabricTypeAliasAsync(
        [FromBody] SaveFabricTypeAliasCommand command,
        ICommandHandler<SaveFabricTypeAliasCommand, ApplicationResult> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ParseDplAsync(
        IFormFile file,
        ICurrentBranchService branchService,
        ImportContainerExcelHandler handler,
        CancellationToken cancellationToken)
    {
        if (branchService.CompanyId is not Guid companyId)
            return Unauthorized();

        var result = await handler.HandleAsync(new ParseContainerExcelQuery
        {
            CompanyId = companyId,
            FileName = file.FileName,
            FileContent = await ReadFileBytesAsync(file, cancellationToken)
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ParseInvoiceAsync(
        IFormFile file,
        ParseChinaInvoiceExcelHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ParseChinaInvoiceExcelQuery
        {
            FileName = file.FileName,
            FileContent = await ReadFileBytesAsync(file, cancellationToken)
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult> ParsePackingSummaryAsync(
        IFormFile file,
        ParseChinaPackingSummaryExcelHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ParseChinaPackingSummaryExcelQuery
        {
            FileName = file.FileName,
            FileContent = await ReadFileBytesAsync(file, cancellationToken)
        }, cancellationToken);

        return ApplicationResultHttpMapper.ToHttpResult(result);
    }

    private static async Task<IResult?> EnsureGeneralManagerAsync(
        IPermissionService permissions,
        CancellationToken cancellationToken) =>
        await EnsurePermissionAsync(permissions, GeneralManagerAccess.PermissionCode, cancellationToken);

    private static async Task<IResult?> EnsurePermissionAsync(
        IPermissionService permissions,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        if (await permissions.CanAsync(permissionCode, cancellationToken))
            return null;

        return Results.Json(
            new ApiErrorResponse("PermissionDenied", $"Permission denied: {permissionCode}", []),
            statusCode: StatusCodes.Status403Forbidden);
    }

    private static IResult Unauthorized() =>
        Results.Json(
            new ApiErrorResponse("Unauthorized", "Authentication required.", []),
            statusCode: StatusCodes.Status401Unauthorized);

    private static async Task<byte[]> ReadFileBytesAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory, cancellationToken);
        return memory.ToArray();
    }

    private sealed record CreateChinaContainerRequest(
        Guid SupplierId,
        string ContainerNumber,
        DateTime ShipmentDate,
        DateTime? ExpectedArrival,
        string? Notes,
        decimal ExchangeRateToLocalCurrency,
        decimal ChinaInvoiceAmountUsd,
        string? ImportFileName,
        IReadOnlyList<ImportContainerLineCommand> Lines);

    private sealed record CalculateLandingCostRequest(
        decimal TotalLengthMeters,
        decimal ContainerWeightKg,
        decimal CustomsClearanceAmount,
        decimal Shipping,
        decimal Insurance,
        decimal OtherExpense1,
        decimal OtherExpense2,
        decimal OtherExpense3,
        decimal OtherExpense4,
        bool UsesWeightedAllocation,
        IReadOnlyList<ContainerFabricTypeLineCommand> TypeLines,
        decimal CustomsAmount,
        decimal Clearance,
        decimal OtherExpenses);

    private sealed record SetContainerSalePricesRequest(
        IReadOnlyList<ContainerTypeSalePriceCommand> Lines);

    private sealed record MoveContainerToWarehouseRequest(Guid WarehouseId);
}
