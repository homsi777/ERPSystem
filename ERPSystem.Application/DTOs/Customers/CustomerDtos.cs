using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Customers;

public sealed class CustomerListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerType Type { get; init; }
    public CustomerStatus Status { get; init; }
    public decimal Balance { get; init; }
    public decimal CreditLimit { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CustomerDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public CustomerType Type { get; init; }
    public CustomerStatus Status { get; init; }
    public decimal Balance { get; init; }
    public decimal CreditLimit { get; init; }
    public int PaymentTermsDays { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public bool IsActive { get; init; }
}

public sealed class CustomerStatementDto
{
    public Guid CustomerId { get; init; }
    public string CustomerName { get; init; } = "";
    public decimal OpeningBalance { get; init; }
    public decimal ClosingBalance { get; init; }
    public IReadOnlyList<CustomerStatementLineDto> Lines { get; init; } = [];
}

public sealed class CustomerStatementLineDto
{
    public DateTime EntryDate { get; init; }
    public DocumentType DocumentType { get; init; }
    public string DocumentNumber { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
}

public sealed class CustomerOperationsCenterDto
{
    public CustomerDetailsDto Customer { get; init; } = null!;
    public int OpenInvoicesCount { get; init; }
    public decimal TotalOutstanding { get; init; }
    public int PendingReceiptsCount { get; init; }
}
