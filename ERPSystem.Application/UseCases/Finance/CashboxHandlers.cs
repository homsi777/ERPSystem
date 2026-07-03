using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Finance;

public sealed class CreateCashboxHandler(
    ICashboxRepository cashboxRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService)
    : ICommandHandler<CreateCashboxCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateCashboxCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Name), "Cashbox name is required.");

        if (!await permissionService.CanAsync("finance.cashbox.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create cashboxes.");

        var code = string.IsNullOrWhiteSpace(command.Code)
            ? await numberingService.NextCashboxCodeAsync(command.BranchId, cancellationToken)
            : command.Code.Trim();

        if (await cashboxRepository.ExistsByCodeAsync(command.BranchId, code, cancellationToken: cancellationToken))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Cashbox code already exists.");

        var cashbox = Cashbox.Create(command.BranchId, code, command.Name.Trim(), command.Currency);
        await cashboxRepository.AddAsync(cashbox, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(cashbox.Id);
    }
}

public sealed class UpdateCashboxHandler(
    ICashboxRepository cashboxRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateCashboxCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateCashboxCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CashboxId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.CashboxId), "Cashbox is required.");
        if (string.IsNullOrWhiteSpace(command.Name))
            return ApplicationResult.ValidationFailed(nameof(command.Name), "Cashbox name is required.");

        if (!await permissionService.CanAsync("finance.cashbox.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to edit cashboxes.");

        var cashbox = await cashboxRepository.GetByIdAsync(command.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        var code = command.Code.Trim();
        if (await cashboxRepository.ExistsByCodeAsync(cashbox.BranchId, code, command.CashboxId, cancellationToken))
            return ApplicationResult.ValidationFailed(nameof(command.Code), "Cashbox code already exists.");

        cashbox.UpdateProfile(code, command.Name.Trim(), command.Currency);
        await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DeactivateCashboxHandler(
    ICashboxRepository cashboxRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<DeactivateCashboxCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeactivateCashboxCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("finance.cashbox.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to deactivate cashboxes.");

        var cashbox = await cashboxRepository.GetByIdAsync(command.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        cashbox.Deactivate();
        await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class ActivateCashboxHandler(
    ICashboxRepository cashboxRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ActivateCashboxCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ActivateCashboxCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("finance.cashbox.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to activate cashboxes.");

        var cashbox = await cashboxRepository.GetByIdAsync(command.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult.NotFound("Cashbox not found.");

        cashbox.Activate();
        await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class CreateCashboxTransferHandler(
    ICashboxRepository cashboxRepository,
    ICashboxTransferRepository transferRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService)
    : ICommandHandler<CreateCashboxTransferCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateCashboxTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.FromCashboxId == command.ToCashboxId)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.ToCashboxId), "Cannot transfer to the same cashbox.");
        if (command.Amount <= 0)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Amount), "Amount must be greater than zero.");

        if (!await permissionService.CanAsync("finance.cashbox.transfer", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to transfer between cashboxes.");

        var from = await cashboxRepository.GetByIdAsync(command.FromCashboxId, cancellationToken);
        var to = await cashboxRepository.GetByIdAsync(command.ToCashboxId, cancellationToken);
        if (from is null || to is null)
            return ApplicationResult<Guid>.NotFound("Cashbox not found.");
        if (!from.IsActive || !to.IsActive)
            return ApplicationResult<Guid>.ValidationFailed("Cashbox", "Both cashboxes must be active.");
        if (from.Balance.Amount < command.Amount)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Amount), "Insufficient cashbox balance.");

        var number = await numberingService.NextCashboxTransferNumberAsync(command.BranchId, cancellationToken);
        var transfer = CashboxTransfer.Create(
            number,
            command.FromCashboxId,
            command.ToCashboxId,
            new Money(command.Amount, from.Currency),
            command.Notes);

        if (command.PostImmediately)
        {
            transfer.Approve();
            transfer.Post();
            from.ApplyPayment(new Money(command.Amount, from.Currency));
            to.ApplyReceipt(new Money(command.Amount, to.Currency));
            await cashboxRepository.UpdateAsync(from, cancellationToken);
            await cashboxRepository.UpdateAsync(to, cancellationToken);
        }

        await transferRepository.AddAsync(transfer, command.CompanyId, command.BranchId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(transfer.Id);
    }
}

