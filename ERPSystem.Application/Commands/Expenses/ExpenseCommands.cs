using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Expenses;

public sealed class CreateExpenseCommand
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public string Name { get; init; } = "";
    public Guid CategoryId { get; init; }
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string OriginalCurrency { get; init; } = "SAR";
    public decimal OriginalAmount { get; init; }
    public decimal ExchangeRate { get; init; } = 1m;
    public string BaseCurrency { get; init; } = "SAR";
    public ExpensePaymentMethod PaymentMethod { get; init; }
    public string? PayeeName { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? CostCenterId { get; init; }
    public string? Department { get; init; }
    public string? ProjectCode { get; init; }
    public string? Notes { get; init; }
    public bool IsRecurring { get; init; }
    public ExpenseRecurrenceFrequency RecurrenceFrequency { get; init; }
    public int? CustomIntervalDays { get; init; }
    public DateTime? NextDueDate { get; init; }
    public int? RemainingInstallments { get; init; }
    public bool SubmitForApproval { get; init; }
}

public sealed class UpdateExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public string Name { get; init; } = "";
    public Guid CategoryId { get; init; }
    public string? Description { get; init; }
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public string OriginalCurrency { get; init; } = "SAR";
    public decimal OriginalAmount { get; init; }
    public decimal ExchangeRate { get; init; } = 1m;
    public string BaseCurrency { get; init; } = "SAR";
    public ExpensePaymentMethod PaymentMethod { get; init; }
    public string? PayeeName { get; init; }
    public Guid? SupplierId { get; init; }
    public Guid? CostCenterId { get; init; }
    public string? Department { get; init; }
    public string? ProjectCode { get; init; }
    public string? Notes { get; init; }
    public bool IsRecurring { get; init; }
    public ExpenseRecurrenceFrequency RecurrenceFrequency { get; init; }
    public int? CustomIntervalDays { get; init; }
    public DateTime? NextDueDate { get; init; }
    public int? RemainingInstallments { get; init; }
}

public sealed class TransitionExpenseStatusCommand
{
    public Guid ExpenseId { get; init; }
    public ExpenseStatus TargetStatus { get; init; }
    public string? Reason { get; init; }
}

public sealed class ApproveExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public string? Reason { get; init; }
}

public sealed class RejectExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public string? Reason { get; init; }
}

public sealed class ScheduleExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public IReadOnlyList<ExpenseInstallmentInput> Installments { get; init; } = [];
}

public sealed class ExpenseInstallmentInput
{
    public int InstallmentNumber { get; init; }
    public DateTime DueDate { get; init; }
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public string Currency { get; init; } = "SAR";
}

public sealed class ArchiveExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public string? Reason { get; init; }
}

public sealed class CancelExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public string? Reason { get; init; }
}

public sealed class CloseExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public string? Reason { get; init; }
}

public sealed class DeleteExpenseCommand
{
    public Guid ExpenseId { get; init; }
}

public sealed class DuplicateExpenseCommand
{
    public Guid ExpenseId { get; init; }
    public Guid BranchId { get; init; }
}

public sealed class RecordExpensePaymentCommand
{
    public Guid ExpenseId { get; init; }
    public DateTime PaymentDate { get; init; }
    public DateTime? DueDate { get; init; }
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal ExchangeRateSnapshot { get; init; } = 1m;
    public ExpensePaymentMethod PaymentMethod { get; init; }
    public ExpenseFundingSource FundingSource { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public int? InstallmentNumber { get; init; }
    public Guid? AttachmentId { get; init; }
    public Guid? CashboxId { get; init; }
}

public sealed class ScheduleExpensePaymentCommand
{
    public Guid ExpenseId { get; init; }
    public DateTime DueDate { get; init; }
    public decimal AmountOriginal { get; init; }
    public decimal AmountBase { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal ExchangeRateSnapshot { get; init; } = 1m;
    public ExpensePaymentMethod PaymentMethod { get; init; }
    public ExpenseFundingSource FundingSource { get; init; }
    public string? ReferenceNumber { get; init; }
    public string? Notes { get; init; }
    public int? InstallmentNumber { get; init; }
}

public sealed class CancelExpensePaymentCommand
{
    public Guid ExpenseId { get; init; }
    public Guid PaymentId { get; init; }
    public string? Reason { get; init; }
}

public sealed class AdjustExpensePaymentCommand
{
    public Guid ExpenseId { get; init; }
    public Guid PaymentId { get; init; }
    public decimal NewAmountOriginal { get; init; }
    public decimal NewAmountBase { get; init; }
    public string? Notes { get; init; }
}

public sealed class CreateCostCenterCommand
{
    public Guid CompanyId { get; set; }
    public string Code { get; init; } = "";
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public Guid? ParentCostCenterId { get; init; }
}

public sealed class UpdateCostCenterCommand
{
    public Guid CostCenterId { get; init; }
    public string Name { get; init; } = "";
    public string? Description { get; init; }
    public Guid? ParentCostCenterId { get; init; }
    public CostCenterStatus Status { get; init; }
}
