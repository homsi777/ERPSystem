using ERPSystem.Application.Results;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Application.Common;

public static class ApplicationExceptionMapper
{
    public static ApplicationResult ToFailureResult(this Exception exception) =>
        exception switch
        {
            ValidationException ex => ApplicationResult.ValidationFailed("_", ex.Message),
            CreditLimitExceededException ex => ApplicationResult.Conflict(ex.Message),
            InvalidInvoiceWorkflowException ex => ApplicationResult.Conflict(ex.Message),
            ContainerApprovalException ex => ApplicationResult.Conflict(ex.Message),
            WarehouseDetailingException ex => ApplicationResult.Conflict(ex.Message),
            InventoryException ex => ApplicationResult.Conflict(ex.Message),
            AccountingException ex => ApplicationResult.Conflict(ex.Message),
            DomainException ex => ApplicationResult.Failure(ex.Message),
            _ => ApplicationResult.Failure(exception.Message)
        };

    public static ApplicationResult<T> ToFailureResult<T>(this Exception exception) =>
        exception switch
        {
            ValidationException ex => ApplicationResult<T>.ValidationFailed("_", ex.Message),
            CreditLimitExceededException ex => ApplicationResult<T>.Conflict(ex.Message),
            InvalidInvoiceWorkflowException ex => ApplicationResult<T>.Conflict(ex.Message),
            ContainerApprovalException ex => ApplicationResult<T>.Conflict(ex.Message),
            WarehouseDetailingException ex => ApplicationResult<T>.Conflict(ex.Message),
            InventoryException ex => ApplicationResult<T>.Conflict(ex.Message),
            AccountingException ex => ApplicationResult<T>.Conflict(ex.Message),
            DomainException ex => ApplicationResult<T>.Failure(ex.Message),
            _ => ApplicationResult<T>.Failure(exception.Message)
        };
}