public sealed class PostCashboxTransferHandler(
    ICashboxRepository cashboxRepository,
    ICashboxTransferRepository transferRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<PostCashboxTransferCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostCashboxTransferCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("finance.cashbox.transfer", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to post cashbox transfers.");

        var transfer = await transferRepository.GetByIdAsync(command.TransferId, cancellationToken);
        if (transfer is null)
            return ApplicationResult.NotFound("Transfer not found.");
        if (transfer.Status == VoucherStatus.Posted)
            return ApplicationResult.ValidationFailed("Status", "Transfer already posted.");

        var from = await cashboxRepository.GetByIdAsync(transfer.FromCashboxId, cancellationToken);
        var to = await cashboxRepository.GetByIdAsync(transfer.ToCashboxId, cancellationToken);
        if (from is null || to is null)
            return ApplicationResult.NotFound("Cashbox not found.");
        if (from.Balance.Amount < transfer.Amount.Amount)
            return ApplicationResult.ValidationFailed("Amount", "Insufficient cashbox balance.");

        if (transfer.Status == VoucherStatus.Draft)
            transfer.Approve();
        transfer.Post();

        from.ApplyPayment(transfer.Amount);
        to.ApplyReceipt(transfer.Amount);
        await cashboxRepository.UpdateAsync(from, cancellationToken);
        await cashboxRepository.UpdateAsync(to, cancellationToken);
        await transferRepository.UpdateAsync(transfer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class GetCashboxListHandler(ICashboxRepository cashboxRepository)
    : IQueryHandler<GetCashboxListQuery, ApplicationResult<IReadOnlyList<CashboxListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<CashboxListDto>>> HandleAsync(
        GetCashboxListQuery query,
        CancellationToken cancellationToken = default)
    {
        var boxes = await cashboxRepository.GetListAsync(query.BranchId, cancellationToken);
        var items = boxes
            .Where(b => query.IncludeInactive || b.IsActive)
            .Select(b => new CashboxListDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                Balance = b.Balance.Amount,
                Currency = b.Currency,
                IsActive = b.IsActive
            })
            .OrderBy(b => b.Code)
            .ToList();
        return ApplicationResult<IReadOnlyList<CashboxListDto>>.Success(items);
    }
}

public sealed class GetCashboxDetailsHandler(ICashboxRepository cashboxRepository)
    : IQueryHandler<GetCashboxDetailsQuery, ApplicationResult<CashboxDetailsDto>>
{
    public async Task<ApplicationResult<CashboxDetailsDto>> HandleAsync(
        GetCashboxDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var cashbox = await cashboxRepository.GetByIdAsync(query.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult<CashboxDetailsDto>.NotFound("Cashbox not found.");

        var (receipts, payments) = await cashboxRepository.GetTodayTotalsAsync(query.CashboxId, cancellationToken);
        return ApplicationResult<CashboxDetailsDto>.Success(new CashboxDetailsDto
        {
            Id = cashbox.Id,
            Code = cashbox.Code,
            Name = cashbox.Name,
            Balance = cashbox.Balance.Amount,
            Currency = cashbox.Currency,
            IsActive = cashbox.IsActive,
            TodayReceipts = receipts,
            TodayPayments = payments
        });
    }
}

public sealed class GetCashboxMovementsHandler(ICashboxRepository cashboxRepository)
    : IQueryHandler<GetCashboxMovementsQuery, ApplicationResult<IReadOnlyList<CashboxMovementDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<CashboxMovementDto>>> HandleAsync(
        GetCashboxMovementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var rows = await cashboxRepository.GetMovementsAsync(
            query.CashboxId, query.FromDate, query.ToDate, cancellationToken);
        var items = rows.Select(r => new CashboxMovementDto
        {
            MovementDate = r.Date,
            ReferenceType = r.Type,
            ReferenceNumber = r.Number,
            Description = r.Description,
            Amount = r.Amount,
            IsInbound = r.IsInbound
        }).ToList();
        return ApplicationResult<IReadOnlyList<CashboxMovementDto>>.Success(items);
    }
}

public sealed class GetCashboxTransferListHandler(
    ICashboxTransferRepository transferRepository,
    ICashboxRepository cashboxRepository)
    : IQueryHandler<GetCashboxTransferListQuery, ApplicationResult<IReadOnlyList<CashboxTransferListDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<CashboxTransferListDto>>> HandleAsync(
        GetCashboxTransferListQuery query,
        CancellationToken cancellationToken = default)
    {
        var transfers = await transferRepository.GetListAsync(
            query.BranchId, query.Status, query.CashboxId, cancellationToken);
        var boxes = await cashboxRepository.GetListAsync(query.BranchId, cancellationToken);
        var nameMap = boxes.ToDictionary(b => b.Id, b => b.Name);

        var items = transfers.Select(t => new CashboxTransferListDto
        {
            Id = t.Id,
            TransferNumber = t.Number,
            FromCashboxName = nameMap.GetValueOrDefault(t.FromCashboxId, "—"),
            ToCashboxName = nameMap.GetValueOrDefault(t.ToCashboxId, "—"),
            TransferDate = t.TransferDate,
            Amount = t.Amount.Amount,
            Currency = t.Amount.Currency,
            Status = t.Status,
            StatusDisplay = VoucherStatusDisplay(t.Status)
        }).ToList();

        return ApplicationResult<IReadOnlyList<CashboxTransferListDto>>.Success(items);
    }

    private static string VoucherStatusDisplay(VoucherStatus status) => status switch
    {
        VoucherStatus.Draft => "مسودة",
        VoucherStatus.Approved => "معتمد",
        VoucherStatus.Posted => "مرحّل",
        VoucherStatus.Cancelled => "ملغى",
        _ => status.ToString()
    };
}

