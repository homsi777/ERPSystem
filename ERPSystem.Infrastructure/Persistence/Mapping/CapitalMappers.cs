using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Capital;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence.Models.Capital;

namespace ERPSystem.Infrastructure.Persistence.Mapping;

internal static class CapitalMapper
{
    public static CapitalPartnerAggregate ToAggregate(CapitalPartnerEntity entity)
    {
        var participations = entity.Participations.Select(ToParticipationDomain).ToList();
        var bankAccounts = entity.BankAccounts.Select(ToBankAccountDomain).ToList();
        var transactions = entity.Transactions.Select(ToTransactionDomain).ToList();

        var partner = CapitalPartner.Rehydrate(
            entity.Id,
            entity.CompanyId,
            entity.Code,
            entity.FullName,
            entity.PhotoPath,
            entity.NationalId,
            entity.Phone,
            entity.Email,
            entity.Address,
            entity.Notes,
            entity.DefaultCurrency,
            (PartnerStatus)entity.Status,
            (PartnerRiskLevel)entity.RiskLevel,
            participations,
            bankAccounts,
            transactions);

        return CapitalPartnerAggregate.FromPartner(partner);
    }

    public static CapitalPartnerEntity ToEntity(CapitalPartnerAggregate aggregate)
    {
        var p = aggregate.Partner;
        return new CapitalPartnerEntity
        {
            Id = p.Id,
            CompanyId = p.CompanyId,
            Code = p.Code,
            FullName = p.FullName,
            PhotoPath = p.PhotoPath,
            NationalId = p.NationalId,
            Phone = p.Phone,
            Email = p.Email,
            Address = p.Address,
            Notes = p.Notes,
            DefaultCurrency = p.DefaultCurrency,
            Status = (int)p.Status,
            RiskLevel = (int)p.RiskLevel,
            IsActive = p.Status == PartnerStatus.Active,
            IsArchived = p.Status == PartnerStatus.Archived,
            Participations = p.Participations.Select(x => ToParticipationEntity(x, p.Id)).ToList(),
            BankAccounts = p.BankAccounts.Select(x => ToBankAccountEntity(x, p.Id)).ToList(),
            Transactions = p.Transactions.Select(x => ToTransactionEntity(x, p.Id)).ToList()
        };
    }

    public static void UpdateEntity(CapitalPartnerEntity entity, CapitalPartnerAggregate aggregate)
    {
        var p = aggregate.Partner;
        entity.FullName = p.FullName;
        entity.PhotoPath = p.PhotoPath;
        entity.NationalId = p.NationalId;
        entity.Phone = p.Phone;
        entity.Email = p.Email;
        entity.Address = p.Address;
        entity.Notes = p.Notes;
        entity.DefaultCurrency = p.DefaultCurrency;
        entity.Status = (int)p.Status;
        entity.RiskLevel = (int)p.RiskLevel;
        entity.IsActive = p.Status == PartnerStatus.Active;
        entity.IsArchived = p.Status == PartnerStatus.Archived;

        SyncParticipations(entity, p);
        SyncBankAccounts(entity, p);
        SyncTransactions(entity, p);
    }

    private static void SyncParticipations(CapitalPartnerEntity entity, CapitalPartner partner)
    {
        var incoming = partner.Participations.ToDictionary(x => x.Id);
        foreach (var existing in entity.Participations.ToList())
        {
            if (!incoming.ContainsKey(existing.Id))
                entity.Participations.Remove(existing);
        }

        foreach (var part in partner.Participations)
        {
            var row = entity.Participations.FirstOrDefault(x => x.Id == part.Id);
            if (row is null)
            {
                entity.Participations.Add(ToParticipationEntity(part, partner.Id));
                continue;
            }

            row.Scope = (int)part.Scope;
            row.OwnershipPercentage = part.OwnershipPercentage;
            row.ProjectCode = part.ProjectCode;
            row.ContainerId = part.ContainerId;
            row.ContainerNumber = part.ContainerNumber;
            row.IsActive = part.IsActive;
            row.EffectiveFrom = part.EffectiveFrom;
        }
    }

    private static void SyncBankAccounts(CapitalPartnerEntity entity, CapitalPartner partner)
    {
        var incoming = partner.BankAccounts.ToDictionary(x => x.Id);
        foreach (var existing in entity.BankAccounts.ToList())
        {
            if (!incoming.ContainsKey(existing.Id))
                entity.BankAccounts.Remove(existing);
        }

        foreach (var account in partner.BankAccounts)
        {
            var row = entity.BankAccounts.FirstOrDefault(x => x.Id == account.Id);
            if (row is null)
            {
                entity.BankAccounts.Add(ToBankAccountEntity(account, partner.Id));
                continue;
            }

            row.BankName = account.BankName;
            row.AccountNumber = account.AccountNumber;
            row.Iban = account.Iban;
            row.Currency = account.Currency;
            row.IsDefault = account.IsDefault;
        }
    }

