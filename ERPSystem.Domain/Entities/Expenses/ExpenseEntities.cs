using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.Services;

namespace ERPSystem.Domain.Entities.Expenses;

public sealed class ExpenseCategory
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public ExpenseCategoryKind Kind { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public bool IsActive { get; private set; } = true;

    private ExpenseCategory() { }

    public static ExpenseCategory CreateSystem(
        Guid companyId,
        ExpenseCategoryKind kind,
        string code,
        string nameAr,
        string nameEn,
        string? description = null) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Kind = kind,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        Description = description,
        IsSystem = true,
        IsActive = true
    };

    public static ExpenseCategory Rehydrate(
        Guid id,
        Guid companyId,
        ExpenseCategoryKind kind,
        string code,
        string nameAr,
        string nameEn,
        string? description,
        bool isSystem,
        bool isActive) => new()
    {
        Id = id,
        CompanyId = companyId,
        Kind = kind,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        Description = description,
        IsSystem = isSystem,
        IsActive = isActive
    };
}

public sealed class Expense
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public Guid CategoryId { get; private set; }
    public ExpenseCategoryKind CategoryKind { get; private set; }
    public string? Description { get; private set; }
    public ExpenseStatus Status { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string OriginalCurrency { get; private set; } = "USD";
    public decimal OriginalAmount { get; private set; }
    public decimal ExchangeRate { get; private set; } = 1m;
    public string BaseCurrency { get; private set; } = "USD";
    public decimal BaseAmount { get; private set; }
    public ExpensePaymentMethod PaymentMethod { get; private set; }
    public string? PayeeName { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? CostCenterId { get; private set; }
    public string? Department { get; private set; }
    public string? ProjectCode { get; private set; }
    public string? Notes { get; private set; }
    public bool IsRecurring { get; private set; }
    public ExpenseRecurrenceFrequency RecurrenceFrequency { get; private set; }
    public int? CustomIntervalDays { get; private set; }
    public DateTime? NextDueDate { get; private set; }
    public int? RemainingInstallments { get; private set; }
    public string? IntegrationReferenceType { get; private set; }
    public Guid? IntegrationReferenceId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsArchived { get; private set; }

    private List<ExpensePayment> _payments = [];
    private List<ExpenseAttachment> _attachments = [];
    private List<ExpenseInstallment> _installments = [];
    public IReadOnlyList<ExpensePayment> Payments => _payments;
    public IReadOnlyList<ExpenseAttachment> Attachments => _attachments;
    public IReadOnlyList<ExpenseInstallment> Installments => _installments;

    public decimal PaidAmountBase => _payments
        .Where(p => p.Status == ExpensePaymentStatus.Completed)
        .Sum(p => p.AmountBase);

    public decimal RemainingBalanceBase => Math.Max(0, BaseAmount - PaidAmountBase);

    private Expense() { }

    public static Expense Create(
        Guid companyId,
        Guid branchId,
        string code,
        string name,
        Guid categoryId,
        ExpenseCategoryKind categoryKind,
        DateTime startDate,
        string originalCurrency,
        decimal originalAmount,
        decimal exchangeRate,
        string baseCurrency,
        ExpensePaymentMethod paymentMethod)
    {
        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            BranchId = branchId,
            Code = code,
            Name = name,
            CategoryId = categoryId,
            CategoryKind = categoryKind,
            StartDate = startDate.Date,
            OriginalCurrency = originalCurrency,
            OriginalAmount = originalAmount,
            ExchangeRate = exchangeRate,
            BaseCurrency = baseCurrency,
            BaseAmount = Math.Round(originalAmount * exchangeRate, 4),
            PaymentMethod = paymentMethod,
            Status = ExpenseStatus.Draft
        };
        return expense;
    }

    /// <summary>مصروف تعريفي بدون ميزانية — جاهز لاستقبال القيود مباشرة.</summary>
    public void ActivateForEntries()
    {
        if (BaseAmount > 0 || Status != ExpenseStatus.Draft)
            return;
        Status = ExpenseStatus.Scheduled;
    }

    public void UpdateProfile(
        string name,
        Guid categoryId,
        ExpenseCategoryKind categoryKind,
        string? description,
        DateTime startDate,
        DateTime? endDate,
        string originalCurrency,
        decimal originalAmount,
        decimal exchangeRate,
        string baseCurrency,
        ExpensePaymentMethod paymentMethod,
        string? payeeName,
        Guid? supplierId,
        Guid? costCenterId,
        string? department,
        string? projectCode,
        string? notes,
        bool isRecurring,
        ExpenseRecurrenceFrequency recurrenceFrequency,
        int? customIntervalDays,
        DateTime? nextDueDate,
        int? remainingInstallments)
    {
        if (Status is ExpenseStatus.Archived or ExpenseStatus.Cancelled)
            throw new ExpenseLifecycleException("Cannot edit archived or cancelled expenses.");

        Name = name;
        CategoryId = categoryId;
        CategoryKind = categoryKind;
        Description = description;
        StartDate = startDate.Date;
        EndDate = endDate?.Date;
        OriginalCurrency = originalCurrency;
        OriginalAmount = originalAmount;
        ExchangeRate = exchangeRate;
        BaseCurrency = baseCurrency;
        BaseAmount = Math.Round(originalAmount * exchangeRate, 4);
        PaymentMethod = paymentMethod;
        PayeeName = payeeName;
        SupplierId = supplierId;
        CostCenterId = costCenterId;
        Department = department;
        ProjectCode = projectCode;
        Notes = notes;
        IsRecurring = isRecurring;
        RecurrenceFrequency = recurrenceFrequency;
        CustomIntervalDays = customIntervalDays;
        NextDueDate = nextDueDate?.Date;
        RemainingInstallments = remainingInstallments;
    }

    public void TransitionTo(ExpenseStatus target, string? reason = null)
    {
        ExpenseLifecycle.ValidateTransition(Status, target);
        Status = target;
        if (target == ExpenseStatus.Archived)
            IsArchived = true;
    }

    public void SubmitForApproval() => TransitionTo(ExpenseStatus.PendingApproval);

    public void Approve() => TransitionTo(ExpenseStatus.Approved);

    public void Reject(string? reason = null) => TransitionTo(ExpenseStatus.Cancelled);

    public void Schedule() => TransitionTo(ExpenseStatus.Scheduled);

    public void Close() => TransitionTo(ExpenseStatus.Closed);

    public void Cancel(string? reason = null)
    {
        if (Status is ExpenseStatus.Archived)
            throw new ExpenseLifecycleException("Archived expenses cannot be cancelled.");
        TransitionTo(ExpenseStatus.Cancelled);
    }

    public void Archive()
    {
        if (Status is not (ExpenseStatus.Closed or ExpenseStatus.Cancelled))
            throw new ExpenseLifecycleException("Only closed or cancelled expenses can be archived.");
        TransitionTo(ExpenseStatus.Archived);
    }

    public void RestoreFromArchive()
    {
        if (!IsArchived)
            return;
        IsArchived = false;
        Status = ExpenseStatus.Closed;
    }

    public void Deactivate() => IsActive = false;

    public ExpensePayment RecordPayment(
        DateTime paymentDate,
        DateTime? dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency,
        decimal exchangeRateSnapshot,
        ExpensePaymentMethod method,
        ExpenseFundingSource fundingSource,
        string? reference,
        string? notes,
        int? installmentNumber = null,
        Guid? attachmentId = null,
        Guid? cashboxId = null)
    {
        EnsureReadyForPaymentRecording();

        var activePaid = PaidAmountBase;
        if (BaseAmount > 0 && activePaid + amountBase > BaseAmount)
            throw new ExpensePaymentException("Payment would exceed the expense amount. Overpayments are not allowed.");

        var payment = ExpensePayment.Create(
            Id, paymentDate, dueDate, amountOriginal, amountBase, currency,
            exchangeRateSnapshot, method, fundingSource, reference, notes,
            installmentNumber, attachmentId, cashboxId);
        _payments.Add(payment);

        if (BaseAmount <= 0)
            Status = ExpenseStatus.Scheduled;
        else
        {
            var targetStatus = ExpenseLifecycle.ResolvePaymentStatus(BaseAmount, activePaid + amountBase);
            ExpenseLifecycle.ValidateTransition(Status, targetStatus);
            Status = targetStatus;
        }

        return payment;
    }

    private void EnsureReadyForPaymentRecording()
    {
        if (IsArchived || Status == ExpenseStatus.Archived)
            throw new ExpensePaymentException("لا يمكن تسجيل قيود على مصروف مؤرشف.");

        if (Status == ExpenseStatus.Cancelled)
            throw new ExpensePaymentException("لا يمكن تسجيل قيود على مصروف ملغى.");

        if (BaseAmount <= 0)
        {
            if (Status is ExpenseStatus.Draft or ExpenseStatus.Approved
                or ExpenseStatus.PendingApproval or ExpenseStatus.Paid)
                Status = ExpenseStatus.Scheduled;
        }
        else if (Status == ExpenseStatus.Draft)
        {
            ActivateForEntries();
        }

        if (Status is not (ExpenseStatus.Scheduled or ExpenseStatus.PartiallyPaid))
            throw new ExpensePaymentException("لا يمكن تسجيل القيد في الحالة الحالية للمصروف. تأكد أن التعريف جاهز لاستقبال القيود.");
    }

    public void ScheduleInstallments(IEnumerable<ExpenseInstallment> installments)
    {
        if (Status != ExpenseStatus.Approved)
            throw new ExpenseLifecycleException("Installments can only be scheduled for approved expenses.");

        _installments = installments.ToList();
        TransitionTo(ExpenseStatus.Scheduled);
    }

    public ExpensePayment ScheduleFuturePayment(
        DateTime dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency,
        decimal exchangeRateSnapshot,
        ExpensePaymentMethod method,
        ExpenseFundingSource fundingSource,
        string? reference,
        string? notes,
        int? installmentNumber = null)
    {
        if (Status is not (ExpenseStatus.Approved or ExpenseStatus.Scheduled or ExpenseStatus.PartiallyPaid))
            throw new ExpensePaymentException("Future payments can only be scheduled for approved or in-payment expenses.");

        var payment = ExpensePayment.CreateScheduled(
            Id, dueDate, amountOriginal, amountBase, currency,
            exchangeRateSnapshot, method, fundingSource, reference, notes, installmentNumber);
        _payments.Add(payment);
        if (Status == ExpenseStatus.Approved)
            TransitionTo(ExpenseStatus.Scheduled);
        return payment;
    }

    public void CancelPayment(Guid paymentId, string? reason = null)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new ExpensePaymentException("Payment not found.");
        payment.Cancel(reason);

        if (PaidAmountBase <= 0 && Status is ExpenseStatus.PartiallyPaid or ExpenseStatus.Paid)
            Status = ExpenseStatus.Scheduled;
        else if (PaidAmountBase > 0 && PaidAmountBase < BaseAmount)
            Status = ExpenseStatus.PartiallyPaid;
        else if (PaidAmountBase >= BaseAmount)
            Status = ExpenseStatus.Paid;
    }

    public ExpensePayment AdjustPayment(
        Guid paymentId,
        decimal newAmountOriginal,
        decimal newAmountBase,
        string? notes)
    {
        var payment = _payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new ExpensePaymentException("Payment not found.");

        var otherPaid = PaidAmountBase - (payment.Status == ExpensePaymentStatus.Completed ? payment.AmountBase : 0);
        if (otherPaid + newAmountBase > BaseAmount)
            throw new ExpensePaymentException("Adjusted payment would exceed the expense amount.");

        payment.MarkAdjusted();
        var adjusted = ExpensePayment.CreateAdjustment(
            Id, payment, newAmountOriginal, newAmountBase, notes);
        _payments.Add(adjusted);

        var targetStatus = ExpenseLifecycle.ResolvePaymentStatus(BaseAmount, otherPaid + newAmountBase);
        if (GetAllowedPaymentTransitions().Contains(targetStatus))
            Status = targetStatus;

        return adjusted;
    }

    private IReadOnlyList<ExpenseStatus> GetAllowedPaymentTransitions() =>
        ExpenseLifecycle.GetAllowedTransitions(Status);

    public ExpenseAttachment AddAttachment(string fileName, string storagePath, string contentType, long sizeBytes)
    {
        var attachment = ExpenseAttachment.Create(Id, fileName, storagePath, contentType, sizeBytes);
        _attachments.Add(attachment);
        return attachment;
    }

    public Expense Duplicate(string newCode)
    {
        var duplicate = Create(
            CompanyId,
            BranchId,
            newCode,
            $"{Name} (نسخة)",
            CategoryId,
            CategoryKind,
            StartDate,
            OriginalCurrency,
            OriginalAmount,
            ExchangeRate,
            BaseCurrency,
            PaymentMethod);
        return duplicate;
    }

    public static Expense Rehydrate(
        Guid id,
        Guid companyId,
        Guid branchId,
        string code,
        string name,
        Guid categoryId,
        ExpenseCategoryKind categoryKind,
        string? description,
        ExpenseStatus status,
        DateTime startDate,
        DateTime? endDate,
        string originalCurrency,
        decimal originalAmount,
        decimal exchangeRate,
        string baseCurrency,
        decimal baseAmount,
        ExpensePaymentMethod paymentMethod,
        string? payeeName,
        Guid? supplierId,
        Guid? costCenterId,
        string? department,
        string? projectCode,
        string? notes,
        bool isRecurring,
        ExpenseRecurrenceFrequency recurrenceFrequency,
        int? customIntervalDays,
        DateTime? nextDueDate,
        int? remainingInstallments,
        string? integrationReferenceType,
        Guid? integrationReferenceId,
        bool isActive,
        bool isArchived,
        IEnumerable<ExpensePayment> payments,
        IEnumerable<ExpenseAttachment> attachments,
        IEnumerable<ExpenseInstallment> installments) => new()
    {
        Id = id,
        CompanyId = companyId,
        BranchId = branchId,
        Code = code,
        Name = name,
        CategoryId = categoryId,
        CategoryKind = categoryKind,
        Description = description,
        Status = status,
        StartDate = startDate,
        EndDate = endDate,
        OriginalCurrency = originalCurrency,
        OriginalAmount = originalAmount,
        ExchangeRate = exchangeRate,
        BaseCurrency = baseCurrency,
        BaseAmount = baseAmount,
        PaymentMethod = paymentMethod,
        PayeeName = payeeName,
        SupplierId = supplierId,
        CostCenterId = costCenterId,
        Department = department,
        ProjectCode = projectCode,
        Notes = notes,
        IsRecurring = isRecurring,
        RecurrenceFrequency = recurrenceFrequency,
        CustomIntervalDays = customIntervalDays,
        NextDueDate = nextDueDate,
        RemainingInstallments = remainingInstallments,
        IntegrationReferenceType = integrationReferenceType,
        IntegrationReferenceId = integrationReferenceId,
        IsActive = isActive,
        IsArchived = isArchived,
        _payments = payments.ToList(),
        _attachments = attachments.ToList(),
        _installments = installments.ToList()
    };
}

