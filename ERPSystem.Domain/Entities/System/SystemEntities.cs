namespace ERPSystem.Domain.Entities.System;

public class AuditLog
{
    public Guid Id { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public Guid? UserId { get; private set; }
    public string Action { get; private set; } = "";
    public string EntityType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public string? OldValuesJson { get; private set; }
    public string? NewValuesJson { get; private set; }
    public Guid? BranchId { get; private set; }

    private AuditLog() { }

    public static AuditLog Record(
        Guid? userId,
        string action,
        string entityType,
        Guid entityId,
        string? oldValues = null,
        string? newValues = null,
        Guid? branchId = null) => new()
    {
        Id = Guid.NewGuid(),
        OccurredAt = DateTime.UtcNow,
        UserId = userId,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        OldValuesJson = oldValues,
        NewValuesJson = newValues,
        BranchId = branchId
    };
}

public class DocumentTemplate
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string DocumentType { get; private set; } = "";
    public bool IsActive { get; private set; } = true;

    private DocumentTemplate() { }

    public static DocumentTemplate Create(string code, string nameAr, string documentType) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        NameAr = nameAr,
        DocumentType = documentType
    };
}

public class SystemSetting
{
    public Guid Id { get; private set; }
    public string Key { get; private set; } = "";
    public string Value { get; private set; } = "";
    public Guid? CompanyId { get; private set; }
    public Guid? BranchId { get; private set; }

    private SystemSetting() { }

    public static SystemSetting Create(string key, string value, Guid? companyId = null) => new()
    {
        Id = Guid.NewGuid(),
        Key = key,
        Value = value,
        CompanyId = companyId
    };
}