    private static void SyncTransactions(CapitalPartnerEntity entity, CapitalPartner partner)
    {
        var incoming = partner.Transactions.ToDictionary(x => x.Id);
        foreach (var existing in entity.Transactions.ToList())
        {
            if (!incoming.ContainsKey(existing.Id))
                entity.Transactions.Remove(existing);
        }

        foreach (var tx in partner.Transactions)
        {
            var row = entity.Transactions.FirstOrDefault(x => x.Id == tx.Id);
            if (row is null)
            {
                entity.Transactions.Add(ToTransactionEntity(tx, partner.Id));
                continue;
            }

            row.ParticipationId = tx.ParticipationId;
            row.Type = (int)tx.Type;
            row.AmountOriginal = tx.AmountOriginal;
            row.Currency = tx.Currency;
            row.ExchangeRate = tx.ExchangeRate;
            row.BaseCurrency = tx.BaseCurrency;
            row.AmountBase = tx.AmountBase;
            row.TransactionDate = tx.TransactionDate;
            row.Scope = (int)tx.Scope;
            row.ProjectId = tx.ProjectId;
            row.ProjectCode = tx.ProjectCode;
            row.ContainerId = tx.ContainerId;
            row.ApprovalStatus = (int)tx.ApprovalStatus;
            row.ReferenceNumber = tx.ReferenceNumber;
            row.Notes = tx.Notes;
            row.ProfitDistributionId = tx.ProfitDistributionId;
        }
    }

    public static ProfitDistribution ToDistributionDomain(ProfitDistributionEntity entity)
    {
        var lines = entity.Lines.Select(ToDistributionLineDomain).ToList();
        return ProfitDistribution.Rehydrate(
            entity.Id,
            entity.CompanyId,
            entity.Code,
            (PartnershipScope)entity.Scope,
            entity.ProjectCode,
            entity.ContainerId,
            entity.PeriodStart,
            entity.PeriodEnd,
            entity.GrossRevenue,
            entity.TotalCosts,
            entity.NetProfit,
            entity.NetLoss,
            entity.BaseCurrency,
            (DistributionStatus)entity.Status,
            entity.Notes,
            lines);
    }

    public static ProfitDistributionEntity ToDistributionEntity(ProfitDistribution distribution) => new()
    {
        Id = distribution.Id,
        CompanyId = distribution.CompanyId,
        Code = distribution.Code,
        Scope = (int)distribution.Scope,
        ProjectCode = distribution.ProjectCode,
        ContainerId = distribution.ContainerId,
        PeriodStart = distribution.PeriodStart,
        PeriodEnd = distribution.PeriodEnd,
        GrossRevenue = distribution.GrossRevenue,
        TotalCosts = distribution.TotalCosts,
        NetProfit = distribution.NetProfit,
        NetLoss = distribution.NetLoss,
        BaseCurrency = distribution.BaseCurrency,
        Status = (int)distribution.Status,
        Notes = distribution.Notes,
        Lines = distribution.Lines.Select(l => ToDistributionLineEntity(l, distribution.Id)).ToList()
    };

    public static void UpdateDistributionEntity(ProfitDistributionEntity entity, ProfitDistribution distribution)
    {
        entity.GrossRevenue = distribution.GrossRevenue;
        entity.TotalCosts = distribution.TotalCosts;
        entity.NetProfit = distribution.NetProfit;
        entity.NetLoss = distribution.NetLoss;
        entity.Status = (int)distribution.Status;
        entity.Notes = distribution.Notes;
        entity.Lines.Clear();
        foreach (var l in distribution.Lines)
            entity.Lines.Add(ToDistributionLineEntity(l, distribution.Id));
    }

    public static PartnerAuditLogEntity ToAuditEntity(PartnerAuditEntry entry) => new()
    {
        Id = entry.Id,
        PartnerId = entry.PartnerId,
        Action = entry.Action,
        FieldName = entry.FieldName,
        PreviousValue = entry.PreviousValue,
        NewValue = entry.NewValue,
        UserId = entry.UserId,
        UserName = entry.UserName,
        Timestamp = entry.Timestamp,
        Notes = entry.Notes
    };

    public static PartnerAuditEntry ToAuditDomain(PartnerAuditLogEntity entity) =>
        PartnerAuditEntry.Rehydrate(
            entity.Id, entity.PartnerId, entity.Action, entity.UserId, entity.UserName,
            entity.Timestamp, entity.FieldName, entity.PreviousValue, entity.NewValue, entity.Notes);

