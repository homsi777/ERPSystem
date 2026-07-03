using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence.Models.Expenses;
using ERPSystem.Infrastructure.Persistence.Models.Finance;

namespace ERPSystem.Infrastructure.Persistence.Mapping;

internal static class ExpenseMapper
{
    public static CostCenter ToDomain(CostCenterEntity entity) =>
        CostCenter.Rehydrate(
            entity.Id,
            entity.CompanyId,
            entity.Code,
            entity.Name,
            entity.Description,
            entity.ParentCostCenterId,
            (CostCenterStatus)entity.Status,
            entity.CreatedAt);

    public static CostCenterEntity ToEntity(CostCenter costCenter) => new()
    {
        Id = costCenter.Id,
        CompanyId = costCenter.CompanyId,
        Code = costCenter.Code,
        Name = costCenter.Name,
        Description = costCenter.Description,
        ParentCostCenterId = costCenter.ParentCostCenterId,
        Status = (int)costCenter.Status,
        CreatedAt = costCenter.CreatedAt,
        IsActive = costCenter.Status == CostCenterStatus.Active
    };

    public static ExpenseCategory ToDomain(ExpenseCategoryEntity entity) =>
        ExpenseCategory.Rehydrate(
            entity.Id,
            entity.CompanyId,
            (ExpenseCategoryKind)entity.Kind,
            entity.Code,
            entity.NameAr,
            entity.NameEn,
            entity.Description,
            entity.IsSystem,
            entity.IsActive);

    public static ExpenseCategoryEntity ToCategoryEntity(ExpenseCategory category) => new()
    {
        Id = category.Id,
        CompanyId = category.CompanyId,
        Kind = (int)category.Kind,
        Code = category.Code,
        NameAr = category.NameAr,
        NameEn = category.NameEn,
        Description = category.Description,
        IsSystem = category.IsSystem,
        IsActive = category.IsActive
    };

    public static ExpenseAggregate ToAggregate(ExpenseEntity entity)
    {
        var payments = entity.Payments.Select(ToPaymentDomain).ToList();
        var attachments = entity.Attachments.Select(a => ExpenseAttachment.Rehydrate(
            a.Id, a.ExpenseId, a.FileName, a.StoragePath, a.ContentType, a.SizeBytes)).ToList();
        var installments = entity.Installments.Select(ToInstallmentDomain).ToList();

        var expense = Expense.Rehydrate(
            entity.Id,
            entity.CompanyId,
            entity.BranchId,
            entity.Code,
            entity.Name,
            entity.CategoryId,
            (ExpenseCategoryKind)entity.CategoryKind,
            entity.Description,
            (ExpenseStatus)entity.Status,
            entity.StartDate,
            entity.EndDate,
            entity.OriginalCurrency,
            entity.OriginalAmount,
            entity.ExchangeRate,
            entity.BaseCurrency,
            entity.BaseAmount,
            (ExpensePaymentMethod)entity.PaymentMethod,
            entity.PayeeName,
            entity.SupplierId,
            entity.CostCenterId,
            entity.Department,
            entity.ProjectCode,
            entity.Notes,
            entity.IsRecurring,
            (ExpenseRecurrenceFrequency)entity.RecurrenceFrequency,
            entity.CustomIntervalDays,
            entity.NextDueDate,
            entity.RemainingInstallments,
            entity.IntegrationReferenceType,
            entity.IntegrationReferenceId,
            entity.IsActive,
            entity.IsArchived,
            payments,
            attachments,
            installments);

        return ExpenseAggregate.FromExpense(expense);
    }

    public static ExpenseEntity ToEntity(ExpenseAggregate aggregate)
    {
        var e = aggregate.Expense;
        return new ExpenseEntity
        {
            Id = e.Id,
            CompanyId = e.CompanyId,
            BranchId = e.BranchId,
            Code = e.Code,
            Name = e.Name,
            CategoryId = e.CategoryId,
            CategoryKind = (int)e.CategoryKind,
            Description = e.Description,
            Status = (int)e.Status,
            StartDate = e.StartDate,
            EndDate = e.EndDate,
            OriginalCurrency = e.OriginalCurrency,
            OriginalAmount = e.OriginalAmount,
            ExchangeRate = e.ExchangeRate,
            BaseCurrency = e.BaseCurrency,
            BaseAmount = e.BaseAmount,
            PaymentMethod = (int)e.PaymentMethod,
            PayeeName = e.PayeeName,
            SupplierId = e.SupplierId,
            CostCenterId = e.CostCenterId,
            Department = e.Department,
            ProjectCode = e.ProjectCode,
            Notes = e.Notes,
            IsRecurring = e.IsRecurring,
            RecurrenceFrequency = (int)e.RecurrenceFrequency,
            CustomIntervalDays = e.CustomIntervalDays,
            NextDueDate = e.NextDueDate,
            RemainingInstallments = e.RemainingInstallments,
            IntegrationReferenceType = e.IntegrationReferenceType,
            IntegrationReferenceId = e.IntegrationReferenceId,
            IsActive = e.IsActive,
            IsArchived = e.IsArchived,
            Payments = e.Payments.Select(p => ToPaymentEntity(p, e.Id)).ToList(),
            Attachments = e.Attachments.Select(a => new ExpenseAttachmentEntity
            {
                Id = a.Id,
                ExpenseId = e.Id,
                FileName = a.FileName,
                StoragePath = a.StoragePath,
                ContentType = a.ContentType,
                SizeBytes = a.SizeBytes
            }).ToList(),
            Installments = e.Installments.Select(i => ToInstallmentEntity(i, e.Id)).ToList()
        };
    }

