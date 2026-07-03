using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.Services;

namespace ERPSystem.Domain.Entities.Capital;

public sealed class CapitalPartner
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string FullName { get; private set; } = "";
    public string? PhotoPath { get; private set; }
    public string? NationalId { get; private set; }
    public string? Phone { get; private set; }
    public string? Email { get; private set; }
    public string? Address { get; private set; }
    public string? Notes { get; private set; }
    public string DefaultCurrency { get; private set; } = "USD";
    public PartnerStatus Status { get; private set; }
    public PartnerRiskLevel RiskLevel { get; private set; }

    private List<PartnerParticipation> _participations = [];
    private List<PartnerBankAccount> _bankAccounts = [];
    private List<CapitalTransaction> _transactions = [];

    public IReadOnlyList<PartnerParticipation> Participations => _participations;
    public IReadOnlyList<PartnerBankAccount> BankAccounts => _bankAccounts;
    public IReadOnlyList<CapitalTransaction> Transactions => _transactions;

    private CapitalPartner() { }

    public static CapitalPartner Create(
        Guid companyId,
        string code,
        string fullName,
        string defaultCurrency = "USD",
        PartnerRiskLevel riskLevel = PartnerRiskLevel.Medium)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ValidationException("Partner name is required.");

        return new CapitalPartner
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Code = code.Trim(),
            FullName = fullName.Trim(),
            DefaultCurrency = defaultCurrency,
            RiskLevel = riskLevel,
            Status = PartnerStatus.Active
        };
    }

    public void UpdateProfile(
        string fullName,
        string? nationalId,
        string? phone,
        string? email,
        string? address,
        string? notes,
        string defaultCurrency,
        PartnerRiskLevel riskLevel,
        string? photoPath = null)
    {
        if (Status == PartnerStatus.Archived)
            throw new DomainException("Archived partners cannot be edited.");

        FullName = fullName.Trim();
        NationalId = nationalId;
        Phone = phone;
        Email = email;
        Address = address;
        Notes = notes;
        DefaultCurrency = defaultCurrency;
        RiskLevel = riskLevel;
        if (photoPath is not null)
            PhotoPath = photoPath;
    }

    public PartnerParticipation AddParticipation(
        PartnershipScope scope,
        decimal ownershipPercentage,
        string? projectCode = null,
        Guid? containerId = null,
        string? containerNumber = null)
    {
        if (ownershipPercentage <= 0 || ownershipPercentage > 100)
            throw new ValidationException("Ownership percentage must be between 0 and 100.");

        ValidateScopeReferences(scope, projectCode, containerId);

        var participation = PartnerParticipation.Create(
            Id, scope, ownershipPercentage, projectCode, containerId, containerNumber);
        _participations.Add(participation);
        return participation;
    }

    public PartnerParticipation SetCompanyOwnership(decimal ownershipPercentage)
    {
        if (ownershipPercentage <= 0)
            throw new ValidationException("Ownership percentage is required.");

        var existing = _participations.FirstOrDefault(p =>
            p.Scope == PartnershipScope.Company && p.IsActive);

        if (existing is null)
            return AddParticipation(PartnershipScope.Company, ownershipPercentage);

        existing.UpdateOwnershipPercentage(ownershipPercentage);
        return existing;
    }

    public CapitalTransaction RecordTransaction(
        CapitalTransactionType type,
        decimal amountOriginal,
        string currency,
        decimal exchangeRate,
        string baseCurrency,
        DateTime transactionDate,
        PartnershipScope scope,
        Guid? participationId = null,
        Guid? projectId = null,
        string? projectCode = null,
        Guid? containerId = null,
        string? referenceNumber = null,
        string? notes = null)
    {
        if (amountOriginal <= 0 && type is not CapitalTransactionType.ManualAdjustment and not CapitalTransactionType.CurrencyAdjustment)
            throw new ValidationException("Amount must be greater than zero.");

        var baseAmount = Math.Round(amountOriginal * exchangeRate, 4);
        var transaction = CapitalTransaction.Create(
            Id, participationId, type, amountOriginal, currency, exchangeRate,
            baseCurrency, baseAmount, transactionDate, scope, projectId, projectCode,
            containerId, referenceNumber, notes);
        _transactions.Add(transaction);
        return transaction;
    }

    public void Archive()
    {
        if (Status == PartnerStatus.Archived)
            return;
        Status = PartnerStatus.Archived;
    }

    public decimal CurrentCapitalBase =>
        _transactions.Where(t => t.ApprovalStatus == CapitalApprovalStatus.Approved)
            .Sum(t => t.SignedBaseAmount);

    public decimal TotalInvestmentsBase =>
        _transactions.Where(t => t.ApprovalStatus == CapitalApprovalStatus.Approved && t.SignedBaseAmount > 0)
            .Sum(t => t.SignedBaseAmount);

    public decimal TotalWithdrawalsBase =>
        _transactions.Where(t => t.ApprovalStatus == CapitalApprovalStatus.Approved && t.SignedBaseAmount < 0)
            .Sum(t => Math.Abs(t.SignedBaseAmount));

    private static void ValidateScopeReferences(PartnershipScope scope, string? projectCode, Guid? containerId)
    {
        switch (scope)
        {
            case PartnershipScope.Project when string.IsNullOrWhiteSpace(projectCode):
                throw new ValidationException("Project code is required for project partnership.");
            case PartnershipScope.Container when containerId is null:
                throw new ValidationException("Container is required for container partnership.");
        }
    }

    public static CapitalPartner Rehydrate(
        Guid id,
        Guid companyId,
        string code,
        string fullName,
        string? photoPath,
        string? nationalId,
        string? phone,
        string? email,
        string? address,
        string? notes,
        string defaultCurrency,
        PartnerStatus status,
        PartnerRiskLevel riskLevel,
        IEnumerable<PartnerParticipation> participations,
        IEnumerable<PartnerBankAccount> bankAccounts,
        IEnumerable<CapitalTransaction> transactions) => new()
    {
        Id = id,
        CompanyId = companyId,
        Code = code,
        FullName = fullName,
        PhotoPath = photoPath,
        NationalId = nationalId,
        Phone = phone,
        Email = email,
        Address = address,
        Notes = notes,
        DefaultCurrency = defaultCurrency,
        Status = status,
        RiskLevel = riskLevel,
        _participations = participations.ToList(),
        _bankAccounts = bankAccounts.ToList(),
        _transactions = transactions.ToList()
    };
}