public sealed class GetCashboxOperationsCenterHandler(
    ICashboxRepository cashboxRepository,
    ICashboxTransferRepository transferRepository)
    : IQueryHandler<GetCashboxOperationsCenterQuery, ApplicationResult<CashboxOperationsCenterDto>>
{
    public async Task<ApplicationResult<CashboxOperationsCenterDto>> HandleAsync(
        GetCashboxOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var detailsHandler = new GetCashboxDetailsHandler(cashboxRepository);
        var details = await detailsHandler.HandleAsync(
            new GetCashboxDetailsQuery { CashboxId = query.CashboxId }, cancellationToken);
        if (!details.IsSuccess || details.Value is null)
            return ApplicationResult<CashboxOperationsCenterDto>.NotFound("Cashbox not found.");

        var cashbox = await cashboxRepository.GetByIdAsync(query.CashboxId, cancellationToken);
        if (cashbox is null)
            return ApplicationResult<CashboxOperationsCenterDto>.NotFound("Cashbox not found.");

        var movements = await cashboxRepository.GetMovementsAsync(query.CashboxId, cancellationToken: cancellationToken);
        var movementDtos = movements.Take(50).Select(r => new CashboxMovementDto
        {
            MovementDate = r.Date,
            ReferenceType = r.Type,
            ReferenceNumber = r.Number,
            Description = r.Description,
            Amount = r.Amount,
            IsInbound = r.IsInbound
        }).ToList();

        var transfers = await transferRepository.GetListAsync(
            cashbox.BranchId, cashboxId: query.CashboxId, cancellationToken: cancellationToken);
        var boxes = await cashboxRepository.GetListAsync(cashbox.BranchId, cancellationToken);
        var nameMap = boxes.ToDictionary(b => b.Id, b => b.Name);
        var transferDtos = transfers.Take(20).Select(t => new CashboxTransferListDto
        {
            Id = t.Id,
            TransferNumber = t.Number,
            FromCashboxName = nameMap.GetValueOrDefault(t.FromCashboxId, "—"),
            ToCashboxName = nameMap.GetValueOrDefault(t.ToCashboxId, "—"),
            TransferDate = t.TransferDate,
            Amount = t.Amount.Amount,
            Currency = t.Amount.Currency,
            Status = t.Status,
            StatusDisplay = t.Status switch
            {
                VoucherStatus.Posted => "مرحّل",
                VoucherStatus.Draft => "مسودة",
                _ => t.Status.ToString()
            }
        }).ToList();

        return ApplicationResult<CashboxOperationsCenterDto>.Success(new CashboxOperationsCenterDto
        {
            Cashbox = details.Value,
            RecentMovements = movementDtos,
            RecentTransfers = transferDtos
        });
    }
}
