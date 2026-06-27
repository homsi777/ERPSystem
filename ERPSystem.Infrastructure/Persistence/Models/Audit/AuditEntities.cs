namespace ERPSystem.Infrastructure.Persistence.Models.Audit;

public class AuditLogEntity
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string? OldValuesJson { get; set; }
    public string? NewValuesJson { get; set; }
    public Guid? BranchId { get; set; }
}
