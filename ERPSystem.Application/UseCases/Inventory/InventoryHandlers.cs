using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Inventory;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Inventory;

public static class InventoryTrailRecorder
{
    public static async Task RecordAuditAsync(
        IInventoryManagementRepository repository,
        ICurrentUserService user,
        Guid entityId,
        string entityType,
        string action,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null,
        string? sourceModule = null,
        CancellationToken cancellationToken = default)
    {
        var entry = InventoryAuditEntry.Record(
            entityId, entityType, action,
            user.UserId ?? Guid.Empty, user.Username ?? "system",
            fieldName, previousValue, newValue, reason, sourceModule);
        await repository.AddAuditEntryAsync(entry, cancellationToken);
    }

    public static async Task RecordTimelineAsync(
        IInventoryManagementRepository repository,
        ICurrentUserService user,
        Guid entityId,
        string entityType,
        string eventType,
        string title,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var entry = InventoryTimelineEvent.Record(
            entityId, entityType, eventType, title,
            user.UserId ?? Guid.Empty, user.Username ?? "system", description);
        await repository.AddTimelineEventAsync(entry, cancellationToken);
    }
}

public sealed class CreateWarehouseHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<CreateWarehouseCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Warehouse code is required.");
        if (string.IsNullOrWhiteSpace(command.NameAr))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.NameAr), "Warehouse name is required.");

        if (await repository.WarehouseCodeExistsAsync(command.BranchId, command.Code.Trim(), cancellationToken: cancellationToken))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Warehouse code already exists.");

        var warehouse = Warehouse.Create(
            command.BranchId, command.Code.Trim(), command.NameAr.Trim(), command.City.Trim(),
            command.NameEn?.Trim(), command.Description?.Trim(), command.Address?.Trim(),
            command.Manager?.Trim(), command.CostCenterId, command.Notes?.Trim(),
            command.IsDefault, command.CapacityRolls);

        await repository.AddWarehouseAsync(warehouse, cancellationToken);
        await InventoryTrailRecorder.RecordAuditAsync(repository, user, warehouse.Id, "Warehouse", "Created", cancellationToken: cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, warehouse.Id, "Warehouse", "Lifecycle", "تم إنشاء المستودع", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(warehouse.Id);
    }
}

