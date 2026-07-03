namespace ERPSystem.Infrastructure.Persistence.Models.Company;

public class CompanyEntity : PersistenceEntity
{
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string DefaultCurrency { get; set; } = "USD";
}

public class BranchEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
}
