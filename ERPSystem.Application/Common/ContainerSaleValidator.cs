using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Application.Common;

public static class ContainerSaleValidator
{
    public static async Task EnsureReadyForSaleAsync(
        IChinaContainerRepository containerRepository,
        IInventoryRepository inventoryRepository,
        Guid containerId,
        CancellationToken cancellationToken = default)
    {
        if (containerId == Guid.Empty)
            throw new ValidationException("Container is required.");

        var container = await containerRepository.GetByIdAsync(containerId, cancellationToken)
            ?? throw new ValidationException("Container not found.");

        if (container.IsArchived || container.Status == ChinaContainerStatus.Archived)
            throw new ValidationException("Cannot sell from an archived container.");

        if (container.Status == ChinaContainerStatus.Cancelled)
            throw new ValidationException("Cannot sell from a cancelled container.");

        if (container.Status != ChinaContainerStatus.InWarehouse)
            throw new ValidationException("Container must be in warehouse (ready for sale) before selling.");

        if (!await inventoryRepository.IsStockPostedForContainerAsync(containerId, cancellationToken))
            throw new ValidationException("Container inventory has not been posted to warehouse yet.");
    }
}
