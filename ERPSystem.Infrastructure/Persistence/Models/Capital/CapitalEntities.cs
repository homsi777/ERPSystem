using ERPSystem.Infrastructure.Persistence.Models;

namespace ERPSystem.Infrastructure.Persistence.Models.Capital;

public class CapitalPartnerEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? PhotoPath { get; set; }
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string DefaultCurrency { get; set; } = "SAR";
    public int Status { get; set; }
    public int RiskLevel { get; set; }

    public ICollection<PartnerParticipationEntity> Participations { get; set; } = [];
    public ICollection<PartnerBankAccountEntity> BankAccounts { get; set; } = [];
    public ICollection<CapitalTransactionEntity> Transactions { get; set; } = [];
}

public class PartnerParticipationEntity
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public int Scope { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public string? ProjectCode { get; set; }
    public Guid? ContainerId { get; set; }
    public string? ContainerNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public CapitalPartnerEntity? Partner { get; set; }
}

public class PartnerBankAccountEntity
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public string? Iban { get; set; }
    public string Currency { get; set; } = "SAR";
    public bool IsDefault { get; set; }
    public CapitalPartnerEntity? Partner { get; set; }
}

public class CapitalTransactionEntity
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public Guid? ParticipationId { get; set; }
    public int Type { get; set; }
    public decimal AmountOriginal { get; set; }
    public string Currency { get; set; } = "SAR";
    public decimal ExchangeRate { get; set; } = 1m;
    public string BaseCurrency { get; set; } = "SAR";
    public decimal AmountBase { get; set; }
    public DateTime TransactionDate { get; set; }
    public int Scope { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectCode { get; set; }
    public Guid? ContainerId { get; set; }
    public int ApprovalStatus { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public Guid? ProfitDistributionId { get; set; }
    public CapitalPartnerEntity? Partner { get; set; }
}

public class ProfitDistributionEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public int Scope { get; set; }
    public string? ProjectCode { get; set; }
    public Guid? ContainerId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal GrossRevenue { get; set; }
    public decimal TotalCosts { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetLoss { get; set; }
    public string BaseCurrency { get; set; } = "SAR";
    public int Status { get; set; }
    public string? Notes { get; set; }
    public ICollection<ProfitDistributionLineEntity> Lines { get; set; } = [];
}

public class ProfitDistributionLineEntity
{
    public Guid Id { get; set; }
    public Guid DistributionId { get; set; }
    public Guid PartnerId { get; set; }
    public decimal OwnershipPercentage { get; set; }
    public decimal PartnerShare { get; set; }
    public decimal CompanyShare { get; set; }
    public ProfitDistributionEntity? Distribution { get; set; }
}

public class PartnerAuditLogEntity
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string Action { get; set; } = "";
    public string? FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}

public class PartnerTimelineEventEntity
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string EventType { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Notes { get; set; }
}
