using ERPSystem.Infrastructure.Persistence.Models.Finance;

namespace ERPSystem.Infrastructure.Persistence.Models.Expenses;

public class ExpenseCategoryEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public int Kind { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
}

public class ExpenseEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public Guid CategoryId { get; set; }
    public int CategoryKind { get; set; }
    public string? Description { get; set; }
    public int Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string OriginalCurrency { get; set; } = "SAR";
    public decimal OriginalAmount { get; set; }
    public decimal ExchangeRate { get; set; } = 1m;
    public string BaseCurrency { get; set; } = "SAR";
    public decimal BaseAmount { get; set; }
    public int PaymentMethod { get; set; }
    public string? PayeeName { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CostCenterId { get; set; }
    public string? Department { get; set; }
    public string? ProjectCode { get; set; }
    public string? Notes { get; set; }
    public bool IsRecurring { get; set; }
    public int RecurrenceFrequency { get; set; }
    public int? CustomIntervalDays { get; set; }
    public DateTime? NextDueDate { get; set; }
    public int? RemainingInstallments { get; set; }
    public string? IntegrationReferenceType { get; set; }
    public Guid? IntegrationReferenceId { get; set; }

    public CostCenterEntity? CostCenter { get; set; }
    public ICollection<ExpensePaymentEntity> Payments { get; set; } = [];
    public ICollection<ExpenseAttachmentEntity> Attachments { get; set; } = [];
    public ICollection<ExpenseInstallmentEntity> Installments { get; set; } = [];
}

public class ExpensePaymentEntity : PersistenceEntity
{
    public Guid ExpenseId { get; set; }
    public DateTime PaymentDate { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal AmountOriginal { get; set; }
    public decimal AmountBase { get; set; }
    public string Currency { get; set; } = "SAR";
    public decimal ExchangeRateSnapshot { get; set; } = 1m;
    public int PaymentMethod { get; set; }
    public int FundingSource { get; set; }
    public int Status { get; set; }
    public int ApprovalStatus { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public int? InstallmentNumber { get; set; }
    public Guid? AttachmentId { get; set; }
    public Guid? AdjustedFromPaymentId { get; set; }
    public Guid? CashboxId { get; set; }
    public ExpenseEntity? Expense { get; set; }
}

public class ExpenseInstallmentEntity : PersistenceEntity
{
    public Guid ExpenseId { get; set; }
    public int InstallmentNumber { get; set; }
    public DateTime DueDate { get; set; }
    public decimal AmountOriginal { get; set; }
    public decimal AmountBase { get; set; }
    public string Currency { get; set; } = "SAR";
    public int Status { get; set; }
    public Guid? PaymentId { get; set; }
    public ExpenseEntity? Expense { get; set; }
}

public class ExpenseAttachmentEntity : PersistenceEntity
{
    public Guid ExpenseId { get; set; }
    public string FileName { get; set; } = "";
    public string StoragePath { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
    public ExpenseEntity? Expense { get; set; }
}

public class ExpenseAuditLogEntity
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public string Action { get; set; } = "";
    public string? FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Reason { get; set; }
}

public class ExpenseTimelineEventEntity
{
    public Guid Id { get; set; }
    public Guid ExpenseId { get; set; }
    public string EventType { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string? Reason { get; set; }
}
