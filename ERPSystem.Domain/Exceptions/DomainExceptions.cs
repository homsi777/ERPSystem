namespace ERPSystem.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}

public sealed class ValidationException : DomainException
{
    public ValidationException(string message) : base(message) { }
}

public sealed class CreditLimitExceededException : DomainException
{
    public CreditLimitExceededException(decimal limit, decimal projectedBalance)
        : base($"Credit limit exceeded. Limit: {limit:N2}, projected balance: {projectedBalance:N2}.") { }
}

public sealed class InvalidInvoiceWorkflowException : DomainException
{
    public InvalidInvoiceWorkflowException(string message) : base(message) { }
}

public sealed class ContainerApprovalException : DomainException
{
    public ContainerApprovalException(string message) : base(message) { }
}

public sealed class WarehouseDetailingException : DomainException
{
    public WarehouseDetailingException(string message) : base(message) { }
}

public sealed class InventoryException : DomainException
{
    public InventoryException(string message) : base(message) { }
}

public sealed class AccountingException : DomainException
{
    public AccountingException(string message) : base(message) { }
}
