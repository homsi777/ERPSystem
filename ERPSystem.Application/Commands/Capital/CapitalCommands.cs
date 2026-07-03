using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Capital;

public sealed class CreateCapitalPartnerCommand
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string FullName { get; init; } = "";
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public PartnerRiskLevel RiskLevel { get; init; } = PartnerRiskLevel.Medium;
}

public sealed class CreateCapitalPartnerWithSetupCommand
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string FullName { get; init; } = "";
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public PartnerRiskLevel RiskLevel { get; init; } = PartnerRiskLevel.Medium;
    public decimal OwnershipPercentage { get; init; }
    public decimal InitialInvestmentAmount { get; init; }
}

public sealed class UpdateCapitalPartnerCommand
{
    public Guid PartnerId { get; init; }
    public string FullName { get; init; } = "";
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public string DefaultCurrency { get; init; } = "USD";
    public PartnerRiskLevel RiskLevel { get; init; }
}

public sealed class AddPartnerParticipationCommand
{
    public Guid PartnerId { get; init; }
    public PartnershipScope Scope { get; init; }
    public decimal OwnershipPercentage { get; init; }
    public string? ProjectCode { get; init; }
    public Guid? ContainerId { get; init; }
    public string? ContainerNumber { get; init; }
}

public sealed class RecordCapitalTransactionCommand
{
    public Guid PartnerId { get; init; }
    public Guid? ParticipationId { get; init; }
    public CapitalTransactionType Type { get; init; }
    public decimal AmountOriginal { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal ExchangeRate { get; init; } = 1m;
    public string BaseCurrency { get; init; } = "USD";
    public DateTime TransactionDate { get; init; }
    public PartnershipScope Scope { get; init; }
    public string? ProjectCode { get; init; }
    public Guid? ContainerId { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
}

public sealed class SetPartnerCompanyOwnershipCommand
{
    public Guid PartnerId { get; init; }
    public decimal OwnershipPercentage { get; init; }
}

public sealed class ArchiveCapitalPartnerCommand
{
    public Guid PartnerId { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreateProfitDistributionCommand
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public PartnershipScope Scope { get; init; }
    public string? ProjectCode { get; init; }
    public Guid? ContainerId { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal GrossRevenue { get; init; }
    public decimal TotalCosts { get; init; }
    public string? Notes { get; init; }
}

public sealed class ApproveProfitDistributionCommand
{
    public Guid DistributionId { get; init; }
}

public sealed class PostProfitDistributionCommand
{
    public Guid DistributionId { get; init; }
}

public sealed class CloseProfitDistributionCommand
{
    public Guid DistributionId { get; init; }
}
