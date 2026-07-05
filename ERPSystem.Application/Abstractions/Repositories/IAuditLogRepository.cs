using ERPSystem.Domain.Entities.System;

namespace ERPSystem.Application.Abstractions.Repositories;

public sealed record AuditActivityItem(
    DateTime OccurredAt,
    Guid? UserId,
    string Action,
    string EntityType,
    Guid EntityId);

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog auditLog, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditLog>> GetByEntityAsync(
        string entityType,
        Guid entityId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AuditActivityItem>> GetRecentAsync(
        int limit,
        CancellationToken cancellationToken = default);
}
