using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public AuditInfo Created { get; protected set; } = AuditInfo.CreateSystem();
    public AuditInfo? Modified { get; protected set; }

    protected void MarkModified(Guid userId, string userName) =>
        Modified = AuditInfo.Create(userId, userName);
}
