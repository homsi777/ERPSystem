using ERPSystem.Domain.Common;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Services;

namespace ERPSystem.Domain.Aggregates;

public sealed class ExpenseAggregate : AggregateRoot
{
    public Expense Expense { get; private set; } = null!;

    private ExpenseAggregate() { }

    public static ExpenseAggregate FromExpense(Expense expense) => new()
    {
        Id = expense.Id,
        Expense = expense
    };

    public void SubmitForApproval() => Expense.SubmitForApproval();

    public void Approve() => Expense.Approve();

    public void Reject(string? reason = null) => Expense.Reject(reason);

    public void Schedule() => Expense.Schedule();

    public void Close() => Expense.Close();

    public void Cancel(string? reason = null) => Expense.Cancel(reason);

    public void Archive() => Expense.Archive();

    public void TransitionTo(ExpenseStatus target, string? reason = null) =>
        Expense.TransitionTo(target, reason);

    public IReadOnlyList<ExpenseStatus> AllowedTransitions =>
        ExpenseLifecycle.GetAllowedTransitions(Expense.Status);
}
