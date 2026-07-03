using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Capital;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Services;

namespace ERPSystem.Application.Mapping;

public static class CapitalMapper
{
    public static CapitalPartnerListDto ToListDto(CapitalPartnerAggregate aggregate) =>
        ToListDto(aggregate.Partner);

    public static CapitalPartnerListDto ToListDto(CapitalPartner partner) => new()
    {
        Id = partner.Id,
        Code = partner.Code,
        FullName = partner.FullName,
        Status = partner.Status,
        StatusDisplay = partner.Status.ToArabic(),
        RiskLevel = partner.RiskLevel,
        RiskLevelDisplay = partner.RiskLevel.ToArabic(),
        DefaultCurrency = partner.DefaultCurrency,
        CurrentCapitalBase = partner.CurrentCapitalBase,
        TotalInvestmentsBase = partner.TotalInvestmentsBase,
        TotalWithdrawalsBase = partner.TotalWithdrawalsBase,
        ParticipationsCount = partner.Participations.Count(p => p.IsActive),
        CompanyOwnershipPercentage = partner.Participations
            .FirstOrDefault(p => p.Scope == PartnershipScope.Company && p.IsActive)?.OwnershipPercentage,
        Phone = partner.Phone
    };

    public static CapitalPartnerDetailsDto ToDetailsDto(
        CapitalPartner partner,
        DateTime createdAt,
        string? createdByName,
        DateTime? updatedAt) => new()
    {
        Id = partner.Id,
        Code = partner.Code,
        FullName = partner.FullName,
        PhotoPath = partner.PhotoPath,
        NationalId = partner.NationalId,
        Phone = partner.Phone,
        Email = partner.Email,
        Address = partner.Address,
        Notes = partner.Notes,
        DefaultCurrency = partner.DefaultCurrency,
        Status = partner.Status,
        StatusDisplay = partner.Status.ToArabic(),
        RiskLevel = partner.RiskLevel,
        RiskLevelDisplay = partner.RiskLevel.ToArabic(),
        CurrentCapitalBase = partner.CurrentCapitalBase,
        TotalInvestmentsBase = partner.TotalInvestmentsBase,
        TotalWithdrawalsBase = partner.TotalWithdrawalsBase,
        DistributedProfitBase = partner.Transactions
            .Where(t => t.Type == CapitalTransactionType.ProfitDistribution && t.ApprovalStatus == CapitalApprovalStatus.Approved)
            .Sum(t => t.AmountBase),
        CreatedAt = createdAt,
        CreatedByName = createdByName,
        UpdatedAt = updatedAt,
        Participations = partner.Participations.Select(ToParticipationDto).ToList(),
        Transactions = partner.Transactions.OrderByDescending(t => t.TransactionDate).Select(ToTransactionDto).ToList(),
        BankAccounts = partner.BankAccounts.Select(ToBankAccountDto).ToList()
    };

    public static CapitalOperationsCenterDto ToOperationsCenterDto(
        CapitalPartnerWithAudit bundle,
        IReadOnlyList<PartnerTimelineEvent> timeline,
        IReadOnlyList<PartnerAuditEntry> audit) 
    {
        var partner = bundle.Aggregate.Partner;
        var details = ToDetailsDto(partner, bundle.CreatedAt, bundle.CreatedByName, bundle.UpdatedAt);
        var signed = partner.Transactions
            .Where(t => t.ApprovalStatus == CapitalApprovalStatus.Approved)
            .Select(t => t.SignedBaseAmount);

        var distributed = partner.Transactions
            .Where(t => t.Type == CapitalTransactionType.ProfitDistribution && t.ApprovalStatus == CapitalApprovalStatus.Approved)
            .Sum(t => t.AmountBase);

        return new CapitalOperationsCenterDto
        {
            Details = details,
            Financial = new CapitalFinancialSummaryDto
            {
                CurrentCapitalBase = partner.CurrentCapitalBase,
                TotalInvestmentsBase = partner.TotalInvestmentsBase,
                TotalWithdrawalsBase = partner.TotalWithdrawalsBase,
                DistributedProfitBase = distributed,
                UndistributedProfitBase = 0,
                BaseCurrency = partner.DefaultCurrency,
                TransactionCount = partner.Transactions.Count,
                ParticipationCount = partner.Participations.Count(p => p.IsActive)
            },
            ScopeSummaries = BuildScopeSummaries(partner),
            Timeline = timeline.Select(ToTimelineDto).ToList(),
            RecentAudit = audit.Take(20).Select(ToAuditDto).ToList(),
            Statistics = new CapitalPartnerStatisticsDto
            {
                TotalTransactions = partner.Transactions.Count,
                AuditEventCount = audit.Count,
                DaysSinceCreated = (DateTime.UtcNow.Date - bundle.CreatedAt.Date).Days
            }
        };
    }

