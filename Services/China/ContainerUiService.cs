using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Queries.Warehouses;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.China;

public static class ContainerListRefreshHub
{
    public static event EventHandler? RefreshRequested;

    public static void RequestRefresh() => RefreshRequested?.Invoke(null, EventArgs.Empty);
}

public sealed class SupplierPickItem
{
    public Guid Id { get; init; }
    public string Display { get; init; } = "";
}

public sealed class ContainerUiService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ICurrentBranchService _branch;

    public ContainerUiService(IServiceScopeFactory scopeFactory, ICurrentBranchService branch)
    {
        _scopeFactory = scopeFactory;
        _branch = branch;
    }

    public static ContainerUiService Instance =>
        AppServices.GetRequiredService<ContainerUiService>();

    private Guid CompanyId =>
        _branch.CompanyId ?? throw new InvalidOperationException("Company context is not set.");

    private Guid BranchId =>
        _branch.BranchId ?? throw new InvalidOperationException("Branch context is not set.");

    public async Task<ApplicationResult<PagedResult<ContainerListDto>>> GetListAsync(
        string? search,
        ChinaContainerStatus? status = null,
        int page = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetChinaContainerListHandler>();
        var result = await handler.HandleAsync(new GetChinaContainerListQuery
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            Status = status,
            Page = page,
            PageSize = pageSize
        }, cancellationToken);

        if (!result.IsSuccess || result.Value is null || string.IsNullOrWhiteSpace(search))
            return result;

        var term = search.Trim();
        var filtered = result.Value.Items
            .Where(c =>
                c.ContainerNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                c.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return ApplicationResult<PagedResult<ContainerListDto>>.Success(new PagedResult<ContainerListDto>
        {
            Items = filtered,
            TotalCount = filtered.Count,
            Page = page,
            PageSize = pageSize
        });
    }

    public async Task<ApplicationResult<ContainerExcelParseResultDto>> ParseExcelAsync(
        string fileName,
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ImportContainerExcelHandler>();
        return await handler.HandleAsync(new ParseContainerExcelQuery
        {
            CompanyId = CompanyId,
            FileName = fileName,
            FileContent = fileContent
        }, cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> CreateContainerAsync(
        CreateChinaContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ICommandHandler<CreateChinaContainerCommand, ApplicationResult<Guid>>>();
        return await handler.HandleAsync(new CreateChinaContainerCommand
        {
            CompanyId = CompanyId,
            BranchId = BranchId,
            SupplierId = command.SupplierId,
            ContainerNumber = command.ContainerNumber,
            ShipmentDate = command.ShipmentDate,
            ExpectedArrival = command.ExpectedArrival,
            Notes = command.Notes,
            ExchangeRateToLocalCurrency = command.ExchangeRateToLocalCurrency,
            ImportFileName = command.ImportFileName,
            Lines = command.Lines
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierPickItem>> GetSuppliersAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISupplierRepository>();
        var suppliers = await repository.GetListAsync(CompanyId, cancellationToken: cancellationToken);
        return suppliers
            .Select(s => new SupplierPickItem { Id = s.Supplier.Id, Display = s.Supplier.Name })
            .ToList();
    }

    public async Task<bool> CanCreateAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var permissions = scope.ServiceProvider.GetRequiredService<IPermissionService>();
        return await permissions.CanAsync("containers.create", cancellationToken);
    }

    public async Task<ApplicationResult<Guid>> SubmitCostEntryAsync(
        ChinaImportHeaderDraft header,
        ContainerExcelParseResultDto parse,
        string? fileName,
        ChinaImportCostEntryInput input,
        CancellationToken cancellationToken = default)
    {
        var lines = PackingListImportLineBuilder.BuildLines(parse);
        if (lines.Count == 0)
            return ApplicationResult<Guid>.ValidationFailed("Lines", "لا توجد بنود صالحة للاستيراد بعد ربط الأكواد.");

        var createResult = await CreateContainerAsync(new CreateChinaContainerCommand
        {
            SupplierId = header.SupplierId,
            ContainerNumber = header.ContainerNumber,
            ShipmentDate = header.ShipmentDate,
            ExpectedArrival = header.ExpectedArrival,
            Notes = header.Notes,
            ExchangeRateToLocalCurrency = header.ExchangeRateToLocalCurrency,
            ImportFileName = fileName,
            Lines = lines
        }, cancellationToken);

        if (!createResult.IsSuccess || createResult.Value == Guid.Empty)
            return createResult;

        using var scope = _scopeFactory.CreateScope();
        var landingHandler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<CalculateLandingCostCommand, ApplicationResult>>();

        var landingResult = await landingHandler.HandleAsync(new CalculateLandingCostCommand
        {
            ContainerId = createResult.Value,
            TotalLengthMeters = parse.GrandTotal.ParsedTotalMeters,
            ContainerWeightKg = input.ContainerWeightKg,
            CustomsAmount = input.CustomsAmountUsd,
            Shipping = input.ShippingUsd,
            Clearance = input.ClearanceUsd,
            OtherExpenses = input.OtherExpensesUsd
        }, cancellationToken);

        return landingResult.IsSuccess
            ? createResult
            : ApplicationResult<Guid>.ValidationFailed("LandingCost", "تعذّر حفظ تكاليف الوصول.");
    }

    public async Task<ApplicationResult<ContainerOperationsCenterDto>> GetOperationsCenterAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetContainerOperationsCenterHandler>();
        return await handler.HandleAsync(new GetContainerOperationsCenterQuery
        {
            ContainerId = containerId
        }, cancellationToken);
    }

    public async Task<ApplicationResult> ApproveContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<ApproveContainerCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ApproveContainerCommand { ContainerId = containerId }, cancellationToken);
    }

    public async Task<IReadOnlyList<WarehouseListDto>> GetWarehousesAsync(
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<GetWarehouseListHandler>();
        var result = await handler.HandleAsync(new GetWarehouseListQuery { BranchId = BranchId }, cancellationToken);
        return result.IsSuccess && result.Value is not null ? result.Value : [];
    }

    public async Task<ApplicationResult> MoveToWarehouseAsync(
        Guid containerId,
        Guid warehouseId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<MoveContainerToWarehouseCommand, ApplicationResult>>();
        return await handler.HandleAsync(new MoveContainerToWarehouseCommand
        {
            ContainerId = containerId,
            WarehouseId = warehouseId
        }, cancellationToken);
    }
}
