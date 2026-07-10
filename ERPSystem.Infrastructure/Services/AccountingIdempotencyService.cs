using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Services;

internal sealed class AccountingIdempotencyService(ErpDbContext context) : IAccountingIdempotencyService
{
    public async Task<IdempotencyExecutionResult> BeginAsync(
        Guid companyId,
        Guid userId,
        string operation,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return new IdempotencyExecutionResult
            {
                IsReplay = false,
                IsConflict = false
            };
        }

        var existing = await context.AccountingIdempotencyRecords
            .FirstOrDefaultAsync(r =>
                    r.CompanyId == companyId
                    && r.UserId == userId
                    && r.Operation == operation
                    && r.IdempotencyKey == idempotencyKey,
                cancellationToken);

        if (existing is null)
        {
            var record = new AccountingIdempotencyRecordEntity
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                UserId = userId,
                Operation = operation,
                IdempotencyKey = idempotencyKey,
                RequestHash = requestHash,
                Status = (int)IdempotencyRecordStatus.InProgress,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                await context.AccountingIdempotencyRecords.AddAsync(record, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
                return new IdempotencyExecutionResult
                {
                    IsReplay = false,
                    IsConflict = false,
                    RecordId = record.Id
                };
            }
            catch (DbUpdateException)
            {
                context.Entry(record).State = EntityState.Detached;
                existing = await context.AccountingIdempotencyRecords.AsNoTracking()
                    .FirstOrDefaultAsync(r =>
                            r.CompanyId == companyId
                            && r.UserId == userId
                            && r.Operation == operation
                            && r.IdempotencyKey == idempotencyKey,
                        cancellationToken);
            }
        }

        if (existing is null)
        {
            return new IdempotencyExecutionResult
            {
                IsReplay = false,
                IsConflict = false
            };
        }

        if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
        {
            return new IdempotencyExecutionResult
            {
                IsReplay = false,
                IsConflict = true,
                RecordId = existing.Id
            };
        }

        if (existing.Status == (int)IdempotencyRecordStatus.Completed)
        {
            return new IdempotencyExecutionResult
            {
                IsReplay = true,
                IsConflict = false,
                StoredResponseJson = existing.ResponseJson,
                RecordId = existing.Id
            };
        }

        return new IdempotencyExecutionResult
        {
            IsReplay = false,
            IsConflict = false,
            RecordId = existing.Id
        };
    }

    public async Task CompleteAsync(
        Guid recordId,
        string responseJson,
        CancellationToken cancellationToken = default)
    {
        var record = await context.AccountingIdempotencyRecords
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken)
            ?? throw new AccountingException("Idempotency record not found.");

        record.Status = (int)IdempotencyRecordStatus.Completed;
        record.ResponseJson = responseJson;
        record.CompletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(
        Guid recordId,
        string failureCode,
        CancellationToken cancellationToken = default)
    {
        var record = await context.AccountingIdempotencyRecords
            .FirstOrDefaultAsync(r => r.Id == recordId, cancellationToken);
        if (record is null)
            return;

        record.Status = (int)IdempotencyRecordStatus.Failed;
        record.FailureCode = failureCode;
        record.CompletedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(cancellationToken);
    }
}
