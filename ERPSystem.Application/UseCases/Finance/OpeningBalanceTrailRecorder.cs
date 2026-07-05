using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Domain.Entities.Finance;

namespace ERPSystem.Application.UseCases.Finance;

public static class OpeningBalanceTrailRecorder
{
    public static async Task RecordAsync(
        IOpeningBalanceRepository repository,
        ICurrentUserService user,
        Guid documentId,
        string action,
        string? oldValues = null,
        string? newValues = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var evt = OpeningBalanceEvent.Record(
            documentId,
            user.UserId,
            user.Username ?? "system",
            action,
            oldValues,
            newValues,
            notes,
            Environment.MachineName,
            null);
        await repository.AddEventAsync(evt, cancellationToken);
    }
}
