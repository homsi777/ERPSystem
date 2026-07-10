using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Notifications;
using ERPSystem.Domain.Events.ChinaImport;
using ERPSystem.Domain.Events.Inventory;
using ERPSystem.Domain.Events.Sales;
using ERPSystem.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Application.DomainEvents;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            switch (domainEvent)
            {
                case ContainerApproved approved:
                    await HandleContainerApprovedAsync(approved, cancellationToken);
                    break;
                case ContainerMovedToWarehouse moved:
                    await HandleContainerMovedToWarehouseAsync(moved, cancellationToken);
                    break;
                case LandingCostCalculated:
                    break;
                case InventoryCreated created:
                    await PublishAsync(new InventoryChangedNotification
                    {
                        ContainerId = created.ContainerId,
                        WarehouseId = created.WarehouseId
                    }, cancellationToken);
                    break;
                case InventoryReserved reserved:
                    await PublishAsync(new InventoryChangedNotification
                    {
                        WarehouseId = reserved.WarehouseId,
                        FabricItemId = reserved.FabricItemId
                    }, cancellationToken);
                    break;
                case InventoryDeducted deducted:
                    await PublishAsync(new InventoryChangedNotification
                    {
                        WarehouseId = deducted.WarehouseId,
                        FabricItemId = deducted.FabricItemId
                    }, cancellationToken);
                    break;
                case SalesInvoiceApproved:
                    break;
            }
        }
    }

    private async Task HandleContainerApprovedAsync(ContainerApproved e, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        await notifications.PublishAsync(new ContainerApprovedNotification
        {
            ContainerId = e.ContainerId,
            ContainerNumber = e.ContainerNumber
        }, cancellationToken);
    }

    private Task HandleContainerMovedToWarehouseAsync(ContainerMovedToWarehouse e, CancellationToken cancellationToken) =>
        PublishAsync(new InventoryChangedNotification { ContainerId = e.ContainerId }, cancellationToken);

    private async Task PublishAsync(InventoryChangedNotification notification, CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notifications.PublishAsync(notification, cancellationToken);
    }
}