public sealed class PartnerParticipation
{
    public Guid Id { get; private set; }
    public Guid PartnerId { get; private set; }
    public PartnershipScope Scope { get; private set; }
    public decimal OwnershipPercentage { get; private set; }
    public string? ProjectCode { get; private set; }
    public Guid? ContainerId { get; private set; }
    public string? ContainerNumber { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime EffectiveFrom { get; private set; }

    private PartnerParticipation() { }

    public static PartnerParticipation Create(
        Guid partnerId,
        PartnershipScope scope,
        decimal ownershipPercentage,
        string? projectCode,
        Guid? containerId,
        string? containerNumber) => new()
    {
        Id = Guid.NewGuid(),
        PartnerId = partnerId,
        Scope = scope,
        OwnershipPercentage = ownershipPercentage,
        ProjectCode = projectCode,
        ContainerId = containerId,
        ContainerNumber = containerNumber,
        EffectiveFrom = DateTime.UtcNow.Date
    };

    public static PartnerParticipation Rehydrate(
        Guid id,
        Guid partnerId,
        PartnershipScope scope,
        decimal ownershipPercentage,
        string? projectCode,
        Guid? containerId,
        string? containerNumber,
        bool isActive,
        DateTime effectiveFrom) => new()
    {
        Id = id,
        PartnerId = partnerId,
        Scope = scope,
        OwnershipPercentage = ownershipPercentage,
        ProjectCode = projectCode,
        ContainerId = containerId,
        ContainerNumber = containerNumber,
        IsActive = isActive,
        EffectiveFrom = effectiveFrom
    };

    public void UpdateOwnershipPercentage(decimal ownershipPercentage)
    {
        if (ownershipPercentage <= 0 || ownershipPercentage > 100)
            throw new ValidationException("Ownership percentage must be between 0 and 100.");
        OwnershipPercentage = ownershipPercentage;
    }
}

public sealed class PartnerBankAccount
{
    public Guid Id { get; private set; }
    public Guid PartnerId { get; private set; }
    public string BankName { get; private set; } = "";
    public string AccountNumber { get; private set; } = "";
    public string? Iban { get; private set; }
    public string Currency { get; private set; } = "USD";
    public bool IsDefault { get; private set; }

