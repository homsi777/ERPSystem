using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Capital;

public sealed class CapitalPartnerListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string FullName { get; init; } = "";
    public PartnerStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public PartnerRiskLevel RiskLevel { get; init; }
    public string RiskLevelDisplay { get; init; } = "";
    public string DefaultCurrency { get; init; } = "";
    public decimal CurrentCapitalBase { get; init; }
    public decimal TotalInvestmentsBase { get; init; }
    public decimal TotalWithdrawalsBase { get; init; }
    public int ParticipationsCount { get; init; }
    public decimal? CompanyOwnershipPercentage { get; init; }
    public string? Phone { get; init; }
}

public sealed class CapitalPartnerDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string FullName { get; init; } = "";
    public string? PhotoPath { get; init; }
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Notes { get; init; }
    public string DefaultCurrency { get; init; } = "";
    public PartnerStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public PartnerRiskLevel RiskLevel { get; init; }
    public string RiskLevelDisplay { get; init; } = "";
    public decimal CurrentCapitalBase { get; init; }
    public decimal TotalInvestmentsBase { get; init; }
    public decimal TotalWithdrawalsBase { get; init; }
    public decimal DistributedProfitBase { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedByName { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<PartnerParticipationDto> Participations { get; init; } = [];
    public IReadOnlyList<CapitalTransactionDto> Transactions { get; init; } = [];
    public IReadOnlyList<PartnerBankAccountDto> BankAccounts { get; init; } = [];
}

public sealed class PartnerParticipationDto
{
    public Guid Id { get; init; }
    public PartnershipScope Scope { get; init; }
    public string ScopeDisplay { get; init; } = "";
    public decimal OwnershipPercentage { get; init; }
    public string? ProjectCode { get; init; }
    public Guid? ContainerId { get; init; }
    public string? ContainerNumber { get; init; }
    public bool IsActive { get; init; }
    public DateTime EffectiveFrom { get; init; }
}

public sealed class PartnerBankAccountDto
{
    public Guid Id { get; init; }
    public string BankName { get; init; } = "";
    public string AccountNumber { get; init; } = "";
    public string? Iban { get; init; }
    public string Currency { get; init; } = "";
    public bool IsDefault { get; init; }
}

public sealed class CapitalTransactionDto
{
    public Guid Id { get; init; }
    public CapitalTransactionType Type { get; init; }
    public string TypeDisplay { get; init; } = "";
    public decimal AmountOriginal { get; init; }
    public string Currency { get; init; } = "";
    public decimal ExchangeRate { get; init; }
    public decimal AmountBase { get; init; }
    public decimal SignedBaseAmount { get; init; }
    public DateTime TransactionDate { get; init; }
    public PartnershipScope Scope { get; init; }
    public string ScopeDisplay { get; init; } = "";
    public string? ProjectCode { get; init; }
    public Guid? ContainerId { get; init; }
    public CapitalApprovalStatus ApprovalStatus { get; init; }
    public string ApprovalStatusDisplay { get; init; } = "";
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
}

public sealed class CapitalTransactionListDto
{
    public Guid Id { get; init; }
    public Guid PartnerId { get; init; }
    public string PartnerCode { get; init; } = "";
    public string PartnerName { get; init; } = "";
    public CapitalTransactionType Type { get; init; }
    public string TypeDisplay { get; init; } = "";
    public decimal AmountOriginal { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal SignedBaseAmount { get; init; }
    public DateTime TransactionDate { get; init; }
    public string? Notes { get; init; }
}

public sealed class CapitalOperationsCenterDto
{
    public CapitalPartnerDetailsDto Details { get; init; } = null!;
    public CapitalFinancialSummaryDto Financial { get; init; } = null!;
    public IReadOnlyList<CapitalScopeSummaryDto> ScopeSummaries { get; init; } = [];
    public IReadOnlyList<PartnerTimelineEventDto> Timeline { get; init; } = [];
    public IReadOnlyList<PartnerAuditEntryDto> RecentAudit { get; init; } = [];
    public CapitalPartnerStatisticsDto Statistics { get; init; } = null!;
}

public sealed class CapitalFinancialSummaryDto
{
    public decimal CurrentCapitalBase { get; init; }
    public decimal TotalInvestmentsBase { get; init; }
    public decimal TotalWithdrawalsBase { get; init; }
    public decimal DistributedProfitBase { get; init; }
    public decimal UndistributedProfitBase { get; init; }
    public string BaseCurrency { get; init; } = "USD";
    public int TransactionCount { get; init; }
    public int ParticipationCount { get; init; }
}

public sealed class CapitalScopeSummaryDto
{
    public PartnershipScope Scope { get; init; }
    public string ScopeDisplay { get; init; } = "";
    public decimal CapitalBase { get; init; }
    public int Count { get; init; }
}

public sealed class CapitalPartnerStatisticsDto
{
    public int TotalTransactions { get; init; }
    public int AuditEventCount { get; init; }
    public int DaysSinceCreated { get; init; }
}

public sealed class PartnerAuditEntryDto
{
    public Guid Id { get; init; }
    public string Action { get; init; } = "";
    public string? FieldName { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string UserName { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string? Notes { get; init; }
}

public sealed class PartnerTimelineEventDto
{
    public Guid Id { get; init; }
    public string EventType { get; init; } = "";
    public string Title { get; init; } = "";
    public string? Description { get; init; }
    public string? PreviousValue { get; init; }
    public string? NewValue { get; init; }
    public string UserName { get; init; } = "";
    public DateTime Timestamp { get; init; }
    public string? Notes { get; init; }
}

public sealed class CapitalDashboardDto
{
    public decimal TotalCapitalBase { get; init; }
    public string BaseCurrency { get; init; } = "USD";
    public int ActivePartnersCount { get; init; }
    public int ActiveParticipationsCount { get; init; }
    public decimal MonthlyDistributedProfit { get; init; }
    public decimal PendingSettlementsBase { get; init; }
    public string LargestInvestorName { get; init; } = "";
    public decimal LargestInvestorBase { get; init; }
    public IReadOnlyList<CapitalScopeBreakdownDto> ScopeBreakdown { get; init; } = [];
    public IReadOnlyList<CapitalCurrencyBreakdownDto> CurrencyBreakdown { get; init; } = [];
    public IReadOnlyList<CapitalMonthlyTrendDto> InvestmentTrend { get; init; } = [];
    public IReadOnlyList<CapitalTopInvestorDto> TopInvestors { get; init; } = [];
    public IReadOnlyList<CapitalPendingDistributionDto> PendingDistributions { get; init; } = [];
}

public sealed class CapitalScopeBreakdownDto
{
    public PartnershipScope Scope { get; init; }
    public string ScopeDisplay { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class CapitalCurrencyBreakdownDto
{
    public string Currency { get; init; } = "";
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
}

public sealed class CapitalMonthlyTrendDto
{
    public int Year { get; init; }
    public int Month { get; init; }
    public string Label { get; init; } = "";
    public decimal AmountBase { get; init; }
}

public sealed class CapitalTopInvestorDto
{
    public Guid PartnerId { get; init; }
    public string PartnerName { get; init; } = "";
    public decimal CapitalBase { get; init; }
}

public sealed class CapitalPendingDistributionDto
{
    public Guid DistributionId { get; init; }
    public string Code { get; init; } = "";
    public DistributionStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public decimal NetAmount { get; init; }
}

public sealed class ProfitDistributionListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public PartnershipScope Scope { get; init; }
    public string ScopeDisplay { get; init; } = "";
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public decimal NetProfit { get; init; }
    public decimal NetLoss { get; init; }
    public DistributionStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class CapitalReportDto
{
    public string ReportType { get; init; } = "";
    public string Title { get; init; } = "";
    public DateTime GeneratedAt { get; init; }
    public IReadOnlyList<CapitalReportRowDto> Rows { get; init; } = [];
    public decimal TotalBase { get; init; }
    public string BaseCurrency { get; init; } = "USD";
}

public sealed class CapitalReportRowDto
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string? SubLabel { get; init; }
    public decimal Amount { get; init; }
    public string? Currency { get; init; }
}
