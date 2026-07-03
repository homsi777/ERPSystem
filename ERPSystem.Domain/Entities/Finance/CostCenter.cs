using ERPSystem.Domain.Enums;

namespace ERPSystem.Domain.Entities.Finance;

public sealed class CostCenter
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public Guid? ParentCostCenterId { get; private set; }
    public CostCenterStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private CostCenter() { }

    public static CostCenter Create(
        Guid companyId,
        string code,
        string name,
        string? description = null,
        Guid? parentCostCenterId = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Code = code.Trim(),
        Name = name.Trim(),
        Description = description,
        ParentCostCenterId = parentCostCenterId,
        Status = CostCenterStatus.Active,
        CreatedAt = DateTime.UtcNow
    };

    public void Update(string name, string? description, Guid? parentCostCenterId)
    {
        Name = name.Trim();
        Description = description;
        ParentCostCenterId = parentCostCenterId;
    }

    public void SetStatus(CostCenterStatus status) => Status = status;

    public static CostCenter Rehydrate(
        Guid id,
        Guid companyId,
        string code,
        string name,
        string? description,
        Guid? parentCostCenterId,
        CostCenterStatus status,
        DateTime createdAt) => new()
    {
        Id = id,
        CompanyId = companyId,
        Code = code,
        Name = name,
        Description = description,
        ParentCostCenterId = parentCostCenterId,
        Status = status,
        CreatedAt = createdAt
    };
}