    private PartnerBankAccount() { }

    public static PartnerBankAccount Create(
        Guid partnerId,
        string bankName,
        string accountNumber,
        string currency,
        string? iban = null,
        bool isDefault = false) => new()
    {
        Id = Guid.NewGuid(),
        PartnerId = partnerId,
        BankName = bankName.Trim(),
        AccountNumber = accountNumber.Trim(),
        Iban = iban,
        Currency = currency,
        IsDefault = isDefault
    };

    public static PartnerBankAccount Rehydrate(
        Guid id,
        Guid partnerId,
        string bankName,
        string accountNumber,
        string? iban,
        string currency,
        bool isDefault) => new()
    {
        Id = id,
        PartnerId = partnerId,
        BankName = bankName,
        AccountNumber = accountNumber,
        Iban = iban,
        Currency = currency,
        IsDefault = isDefault
    };
}

public sealed class CapitalTransaction
{
    public Guid Id { get; private set; }
    public Guid PartnerId { get; private set; }
    public Guid? ParticipationId { get; private set; }
    public CapitalTransactionType Type { get; private set; }
    public decimal AmountOriginal { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal ExchangeRate { get; private set; } = 1m;
    public string BaseCurrency { get; private set; } = "USD";
    public decimal AmountBase { get; private set; }
    public DateTime TransactionDate { get; private set; }
    public PartnershipScope Scope { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string? ProjectCode { get; private set; }
    public Guid? ContainerId { get; private set; }
    public CapitalApprovalStatus ApprovalStatus { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public Guid? ProfitDistributionId { get; private set; }

    public decimal SignedBaseAmount => IsOutflow ? -AmountBase : AmountBase;

    private bool IsOutflow => Type is CapitalTransactionType.PartialWithdrawal
        or CapitalTransactionType.FullWithdrawal
        or CapitalTransactionType.LossDistribution;

    private CapitalTransaction() { }

    public static CapitalTransaction Create(
        Guid partnerId,
        Guid? participationId,
        CapitalTransactionType type,
        decimal amountOriginal,
        string currency,
        decimal exchangeRate,
        string baseCurrency,
        decimal amountBase,
        DateTime transactionDate,
        PartnershipScope scope,
        Guid? projectId,
        string? projectCode,
        Guid? containerId,
        string? referenceNumber,
        string? notes) => new()
    {
        Id = Guid.NewGuid(),
        PartnerId = partnerId,
        ParticipationId = participationId,
        Type = type,
        AmountOriginal = amountOriginal,
        Currency = currency,
        ExchangeRate = exchangeRate,
        BaseCurrency = baseCurrency,
        AmountBase = amountBase,
        TransactionDate = transactionDate.Date,
        Scope = scope,
        ProjectId = projectId,
        ProjectCode = projectCode,
        ContainerId = containerId,
        ReferenceNumber = referenceNumber,
        Notes = notes,
        ApprovalStatus = CapitalApprovalStatus.Approved
    };

    public void Approve() => ApprovalStatus = CapitalApprovalStatus.Approved;
    public void Reject() => ApprovalStatus = CapitalApprovalStatus.Rejected;

    public void LinkDistribution(Guid distributionId) => ProfitDistributionId = distributionId;

    public static CapitalTransaction Rehydrate(
        Guid id,
        Guid partnerId,
        Guid? participationId,
        CapitalTransactionType type,
        decimal amountOriginal,
        string currency,
        decimal exchangeRate,
        string baseCurrency,
        decimal amountBase,
        DateTime transactionDate,
        PartnershipScope scope,
        Guid? projectId,
        string? projectCode,
        Guid? containerId,
        CapitalApprovalStatus approvalStatus,
        string? referenceNumber,
        string? notes,
        Guid? profitDistributionId) => new()
    {
        Id = id,
        PartnerId = partnerId,
        ParticipationId = participationId,
        Type = type,
        AmountOriginal = amountOriginal,
        Currency = currency,
        ExchangeRate = exchangeRate,
        BaseCurrency = baseCurrency,
        AmountBase = amountBase,
        TransactionDate = transactionDate,
        Scope = scope,
        ProjectId = projectId,
        ProjectCode = projectCode,
        ContainerId = containerId,
        ApprovalStatus = approvalStatus,
        ReferenceNumber = referenceNumber,
        Notes = notes,
        ProfitDistributionId = profitDistributionId
    };
}

public sealed class ProfitDistribution
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public PartnershipScope Scope { get; private set; }
    public string? ProjectCode { get; private set; }
    public Guid? ContainerId { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public decimal GrossRevenue { get; private set; }
    public decimal TotalCosts { get; private set; }
    public decimal NetProfit { get; private set; }
    public decimal NetLoss { get; private set; }
    public string BaseCurrency { get; private set; } = "USD";
    public DistributionStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private List<ProfitDistributionLine> _lines = [];
    public IReadOnlyList<ProfitDistributionLine> Lines => _lines;

    private ProfitDistribution() { }

    public static ProfitDistribution CreateDraft(
        Guid companyId,
        string code,
        PartnershipScope scope,
        DateTime periodStart,
        DateTime periodEnd,
        string? projectCode = null,
        Guid? containerId = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Code = code,
        Scope = scope,
        ProjectCode = projectCode,
        ContainerId = containerId,
        PeriodStart = periodStart.Date,
        PeriodEnd = periodEnd.Date,
        Status = DistributionStatus.Draft
    };

    public void Calculate(decimal grossRevenue, decimal totalCosts)
    {
        if (Status is not (DistributionStatus.Draft or DistributionStatus.Calculated))
            throw new DomainException("Distribution cannot be recalculated in current status.");

        GrossRevenue = grossRevenue;
        TotalCosts = totalCosts;
        var net = grossRevenue - totalCosts;
        if (net >= 0)
        {
            NetProfit = net;
            NetLoss = 0;
        }
        else
        {
            NetLoss = Math.Abs(net);
            NetProfit = 0;
        }
        Status = DistributionStatus.Calculated;
    }

    public void SetLines(IEnumerable<ProfitDistributionLine> lines)
    {
        _lines = lines.ToList();
    }

    public void SubmitForApproval()
    {
        DistributionLifecycle.ValidateTransition(Status, DistributionStatus.PendingApproval);
        Status = DistributionStatus.PendingApproval;
    }

    public void Approve()
    {
        DistributionLifecycle.ValidateTransition(Status, DistributionStatus.Approved);
        Status = DistributionStatus.Approved;
    }

    public void Post()
    {
        DistributionLifecycle.ValidateTransition(Status, DistributionStatus.Posted);
        Status = DistributionStatus.Posted;
    }

    public void Close()
    {
        DistributionLifecycle.ValidateTransition(Status, DistributionStatus.Closed);
        Status = DistributionStatus.Closed;
    }

    public static ProfitDistribution Rehydrate(
        Guid id,
        Guid companyId,
        string code,
        PartnershipScope scope,
        string? projectCode,
        Guid? containerId,
        DateTime periodStart,
        DateTime periodEnd,
        decimal grossRevenue,
        decimal totalCosts,
        decimal netProfit,
        decimal netLoss,
        string baseCurrency,
        DistributionStatus status,
        string? notes,
        IEnumerable<ProfitDistributionLine> lines) => new()
    {
        Id = id,
        CompanyId = companyId,
        Code = code,
        Scope = scope,
        ProjectCode = projectCode,
        ContainerId = containerId,
        PeriodStart = periodStart,
        PeriodEnd = periodEnd,
        GrossRevenue = grossRevenue,
        TotalCosts = totalCosts,
        NetProfit = netProfit,
        NetLoss = netLoss,
        BaseCurrency = baseCurrency,
        Status = status,
        Notes = notes,
        _lines = lines.ToList()
    };
}

public sealed class ProfitDistributionLine
{
    public Guid Id { get; private set; }
    public Guid DistributionId { get; private set; }
    public Guid PartnerId { get; private set; }
    public decimal OwnershipPercentage { get; private set; }
    public decimal PartnerShare { get; private set; }
    public decimal CompanyShare { get; private set; }

    private ProfitDistributionLine() { }

    public static ProfitDistributionLine Create(
        Guid distributionId,
        Guid partnerId,
        decimal ownershipPercentage,
        decimal partnerShare,
        decimal companyShare) => new()
    {
        Id = Guid.NewGuid(),
        DistributionId = distributionId,
        PartnerId = partnerId,
        OwnershipPercentage = ownershipPercentage,
        PartnerShare = partnerShare,
        CompanyShare = companyShare
    };

    public static ProfitDistributionLine Rehydrate(
        Guid id,
        Guid distributionId,
        Guid partnerId,
        decimal ownershipPercentage,
        decimal partnerShare,
        decimal companyShare) => new()
    {
        Id = id,
        DistributionId = distributionId,
        PartnerId = partnerId,
        OwnershipPercentage = ownershipPercentage,
        PartnerShare = partnerShare,
        CompanyShare = companyShare
    };
}

public sealed class PartnerAuditEntry
{
    public Guid Id { get; private set; }
    public Guid PartnerId { get; private set; }
    public string Action { get; private set; } = "";
    public string? FieldName { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = "";
    public DateTime Timestamp { get; private set; }
    public string? Notes { get; private set; }

    private PartnerAuditEntry() { }

    public static PartnerAuditEntry Record(
        Guid partnerId,
        string action,
        Guid userId,
        string userName,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        PartnerId = partnerId,
        Action = action,
        FieldName = fieldName,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = DateTime.UtcNow,
        Notes = notes
    };

    public static PartnerAuditEntry Rehydrate(
        Guid id,
        Guid partnerId,
        string action,
        Guid userId,
        string userName,
        DateTime timestamp,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null) => new()
    {
        Id = id,
        PartnerId = partnerId,
        Action = action,
        FieldName = fieldName,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = timestamp,
        Notes = notes
    };
}

public sealed class PartnerTimelineEvent
{
    public Guid Id { get; private set; }
    public Guid PartnerId { get; private set; }
    public string EventType { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = "";
    public DateTime Timestamp { get; private set; }
    public string? Notes { get; private set; }

    private PartnerTimelineEvent() { }

    public static PartnerTimelineEvent Record(
        Guid partnerId,
        string eventType,
        string title,
        Guid userId,
        string userName,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null) => new()
    {
        Id = Guid.NewGuid(),
        PartnerId = partnerId,
        EventType = eventType,
        Title = title,
        Description = description,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = DateTime.UtcNow,
        Notes = notes
    };

    public static PartnerTimelineEvent Rehydrate(
        Guid id,
        Guid partnerId,
        string eventType,
        string title,
        Guid userId,
        string userName,
        DateTime timestamp,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? notes = null) => new()
    {
        Id = id,
        PartnerId = partnerId,
        EventType = eventType,
        Title = title,
        Description = description,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = timestamp,
        Notes = notes
    };
}
