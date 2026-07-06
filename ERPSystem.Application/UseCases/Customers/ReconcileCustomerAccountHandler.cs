using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Common;
using ERPSystem.Application.Results;
using ERPSystem.Domain.Entities.System;

namespace ERPSystem.Application.UseCases.Customers;

public sealed class ReconcileCustomerAccountHandler(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    IPermissionService permissionService,
    IAuditLogRepository auditLogRepository,
    ICurrentUserService currentUserService,
    ICurrentBranchService branchService)
    : ICommandHandler<ReconcileCustomerAccountCommand, ApplicationResult>
{
    public async Task<ApplicationResult> HandleAsync(
        ReconcileCustomerAccountCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.CustomerId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.CustomerId), "Customer is required.");
        if (command.DocumentId == Guid.Empty)
            return ApplicationResult.ValidationFailed(nameof(command.DocumentId), "Reference document is required.");

        // DECISION PENDING: no dedicated reconciliation permission yet — reusing customers.create like customer updates.
        if (!await permissionService.CanAsync("customers.create", cancellationToken))
            return ApplicationResult.PermissionDenied("Not allowed to reconcile customer accounts.");

        var aggregate = await customerRepository.GetByIdAsync(command.CustomerId, cancellationToken);
        if (aggregate is null)
            return ApplicationResult.NotFound("Customer not found.");

        try
        {
            var previousDate = aggregate.Customer.LastReconciliationDate;
            var previousBalance = aggregate.Customer.LastReconciliationBalance;

            aggregate.Customer.RecordReconciliation(
                command.ReconciliationDate,
                command.BalanceAtReconciliation,
                command.DocumentId);

            await customerRepository.UpdateAsync(aggregate, cancellationToken);

            await auditLogRepository.AddAsync(
                AuditLog.Record(
                    currentUserService.UserId,
                    "Reconcile",
                    "Customer",
                    aggregate.Id,
                    $"{{\"date\":\"{previousDate:O}\",\"balance\":{previousBalance}}}",
                    $"{{\"date\":\"{command.ReconciliationDate:O}\",\"balance\":{command.BalanceAtReconciliation},\"documentId\":\"{command.DocumentId}\"}}",
                    branchService.BranchId),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return ApplicationResult.Success();
        }
        catch (Exception ex)
        {
            return ex.ToFailureResult();
        }
    }
}