    public static void UpdateEntity(ExpenseEntity entity, ExpenseAggregate aggregate)
    {
        var e = aggregate.Expense;
        entity.Name = e.Name;
        entity.CategoryId = e.CategoryId;
        entity.CategoryKind = (int)e.CategoryKind;
        entity.Description = e.Description;
        entity.Status = (int)e.Status;
        entity.StartDate = e.StartDate;
        entity.EndDate = e.EndDate;
        entity.OriginalCurrency = e.OriginalCurrency;
        entity.OriginalAmount = e.OriginalAmount;
        entity.ExchangeRate = e.ExchangeRate;
        entity.BaseCurrency = e.BaseCurrency;
        entity.BaseAmount = e.BaseAmount;
        entity.PaymentMethod = (int)e.PaymentMethod;
        entity.PayeeName = e.PayeeName;
        entity.SupplierId = e.SupplierId;
        entity.CostCenterId = e.CostCenterId;
        entity.Department = e.Department;
        entity.ProjectCode = e.ProjectCode;
        entity.Notes = e.Notes;
        entity.IsRecurring = e.IsRecurring;
        entity.RecurrenceFrequency = (int)e.RecurrenceFrequency;
        entity.CustomIntervalDays = e.CustomIntervalDays;
        entity.NextDueDate = e.NextDueDate;
        entity.RemainingInstallments = e.RemainingInstallments;
        entity.IsActive = e.IsActive;
        entity.IsArchived = e.IsArchived;

        SyncPayments(entity, e);
        SyncAttachments(entity, e);
        SyncInstallments(entity, e);
    }

    private static void SyncPayments(ExpenseEntity entity, Expense e)
    {
        var incoming = e.Payments.ToDictionary(x => x.Id);
        foreach (var existing in entity.Payments.ToList())
        {
            if (!incoming.ContainsKey(existing.Id))
                entity.Payments.Remove(existing);
        }

        foreach (var p in e.Payments)
        {
            var row = entity.Payments.FirstOrDefault(x => x.Id == p.Id);
            if (row is null)
            {
                entity.Payments.Add(ToPaymentEntity(p, e.Id));
                continue;
            }

            row.PaymentDate = p.PaymentDate;
            row.DueDate = p.DueDate;
            row.AmountOriginal = p.AmountOriginal;
            row.AmountBase = p.AmountBase;
            row.Currency = p.Currency;
            row.ExchangeRateSnapshot = p.ExchangeRateSnapshot;
            row.PaymentMethod = (int)p.PaymentMethod;
            row.FundingSource = (int)p.FundingSource;
            row.Status = (int)p.Status;
            row.ApprovalStatus = (int)p.ApprovalStatus;
            row.ReferenceNumber = p.ReferenceNumber;
            row.Notes = p.Notes;
            row.InstallmentNumber = p.InstallmentNumber;
            row.AttachmentId = p.AttachmentId;
            row.AdjustedFromPaymentId = p.AdjustedFromPaymentId;
            row.CashboxId = p.CashboxId;
        }
    }

    private static void SyncAttachments(ExpenseEntity entity, Expense e)
    {
        var incoming = e.Attachments.ToDictionary(x => x.Id);
        foreach (var existing in entity.Attachments.ToList())
        {
            if (!incoming.ContainsKey(existing.Id))
                entity.Attachments.Remove(existing);
        }

        foreach (var a in e.Attachments)
        {
            var row = entity.Attachments.FirstOrDefault(x => x.Id == a.Id);
            if (row is null)
            {
                entity.Attachments.Add(new ExpenseAttachmentEntity
                {
                    Id = a.Id,
                    ExpenseId = e.Id,
                    FileName = a.FileName,
                    StoragePath = a.StoragePath,
                    ContentType = a.ContentType,
                    SizeBytes = a.SizeBytes
                });
                continue;
            }

            row.FileName = a.FileName;
            row.StoragePath = a.StoragePath;
            row.ContentType = a.ContentType;
            row.SizeBytes = a.SizeBytes;
        }
    }