    public static CapitalDashboardDto ToDashboardDto(CapitalDashboardData data) => new()
    {
        TotalCapitalBase = data.TotalCapitalBase,
        ActivePartnersCount = data.ActivePartnersCount,
        ActiveParticipationsCount = data.ActiveParticipationsCount,
        MonthlyDistributedProfit = data.MonthlyDistributedProfit,
        PendingSettlementsBase = data.PendingSettlementsBase,
        LargestInvestorName = data.LargestInvestorName,
        LargestInvestorBase = data.LargestInvestorBase,
        ScopeBreakdown = data.ScopeBreakdown.Select(s => new CapitalScopeBreakdownDto
        {
            Scope = s.Scope,
            ScopeDisplay = s.Scope.ToArabic(),
            AmountBase = s.AmountBase
        }).ToList(),
        CurrencyBreakdown = data.CurrencyBreakdown.Select(c => new CapitalCurrencyBreakdownDto
        {
            Currency = c.Currency,
            AmountOriginal = c.AmountOriginal,
            AmountBase = c.AmountBase
        }).ToList(),
        InvestmentTrend = data.InvestmentTrend.Select(t => new CapitalMonthlyTrendDto
        {
            Year = t.Year,
            Month = t.Month,
            Label = $"{t.Year}/{t.Month:D2}",
            AmountBase = t.AmountBase
        }).ToList(),
        TopInvestors = data.TopInvestors.Select(t => new CapitalTopInvestorDto
        {
            PartnerId = t.PartnerId,
            PartnerName = t.PartnerName,
            CapitalBase = t.CapitalBase
        }).ToList(),
        PendingDistributions = data.PendingDistributions.Select(p => new CapitalPendingDistributionDto
        {
            DistributionId = p.DistributionId,
            Code = p.Code,
            Status = p.Status,
            StatusDisplay = p.Status.ToArabic(),
            NetAmount = p.NetAmount
        }).ToList()
    };

    public static ProfitDistributionListDto ToDistributionListDto(ProfitDistribution d) => new()
    {
        Id = d.Id,
        Code = d.Code,
        Scope = d.Scope,
        ScopeDisplay = d.Scope.ToArabic(),
        PeriodStart = d.PeriodStart,
        PeriodEnd = d.PeriodEnd,
        NetProfit = d.NetProfit,
        NetLoss = d.NetLoss,
        Status = d.Status,
        StatusDisplay = d.Status.ToArabic()
    };

    private static IReadOnlyList<CapitalScopeSummaryDto> BuildScopeSummaries(CapitalPartner partner) =>
        partner.Participations
            .Where(p => p.IsActive)
            .GroupBy(p => p.Scope)
            .Select(g => new CapitalScopeSummaryDto
            {
                Scope = g.Key,
                ScopeDisplay = g.Key.ToArabic(),
                Count = g.Count(),
                CapitalBase = partner.Transactions
                    .Where(t => t.Scope == g.Key && t.ApprovalStatus == CapitalApprovalStatus.Approved)
                    .Sum(t => t.SignedBaseAmount)
            })
            .ToList();

    private static PartnerParticipationDto ToParticipationDto(PartnerParticipation p) => new()
    {
        Id = p.Id,
        Scope = p.Scope,
        ScopeDisplay = p.Scope.ToArabic(),
        OwnershipPercentage = p.OwnershipPercentage,
        ProjectCode = p.ProjectCode,
        ContainerId = p.ContainerId,
        ContainerNumber = p.ContainerNumber,
        IsActive = p.IsActive,
        EffectiveFrom = p.EffectiveFrom
    };

    private static PartnerBankAccountDto ToBankAccountDto(PartnerBankAccount b) => new()
    {
        Id = b.Id,
        BankName = b.BankName,
        AccountNumber = b.AccountNumber,
        Iban = b.Iban,
        Currency = b.Currency,
        IsDefault = b.IsDefault
    };

    private static CapitalTransactionDto ToTransactionDto(CapitalTransaction t) => new()
    {
        Id = t.Id,
        Type = t.Type,
        TypeDisplay = t.Type.ToArabic(),
        AmountOriginal = t.AmountOriginal,
        Currency = t.Currency,
        ExchangeRate = t.ExchangeRate,
        AmountBase = t.AmountBase,
        SignedBaseAmount = t.SignedBaseAmount,
        TransactionDate = t.TransactionDate,
        Scope = t.Scope,
        ScopeDisplay = t.Scope.ToArabic(),
        ProjectCode = t.ProjectCode,
        ContainerId = t.ContainerId,
        ApprovalStatus = t.ApprovalStatus,
        ApprovalStatusDisplay = t.ApprovalStatus.ToArabic(),
        ReferenceNumber = t.ReferenceNumber,
        Notes = t.Notes
    };

    private static PartnerAuditEntryDto ToAuditDto(PartnerAuditEntry e) => new()
    {
        Id = e.Id,
        Action = e.Action,
        FieldName = e.FieldName,
        PreviousValue = e.PreviousValue,
        NewValue = e.NewValue,
        UserName = e.UserName,
        Timestamp = e.Timestamp,
        Notes = e.Notes
    };

    private static PartnerTimelineEventDto ToTimelineDto(PartnerTimelineEvent e) => new()
    {
        Id = e.Id,
        EventType = e.EventType,
        Title = e.Title,
        Description = e.Description,
        PreviousValue = e.PreviousValue,
        NewValue = e.NewValue,
        UserName = e.UserName,
        Timestamp = e.Timestamp,
        Notes = e.Notes
    };
}
