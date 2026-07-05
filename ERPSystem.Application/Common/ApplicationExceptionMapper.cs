using ERPSystem.Application.Results;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Application.Common;

public static class ApplicationExceptionMapper
{
    public static ApplicationResult ToFailureResult(this Exception exception) =>
        exception switch
        {
            ValidationException ex => ApplicationResult.ValidationFailed("_", ex.Message),
            CreditLimitExceededException ex => ApplicationResult.Conflict(
                $"تجاوز حد الائتمان — الحد: {ex.Limit:N2} $ | الرصيد المتوقع بعد الفاتورة: {ex.ProjectedBalance:N2} $"),
            InvalidInvoiceWorkflowException ex => ApplicationResult.Conflict(ex.Message),
            ContainerApprovalException ex => ApplicationResult.Conflict(ex.Message),
            WarehouseDetailingException ex => ApplicationResult.Conflict(ex.Message),
            InventoryException ex => ApplicationResult.Conflict(ex.Message),
            AccountingException ex => ApplicationResult.Conflict(ex.Message),
            ExpensePaymentException ex => ApplicationResult.Conflict(ex.Message),
            DomainException ex => ApplicationResult.Failure(ex.Message),
            _ => ApplicationResult.Failure(ResolveUserMessage(exception))
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
            ExpensePaymentException ex => ApplicationResult<T>.Conflict(ex.Message),
            DomainException ex => ApplicationResult<T>.Failure(ex.Message),
            _ => ApplicationResult<T>.Failure(ResolveUserMessage(exception))
        };

    private static string ResolveUserMessage(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var message = current.Message;
            if (string.IsNullOrWhiteSpace(message))
                continue;

            if (IsDuplicateContainerNumber(message))
            {
                return "رقم الحاوية مستخدم مسبقاً لنفس الشركة. غيّر رقم الحاوية في الخطوة 1، أو افتح الحاوية الموجودة من قائمة الحاويات للمتابعة.";
            }

            if (message.Contains("column", StringComparison.OrdinalIgnoreCase) &&
                message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                return "قاعدة البيانات غير محدّثة. أعد تشغيل البرنامج ليتم تطبيق التحديثات، ثم حاول مرة أخرى.";
            }

            if (message.Contains("DateTime with Kind=Local", StringComparison.OrdinalIgnoreCase)
                || message.Contains("only UTC is supported", StringComparison.OrdinalIgnoreCase))
            {
                return "تعذّر حفظ التاريخ. أعد تشغيل البرنامج بعد التحديث ثم حاول مرة أخرى.";
            }

            if (message.Contains("expected to affect 1 row", StringComparison.OrdinalIgnoreCase)
                || message.Contains("optimistic concurrency", StringComparison.OrdinalIgnoreCase))
            {
                return "تعذّر حفظ البيانات بسبب تعارض في قاعدة البيانات. أعد تشغيل البرنامج بعد التحديث ثم حاول مرة أخرى.";
            }

            if (!IsGenericEntityFrameworkMessage(message))
                return message;
        }

        return exception.Message;
    }

    private static bool IsDuplicateContainerNumber(string message) =>
        message.Contains("IX_containers_CompanyId_ContainerNumber", StringComparison.OrdinalIgnoreCase)
        || (message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            && message.Contains("ContainerNumber", StringComparison.OrdinalIgnoreCase));

    private static bool IsGenericEntityFrameworkMessage(string message) =>
        message.StartsWith("An error occurred while saving the entity changes", StringComparison.OrdinalIgnoreCase)
        || message.StartsWith("See the inner exception for details", StringComparison.OrdinalIgnoreCase);
}
