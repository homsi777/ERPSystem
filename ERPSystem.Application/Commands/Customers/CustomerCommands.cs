using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Customers;

public sealed class CreateCustomerCommand
{
    public Guid CompanyId { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerType Type { get; init; }
    public decimal CreditLimit { get; init; }
    public bool CreditLimitEnabled { get; init; }
}

public sealed class UpdateCustomerCommand
{
    public Guid CustomerId { get; init; }
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public decimal CreditLimit { get; init; }
    public bool CreditLimitEnabled { get; init; }
    public int PaymentTermsDays { get; init; }
}

public sealed class DeactivateCustomerCommand
{
    public Guid CustomerId { get; init; }
}

public sealed class PostCustomerOpeningBalanceCommand
{
    public Guid CustomerId { get; init; }
    public decimal Amount { get; init; }
    public DateTime PostingDate { get; init; }
    public string? ReferenceNote { get; init; }
}
