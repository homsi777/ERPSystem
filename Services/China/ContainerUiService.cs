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
using ERPSystem.Domain.Entities.System;
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

    public async Task<ApplicationResult<ChinaInvoiceParseResultDto>> ParseInvoiceAsync(
        string fileName,
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ParseChinaInvoiceExcelHandler>();
        return await handler.HandleAsync(new ParseChinaInvoiceExcelQuery
        {
            FileName = fileName,
            FileContent = fileContent
        }, cancellationToken);
    }

    public async Task<ApplicationResult<ChinaPackingSummaryParseResultDto>> ParsePackingSummaryAsync(
        string fileName,
        byte[] fileContent,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ParseChinaPackingSummaryExcelHandler>();
        return await handler.HandleAsync(new ParseChinaPackingSummaryExcelQuery
        {
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

        using var scope = _scopeFactory.CreateScope();
        var services = scope.ServiceProvider;
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var containerRepository = services.GetRequiredService<IChinaContainerRepository>();
        var createHandler = services
            .GetRequiredService<ICommandHandler<CreateChinaContainerCommand, ApplicationResult<Guid>>>();
        var landingHandler = services
            .GetRequiredService<ICommandHandler<CalculateLandingCostCommand, ApplicationResult>>();

        var containerNumber = header.ContainerNumber.Trim();
        Guid containerId;

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var existing = !string.IsNullOrWhiteSpace(containerNumber)
                ? await containerRepository.GetByNumberAsync(containerNumber, cancellationToken)
                : null;

            if (existing is not null)
            {
                if (existing.LandingCost is not null)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApplicationResult<Guid>.Conflict(
                        $"الحاوية «{containerNumber}» مسجّلة مسبقاً. افتحها من قائمة الحاويات للمتابعة.");
                }

                if (existing.Status is ChinaContainerStatus.Approved
                    or ChinaContainerStatus.InWarehouse
                    or ChinaContainerStatus.Closed
                    or ChinaContainerStatus.Archived)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApplicationResult<Guid>.Conflict(
                        $"الحاوية «{containerNumber}» في حالة «{existing.Status}» ولا يمكن تعديل تكلفتها.");
                }

                existing.SetChinaInvoiceFinancials(input.ChinaInvoiceAmountUsd, header.ExchangeRateToLocalCurrency);
                await containerRepository.UpdateAsync(existing, cancellationToken);
                containerId = existing.Id;
            }
            else
            {
                var createResult = await createHandler.HandleAsync(new CreateChinaContainerCommand
                {
                    CompanyId = CompanyId,
                    BranchId = BranchId,
                    SupplierId = header.SupplierId,
                    ContainerNumber = containerNumber,
                    ShipmentDate = header.ShipmentDate,
                    ExpectedArrival = header.ExpectedArrival,
                    Notes = header.Notes,
                    ExchangeRateToLocalCurrency = header.ExchangeRateToLocalCurrency,
                    ChinaInvoiceAmountUsd = input.ChinaInvoiceAmountUsd,
                    ImportFileName = fileName,
                    Lines = lines
                }, cancellationToken);

                if (!createResult.IsSuccess || createResult.Value == Guid.Empty)
                {
                    await unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return createResult;
                }

                containerId = createResult.Value;
            }

            var session = ChinaImportNavigationContext.GetMultiFileSession();
            var usesWeighted = input.UsesWeightedAllocation && session?.UsesWeightedAllocation == true;
            var typeLineCommands = BuildTypeLineCommands(session, usesWeighted);

            var landingResult = await landingHandler.HandleAsync(new CalculateLandingCostCommand
            {
                ContainerId = containerId,
                TotalLengthMeters = parse.GrandTotal.ParsedTotalMeters,
                ContainerWeightKg = input.ContainerWeightKg,
                CustomsClearanceAmount = input.CustomsClearanceUsd > 0
                    ? input.CustomsClearanceUsd
                    : input.CustomsAmountUsd + input.ClearanceUsd,
                Shipping = input.ShippingUsd,
                Insurance = input.InsuranceUsd,
                OtherExpense1 = input.OtherExpense1Usd,
                OtherExpense2 = input.OtherExpense2Usd,
                OtherExpense3 = input.OtherExpense3Usd,
                OtherExpense4 = input.OtherExpense4Usd,
                UsesWeightedAllocation = usesWeighted,
                TypeLines = typeLineCommands,
                CustomsAmount = input.CustomsAmountUsd,
                Clearance = input.ClearanceUsd,
                OtherExpenses = input.OtherExpensesUsd
            }, cancellationToken);

            if (!landingResult.IsSuccess)
            {
                await unitOfWork.RollbackTransactionAsync(cancellationToken);
                return ApplicationResult<Guid>.Failure(
                    landingResult.ErrorMessage ?? "تعذّر حفظ تكاليف الوصول.");
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);
            ContainerListRefreshHub.RequestRefresh();
            return ApplicationResult<Guid>.Success(containerId);
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ex.ToFailureResult<Guid>();
        }
    }

    public async Task<ApplicationResult> SetTypeSalePricesAsync(
        Guid containerId,
        IReadOnlyList<ContainerTypeSalePriceCommand> lines,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<SetContainerTypeSalePricesCommand, ApplicationResult>>();
        return await handler.HandleAsync(new SetContainerTypeSalePricesCommand
        {
            ContainerId = containerId,
            Lines = lines
        }, cancellationToken);
    }

    private static IReadOnlyList<ContainerFabricTypeLineCommand> BuildTypeLineCommands(
        ChinaImportMultiFileSessionDto? session,
        bool usesWeighted)
    {
        if (session is null || session.TypeLines.Count == 0)
            return [];

        return session.TypeLines.Select(t => new ContainerFabricTypeLineCommand
        {
            LineNumber = t.LineIndex,
            TypeDisplayName = t.TypeDisplayName,
            MatchKey = t.MatchKey,
            FabricItemId = t.FabricItemId,
            FabricColorId = t.FabricColorId,
            LengthMeters = t.LengthMeters,
            RollCount = t.RollCount,
            NetWeightKg = t.NetWeightKg,
            Cbm = t.Cbm,
            ChinaUnitPriceUsd = t.ChinaUnitPriceUsd,
            InvoiceLineAmountUsd = t.InvoiceLineAmountUsd,
            HasInvoiceMatch = t.HasInvoice,
            HasPlMatch = t.HasPackingSummary,
            HasDplMatch = t.HasDpl,
            MatchWarnings = t.MismatchWarnings.Count > 0
                ? string.Join("؛ ", t.MismatchWarnings)
                : (t.MissingSources.Count > 0 ? $"ناقص: {string.Join("، ", t.MissingSources)}" : null)
        }).ToList();
    }

    public async Task<ChinaImportMultiFileSessionDto?> RefreshMultiFileSessionAsync(
        CancellationToken cancellationToken = default)
    {
        var header = ChinaImportNavigationContext.HeaderDraft;
        if (header is null)
            return ChinaImportNavigationContext.GetMultiFileSession();

        using var scope = _scopeFactory.CreateScope();
        var aliasRepo = scope.ServiceProvider.GetRequiredService<IFabricTypeAliasRepository>();
        var aliases = await aliasRepo.GetBySupplierAsync(CompanyId, header.SupplierId, cancellationToken);

        var session = ChinaImportCrossFileMatcher.BuildSession(
            ChinaImportNavigationContext.GetParseResult(),
            ChinaImportNavigationContext.LastInvoiceParse,
            ChinaImportNavigationContext.LastPackingSummaryParse,
            new ChinaImportMatchContext
            {
                SupplierId = header.SupplierId,
                PersistedAliases = aliases,
                SessionDplToInvoiceKeys = ChinaImportNavigationContext.GetSessionDplLinks()
            });

        ChinaImportNavigationContext.SetMultiFileSession(session);
        return session;
    }

    public async Task<ApplicationResult> ConfirmDplLinkAsync(
        string dplMatchKey,
        string invoiceDescriptionMatchKey,
        string invoiceDescription,
        Guid fabricItemId,
        Guid fabricColorId,
        CancellationToken cancellationToken = default)
    {
        var header = ChinaImportNavigationContext.HeaderDraft;
        if (header is null)
            return ApplicationResult.ValidationFailed("Header", "لا توجد بيانات استيراد.");

        ChinaImportNavigationContext.SetSessionDplLink(dplMatchKey, invoiceDescriptionMatchKey);

        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<SaveFabricTypeAliasCommand, ApplicationResult>>();
        var result = await handler.HandleAsync(new SaveFabricTypeAliasCommand
        {
            CompanyId = CompanyId,
            SupplierId = header.SupplierId,
            FabricItemId = fabricItemId,
            FabricColorId = fabricColorId,
            DplMatchKey = dplMatchKey,
            InvoiceDescriptionMatchKey = invoiceDescriptionMatchKey,
            InvoiceDescription = invoiceDescription
        }, cancellationToken);

        if (!result.IsSuccess)
            return result;

        await RefreshMultiFileSessionAsync(cancellationToken);
        return ApplicationResult.Success();
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

    public async Task<ApplicationResult> ArchiveContainerAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<ICommandHandler<ArchiveContainerCommand, ApplicationResult>>();
        return await handler.HandleAsync(new ArchiveContainerCommand { ContainerId = containerId }, cancellationToken);
    }

    public async Task<IReadOnlyList<AuditLog>> GetAuditTrailAsync(
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
        return await repository.GetByEntityAsync(
            ContainerAuditRecorder.EntityTypeName,
            containerId,
            cancellationToken);
    }
}
