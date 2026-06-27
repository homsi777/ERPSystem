using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Events.Audit;

public sealed record AuditActionRecorded(
    Guid? UserId,
    string Action,
    string EntityType,
    Guid EntityId) : DomainEvent;
