using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.UseCases.Expenses;

public static class ExpenseTrailRecorder
{
    public static async Task RecordAuditAsync(
        IExpenseRepository repository,
        ICurrentUserService user,
        Guid expenseId,
        string action,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var entry = ExpenseAuditEntry.Record(
            expenseId,
            action,
            user.UserId ?? Guid.Empty,
            user.Username ?? "system",
            fieldName,
            previousValue,
            newValue,
            reason);
        await repository.AddAuditEntryAsync(entry, cancellationToken);
    }

    public static async Task RecordTimelineAsync(
        IExpenseRepository repository,
        ICurrentUserService user,
        Guid expenseId,
        string eventType,
        string title,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var entry = ExpenseTimelineEvent.Record(
            expenseId,
            eventType,
            title,
            user.UserId ?? Guid.Empty,
            user.Username ?? "system",
            description,
            previousValue,
            newValue,
            reason);
        await repository.AddTimelineEventAsync(entry, cancellationToken);
    }

    public static async Task RecordStatusChangeAsync(
        IExpenseRepository repository,
        ICurrentUserService user,
        Guid expenseId,
        ExpenseStatus from,
        ExpenseStatus to,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        await RecordAuditAsync(
            repository, user, expenseId, "StatusChange",
            "Status", from.ToString(), to.ToString(), reason, cancellationToken);

        await RecordTimelineAsync(
            repository, user, expenseId, "Lifecycle",
            $"تغيير الحالة: {from} → {to}",
            previousValue: from.ToString(),
            newValue: to.ToString(),
            reason: reason,
            cancellationToken: cancellationToken);
    }
}
