using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.ChinaImport;
using ERPSystem.Domain.Specifications;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Containers;

public sealed class CreateChinaContainerHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService,
    ICurrentUserService currentUserService)
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
                    line.BuyerCustomerId));
            }

            aggregate.BeginReview();
            Domain.Validators.ContainerValidator.Validate(aggregate);

            await containerRepository.AddAsync(aggregate, cancellationToken);
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
    IPermissionService permissionService)
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
            var landingCost = LandingCost.Create(
                new LengthInMeters(command.TotalLengthMeters),
                new WeightInKg(command.ContainerWeightKg),
                new Money(command.CustomsAmount),
                new Money(command.Shipping),
                new Money(command.Clearance),
                new Money(command.OtherExpenses));

            Domain.Validators.LandingCostValidator.Validate(landingCost);
            aggregate.SetLandingCost(landingCost);

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

public sealed class ApproveContainerHandler(
    IChinaContainerRepository containerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService,
    ICurrentUserService currentUserService)
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
            var userId = currentUserService.UserId ?? Guid.Empty;
            aggregate.Approve(userId);

            await containerRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await notificationService.PublishAsync(new ContainerApprovedNotification
            {
                ContainerId = aggregate.Id,
                ContainerNumber = aggregate.ContainerNumber.Value
            }, cancellationToken);

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
    IPermissionService permissionService)
    : ICommandHandler<MoveContainerToWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        MoveContainerToWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ContainerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.ContainerId), "Container is required.");

        if (!await permissionService.CanAsync("containers.move-to-warehouse", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to move container to warehouse.");

        var aggregate = await containerRepository.GetByIdAsync(command.ContainerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Container not found.");

        try
        {
            aggregate.MoveToWarehouse();
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