public sealed class ExpensePayment
{
    public Guid Id { get; private set; }
    public Guid ExpenseId { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public DateTime? DueDate { get; private set; }
    public decimal AmountOriginal { get; private set; }
    public decimal AmountBase { get; private set; }
    public string Currency { get; private set; } = "USD";
    public decimal ExchangeRateSnapshot { get; private set; } = 1m;
    public ExpensePaymentMethod PaymentMethod { get; private set; }
    public ExpenseFundingSource FundingSource { get; private set; }
    public ExpensePaymentStatus Status { get; private set; }
    public ExpensePaymentApprovalStatus ApprovalStatus { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public int? InstallmentNumber { get; private set; }
    public Guid? AttachmentId { get; private set; }
    public Guid? AdjustedFromPaymentId { get; private set; }
    public Guid? CashboxId { get; private set; }

    private ExpensePayment() { }

    public static ExpensePayment Create(
        Guid expenseId,
        DateTime paymentDate,
        DateTime? dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency,
        decimal exchangeRateSnapshot,
        ExpensePaymentMethod method,
        ExpenseFundingSource fundingSource,
        string? reference,
        string? notes,
        int? installmentNumber = null,
        Guid? attachmentId = null,
        Guid? cashboxId = null) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        PaymentDate = paymentDate.Date,
        DueDate = dueDate?.Date,
        AmountOriginal = amountOriginal,
        AmountBase = amountBase,
        Currency = currency,
        ExchangeRateSnapshot = exchangeRateSnapshot,
        PaymentMethod = method,
        FundingSource = fundingSource,
        Status = ExpensePaymentStatus.Completed,
        ApprovalStatus = ExpensePaymentApprovalStatus.Approved,
        ReferenceNumber = reference,
        Notes = notes,
        InstallmentNumber = installmentNumber,
        AttachmentId = attachmentId,
        CashboxId = cashboxId
    };

    public static ExpensePayment CreateScheduled(
        Guid expenseId,
        DateTime dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency,
        decimal exchangeRateSnapshot,
        ExpensePaymentMethod method,
        ExpenseFundingSource fundingSource,
        string? reference,
        string? notes,
        int? installmentNumber = null) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        PaymentDate = dueDate.Date,
        DueDate = dueDate.Date,
        AmountOriginal = amountOriginal,
        AmountBase = amountBase,
        Currency = currency,
        ExchangeRateSnapshot = exchangeRateSnapshot,
        PaymentMethod = method,
        FundingSource = fundingSource,
        Status = ExpensePaymentStatus.Scheduled,
        ApprovalStatus = ExpensePaymentApprovalStatus.Pending,
        ReferenceNumber = reference,
        Notes = notes,
        InstallmentNumber = installmentNumber
    };

