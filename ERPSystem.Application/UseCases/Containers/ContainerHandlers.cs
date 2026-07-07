using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Specifications;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Containers;

public sealed class CreateChinaContainerHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    IAuditLogRepository auditLogRepository,
    ICurrentBranchService currentBranchService)
    : ICommandHandler<CreateChinaContainerCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateChinaContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("containers.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create containers.");

        try
        {
            var number = string.IsNullOrWhiteSpace(command.ContainerNumber)
                ? await numberingService.NextContainerNumberAsync(command.BranchId, cancellationToken)
                : command.ContainerNumber;

            var aggregate = ContainerAggregate.CreateDraft(
                new ContainerNumber(number),
                command.CompanyId,
                command.BranchId,
                command.SupplierId,
                command.ShipmentDate);

            aggregate.SetHeaderDetails(
                command.ExpectedArrival,
                command.Notes,
                command.ExchangeRateToLocalCurrency);

            if (command.ChinaInvoiceAmountUsd > 0)
                aggregate.SetChinaInvoiceFinancials(command.ChinaInvoiceAmountUsd, command.ExchangeRateToLocalCurrency);

            if (!string.IsNullOrWhiteSpace(command.ImportFileName))
            {
                var userId = currentUserService.UserId ?? Guid.Empty;
                var batch = ChinaImportBatch.Create(
                    $"IMP-{DateTime.UtcNow:yyyyMMddHHmmss}",
                    command.ImportFileName,
                    userId);
                batch.SetCounts(command.Lines.Count, 0);
                aggregate.AddImportBatch(batch);
            }

            foreach (var line in command.Lines)
            {
                aggregate.AddItem(ChinaContainerItem.Create(
                    line.LineNumber,
                    line.FabricItemId,
                    line.FabricColorId,
                    line.RollCount > 0 ? line.RollCount : 1,
                    new LengthInMeters(line.LengthMeters),
                    line.WeightKg.HasValue ? new WeightInKg(line.WeightKg.Value) : null,
                    line.LotCode,
                    line.BuyerCustomerId,
                    line.SupplierRollNumber));
            }

            var previousStatus = aggregate.Status;
            aggregate.BeginReview();
            Domain.Validators.ContainerValidator.Validate(aggregate);

            await containerRepository.AddAsync(aggregate, cancellationToken);
            await ContainerAuditRecorder.RecordStatusChangeAsync(
                auditLogRepository,
                currentUserService,
                currentBranchService,
                aggregate.Id,
                "CreateContainer",
                previousStatus,
                aggregate.Status,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class CalculateLandingCostHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IAuditLogRepository auditLogRepository,
    ICurrentUserService currentUserService,
    ICurrentBranchService currentBranchService)
    : ICommandHandler<CalculateLandingCostCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CalculateLandingCostCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("containers.landing-cost", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to calculate landing cost.");

        var aggregate = await containerRepository.GetByIdAsync(command.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Container not found.");

        try
        {
            var previousStatus = aggregate.Status;

            var customsClearance = command.CustomsClearanceAmount > 0
                ? command.CustomsClearanceAmount
                : command.CustomsAmount + command.Clearance;

            var otherLegacy = command.OtherExpenses;
            var usesWeighted = command.UsesWeightedAllocation && command.TypeLines.Count > 0;

            var landingCost = LandingCost.CreateExtended(
                new LengthInMeters(command.TotalLengthMeters),
                new WeightInKg(command.ContainerWeightKg),
                new Money(customsClearance),
                new Money(command.Shipping),
                new Money(command.Insurance),
                Money.Zero(),
                new Money(otherLegacy),
                new Money(command.OtherExpense1),
                new Money(command.OtherExpense2),
                new Money(command.OtherExpense3),
                new Money(command.OtherExpense4),
                usesWeighted);

            Domain.Validators.LandingCostValidator.Validate(landingCost);
            aggregate.SetLandingCost(landingCost);

            if (command.TypeLines.Count > 0)
            {
                var typeLines = command.TypeLines.Select(t => ContainerFabricTypeLine.Create(
                    t.LineNumber,
                    t.TypeDisplayName,
                    t.MatchKey,
                    t.FabricItemId,
                    t.FabricColorId,
                    t.LengthMeters,
                    t.RollCount,
                    t.NetWeightKg,
                    t.Cbm,
                    t.ChinaUnitPriceUsd,
                    t.InvoiceLineAmountUsd,
                    t.HasInvoiceMatch,
                    t.HasPlMatch,
                    t.HasDplMatch,
                    t.MatchWarnings,
                    usesWeighted)).ToList();

                var sharedTotal = landingCost.TotalSharedExpenses.Amount;
                if (usesWeighted)
                    ChinaImportTypeCostAllocator.ApplyWeightedAllocation(typeLines, sharedTotal);
                else
                    ChinaImportTypeCostAllocator.ApplyFlatFallback(typeLines, sharedTotal);

                aggregate.SetFabricTypeLines(typeLines);
            }

            await containerRepository.UpdateAsync(aggregate, cancellationToken);
            if (previousStatus != aggregate.Status)
            {
                await ContainerAuditRecorder.RecordStatusChangeAsync(
                    auditLogRepository,
                    currentUserService,
                    currentBranchService,
                    aggregate.Id,
                    "CalculateLandingCost",
                    previousStatus,
                    aggregate.Status,
                    cancellationToken: cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ApproveContainerHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    IAuditLogRepository auditLogRepository,
    ICurrentBranchService currentBranchService,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<ApproveContainerCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ContainerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.ContainerId), "Container is required.");

        if (!await permissionService.CanAsync("containers.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve containers.");

        var aggregate = await containerRepository.GetByIdAsync(command.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Container not found.");

        var spec = new ContainerCanBeApprovedSpecification();
        if (!spec.IsSatisfiedBy(aggregate))
            return ApplicationResult.Conflict(spec.FailureReason);

        try
        {
            var previousStatus = aggregate.Status;
            var userId = currentUserService.UserId ?? Guid.Empty;
            aggregate.Approve(userId);

            await containerRepository.UpdateAsync(aggregate, cancellationToken);
            await ContainerAuditRecorder.RecordStatusChangeAsync(
                auditLogRepository,
                currentUserService,
                currentBranchService,
                aggregate.Id,
                "ApproveContainer",
                previousStatus,
                aggregate.Status,
                cancellationToken: cancellationToken);

            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class MoveContainerToWarehouseHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IAuditLogRepository auditLogRepository,
    ICurrentUserService currentUserService,
    ICurrentBranchService currentBranchService,
    IContainerWarehouseImportService warehouseImportService,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<MoveContainerToWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        MoveContainerToWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ContainerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.ContainerId), "Container is required.");
        if (command.WarehouseId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.WarehouseId), "Warehouse is required.");

        if (!await permissionService.CanAsync("containers.move-to-warehouse", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to move container to warehouse.");

        var aggregate = await containerRepository.GetByIdAsync(command.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Container not found.");

        if (aggregate.Status != ChinaContainerStatus.Approved)
            return ApplicationResult.Conflict("Container must be approved before moving to warehouse.");

        if (aggregate.LandingCost is null)
            return ApplicationResult.Conflict("Landing cost must be calculated before warehouse transfer.");

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            var previousStatus = aggregate.Status;
            aggregate.MoveToWarehouse();
            await containerRepository.UpdateAsync(aggregate, cancellationToken);
            await warehouseImportService.PostContainerStockAsync(command.WarehouseId, aggregate, cancellationToken);
            await ContainerAuditRecorder.RecordStatusChangeAsync(
                auditLogRepository,
                currentUserService,
                currentBranchService,
                aggregate.Id,
                "MoveToWarehouse",
                previousStatus,
                aggregate.Status,
                $"warehouseId={command.WarehouseId}",
                cancellationToken);

            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            return ex.ToFailureResult();
        }
    }
}

public sealed class SetContainerTypeSalePricesHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<SetContainerTypeSalePricesCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SetContainerTypeSalePricesCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ContainerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.ContainerId), "Container is required.");

        if (!await permissionService.CanAsync("containers.landing-cost", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to set sale prices.");

        var aggregate = await containerRepository.GetByIdAsync(command.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Container not found.");

        if (aggregate.FabricTypeLines.Count == 0)
            return ApplicationResult.ValidationFailed("TypeLines", "لا توجد أنواع أقمشة لهذه الحاوية.");

        try
        {
            aggregate.SetTypeSalePrices(command.Lines
                .Select(l => (l.TypeLineId, l.MarginPerMeterUsd))
                .ToList());

            await containerRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ArchiveContainerHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IAuditLogRepository auditLogRepository,
    ICurrentUserService currentUserService,
    ICurrentBranchService currentBranchService)
    : ICommandHandler<ArchiveContainerCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ArchiveContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ContainerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.ContainerId), "Container is required.");

        if (!await permissionService.CanAsync("containers.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to archive containers.");

        var aggregate = await containerRepository.GetByIdAsync(command.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Container not found.");

        try
        {
            var previousStatus = aggregate.Status;
            aggregate.Archive();
            await containerRepository.UpdateAsync(aggregate, cancellationToken);
            await ContainerAuditRecorder.RecordStatusChangeAsync(
                auditLogRepository,
                currentUserService,
                currentBranchService,
                aggregate.Id,
                "ArchiveContainer",
                previousStatus,
                aggregate.Status,
                cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class SaveFabricTypeAliasHandler(
    IFabricTypeAliasRepository aliasRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<SaveFabricTypeAliasCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SaveFabricTypeAliasCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CompanyId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.CompanyId), "Company is required.");
        if (command.SupplierId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.SupplierId), "Supplier is required.");
        if (string.IsNullOrWhiteSpace(command.InvoiceDescriptionMatchKey))
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceDescriptionMatchKey), "Invoice line is required.");

        try
        {
            await aliasRepository.UpsertAsync(
                command.CompanyId,
                command.SupplierId,
                command.FabricItemId,
                command.FabricColorId,
                command.DplMatchKey,
                command.InvoiceDescriptionMatchKey,
                command.InvoiceDescription,
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
