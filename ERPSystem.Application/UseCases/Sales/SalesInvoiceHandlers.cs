using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Sales;
using ERPSystem.Domain.Services;
using ERPSystem.Domain.Specifications;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class CreateSalesInvoiceDraftHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations)
    : ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateSalesInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create sales invoices.");

        try
        {
            await inventoryOperations.ValidateContainerForSaleAsync(command.ChinaContainerId, cancellationToken);
            await inventoryOperations.ValidateInvoiceLinesAsync(
                command.WarehouseId,
                command.ChinaContainerId,
                command.Lines.Select(l => (l.FabricItemId, l.FabricColorId, l.RollCount)).ToList(),
                cancellationToken);

            var invoiceNumberText = string.IsNullOrWhiteSpace(command.InvoiceNumber)
                ? await numberingService.NextInvoiceNumberAsync(command.BranchId, cancellationToken)
                : command.InvoiceNumber.Trim();

            if (await invoiceRepository.GetByNumberAsync(invoiceNumberText, cancellationToken) is not null)
                return ApplicationResult<Guid>.ValidationFailed(nameof(command.InvoiceNumber), "رقم الفاتورة مستخدم مسبقاً.");

            var invoiceNumber = new InvoiceNumber(invoiceNumberText);

            var userId = currentUserService.UserId ?? Guid.Empty;
            var aggregate = SalesInvoiceAggregate.CreateDraft(
                invoiceNumber,
                command.CompanyId,
                command.BranchId,
                command.CustomerId,
                command.WarehouseId,
                command.ChinaContainerId,
                command.PaymentType,
                userId);

            foreach (var line in command.Lines)
            {
                var item = SalesInvoiceItem.Create(
                    line.LineNumber,
                    line.FabricItemId,
                    line.FabricColorId,
                    line.RollCount,
                    new Money(line.UnitPrice),
                    line.Notes);
                aggregate.AddItem(item);
            }

            aggregate.SetDiscountTotal(new Money(command.DiscountAmount));

            Domain.Validators.SalesInvoiceValidator.ValidateDraft(aggregate);

            await invoiceRepository.AddAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateSalesInvoiceDraftHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IInventoryOperationsService inventoryOperations)
    : ICommandHandler<UpdateSalesInvoiceDraftCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateSalesInvoiceDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to update sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            await inventoryOperations.ValidateContainerForSaleAsync(command.ChinaContainerId, cancellationToken);
            await inventoryOperations.ValidateInvoiceLinesAsync(
                command.WarehouseId,
                command.ChinaContainerId,
                command.Lines.Select(l => (l.FabricItemId, l.FabricColorId, l.RollCount)).ToList(),
                cancellationToken);

            aggregate.UpdateDraftHeader(
                command.CustomerId,
                command.WarehouseId,
                command.ChinaContainerId,
                command.PaymentType);

            var items = command.Lines
                .Select(line => SalesInvoiceItem.Create(
                    line.LineNumber,
                    line.FabricItemId,
                    line.FabricColorId,
                    line.RollCount,
                    new Money(line.UnitPrice),
                    line.Notes))
                .ToList();

            aggregate.ReplaceDraftLines(items);
            aggregate.SetDiscountTotal(new Money(command.DiscountAmount));
            Domain.Validators.SalesInvoiceValidator.ValidateDraft(aggregate);

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class UpdateSalesInvoiceDiscountHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSalesInvoiceDiscountCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateSalesInvoiceDiscountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (command.DiscountAmount < 0)
            return ApplicationResult.ValidationFailed(nameof(command.DiscountAmount), "Discount cannot be negative.");

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to update sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            aggregate.SetDiscountTotal(new Money(command.DiscountAmount));
            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class SendSalesInvoiceToWarehouseHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IInventoryOperationsService inventoryOperations,
    IDomainEventDispatcher domainEventDispatcher,
    INotificationService notificationService)
    : ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        SendSalesInvoiceToWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");

        if (!await permissionService.CanAsync("sales.send-to-warehouse", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to send invoice to warehouse.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            aggregate.SendToWarehouse();
            await inventoryOperations.ReserveForInvoiceAsync(aggregate, cancellationToken);
            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);
            await notificationService.PublishAsync(new InventoryChangedNotification
            {
                ContainerId = aggregate.ChinaContainerId,
                WarehouseId = aggregate.WarehouseId
            }, cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class CompleteWarehouseDetailingHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CompleteWarehouseDetailingCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return validation;

        if (!await permissionService.CanAsync("warehouse.detailing", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to complete detailing.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        var spec = new WarehouseCanDetailSpecification();
        if (!spec.IsSatisfiedBy(aggregate))
            return ApplicationResult.Conflict(spec.FailureReason);

        try
        {
            var userId = currentUserService.UserId ?? Guid.Empty;
            foreach (var entry in command.RollEntries)
                aggregate.EnterRollLength(entry.RollDetailId, new LengthInMeters(entry.LengthMeters), userId);

            if (!WarehouseDetailingValidator.CanCompleteDetailing(aggregate))
                return ApplicationResult.Conflict("All roll lengths must be entered before completing detailing.");

            aggregate.CompleteDetailing();
            await inventoryOperations.AssignFabricRollsOnDetailingAsync(aggregate, cancellationToken);

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);

            await notificationService.PublishAsync(new SalesInvoiceDetailedNotification
            {
                InvoiceId = aggregate.Id,
                InvoiceNumber = aggregate.InvoiceNumber.Value,
                GrandTotal = aggregate.GrandTotal.Amount
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ApproveSalesInvoiceHandler(
    ISalesInvoiceRepository invoiceRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService,
    ICurrentUserService currentUserService,
    IInventoryOperationsService inventoryOperations,
    IIntegratedAccountingService accountingService,
    IDomainEventDispatcher domainEventDispatcher)
    : ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveSalesInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");

        if (!await permissionService.CanAsync("sales.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        var spec = new InvoiceCanBeApprovedSpecification();
        if (!spec.IsSatisfiedBy(aggregate))
            return ApplicationResult.Conflict(spec.FailureReason);

        var customerAggregate = await customerRepository.GetByIdAsync(aggregate.CustomerId, cancellationToken);
        if (customerAggregate is null)
            return ApplicationResult.NotFound("Customer not found.");

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            CreditLimitChecker.EnsureWithinLimit(customerAggregate, aggregate.GrandTotal);

            var userId = currentUserService.UserId ?? Guid.Empty;
            aggregate.Approve(userId);
            customerAggregate.RecordPostedInvoice(aggregate.GrandTotal.Amount);

            var cogs = await inventoryOperations.DeductForInvoiceAsync(aggregate, cancellationToken);
            await accountingService.PostSalesInvoiceApprovalAsync(aggregate, cogs, cancellationToken);

            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await customerRepository.UpdateAsync(customerAggregate, cancellationToken);
            await unitOfWork.SaveAndDispatchAsync(domainEventDispatcher, [aggregate], cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            await notificationService.PublishAsync(new SalesInvoiceApprovedNotification
            {
                InvoiceId = aggregate.Id,
                InvoiceNumber = aggregate.InvoiceNumber.Value,
                CustomerId = aggregate.CustomerId,
                GrandTotal = aggregate.GrandTotal.Amount
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            var exceeded = CreditLimitChecker.TryCreateExceededEvent(
                customerAggregate.Customer,
                aggregate.GrandTotal);
            if (exceeded is not null)
                await notificationService.PublishAsync(new CustomerCreditLimitExceededNotification
                {
                    CustomerId = exceeded.CustomerId,
                    RequestedAmount = exceeded.RequestedAmount,
                    CreditLimit = exceeded.CreditLimit
                }, cancellationToken);

            return ex.ToFailureResult();
        }
    }
}

public sealed class CancelSalesInvoiceHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IInventoryOperationsService inventoryOperations,
    INotificationService notificationService)
    : ICommandHandler<CancelSalesInvoiceCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CancelSalesInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (string.IsNullOrWhiteSpace(command.Reason))
            return ApplicationResult.ValidationFailed(nameof(command.Reason), "Cancel reason is required.");

        if (!await permissionService.CanAsync("sales.cancel", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to cancel sales invoices.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            await inventoryOperations.ReleaseForInvoiceAsync(aggregate, cancellationToken);
            aggregate.Cancel(command.Reason);
            await invoiceRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await notificationService.PublishAsync(new InventoryChangedNotification
            {
                ContainerId = aggregate.ChinaContainerId,
                WarehouseId = aggregate.WarehouseId
            }, cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