    public static ExpensePayment CreateAdjustment(
        Guid expenseId,
        ExpensePayment original,
        decimal newAmountOriginal,
        decimal newAmountBase,
        string? notes) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        PaymentDate = DateTime.UtcNow.Date,
        DueDate = original.DueDate,
        AmountOriginal = newAmountOriginal,
        AmountBase = newAmountBase,
        Currency = original.Currency,
        ExchangeRateSnapshot = original.ExchangeRateSnapshot,
        PaymentMethod = original.PaymentMethod,
        FundingSource = original.FundingSource,
        Status = ExpensePaymentStatus.Completed,
        ApprovalStatus = ExpensePaymentApprovalStatus.Approved,
        ReferenceNumber = original.ReferenceNumber,
        Notes = notes ?? original.Notes,
        InstallmentNumber = original.InstallmentNumber,
        AdjustedFromPaymentId = original.Id
    };

    public void Cancel(string? reason = null)
    {
        if (Status == ExpensePaymentStatus.Cancelled)
            return;
        Status = ExpensePaymentStatus.Cancelled;
        if (!string.IsNullOrWhiteSpace(reason))
            Notes = string.IsNullOrWhiteSpace(Notes) ? reason : $"{Notes} | {reason}";
    }

    public void MarkAdjusted() => Status = ExpensePaymentStatus.Adjusted;

    public void Complete()
    {
        if (Status != ExpensePaymentStatus.Scheduled && Status != ExpensePaymentStatus.Pending)
            throw new ExpensePaymentException("Only scheduled or pending payments can be completed.");
        Status = ExpensePaymentStatus.Completed;
        PaymentDate = DateTime.UtcNow.Date;
    }

    public static ExpensePayment Rehydrate(
        Guid id,
        Guid expenseId,
        DateTime paymentDate,
        DateTime? dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency,
        decimal exchangeRateSnapshot,
        ExpensePaymentMethod method,
        ExpenseFundingSource fundingSource,
        ExpensePaymentStatus status,
        ExpensePaymentApprovalStatus approvalStatus,
        string? reference,
        string? notes,
        int? installmentNumber,
        Guid? attachmentId,
        Guid? adjustedFromPaymentId,
        Guid? cashboxId) => new()
    {
        Id = id,
        ExpenseId = expenseId,
        PaymentDate = paymentDate,
        DueDate = dueDate,
        AmountOriginal = amountOriginal,
        AmountBase = amountBase,
        Currency = currency,
        ExchangeRateSnapshot = exchangeRateSnapshot,
        PaymentMethod = method,
        FundingSource = fundingSource,
        Status = status,
        ApprovalStatus = approvalStatus,
        ReferenceNumber = reference,
        Notes = notes,
        InstallmentNumber = installmentNumber,
        AttachmentId = attachmentId,
        AdjustedFromPaymentId = adjustedFromPaymentId,
        CashboxId = cashboxId
    };
}