    public static PartnerTimelineEventEntity ToTimelineEntity(PartnerTimelineEvent entry) => new()
    {
        Id = entry.Id,
        PartnerId = entry.PartnerId,
        EventType = entry.EventType,
        Title = entry.Title,
        Description = entry.Description,
        PreviousValue = entry.PreviousValue,
        NewValue = entry.NewValue,
        UserId = entry.UserId,
        UserName = entry.UserName,
        Timestamp = entry.Timestamp,
        Notes = entry.Notes
    };

    public static PartnerTimelineEvent ToTimelineDomain(PartnerTimelineEventEntity entity) =>
        PartnerTimelineEvent.Rehydrate(
            entity.Id, entity.PartnerId, entity.EventType, entity.Title, entity.UserId, entity.UserName,
            entity.Timestamp, entity.Description, entity.PreviousValue, entity.NewValue, entity.Notes);

    private static PartnerParticipation ToParticipationDomain(PartnerParticipationEntity e) =>
        PartnerParticipation.Rehydrate(
            e.Id, e.PartnerId, (PartnershipScope)e.Scope, e.OwnershipPercentage,
            e.ProjectCode, e.ContainerId, e.ContainerNumber, e.IsActive, e.EffectiveFrom);

    private static PartnerParticipationEntity ToParticipationEntity(PartnerParticipation p, Guid partnerId) => new()
    {
        Id = p.Id,
        PartnerId = partnerId,
        Scope = (int)p.Scope,
        OwnershipPercentage = p.OwnershipPercentage,
        ProjectCode = p.ProjectCode,
        ContainerId = p.ContainerId,
        ContainerNumber = p.ContainerNumber,
        IsActive = p.IsActive,
        EffectiveFrom = p.EffectiveFrom
    };

    private static PartnerBankAccount ToBankAccountDomain(PartnerBankAccountEntity e) =>
        PartnerBankAccount.Rehydrate(e.Id, e.PartnerId, e.BankName, e.AccountNumber, e.Iban, e.Currency, e.IsDefault);

    private static PartnerBankAccountEntity ToBankAccountEntity(PartnerBankAccount b, Guid partnerId) => new()
    {
        Id = b.Id,
        PartnerId = partnerId,
        BankName = b.BankName,
        AccountNumber = b.AccountNumber,
        Iban = b.Iban,
        Currency = b.Currency,
        IsDefault = b.IsDefault
    };

    private static CapitalTransaction ToTransactionDomain(CapitalTransactionEntity e) =>
        CapitalTransaction.Rehydrate(
            e.Id, e.PartnerId, e.ParticipationId, (CapitalTransactionType)e.Type,
            e.AmountOriginal, e.Currency, e.ExchangeRate, e.BaseCurrency, e.AmountBase,
            e.TransactionDate, (PartnershipScope)e.Scope, e.ProjectId, e.ProjectCode,
            e.ContainerId, (CapitalApprovalStatus)e.ApprovalStatus, e.ReferenceNumber, e.Notes, e.ProfitDistributionId);

    private static CapitalTransactionEntity ToTransactionEntity(CapitalTransaction t, Guid partnerId) => new()
    {
        Id = t.Id,
        PartnerId = partnerId,
        ParticipationId = t.ParticipationId,
        Type = (int)t.Type,
        AmountOriginal = t.AmountOriginal,
        Currency = t.Currency,
        ExchangeRate = t.ExchangeRate,
        BaseCurrency = t.BaseCurrency,
        AmountBase = t.AmountBase,
        TransactionDate = t.TransactionDate,
        Scope = (int)t.Scope,
        ProjectId = t.ProjectId,
        ProjectCode = t.ProjectCode,
        ContainerId = t.ContainerId,
        ApprovalStatus = (int)t.ApprovalStatus,
        ReferenceNumber = t.ReferenceNumber,
        Notes = t.Notes,
        ProfitDistributionId = t.ProfitDistributionId
    };

    private static ProfitDistributionLine ToDistributionLineDomain(ProfitDistributionLineEntity e) =>
        ProfitDistributionLine.Rehydrate(e.Id, e.DistributionId, e.PartnerId, e.OwnershipPercentage, e.PartnerShare, e.CompanyShare);

    private static ProfitDistributionLineEntity ToDistributionLineEntity(ProfitDistributionLine l, Guid distributionId) => new()
    {
        Id = l.Id,
        DistributionId = distributionId,
        PartnerId = l.PartnerId,
        OwnershipPercentage = l.OwnershipPercentage,
        PartnerShare = l.PartnerShare,
        CompanyShare = l.CompanyShare
    };
}
