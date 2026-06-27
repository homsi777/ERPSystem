namespace ERPSystem.Infrastructure.Persistence.Models.Parties;

public class CustomerEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public int Type { get; set; }
    public int Status { get; set; }
    public decimal CreditLimit { get; set; }
    public string CreditLimitCurrency { get; set; } = "SAR";
    public decimal Balance { get; set; }
    public string BalanceCurrency { get; set; } = "SAR";
    public int PaymentTermsDays { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressCity { get; set; }
    public Guid? SalesRepUserId { get; set; }
}

public class SupplierEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int Status { get; set; }
    public decimal Balance { get; set; }
    public string BalanceCurrency { get; set; } = "SAR";
}

public class ChinaSupplierEntity : PersistenceEntity
{
    public Guid SupplierId { get; set; }
    public string Port { get; set; } = "";
    public string DefaultIncoterm { get; set; } = "";
    public int LeadTimeDays { get; set; }
}