public sealed class ExpenseInstallment
{
    public Guid Id { get; private set; }
    public Guid ExpenseId { get; private set; }
    public int InstallmentNumber { get; private set; }
    public DateTime DueDate { get; private set; }
    public decimal AmountOriginal { get; private set; }
    public decimal AmountBase { get; private set; }
    public string Currency { get; private set; } = "USD";
    public ExpenseInstallmentStatus Status { get; private set; }
    public Guid? PaymentId { get; private set; }

    private ExpenseInstallment() { }

    public static ExpenseInstallment Create(
        Guid expenseId,
        int installmentNumber,
        DateTime dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        InstallmentNumber = installmentNumber,
        DueDate = dueDate.Date,
        AmountOriginal = amountOriginal,
        AmountBase = amountBase,
        Currency = currency,
        Status = ExpenseInstallmentStatus.Scheduled
    };

    public void MarkPaid(Guid paymentId)
    {
        Status = ExpenseInstallmentStatus.Paid;
        PaymentId = paymentId;
    }

    public void MarkOverdue() => Status = ExpenseInstallmentStatus.Overdue;

    public void Cancel() => Status = ExpenseInstallmentStatus.Cancelled;

    public static ExpenseInstallment Rehydrate(
        Guid id,
        Guid expenseId,
        int installmentNumber,
        DateTime dueDate,
        decimal amountOriginal,
        decimal amountBase,
        string currency,
        ExpenseInstallmentStatus status,
        Guid? paymentId) => new()
    {
        Id = id,
        ExpenseId = expenseId,
        InstallmentNumber = installmentNumber,
        DueDate = dueDate,
        AmountOriginal = amountOriginal,
        AmountBase = amountBase,
        Currency = currency,
        Status = status,
        PaymentId = paymentId
    };
}

