using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using ERPSystem.Application.Posting;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.Services;
using ERPSystem.Domain.ValueObjects;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class AccountingPostingEngine(
    ErpDbContext context,
    IJournalEntryRepository journalRepository,
    INumberingService numberingService) : IAccountingPostingEngine
{
    private const int ProtectedIdentityVersion = 2;
    private const decimal BalanceTolerance = 0.01m;

    public async Task<PostingResult> PostAsync(PostingRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        try
        {
            var existing = await FindProtectedEntryAsync(request, cancellationToken);
            if (existing is not null)
            {
                var replayAttempt = await RecordAttemptAsync(
                    request,
                    (int)PostingAttemptStatus.Posted,
                    existing.Id,
                    "already_posted",
                    null,
                    cancellationToken);

                return PostingResult.Succeeded(
                    existing.Id,
                    existing.EntryNumber,
                    replayAttempt.Id,
                    alreadyPosted: true);
            }

            var attempt = await RecordAttemptAsync(
                request,
                (int)PostingAttemptStatus.Posting,
                null,
                null,
                null,
                cancellationToken);

            await ValidateAccountsAsync(request, cancellationToken);

            var entryNumber = await numberingService.NextJournalEntryNumberAsync(request.BranchId, cancellationToken);
            var aggregate = AccountingAggregate.CreateDraft(
                entryNumber,
                request.PostingDate,
                request.Description,
                request.UserId,
                request.SourceType,
                request.SourceId,
                request.JournalBookId);

            foreach (var line in request.Lines.Where(l => l.Debit > 0 || l.Credit > 0))
            {
                aggregate.AddLine(JournalEntryLine.Create(
                    line.AccountId,
                    new Money(line.Debit),
                    new Money(line.Credit),
                    line.Narrative,
                    line.PartyId));
            }

            aggregate.Post(request.UserId);
            AccountingPostingPolicy.EnsureCanPost(aggregate);

            await journalRepository.AddAsync(
                aggregate,
                request.CompanyId,
                request.BranchId,
                new JournalEntryPostMetadata(
                    request.PostingKind,
                    ProtectedIdentityVersion,
                    request.IdempotencyKey,
                    request.CorrelationId),
                cancellationToken);

            attempt.JournalEntryId = aggregate.Id;
            attempt.Status = (int)PostingAttemptStatus.Posted;
            attempt.CompletedAt = DateTime.UtcNow;
            attempt.UpdatedAt = DateTime.UtcNow;

            return PostingResult.Succeeded(aggregate.Id, aggregate.EntryNumber, attempt.Id);
        }
        catch (Exception ex)
        {
            await MarkLatestAttemptFailedAsync(request, ex, cancellationToken);
            return PostingResult.Failed("posting_failed", ex.Message);
        }
    }

    public async Task<PostingResult> RecoverFromUniqueViolationAsync(
        PostingRequest request,
        CancellationToken cancellationToken = default)
    {
        DetachProtectedPostingEntries(context);

        var existing = await FindProtectedEntryAsync(request, cancellationToken)
            ?? throw new AccountingException(
                "Duplicate posting detected but the existing journal entry could not be loaded.");

        var trackedAttempt = context.ChangeTracker.Entries<AccountingPostingAttemptEntity>()
            .Select(e => e.Entity)
            .Where(a => a.CompanyId == request.CompanyId
                        && a.SourceType == (int)request.SourceType
                        && a.SourceId == request.SourceId
                        && a.PostingKind == (int)request.PostingKind
                        && a.Status == (int)PostingAttemptStatus.Posting)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefault();

        if (trackedAttempt is not null)
        {
            trackedAttempt.JournalEntryId = existing.Id;
            trackedAttempt.Status = (int)PostingAttemptStatus.Posted;
            trackedAttempt.CompletedAt = DateTime.UtcNow;
            trackedAttempt.ErrorCode = "duplicate_posting_identity";
            trackedAttempt.ErrorMessage = "Recovered idempotently after unique constraint.";
            trackedAttempt.UpdatedAt = DateTime.UtcNow;
        }

        return PostingResult.Succeeded(
            existing.Id,
            existing.EntryNumber,
            trackedAttempt?.Id ?? Guid.Empty,
            alreadyPosted: true);
    }

    public Task<ReversalResult> ReverseAsync(ReversalRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReversalResult.NotImplemented());

    private async Task MarkLatestAttemptFailedAsync(
        PostingRequest request,
        Exception ex,
        CancellationToken cancellationToken)
    {
        var attempt = context.ChangeTracker.Entries<AccountingPostingAttemptEntity>()
            .Select(e => e.Entity)
            .Where(a => a.CompanyId == request.CompanyId
                        && a.SourceType == (int)request.SourceType
                        && a.SourceId == request.SourceId
                        && a.PostingKind == (int)request.PostingKind
                        && a.Status == (int)PostingAttemptStatus.Posting)
            .OrderByDescending(a => a.StartedAt)
            .FirstOrDefault();

        if (attempt is null)
            return;

        attempt.Status = (int)PostingAttemptStatus.PostingFailed;
        attempt.ErrorCode = ex.GetType().Name;
        attempt.ErrorMessage = ex.Message;
        attempt.CompletedAt = DateTime.UtcNow;
        attempt.RetryCount += 1;
        attempt.UpdatedAt = DateTime.UtcNow;

        await Task.CompletedTask;
    }

    private static void ValidateRequest(PostingRequest request)
    {
        if (request.CompanyId == Guid.Empty)
            throw new AccountingException("CompanyId is required for posting.");
        if (request.BranchId == Guid.Empty)
            throw new AccountingException("BranchId is required for posting.");
        if (request.SourceId == Guid.Empty)
            throw new AccountingException("SourceId is required for posting.");
        if (request.UserId == Guid.Empty)
            throw new AccountingException("UserId is required for posting.");
        if (request.Lines.Count == 0)
            throw new AccountingException("At least one posting line is required.");

        decimal debit = 0m;
        decimal credit = 0m;
        foreach (var line in request.Lines)
        {
            if (line.Debit < 0 || line.Credit < 0)
                throw new AccountingException("Negative amounts are not allowed.");
            if (line.Debit > 0 && line.Credit > 0)
                throw new AccountingException("A line cannot contain both debit and credit.");
            debit += line.Debit;
            credit += line.Credit;
        }

        if (Math.Abs(debit - credit) > BalanceTolerance)
            throw new AccountingException("Posting lines are not balanced.");
    }

    private async Task ValidateAccountsAsync(PostingRequest request, CancellationToken cancellationToken)
    {
        var accountIds = request.Lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await context.Accounts.AsNoTracking()
            .Where(a => accountIds.Contains(a.Id))
            .Select(a => new { a.Id, a.IsActive })
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Count)
        {
            var missing = accountIds.Except(accounts.Select(a => a.Id)).Count();
            throw new AccountingException($"Required GL accounts are missing ({missing}).");
        }

        if (accounts.Any(a => !a.IsActive))
            throw new AccountingException("One or more GL accounts are inactive.");
    }

    private async Task<JournalEntryEntity?> FindProtectedEntryAsync(
        PostingRequest request,
        CancellationToken cancellationToken) =>
        await context.JournalEntries.AsNoTracking()
            .Where(j => j.CompanyId == request.CompanyId
                        && j.PostingIdentityVersion == ProtectedIdentityVersion
                        && j.SourceType == (int)request.SourceType
                        && j.SourceId == request.SourceId
                        && j.PostingKind == (int)request.PostingKind)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    private async Task<AccountingPostingAttemptEntity> RecordAttemptAsync(
        PostingRequest request,
        int status,
        Guid? journalEntryId,
        string? errorCode,
        string? errorMessage,
        CancellationToken cancellationToken)
    {
        var attempt = new AccountingPostingAttemptEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            BranchId = request.BranchId,
            SourceType = (int)request.SourceType,
            SourceId = request.SourceId,
            PostingKind = (int)request.PostingKind,
            Status = status,
            IdempotencyKey = request.IdempotencyKey,
            CorrelationId = request.CorrelationId,
            UserId = request.UserId,
            JournalEntryId = journalEntryId,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            RetryCount = 0,
            StartedAt = DateTime.UtcNow,
            CompletedAt = status == (int)PostingAttemptStatus.Posting ? null : DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await context.AccountingPostingAttempts.AddAsync(attempt, cancellationToken);
        return attempt;
    }

    internal static void DetachProtectedPostingEntries(ErpDbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries<JournalEntryEntity>()
                     .Where(e => e.State == EntityState.Added && e.Entity.PostingIdentityVersion == ProtectedIdentityVersion)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }

        foreach (var entry in context.ChangeTracker.Entries<JournalEntryLineEntity>()
                     .Where(e => e.State == EntityState.Added)
                     .ToList())
        {
            entry.State = EntityState.Detached;
        }
    }
}
