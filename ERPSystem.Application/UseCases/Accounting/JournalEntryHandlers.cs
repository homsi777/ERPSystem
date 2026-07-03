using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.Common;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Services;
using ERPSystem.Domain.Specifications;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Accounting;

public sealed class CreateJournalEntryHandler(
    IJournalEntryRepository journalEntryRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService,
    ICurrentUserService currentUserService)
    : ICommandHandler<CreateJournalEntryCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        var validation = Validation.ApplicationValidators.Validate(command);
        if (validation is not null)
            return ApplicationResult<Guid>.ValidationFailed(validation.ValidationErrors);

        if (!await permissionService.CanAsync("accounting.journal.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create journal entries.");

        try
        {
            var entryNumber = await numberingService.NextJournalEntryNumberAsync(
                command.BranchId, cancellationToken);

            var userId = currentUserService.UserId ?? Guid.Empty;
            var aggregate = AccountingAggregate.CreateDraft(
                entryNumber,
                ApplicationDateNormalizer.ToUtcDate(command.EntryDate),
                command.Description,
                userId,
                command.SourceType,
                command.SourceId,
                command.JournalBookId ?? JournalBookIds.General);

            foreach (var line in command.Lines)
            {
                aggregate.AddLine(JournalEntryLine.Create(
                    line.AccountId,
                    new Money(line.Debit),
                    new Money(line.Credit),
                    line.Narrative,
                    line.PartyId));
            }

            Domain.Validators.JournalValidator.ValidateDraft(aggregate);

            await journalEntryRepository.AddAsync(aggregate, command.CompanyId, command.BranchId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class PostJournalEntryHandler(
    IJournalEntryRepository journalEntryRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INotificationService notificationService,
    ICurrentUserService currentUserService)
    : ICommandHandler<PostJournalEntryCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        PostJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.EntryId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.EntryId), "Entry is required.");

        if (!await permissionService.CanAsync("accounting.journal.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to post journal entries.");

        var aggregate = await journalEntryRepository.GetByIdAsync(command.EntryId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Journal entry not found.");

        var spec = new BalancedJournalSpecification();
        if (!spec.IsSatisfiedBy(aggregate))
            return ApplicationResult.Conflict(spec.FailureReason);

        try
        {
            AccountingPostingPolicy.EnsureCanPost(aggregate);

            var userId = currentUserService.UserId ?? Guid.Empty;
            aggregate.Post(userId);

            await journalEntryRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            await notificationService.PublishAsync(new JournalEntryPostedNotification
            {
                EntryId = aggregate.Id,
                EntryNumber = aggregate.EntryNumber,
                DebitTotal = aggregate.DebitTotal.Amount
            }, cancellationToken);

            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ApproveJournalEntryHandler(
    IJournalEntryRepository journalEntryRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<ApproveJournalEntryCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ApproveJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.EntryId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.EntryId), "Entry is required.");

        if (!await permissionService.CanAsync("accounting.journal.post", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve journal entries.");

        var aggregate = await journalEntryRepository.GetByIdAsync(command.EntryId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Journal entry not found.");

        try
        {
            aggregate.Approve();
            await journalEntryRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ReverseJournalEntryHandler(
    IJournalEntryRepository journalEntryRepository,
    IUnitOfWork unitOfWork,
    INumberingService numberingService,
    IPermissionService permissionService,
    ICurrentUserService currentUserService,
    ICurrentBranchService currentBranchService)
    : ICommandHandler<ReverseJournalEntryCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        ReverseJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.EntryId == Guid.Empty)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.EntryId), "Entry is required.");

        if (!await permissionService.CanAsync("accounting.journal.reverse", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to reverse journal entries.");

        var aggregate = await journalEntryRepository.GetByIdAsync(command.EntryId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult<Guid>.NotFound("Journal entry not found.");

        try
        {
            var branchId = currentBranchService.BranchId ?? Guid.Empty;
            var companyId = currentBranchService.CompanyId ?? Guid.Empty;
            var reversalNumber = await numberingService.NextJournalEntryNumberAsync(branchId, cancellationToken);
            var userId = currentUserService.UserId ?? Guid.Empty;

            var reversal = aggregate.CreateReversal(reversalNumber, userId);

            await journalEntryRepository.UpdateAsync(aggregate, cancellationToken);
            await journalEntryRepository.AddAsync(reversal, companyId, branchId, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return ApplicationResult<Guid>.Success(reversal.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class CancelJournalEntryHandler(
    IJournalEntryRepository journalEntryRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CancelJournalEntryCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        CancelJournalEntryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.EntryId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.EntryId), "Entry is required.");

        if (!await permissionService.CanAsync("accounting.journal.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to cancel journal entries.");

        var aggregate = await journalEntryRepository.GetByIdAsync(command.EntryId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Journal entry not found.");

        try
        {
            aggregate.Cancel();
            await journalEntryRepository.UpdateAsync(aggregate, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