public sealed class ExpenseAttachment
{
    public Guid Id { get; private set; }
    public Guid ExpenseId { get; private set; }
    public string FileName { get; private set; } = "";
    public string StoragePath { get; private set; } = "";
    public string ContentType { get; private set; } = "";
    public long SizeBytes { get; private set; }

    private ExpenseAttachment() { }

    public static ExpenseAttachment Create(
        Guid expenseId,
        string fileName,
        string storagePath,
        string contentType,
        long sizeBytes) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        FileName = fileName,
        StoragePath = storagePath,
        ContentType = contentType,
        SizeBytes = sizeBytes
    };

    public static ExpenseAttachment Rehydrate(
        Guid id,
        Guid expenseId,
        string fileName,
        string storagePath,
        string contentType,
        long sizeBytes) => new()
    {
        Id = id,
        ExpenseId = expenseId,
        FileName = fileName,
        StoragePath = storagePath,
        ContentType = contentType,
        SizeBytes = sizeBytes
    };
}

public sealed class ExpenseTimelineEvent
{
    public Guid Id { get; private set; }
    public Guid ExpenseId { get; private set; }
    public string EventType { get; private set; } = "";
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = "";
    public DateTime Timestamp { get; private set; }
    public string? Reason { get; private set; }

    private ExpenseTimelineEvent() { }