public sealed class UpdateWarehouseHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<UpdateWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var warehouse = await repository.GetWarehouseByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse is null)
            return ApplicationResult.NotFound("Warehouse not found.");

        warehouse.Update(command.NameAr.Trim(), command.City.Trim(), command.NameEn?.Trim(),
            command.Description?.Trim(), command.Address?.Trim(), command.Manager?.Trim(),
            command.CostCenterId, command.Notes?.Trim(), command.CapacityRolls);
        if (command.IsDefault.HasValue)
            warehouse.SetDefault(command.IsDefault.Value);

        await repository.UpdateWarehouseAsync(warehouse, cancellationToken);
        await InventoryTrailRecorder.RecordAuditAsync(repository, user, warehouse.Id, "Warehouse", "Updated", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DeactivateWarehouseHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<DeactivateWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var warehouse = await repository.GetWarehouseByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse is null) return ApplicationResult.NotFound("Warehouse not found.");
        warehouse.Deactivate();
        await repository.UpdateWarehouseAsync(warehouse, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, warehouse.Id, "Warehouse", "Lifecycle", "تم تعطيل المستودع", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class ArchiveWarehouseHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<ArchiveWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ArchiveWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        if (await repository.WarehouseHasStockAsync(command.WarehouseId, cancellationToken))
            return ApplicationResult.ValidationFailed("Warehouse", "Cannot archive warehouse with stock.");

        await repository.ArchiveWarehouseAsync(command.WarehouseId, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.WarehouseId, "Warehouse", "Lifecycle", "تم أرشفة المستودع", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class ActivateWarehouseHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<ActivateWarehouseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ActivateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var warehouse = await repository.GetWarehouseByIdAsync(command.WarehouseId, cancellationToken);
        if (warehouse is null) return ApplicationResult.NotFound("Warehouse not found.");
        warehouse.Activate();
        await repository.UpdateWarehouseAsync(warehouse, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, warehouse.Id, "Warehouse", "Lifecycle", "تم تفعيل المستودع", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DuplicateWarehouseHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<DuplicateWarehouseCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        DuplicateWarehouseCommand command, CancellationToken cancellationToken = default)
    {
        var source = await repository.GetWarehouseByIdAsync(command.WarehouseId, cancellationToken);
        if (source is null) return ApplicationResult<Guid>.NotFound("Warehouse not found.");

        var baseCode = source.Code;
        var suffix = 2;
        string newCode;
        do
        {
            newCode = $"{baseCode}-{suffix}";
            suffix++;
        } while (await repository.WarehouseCodeExistsAsync(source.BranchId, newCode, cancellationToken: cancellationToken));

        var copy = Warehouse.Create(
            source.BranchId, newCode, $"{source.NameAr} (نسخة)", source.City,
            source.NameEn, source.Description, source.Address, source.Manager,
            source.CostCenterId, source.Notes, isDefault: false, source.CapacityRolls);

        await repository.AddWarehouseAsync(copy, cancellationToken);
        await InventoryTrailRecorder.RecordAuditAsync(repository, user, copy.Id, "Warehouse", "Duplicated", cancellationToken: cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, copy.Id, "Warehouse", "Lifecycle", $"نسخ من {source.Code}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(copy.Id);
    }
}

public sealed class CreateStorageLocationHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<CreateStorageLocationCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateStorageLocationCommand command, CancellationToken cancellationToken = default)
    {
        var location = WarehouseLocation.Create(
            command.WarehouseId, (StorageLocationType)command.LocationType,
            command.Code.Trim(), command.Name.Trim(), command.ParentId,
            command.Zone, command.BinCode, command.CapacityMeters, command.Priority);

        await repository.AddLocationAsync(location, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.WarehouseId, "Warehouse", "Location", $"تم إضافة موقع: {location.Code}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(location.Id);
    }
}

public sealed class CreateStockTransferHandler(
    IInventoryManagementRepository repository,
    INumberingService numbering,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<CreateStockTransferCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateStockTransferCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
            return ApplicationResult<Guid>.ValidationFailed("Lines", "Transfer must have at least one line.");

        if (command.FromWarehouseId == command.ToWarehouseId)
            return ApplicationResult<Guid>.ValidationFailed("Warehouse", "Source and destination must differ.");

        foreach (var line in command.Lines)
        {
            if (line.FabricRollId.HasValue)
            {
                var ok = await repository.ValidateRollTransferAsync(
                    line.FabricRollId.Value, command.FromWarehouseId, line.QuantityMeters, cancellationToken);
                if (!ok)
                    return ApplicationResult<Guid>.ValidationFailed("Stock",
                        $"Roll transfer invalid or insufficient stock for {line.QuantityMeters:N2} m.");
            }
        }

        var number = await numbering.NextStockTransferNumberAsync(command.BranchId, cancellationToken);
        var transfer = StockTransfer.Create(number, command.FromWarehouseId, command.ToWarehouseId,
            command.FromLocationId, command.ToLocationId, command.Notes);

        var lines = command.Lines.Select(l => (l.FabricItemId, l.FabricColorId, l.QuantityMeters, l.RollCount, l.FabricRollId)).ToList();
        await repository.CreateTransferAsync(transfer, lines, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, transfer.Id, "StockTransfer", "Created", $"مناقلة {number}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(transfer.Id);
    }
}

public sealed class ApproveStockTransferHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<ApproveStockTransferCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveStockTransferCommand command, CancellationToken cancellationToken = default)
    {
        var detail = await repository.GetTransferDetailAsync(command.TransferId, cancellationToken);
        if (detail is null)
            return ApplicationResult.NotFound("Transfer not found.");

        if (detail.Status == InventoryDocumentStatus.Completed.ToString())
            return ApplicationResult.ValidationFailed("Status", "Transfer already completed.");

        var userId = user.UserId ?? Guid.Empty;
        await repository.ApproveTransferAsync(command.TransferId, userId, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.TransferId, "StockTransfer", "Approved", "تم اعتماد المناقلة", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class CompleteStockTransferHandler(
    IInventoryManagementRepository repository,
    IInventoryEngine engine,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<CompleteStockTransferCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CompleteStockTransferCommand command, CancellationToken cancellationToken = default)
    {
        await engine.CompleteTransferAsync(command.TransferId, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.TransferId, "StockTransfer", "Completed", "تم إكمال المناقلة", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class CreateStocktakeHandler(
    IInventoryManagementRepository repository,
    INumberingService numbering,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<CreateStocktakeCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateStocktakeCommand command, CancellationToken cancellationToken = default)
    {
        var number = await numbering.NextStocktakeNumberAsync(command.BranchId, cancellationToken);
        var session = StocktakeSession.Create(number, command.WarehouseId, command.Responsible, command.LocationId, command.Notes);
        session.StartCounting();
        await repository.CreateStocktakeAsync(session, cancellationToken);
        await repository.SeedStocktakeLinesAsync(session.Id, command.WarehouseId, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, session.Id, "Stocktake", "Created", $"جرد {number}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(session.Id);
    }
}

public sealed class UpdateStocktakeLinesHandler(
    IInventoryManagementRepository repository,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<UpdateStocktakeLinesCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateStocktakeLinesCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
            return ApplicationResult.ValidationFailed("Lines", "No lines to update.");

        var lines = command.Lines.Select(l => (l.LineId, l.CountedMeters)).ToList();
        await repository.UpdateStocktakeLineCountsAsync(command.SessionId, lines, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.SessionId, "Stocktake", "Counting", "تم تحديث العد", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class PostStocktakeHandler(
    IInventoryManagementRepository repository,
    IInventoryEngine engine,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<PostStocktakeCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostStocktakeCommand command, CancellationToken cancellationToken = default)
    {
        await engine.PostStocktakeAsync(command.SessionId, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.SessionId, "Stocktake", "Posted", "تم ترحيل الجرد", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class CreateOpeningStockHandler(
    IInventoryManagementRepository repository,
    INumberingService numbering,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<CreateOpeningStockCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateOpeningStockCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
            return ApplicationResult<Guid>.ValidationFailed("Lines", "Opening stock must have lines.");

        var number = await numbering.NextOpeningStockNumberAsync(command.BranchId, cancellationToken);
        var doc = OpeningStockDocument.Create(number, command.WarehouseId, command.OpeningDate,
            command.Reference, command.CurrencyCode, command.Notes);
        var lines = command.Lines.Select(l => (l.FabricItemId, l.FabricColorId, l.QuantityMeters, l.RollCount, l.UnitCost)).ToList();
        await repository.CreateOpeningStockAsync(doc, lines, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, doc.Id, "OpeningStock", "Created", $"مواد أول مدة {number}", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(doc.Id);
    }
}

public sealed class PostOpeningStockHandler(
    IInventoryManagementRepository repository,
    IInventoryEngine engine,
    IUnitOfWork unitOfWork,
    ICurrentUserService user)
    : ICommandHandler<PostOpeningStockCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostOpeningStockCommand command, CancellationToken cancellationToken = default)
    {
        await engine.PostOpeningStockAsync(command.DocumentId, cancellationToken);
        await InventoryTrailRecorder.RecordTimelineAsync(repository, user, command.DocumentId, "OpeningStock", "Posted", "تم ترحيل مواد أول المدة", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}
