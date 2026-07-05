using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Expenses;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.Mapping;
using ERPSystem.Application.Notifications;
using ERPSystem.Application.Queries.Expenses;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Aggregates;
using ERPSystem.Domain.Entities.Expenses;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Application.UseCases.Expenses;

public sealed class CreateExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INumberingService numberingService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<CreateExpenseCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        CreateExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.Name))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Name), "Expense name is required.");
        if (command.OriginalAmount < 0)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.OriginalAmount), "Amount cannot be negative.");

        if (!await permissionService.CanAsync("expenses.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to create expenses.");

        var categories = await expenseRepository.GetCategoriesAsync(command.CompanyId, cancellationToken);
        var category = categories.FirstOrDefault(c => c.Id == command.CategoryId);
        if (category is null)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.CategoryId), "Category not found.");

        try
        {
            var startDate = ApplicationDateNormalizer.ToUtcDate(command.StartDate);
            var nextDue = command.IsRecurring
                ? ApplicationDateNormalizer.ToUtcDate(command.NextDueDate)
                  ?? ApplicationDateNormalizer.NextDueFromFrequency(startDate, command.RecurrenceFrequency, command.CustomIntervalDays)
                : null;

            var code = await numberingService.NextExpenseCodeAsync(command.BranchId, cancellationToken);
            var expense = Expense.Create(
                command.CompanyId,
                command.BranchId,
                code,
                command.Name.Trim(),
                command.CategoryId,
                category.Kind,
                startDate,
                command.OriginalCurrency,
                command.OriginalAmount,
                command.ExchangeRate,
                command.BaseCurrency,
                command.PaymentMethod);

            expense.UpdateProfile(
                command.Name.Trim(),
                command.CategoryId,
                category.Kind,
                command.Description,
                startDate,
                endDate: null,
                command.OriginalCurrency,
                command.OriginalAmount,
                command.ExchangeRate,
                command.BaseCurrency,
                command.PaymentMethod,
                command.PayeeName,
                command.SupplierId,
                command.CostCenterId,
                command.Department,
                command.ProjectCode,
                command.Notes,
                command.IsRecurring,
                command.IsRecurring ? (command.RecurrenceFrequency == ExpenseRecurrenceFrequency.None
                    ? ExpenseRecurrenceFrequency.Monthly
                    : command.RecurrenceFrequency) : ExpenseRecurrenceFrequency.None,
                command.CustomIntervalDays,
                nextDue,
                remainingInstallments: null);

            if (command.SubmitForApproval)
                expense.SubmitForApproval();
            else if (command.OriginalAmount == 0)
                expense.ActivateForEntries();

            var aggregate = ExpenseAggregate.FromExpense(expense);
            await expenseRepository.AddAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordAuditAsync(
                expenseRepository, currentUser, aggregate.Id, "Create", cancellationToken: cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, aggregate.Id, "Create", "إنشاء مصروف", cancellationToken: cancellationToken);

            if (command.SubmitForApproval)
            {
                await ExpenseTrailRecorder.RecordStatusChangeAsync(
                    expenseRepository, currentUser, aggregate.Id,
                    ExpenseStatus.Draft, ExpenseStatus.PendingApproval, cancellationToken: cancellationToken);
                await notificationService.PublishAsync(new ExpenseAwaitingApprovalNotification
                {
                    ExpenseId = aggregate.Id,
                    ExpenseCode = expense.Code,
                    ExpenseName = expense.Name,
                    AmountBase = expense.BaseAmount
                }, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult<Guid>.Success(aggregate.Id);
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult<Guid>();
        }
    }
}

