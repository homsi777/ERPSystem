namespace ERPSystem.Infrastructure.Persistence.Models;

public abstract class PersistenceEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Guid? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsArchived { get; set; }
}

public abstract class CancellablePersistenceEntity : PersistenceEntity
{
    public DateTime? CancelledAt { get; set; }
    public Guid? CancelledByUserId { get; set; }
    public string? CancelReason { get; set; }
}
