using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Application.Validation;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Parties;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Customers;

public sealed class CreateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService)
    : ICommandHandler<CreateCustomerCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("customers.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create customers.");

        try
        {
            var customer = Customer.Create(
                command.CompanyId,
                command.Code,
                command.NameAr,
                command.NameEn,
                command.Type,
                new Money(command.CreditLimit));

            Domain.Validators.CustomerValidator.Validate(customer);

            var aggregate = CustomerAggregate.FromCustomer(customer);
            await customerRepository.AddAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await notificationService.PublishAsync(new CustomerCreatedNotification
            {
                CustomerId = aggregate.Id,
                CustomerCode = command.Code,
                CustomerName = command.NameAr
            }, cancellationToken);

            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService)
    : ICommandHandler<UpdateCustomerCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.CustomerId), "Customer is required.");
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult.ValidationFailed(nameof(command.NameAr), "Customer name is required.");

        if (!await permissionService.CanAsync("customers.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to update customers.");

        var aggregate = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Customer not found.");

        try
        {
            aggregate.Customer.UpdateProfile(
                command.NameAr,
                command.NameEn,
                new Money(command.CreditLimit),
                command.PaymentTermsDays);

            Domain.Validators.CustomerValidator.Validate(aggregate.Customer);

            await customerRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await notificationService.PublishAsync(new CustomerUpdatedNotification
            {
                CustomerId = aggregate.Id,
                CustomerName = command.NameAr
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class DeactivateCustomerHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService)
    : ICommandHandler<DeactivateCustomerCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateCustomerCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.CustomerId), "Customer is required.");

        if (!await permissionService.CanAsync("customers.deactivate", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to deactivate customers.");

        var aggregate = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Customer not found.");

        try
        {
            aggregate.Customer.Deactivate();
            await customerRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await notificationService.PublishAsync(new CustomerDeactivatedNotification
            {
                CustomerId = aggregate.Id,
                CustomerName = aggregate.Customer.NameAr
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