    private static void SyncInstallments(ExpenseEntity entity, Expense e)
    {
        var incoming = e.Installments.ToDictionary(x => x.Id);
        foreach (var existing in entity.Installments.ToList())
        {
            if (!incoming.ContainsKey(existing.Id))
                entity.Installments.Remove(existing);
        }

        foreach (var i in e.Installments)
        {
            var row = entity.Installments.FirstOrDefault(x => x.Id == i.Id);
            if (row is null)
            {
                entity.Installments.Add(ToInstallmentEntity(i, e.Id));
                continue;
            }

            row.InstallmentNumber = i.InstallmentNumber;
            row.DueDate = i.DueDate;
            row.AmountOriginal = i.AmountOriginal;
            row.AmountBase = i.AmountBase;
            row.Currency = i.Currency;
            row.Status = (int)i.Status;
            row.PaymentId = i.PaymentId;
        }
    }

    private static ExpensePayment ToPaymentDomain(ExpensePaymentEntity p) =>
        ExpensePayment.Rehydrate(
            p.Id, p.ExpenseId, p.PaymentDate, p.DueDate, p.AmountOriginal, p.AmountBase,
            p.Currency, p.ExchangeRateSnapshot, (ExpensePaymentMethod)p.PaymentMethod,
            (ExpenseFundingSource)p.FundingSource, (ExpensePaymentStatus)p.Status,
            (ExpensePaymentApprovalStatus)p.ApprovalStatus, p.ReferenceNumber, p.Notes,
            p.InstallmentNumber, p.AttachmentId, p.AdjustedFromPaymentId, p.CashboxId);

    public static ExpensePaymentEntity MapPaymentEntity(ExpensePayment p, Guid expenseId) =>
        ToPaymentEntity(p, expenseId);

    private static ExpensePaymentEntity ToPaymentEntity(ExpensePayment p, Guid expenseId) => new()
    {
        Id = p.Id,
        ExpenseId = expenseId,
        PaymentDate = p.PaymentDate,
        DueDate = p.DueDate,
        AmountOriginal = p.AmountOriginal,
        AmountBase = p.AmountBase,
        Currency = p.Currency,
        ExchangeRateSnapshot = p.ExchangeRateSnapshot,
        PaymentMethod = (int)p.PaymentMethod,
        FundingSource = (int)p.FundingSource,
        Status = (int)p.Status,
        ApprovalStatus = (int)p.ApprovalStatus,
        ReferenceNumber = p.ReferenceNumber,
        Notes = p.Notes,
        InstallmentNumber = p.InstallmentNumber,
        AttachmentId = p.AttachmentId,
        AdjustedFromPaymentId = p.AdjustedFromPaymentId,
        CashboxId = p.CashboxId
    };

    private static ExpenseInstallment ToInstallmentDomain(ExpenseInstallmentEntity i) =>
        ExpenseInstallment.Rehydrate(
            i.Id, i.ExpenseId, i.InstallmentNumber, i.DueDate, i.AmountOriginal,
            i.AmountBase, i.Currency, (ExpenseInstallmentStatus)i.Status, i.PaymentId);

    private static ExpenseInstallmentEntity ToInstallmentEntity(ExpenseInstallment i, Guid expenseId) => new()
    {
        Id = i.Id,
        ExpenseId = expenseId,
        InstallmentNumber = i.InstallmentNumber,
        DueDate = i.DueDate,
        AmountOriginal = i.AmountOriginal,
        AmountBase = i.AmountBase,
        Currency = i.Currency,
        Status = (int)i.Status,
        PaymentId = i.PaymentId
    };

    public static ExpenseAuditLogEntity ToAuditEntity(ExpenseAuditEntry entry) => new()
    {
        Id = entry.Id,
        ExpenseId = entry.ExpenseId,
        Action = entry.Action,
        FieldName = entry.FieldName,
        PreviousValue = entry.PreviousValue,
        NewValue = entry.NewValue,
        UserId = entry.UserId,
        UserName = entry.UserName,
        Timestamp = entry.Timestamp,
        Reason = entry.Reason
    };

    public static ExpenseAuditEntry ToAuditDomain(ExpenseAuditLogEntity entity) =>
        ExpenseAuditEntry.Rehydrate(
            entity.Id,
            entity.ExpenseId,
            entity.Action,
            entity.UserId,
            entity.UserName,
            entity.Timestamp,
            entity.FieldName,
            entity.PreviousValue,
            entity.NewValue,
            entity.Reason);

    public static ExpenseTimelineEventEntity ToTimelineEntity(ExpenseTimelineEvent entry) => new()
    {
        Id = entry.Id,
        ExpenseId = entry.ExpenseId,
        EventType = entry.EventType,
        Title = entry.Title,
        Description = entry.Description,
        PreviousValue = entry.PreviousValue,
        NewValue = entry.NewValue,
        UserId = entry.UserId,
        UserName = entry.UserName,
        Timestamp = entry.Timestamp,
        Reason = entry.Reason
    };

    public static ExpenseTimelineEvent ToTimelineDomain(ExpenseTimelineEventEntity entity) =>
        ExpenseTimelineEvent.Rehydrate(
            entity.Id,
            entity.ExpenseId,
            entity.EventType,
            entity.Title,
            entity.UserId,
            entity.UserName,
            entity.Timestamp,
            entity.Description,
            entity.PreviousValue,
            entity.NewValue,
            entity.Reason);
}
