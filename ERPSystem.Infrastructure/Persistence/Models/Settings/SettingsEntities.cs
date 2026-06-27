namespace ERPSystem.Infrastructure.Persistence.Models.Settings;

public class SystemSettingEntity : PersistenceEntity
{
    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
    public Guid? CompanyId { get; set; }
    public Guid? BranchId { get; set; }
}

public class DocumentTemplateEntity : PersistenceEntity
{
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string DocumentType { get; set; } = "";
}
