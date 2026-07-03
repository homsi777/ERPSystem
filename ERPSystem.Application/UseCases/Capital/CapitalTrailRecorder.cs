using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Entities.Capital;

namespace ERPSystem.Application.UseCases.Capital;

public static class CapitalTrailRecorder
{
    public static async Task RecordAuditAsync(
        ICapitalPartnerRepository repository,
        ICurrentUserService user,
        Guid partnerId,
        string action,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var entry = PartnerAuditEntry.Record(
            partnerId,
            action,
            user.UserId ?? Guid.Empty,
            user.Username ?? "system",
            fieldName,
            previousValue,
            newValue,
            notes);
        await repository.AddAuditEntryAsync(entry, cancellationToken);
    }

    public static async Task RecordTimelineAsync(
        ICapitalPartnerRepository repository,
        ICurrentUserService user,
        Guid partnerId,
        string eventType,
        string title,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var entry = PartnerTimelineEvent.Record(
            partnerId,
            eventType,
            title,
            user.UserId ?? Guid.Empty,
            user.Username ?? "system",
            description,
            previousValue,
            newValue,
            notes);
        await repository.AddTimelineEventAsync(entry, cancellationToken);
    }
}