    public static ExpenseTimelineEvent Record(
        Guid expenseId,
        string eventType,
        string title,
        Guid userId,
        string userName,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        EventType = eventType,
        Title = title,
        Description = description,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = DateTime.UtcNow,
        Reason = reason
    };

    public static ExpenseTimelineEvent Rehydrate(
        Guid id,
        Guid expenseId,
        string eventType,
        string title,
        Guid userId,
        string userName,
        DateTime timestamp,
        string? description = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null) => new()
    {
        Id = id,
        ExpenseId = expenseId,
        EventType = eventType,
        Title = title,
        Description = description,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = timestamp,
        Reason = reason
    };
}

public sealed class ExpenseAuditEntry
{
    public Guid Id { get; private set; }
    public Guid ExpenseId { get; private set; }
    public string Action { get; private set; } = "";
    public string? FieldName { get; private set; }
    public string? PreviousValue { get; private set; }
    public string? NewValue { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = "";
    public DateTime Timestamp { get; private set; }
    public string? Reason { get; private set; }

    private ExpenseAuditEntry() { }

    public static ExpenseAuditEntry Record(
        Guid expenseId,
        string action,
        Guid userId,
        string userName,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null) => new()
    {
        Id = Guid.NewGuid(),
        ExpenseId = expenseId,
        Action = action,
        FieldName = fieldName,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = DateTime.UtcNow,
        Reason = reason
    };

    public static ExpenseAuditEntry Rehydrate(
        Guid id,
        Guid expenseId,
        string action,
        Guid userId,
        string userName,
        DateTime timestamp,
        string? fieldName = null,
        string? previousValue = null,
        string? newValue = null,
        string? reason = null) => new()
    {
        Id = id,
        ExpenseId = expenseId,
        Action = action,
        FieldName = fieldName,
        PreviousValue = previousValue,
        NewValue = newValue,
        UserId = userId,
        UserName = userName,
        Timestamp = timestamp,
        Reason = reason
    };
}
