using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Common;
using ERPSystem.Application.Results;

namespace ERPSystem.Application.UseCases.Sales;

public sealed class ConfirmSalesInvoiceDeliveryHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ConfirmSalesInvoiceDeliveryCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ConfirmSalesInvoiceDeliveryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");

        if (!await permissionService.CanAsync("sales.deliver", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to confirm delivery.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            // DatePicker returns Local/Unspecified — PostgreSQL timestamptz requires UTC.
            var deliveryDate = command.DeliveryDate == default
                ? DateTime.UtcNow
                : command.DeliveryDate.Kind switch
                {
                    DateTimeKind.Utc => command.DeliveryDate,
                    DateTimeKind.Local => command.DeliveryDate.ToUniversalTime(),
                    _ => DateTime.SpecifyKind(command.DeliveryDate.Date, DateTimeKind.Utc)
                        .Add(command.DeliveryDate.TimeOfDay)
                };

            aggregate.Deliver(command.ReceivedByName, deliveryDate, command.DriverName, command.Notes);
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

public sealed class UpdateSalesInvoiceWarehouseHandler(
    ISalesInvoiceRepository invoiceRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateSalesInvoiceWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateSalesInvoiceWarehouseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.InvoiceId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.InvoiceId), "Invoice is required.");
        if (command.WarehouseId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.WarehouseId), "Warehouse is required.");

        if (!await permissionService.CanAsync("sales.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed.");

        var aggregate = await invoiceRepository.GetByIdAsync(command.InvoiceId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Invoice not found.");

        try
        {
            aggregate.UpdateWarehouse(command.WarehouseId);
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
