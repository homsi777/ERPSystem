namespace ERPSystem.Application.Commands.Suppliers;

public sealed class CreateSupplierCommand
{
    public Guid CompanyId { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public int PaymentTermsDays { get; init; } = 30;
    public decimal CreditLimit { get; init; }
    public string? TaxNumber { get; init; }
    public Guid? PayablesAccountId { get; init; }
    public string? Notes { get; init; }
}

public sealed class UpdateSupplierCommand
{
    public Guid SupplierId { get; init; }
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Address { get; init; }
    public string? Country { get; init; }
    public string? City { get; init; }
    public string CurrencyCode { get; init; } = "USD";
    public int PaymentTermsDays { get; init; } = 30;
    public decimal CreditLimit { get; init; }
    public string? TaxNumber { get; init; }
    public Guid PayablesAccountId { get; init; }
    public string? Notes { get; init; }
}

public sealed class DeactivateSupplierCommand
{
    public Guid SupplierId { get; init; }
}

public sealed class PostSupplierOpeningBalanceCommand
{
    public Guid SupplierId { get; init; }
    public decimal Amount { get; init; }
    public DateTime PostingDate { get; init; }
    public string? ReferenceNote { get; init; }
}