public sealed class UpdateExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<UpdateExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        UpdateExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.ExpenseId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.ExpenseId), "Expense is required.");

        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to edit expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Expense not found.");

        var categories = await expenseRepository.GetCategoriesAsync(aggregate.Expense.CompanyId, cancellationToken);
        var category = categories.FirstOrDefault(c => c.Id == command.CategoryId);
        if (category is null)
            return ApplicationResult.ValidationFailed(nameof(command.CategoryId), "Category not found.");

        try
        {
            var e = aggregate.Expense;
            var prevAmount = e.BaseAmount.ToString("N2");
            var startDate = ApplicationDateNormalizer.ToUtcDate(command.StartDate);
            var nextDue = command.IsRecurring
                ? ApplicationDateNormalizer.ToUtcDate(command.NextDueDate)
                  ?? ApplicationDateNormalizer.NextDueFromFrequency(startDate, command.RecurrenceFrequency, command.CustomIntervalDays)
                : null;

            e.UpdateProfile(
                command.Name.Trim(),
                command.CategoryId,
                category.Kind,
                command.Description,
                startDate,
                endDate: null,
                command.OriginalCurrency,
                command.OriginalAmount,
                command.ExchangeRate,
                command.BaseCurrency,
                command.PaymentMethod,
                command.PayeeName,
                command.SupplierId,
                command.CostCenterId,
                command.Department,
                command.ProjectCode,
                command.Notes,
                command.IsRecurring,
                command.IsRecurring ? (command.RecurrenceFrequency == ExpenseRecurrenceFrequency.None
                    ? ExpenseRecurrenceFrequency.Monthly
                    : command.RecurrenceFrequency) : ExpenseRecurrenceFrequency.None,
                command.CustomIntervalDays,
                nextDue,
                remainingInstallments: null);

            if (prevAmount != e.BaseAmount.ToString("N2"))
            {
                await ExpenseTrailRecorder.RecordAuditAsync(
                    expenseRepository, currentUser, e.Id, "AmountChange",
                    "BaseAmount", prevAmount, e.BaseAmount.ToString("N2"), cancellationToken: cancellationToken);
            }

            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordAuditAsync(
                expenseRepository, currentUser, e.Id, "Edit", cancellationToken: cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, e.Id, "Edit", "تعديل المصروف", cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class TransitionExpenseStatusHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<TransitionExpenseStatusCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        TransitionExpenseStatusCommand command,
        CancellationToken cancellationToken = default)
    {
        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken: cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Expense not found.");

        if (!await HasPermissionForTransition(command.TargetStatus, permissionService, cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed for this transition.");

        try
        {
            var prev = aggregate.Expense.Status;
            aggregate.TransitionTo(command.TargetStatus, command.Reason);
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, command.ExpenseId, prev, command.TargetStatus, command.Reason, cancellationToken);
            await PublishTransitionNotification(aggregate.Expense, command.TargetStatus, notificationService, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }

    private static async Task<bool> HasPermissionForTransition(
        ExpenseStatus target,
        IPermissionService permissions,
        CancellationToken cancellationToken) => target switch
    {
        ExpenseStatus.PendingApproval => await permissions.CanAsync("expenses.create", cancellationToken),
        ExpenseStatus.Approved => await permissions.CanAsync("expenses.approve", cancellationToken),
        ExpenseStatus.Cancelled => await permissions.CanAsync("expenses.approve", cancellationToken)
            || await permissions.CanAsync("expenses.edit", cancellationToken),
        _ => await permissions.CanAsync("expenses.edit", cancellationToken)
    };

    private static async Task PublishTransitionNotification(
        Expense expense,
        ExpenseStatus target,
        INotificationService notifications,
        CancellationToken cancellationToken)
    {
        switch (target)
        {
            case ExpenseStatus.PendingApproval:
                await notifications.PublishAsync(new ExpenseAwaitingApprovalNotification
                {
                    ExpenseId = expense.Id,
                    ExpenseCode = expense.Code,
                    ExpenseName = expense.Name,
                    AmountBase = expense.BaseAmount
                }, cancellationToken);
                break;
            case ExpenseStatus.Cancelled:
                await notifications.PublishAsync(new ExpenseCancelledNotification
                {
                    ExpenseId = expense.Id,
                    ExpenseCode = expense.Code
                }, cancellationToken);
                break;
            case ExpenseStatus.Archived:
                await notifications.PublishAsync(new ExpenseArchivedNotification
                {
                    ExpenseId = expense.Id,
                    ExpenseCode = expense.Code
                }, cancellationToken);
                break;
        }
    }
}

public sealed class ApproveExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<ApproveExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(ApproveExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to approve expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken: cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prev = aggregate.Expense.Status;
            aggregate.Approve();
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, command.ExpenseId, prev, ExpenseStatus.Approved, command.Reason, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class RejectExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<RejectExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(RejectExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.approve", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to reject expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken: cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prev = aggregate.Expense.Status;
            aggregate.Reject(command.Reason);
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, command.ExpenseId, prev, ExpenseStatus.Cancelled, command.Reason, cancellationToken);
            await notificationService.PublishAsync(new ExpenseCancelledNotification
            {
                ExpenseId = aggregate.Id,
                ExpenseCode = aggregate.Expense.Code,
                Reason = command.Reason
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class ScheduleExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<ScheduleExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(ScheduleExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to schedule expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var installments = command.Installments.Select(i => ExpenseInstallment.Create(
                command.ExpenseId, i.InstallmentNumber, i.DueDate, i.AmountOriginal, i.AmountBase, i.Currency)).ToList();

            var prev = aggregate.Expense.Status;
            aggregate.Expense.ScheduleInstallments(installments);
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, command.ExpenseId, prev, ExpenseStatus.Scheduled, cancellationToken: cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, command.ExpenseId, "Schedule",
                $"جدولة {installments.Count} قسط", cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class CloseExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<CloseExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(CloseExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to close expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken: cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prev = aggregate.Expense.Status;
            aggregate.Close();
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, command.ExpenseId, prev, ExpenseStatus.Closed, command.Reason, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class CancelExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<CancelExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(CancelExpenseCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to cancel expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken: cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prev = aggregate.Expense.Status;
            aggregate.Cancel(command.Reason);
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, command.ExpenseId, prev, ExpenseStatus.Cancelled, command.Reason, cancellationToken);
            await notificationService.PublishAsync(new ExpenseCancelledNotification
            {
                ExpenseId = aggregate.Id,
                ExpenseCode = aggregate.Expense.Code,
                Reason = command.Reason
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class ArchiveExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<ArchiveExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ArchiveExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.archive", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to archive expenses.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, cancellationToken: cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prev = aggregate.Expense.Status;
            aggregate.Archive();
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordStatusChangeAsync(
                expenseRepository, currentUser, aggregate.Id, prev, ExpenseStatus.Archived, command.Reason, cancellationToken);
            await notificationService.PublishAsync(new ExpenseArchivedNotification
            {
                ExpenseId = aggregate.Id,
                ExpenseCode = aggregate.Expense.Code
            }, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class DeleteExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<DeleteExpenseCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        DeleteExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.delete", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to delete expenses.");

        await ExpenseTrailRecorder.RecordAuditAsync(
            expenseRepository, currentUser, command.ExpenseId, "Delete", cancellationToken: cancellationToken);
        await expenseRepository.DeleteAsync(command.ExpenseId, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class DuplicateExpenseHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    INumberingService numberingService,
    ICurrentUserService currentUser)
    : ICommandHandler<DuplicateExpenseCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(
        DuplicateExpenseCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.create", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to duplicate expenses.");

        var source = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (source is null)
            return ApplicationResult<Guid>.NotFound("Expense not found.");

        var code = await numberingService.NextExpenseCodeAsync(command.BranchId, cancellationToken);
        var duplicate = source.Expense.Duplicate(code);
        duplicate.UpdateProfile(
            duplicate.Name,
            source.Expense.CategoryId,
            source.Expense.CategoryKind,
            source.Expense.Description,
            DateTime.UtcNow.Date,
            source.Expense.EndDate,
            source.Expense.OriginalCurrency,
            source.Expense.OriginalAmount,
            source.Expense.ExchangeRate,
            source.Expense.BaseCurrency,
            source.Expense.PaymentMethod,
            source.Expense.PayeeName,
            source.Expense.SupplierId,
            source.Expense.CostCenterId,
            source.Expense.Department,
            source.Expense.ProjectCode,
            source.Expense.Notes,
            source.Expense.IsRecurring,
            source.Expense.RecurrenceFrequency,
            source.Expense.CustomIntervalDays,
            source.Expense.NextDueDate,
            source.Expense.RemainingInstallments);

        var aggregate = ExpenseAggregate.FromExpense(duplicate);
        await expenseRepository.AddAsync(aggregate, cancellationToken);
        await ExpenseTrailRecorder.RecordAuditAsync(
            expenseRepository, currentUser, aggregate.Id, "Duplicate",
            "SourceExpenseId", null, command.ExpenseId.ToString(), cancellationToken: cancellationToken);
        await ExpenseTrailRecorder.RecordTimelineAsync(
            expenseRepository, currentUser, aggregate.Id, "Duplicate", "نسخ مصروف", cancellationToken: cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(aggregate.Id);
    }
}

public sealed class RecordExpensePaymentHandler(
    IExpenseRepository expenseRepository,
    IIntegratedAccountingService integratedAccounting,
    ICashboxRepository cashboxRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<RecordExpensePaymentCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        RecordExpensePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to record payments.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prevStatus = aggregate.Expense.Status;
            var paymentDate = ApplicationDateNormalizer.ToUtcDate(command.PaymentDate);
            var payment = aggregate.Expense.RecordPayment(
                paymentDate,
                ApplicationDateNormalizer.ToUtcDate(command.DueDate),
                command.AmountOriginal,
                command.AmountBase,
                command.Currency,
                command.ExchangeRateSnapshot,
                command.PaymentMethod,
                command.FundingSource,
                command.ReferenceNumber,
                command.Notes,
                command.InstallmentNumber,
                command.AttachmentId,
                command.CashboxId);

            await expenseRepository.RecordPaymentAsync(
                command.ExpenseId,
                payment,
                aggregate.Expense.Status,
                cancellationToken);
            await ExpenseTrailRecorder.RecordAuditAsync(
                expenseRepository, currentUser, command.ExpenseId, "PaymentRecorded",
                "AmountBase", null, command.AmountBase.ToString("N2"), cancellationToken: cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, command.ExpenseId, "Payment",
                $"تسجيل دفعة {command.AmountBase:N2}", cancellationToken: cancellationToken);

            if (prevStatus != aggregate.Expense.Status)
            {
                await ExpenseTrailRecorder.RecordStatusChangeAsync(
                    expenseRepository, currentUser, command.ExpenseId,
                    prevStatus, aggregate.Expense.Status, cancellationToken: cancellationToken);
            }

            var paymentDescription = string.IsNullOrWhiteSpace(command.Notes)
                ? $"صرف مصروف — {aggregate.Expense.Name}"
                : command.Notes.Trim();

            await integratedAccounting.PostExpensePaymentAsync(
                command.ExpenseId,
                payment.Id,
                command.AmountBase,
                paymentDescription,
                cancellationToken);

            // Keep the cashbox entity balance synchronized with the GL cash outflow.
            if (command.CashboxId is Guid cashboxId && cashboxId != Guid.Empty && command.AmountBase > 0)
            {
                var cashbox = await cashboxRepository.GetByIdAsync(cashboxId, cancellationToken);
                if (cashbox is not null)
                {
                    cashbox.ApplyPayment(new Money(command.AmountBase));
                    await cashboxRepository.UpdateAsync(cashbox, cancellationToken);
                }
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}

public sealed class ScheduleExpensePaymentHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser,
    INotificationService notificationService)
    : ICommandHandler<ScheduleExpensePaymentCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(ScheduleExpensePaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to schedule payments.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            var prevStatus = aggregate.Expense.Status;
            aggregate.Expense.ScheduleFuturePayment(
                command.DueDate, command.AmountOriginal, command.AmountBase, command.Currency,
                command.ExchangeRateSnapshot, command.PaymentMethod, command.FundingSource,
                command.ReferenceNumber, command.Notes, command.InstallmentNumber);

            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, command.ExpenseId, "PaymentScheduled",
                $"جدولة دفعة مستقبلية {command.DueDate:yyyy/MM/dd}", cancellationToken: cancellationToken);

            if (prevStatus != aggregate.Expense.Status)
            {
                await ExpenseTrailRecorder.RecordStatusChangeAsync(
                    expenseRepository, currentUser, command.ExpenseId,
                    prevStatus, aggregate.Expense.Status, cancellationToken: cancellationToken);
            }

            var daysUntilDue = (command.DueDate.Date - DateTime.UtcNow.Date).TotalDays;
            if (daysUntilDue is >= 0 and <= 7)
            {
                await notificationService.PublishAsync(new ExpensePaymentDueSoonNotification
                {
                    ExpenseId = command.ExpenseId,
                    ExpenseCode = aggregate.Expense.Code,
                    DueDate = command.DueDate,
                    AmountBase = command.AmountBase
                }, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class CancelExpensePaymentHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<CancelExpensePaymentCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(CancelExpensePaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to cancel payments.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            aggregate.Expense.CancelPayment(command.PaymentId, command.Reason);
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, command.ExpenseId, "PaymentCancelled",
                "إلغاء دفعة", reason: command.Reason, cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class AdjustExpensePaymentHandler(
    IExpenseRepository expenseRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    ICurrentUserService currentUser)
    : ICommandHandler<AdjustExpensePaymentCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(AdjustExpensePaymentCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to adjust payments.");

        var aggregate = await expenseRepository.GetByIdAsync(command.ExpenseId, includeChildren: true, cancellationToken);
        if (aggregate is null) return ApplicationResult.NotFound("Expense not found.");

        try
        {
            aggregate.Expense.AdjustPayment(command.PaymentId, command.NewAmountOriginal, command.NewAmountBase, command.Notes);
            await expenseRepository.UpdateAsync(aggregate, cancellationToken);
            await ExpenseTrailRecorder.RecordTimelineAsync(
                expenseRepository, currentUser, command.ExpenseId, "PaymentAdjusted",
                $"تعديل دفعة إلى {command.NewAmountBase:N2}", cancellationToken: cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex) { return ex.ToFailureResult(); }
    }
}

public sealed class CreateCostCenterHandler(
    ICostCenterRepository costCenterRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<CreateCostCenterCommand, ApplicationResult<Guid>>
{
    public async Task<ApplicationResult<Guid>> HandleAsync(CreateCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult<Guid>.PermissionDenied("Not allowed to manage cost centers.");

        if (string.IsNullOrWhiteSpace(command.Code) || string.IsNullOrWhiteSpace(command.Name))
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Code and name are required.");

        var existing = await costCenterRepository.GetByCodeAsync(command.CompanyId, command.Code.Trim(), cancellationToken);
        if (existing is not null)
            return ApplicationResult<Guid>.ValidationFailed(nameof(command.Code), "Cost center code already exists.");

        var costCenter = CostCenter.Create(command.CompanyId, command.Code, command.Name, command.Description, command.ParentCostCenterId);
        await costCenterRepository.AddAsync(costCenter, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<Guid>.Success(costCenter.Id);
    }
}

public sealed class UpdateCostCenterHandler(
    ICostCenterRepository costCenterRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService)
    : ICommandHandler<UpdateCostCenterCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(UpdateCostCenterCommand command, CancellationToken cancellationToken = default)
    {
        if (!await permissionService.CanAsync("expenses.edit", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to manage cost centers.");

        var costCenter = await costCenterRepository.GetByIdAsync(command.CostCenterId, cancellationToken);
        if (costCenter is null) return ApplicationResult.NotFound("Cost center not found.");

        costCenter.Update(command.Name, command.Description, command.ParentCostCenterId);
        costCenter.SetStatus(command.Status);
        await costCenterRepository.UpdateAsync(costCenter, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult.Success();
    }
}

public sealed class GetExpenseListHandler(
    IExpenseRepository expenseRepository,
    ICostCenterRepository costCenterRepository)
    : IQueryHandler<GetExpenseListQuery, ApplicationResult<PagedResult<ExpenseListDto>>>
{
    public async Task<ApplicationResult<PagedResult<ExpenseListDto>>> HandleAsync(
        GetExpenseListQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await expenseRepository.GetCategoriesAsync(query.CompanyId, cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.NameAr);
        var costCenters = await costCenterRepository.GetByCompanyAsync(query.CompanyId, cancellationToken);
        var costCenterMap = costCenters.ToDictionary(c => c.Id, c => c.Name);

        var (items, total) = await expenseRepository.GetPagedAsync(
            query.CompanyId, query.Filter, query.Page, query.PageSize, cancellationToken);

        var dtos = items.Select(a =>
            ExpenseMapper.ToListDto(
                a,
                categoryMap.GetValueOrDefault(a.Expense.CategoryId, "—"),
                a.Expense.CostCenterId is Guid ccId ? costCenterMap.GetValueOrDefault(ccId) : null)).ToList();

        return ApplicationResult<PagedResult<ExpenseListDto>>.Success(new PagedResult<ExpenseListDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}

public sealed class GetExpenseDetailsHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseDetailsQuery, ApplicationResult<ExpenseDetailsDto>>
{
    public async Task<ApplicationResult<ExpenseDetailsDto>> HandleAsync(
        GetExpenseDetailsQuery query,
        CancellationToken cancellationToken = default)
    {
        var bundle = await expenseRepository.GetWithAuditAsync(query.ExpenseId, includeChildren: true, cancellationToken);
        if (bundle is null)
            return ApplicationResult<ExpenseDetailsDto>.NotFound("Expense not found.");

        var categories = await expenseRepository.GetCategoriesAsync(bundle.Aggregate.Expense.CompanyId, cancellationToken);
        var categoryName = categories.FirstOrDefault(c => c.Id == bundle.Aggregate.Expense.CategoryId)?.NameAr ?? "—";

        return ApplicationResult<ExpenseDetailsDto>.Success(ExpenseMapper.ToDetailsDto(
            bundle.Aggregate.Expense,
            categoryName,
            bundle.CreatedAt,
            bundle.CreatedByName,
            bundle.UpdatedAt,
            bundle.CostCenterName));
    }
}

public sealed class GetExpenseOperationsCenterHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseOperationsCenterQuery, ApplicationResult<ExpenseOperationsCenterDto>>
{
    public async Task<ApplicationResult<ExpenseOperationsCenterDto>> HandleAsync(
        GetExpenseOperationsCenterQuery query,
        CancellationToken cancellationToken = default)
    {
        var bundle = await expenseRepository.GetWithAuditAsync(query.ExpenseId, includeChildren: true, cancellationToken);
        if (bundle is null)
            return ApplicationResult<ExpenseOperationsCenterDto>.NotFound("Expense not found.");

        var categories = await expenseRepository.GetCategoriesAsync(bundle.Aggregate.Expense.CompanyId, cancellationToken);
        var categoryName = categories.FirstOrDefault(c => c.Id == bundle.Aggregate.Expense.CategoryId)?.NameAr ?? "—";
        var details = ExpenseMapper.ToDetailsDto(
            bundle.Aggregate.Expense, categoryName, bundle.CreatedAt, bundle.CreatedByName, bundle.UpdatedAt, bundle.CostCenterName);

        var timeline = await expenseRepository.GetTimelineAsync(query.ExpenseId, cancellationToken);
        var audit = await expenseRepository.GetAuditTrailAsync(query.ExpenseId, cancellationToken);

        return ApplicationResult<ExpenseOperationsCenterDto>.Success(ExpenseMapper.ToOperationsCenterDto(
            details,
            timeline.Select(ExpenseMapper.ToTimelineDto).ToList(),
            audit.Select(ExpenseMapper.ToAuditDto).ToList()));
    }
}

public sealed class GetExpenseDashboardHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseDashboardQuery, ApplicationResult<ExpenseDashboardDto>>
{
    public async Task<ApplicationResult<ExpenseDashboardDto>> HandleAsync(
        GetExpenseDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var data = await expenseRepository.GetDashboardDataAsync(query.CompanyId, cancellationToken);
        return ApplicationResult<ExpenseDashboardDto>.Success(ExpenseMapper.ToDashboardDto(data, "USD"));
    }
}

public sealed class GetExpenseCategoriesHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseCategoriesQuery, ApplicationResult<IReadOnlyList<ExpenseCategoryDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ExpenseCategoryDto>>> HandleAsync(
        GetExpenseCategoriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var categories = await expenseRepository.GetCategoriesAsync(query.CompanyId, cancellationToken);
        return ApplicationResult<IReadOnlyList<ExpenseCategoryDto>>.Success(
            categories.Select(ExpenseMapper.ToCategoryDto).ToList());
    }
}

public sealed class GetCostCentersHandler(ICostCenterRepository costCenterRepository)
    : IQueryHandler<GetCostCentersQuery, ApplicationResult<IReadOnlyList<CostCenterDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<CostCenterDto>>> HandleAsync(
        GetCostCentersQuery query,
        CancellationToken cancellationToken = default)
    {
        var items = await costCenterRepository.GetByCompanyAsync(query.CompanyId, cancellationToken);
        return ApplicationResult<IReadOnlyList<CostCenterDto>>.Success(
            items.Select(ExpenseMapper.ToCostCenterDto).ToList());
    }
}

public sealed class GetExpenseAuditTrailHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseAuditTrailQuery, ApplicationResult<IReadOnlyList<ExpenseAuditEntryDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ExpenseAuditEntryDto>>> HandleAsync(
        GetExpenseAuditTrailQuery query,
        CancellationToken cancellationToken = default)
    {
        var entries = await expenseRepository.GetAuditTrailAsync(query.ExpenseId, cancellationToken);
        return ApplicationResult<IReadOnlyList<ExpenseAuditEntryDto>>.Success(
            entries.Select(ExpenseMapper.ToAuditDto).ToList());
    }
}

public sealed class GetExpenseTimelineHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseTimelineQuery, ApplicationResult<IReadOnlyList<ExpenseTimelineEventDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ExpenseTimelineEventDto>>> HandleAsync(
        GetExpenseTimelineQuery query,
        CancellationToken cancellationToken = default)
    {
        var events = await expenseRepository.GetTimelineAsync(query.ExpenseId, cancellationToken);
        return ApplicationResult<IReadOnlyList<ExpenseTimelineEventDto>>.Success(
            events.Select(ExpenseMapper.ToTimelineDto).ToList());
    }
}

public sealed class GetExpenseReportHandler(
    IExpenseRepository expenseRepository,
    ICostCenterRepository costCenterRepository)
    : IQueryHandler<GetExpenseReportQuery, ApplicationResult<ExpenseReportDto>>
{
    public async Task<ApplicationResult<ExpenseReportDto>> HandleAsync(
        GetExpenseReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var filter = query.ReportType is "OverduePayments"
            ? new ExpenseListFilter
            {
                CategoryKind = query.CategoryKind,
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                CostCenterId = query.CostCenterId,
                IncludeArchived = false,
                Status = ExpenseStatus.PartiallyPaid
            }
            : new ExpenseListFilter
            {
                CategoryKind = query.CategoryKind,
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                CostCenterId = query.CostCenterId,
                IncludeArchived = query.ReportType is "Outstanding" or "Archived" or "Lifecycle"
            };

        var (items, _) = await expenseRepository.GetPagedAsync(
            query.CompanyId, filter, 1, 10_000, cancellationToken);

        if (query.ReportType is "Recurring")
            items = items.Where(a => a.Expense.IsRecurring).ToList();

        var categories = await expenseRepository.GetCategoriesAsync(query.CompanyId, cancellationToken);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.NameAr);

        var costCenters = await costCenterRepository.GetByCompanyAsync(query.CompanyId, cancellationToken);
        var costCenterMap = costCenters.ToDictionary(c => c.Id, c => c.Name);

        var rows = items.Select(a =>
        {
            var e = a.Expense;
            var primaryFunding = e.Payments.FirstOrDefault(p => p.Status == ExpensePaymentStatus.Completed)?.FundingSource;
            return new ExpenseReportRowDto
            {
                ExpenseId = e.Id,
                Code = e.Code,
                Name = e.Name,
                Category = categoryMap.GetValueOrDefault(e.CategoryId, e.CategoryKind.ToArabic()),
                CategoryKindDisplay = e.CategoryKind.ToArabic(),
                Status = e.Status.ToArabic(),
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                OriginalAmount = e.OriginalAmount,
                Currency = e.OriginalCurrency,
                ExchangeRate = e.ExchangeRate,
                BaseAmount = e.BaseAmount,
                PaidAmountBase = e.PaidAmountBase,
                RemainingBalanceBase = e.RemainingBalanceBase,
                Department = e.Department,
                CostCenter = e.CostCenterId is Guid ccId ? costCenterMap.GetValueOrDefault(ccId) : null,
                PayeeName = e.PayeeName,
                FundingSource = primaryFunding?.ToArabic(),
                PaymentMethod = e.PaymentMethod.ToArabic(),
                Description = e.Description,
                Notes = e.Notes,
                IsRecurring = e.IsRecurring,
                NextDueDate = e.NextDueDate,
                PaymentCount = e.Payments.Count(p => p.Status == ExpensePaymentStatus.Completed)
            };
        }).ToList();

        if (query.ReportType is "Outstanding" or "OverduePayments")
            rows = rows.Where(r => r.RemainingBalanceBase > 0).ToList();

        if (query.ReportType is "UpcomingPayments")
        {
            var forecast = await expenseRepository.GetPaymentForecastAsync(query.CompanyId, 30, cancellationToken);
            rows = forecast.Select(f => new ExpenseReportRowDto
            {
                ExpenseId = f.ExpenseId,
                Code = f.ExpenseCode,
                Name = f.ExpenseName,
                Status = f.IsOverdue ? "متأخر" : "قادم",
                StartDate = f.DueDate,
                NextDueDate = f.DueDate,
                BaseAmount = f.AmountBase,
                RemainingBalanceBase = f.AmountBase
            }).ToList();
        }

        var scopeLabel = query.CategoryKind?.ToArabic() ?? "جميع المصاريف";
        var title = query.ReportType switch
        {
            "Detailed" => $"تقرير مفصل — {scopeLabel}",
            "Monthly" => "تقرير المصاريف الشهرية",
            "Annual" => "تقرير المصاريف السنوية",
            "Category" => "تحليل المصاريف حسب الفئة",
            "Currency" => "تحليل المصاريف حسب العملة",
            "Department" => "تحليل المصاريف حسب القسم",
            "CostCenter" => "تحليل المصاريف حسب مركز التكلفة",
            "Capital" => "مصاريف رأس المال",
            "Personal" => "المصاريف الشخصية",
            "Operating" => "المصاريف التشغيلية",
            "Outstanding" => $"المصاريف المستحقة — {scopeLabel}",
            "Forecast" => "توقعات الدفع",
            "CashFlow" => "تأثير التدفق النقدي",
            "Recurring" => $"المصاريف المتكررة — {scopeLabel}",
            "Supplier" => "مصاريف الموردين",
            "Project" => "مصاريف المشاريع",
            "PaymentStatus" => "حالة الدفع",
            "UpcomingPayments" => $"الدفعات القادمة — {scopeLabel}",
            "OverduePayments" => $"الدفعات المتأخرة — {scopeLabel}",
            "Lifecycle" => "تقرير دورة حياة المصروف",
            "FundingSource" => $"تحليل مصادر التمويل — {scopeLabel}",
            _ => $"ملخص المصاريف — {scopeLabel}"
        };

        return ApplicationResult<ExpenseReportDto>.Success(new ExpenseReportDto
        {
            Title = title,
            ReportType = query.ReportType,
            GeneratedAt = DateTime.UtcNow,
            Rows = rows,
            TotalBase = rows.Sum(r => r.BaseAmount),
            TotalPaidBase = rows.Sum(r => r.PaidAmountBase),
            TotalRemainingBase = rows.Sum(r => r.RemainingBalanceBase),
            ExpenseCount = rows.Count,
            BaseCurrency = "USD",
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            ScopeLabel = scopeLabel
        });
    }
}

public sealed class GetExpensePaymentForecastHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpensePaymentForecastQuery, ApplicationResult<IReadOnlyList<ExpensePaymentForecastDto>>>
{
    public async Task<ApplicationResult<IReadOnlyList<ExpensePaymentForecastDto>>> HandleAsync(
        GetExpensePaymentForecastQuery query,
        CancellationToken cancellationToken = default)
    {
        var points = await expenseRepository.GetPaymentForecastAsync(query.CompanyId, query.DaysAhead, cancellationToken);
        return ApplicationResult<IReadOnlyList<ExpensePaymentForecastDto>>.Success(
            points.Select(p => new ExpensePaymentForecastDto
            {
                ExpenseId = p.ExpenseId,
                ExpenseCode = p.ExpenseCode,
                ExpenseName = p.ExpenseName,
                DueDate = p.DueDate,
                AmountBase = p.AmountBase,
                IsOverdue = p.IsOverdue
            }).ToList());
    }
}

public sealed class GetExpenseEntriesHandler(IExpenseRepository expenseRepository)
    : IQueryHandler<GetExpenseEntriesQuery, ApplicationResult<PagedResult<ExpenseEntryListDto>>>
{
    public async Task<ApplicationResult<PagedResult<ExpenseEntryListDto>>> HandleAsync(
        GetExpenseEntriesQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await expenseRepository.GetEntriesPagedAsync(
            query.CompanyId, query.Filter, query.Page, query.PageSize, cancellationToken);

        var dtos = items.Select(r => new ExpenseEntryListDto
        {
            Id = r.Id,
            ExpenseId = r.ExpenseId,
            ExpenseCode = r.ExpenseCode,
            ExpenseName = r.ExpenseName,
            PaymentDate = r.PaymentDate,
            AmountOriginal = r.AmountOriginal,
            AmountBase = r.AmountBase,
            Currency = r.Currency,
            Description = r.Description,
            CashboxId = r.CashboxId,
            CashboxName = r.CashboxName
        }).ToList();

        return ApplicationResult<PagedResult<ExpenseEntryListDto>>.Success(new PagedResult<ExpenseEntryListDto>
        {
            Items = dtos,
            TotalCount = total,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }
}
