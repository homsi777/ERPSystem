namespace ERPSystem.Infrastructure.Persistence.Models.Finance;

public class CostCenterEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public Guid? ParentCostCenterId { get; set; }
    public int Status { get; set; }
    public CostCenterEntity? Parent { get; set; }
}
