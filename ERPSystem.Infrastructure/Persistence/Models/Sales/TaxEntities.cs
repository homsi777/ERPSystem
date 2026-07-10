namespace ERPSystem.Infrastructure.Persistence.Models.Sales;

public class TaxCodeEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Rate { get; set; }
    public int PriceMode { get; set; }
    public int Category { get; set; }
    public Guid? SalesTaxAccountId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}

public class SalesPostingProfileEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid AccountsReceivableAccountId { get; set; }
    public Guid SalesRevenueAccountId { get; set; }
    public Guid SalesDiscountAccountId { get; set; }
    public Guid? VatPayableAccountId { get; set; }
    public Guid InventoryAccountId { get; set; }
    public Guid CogsAccountId { get; set; }
    public Guid? RoundingAccountId { get; set; }
}

public class SalesInvoiceItemTaxEntity : PersistenceEntity
{
    public Guid SalesInvoiceId { get; set; }
    public Guid SalesInvoiceItemId { get; set; }
    public Guid? TaxCodeId { get; set; }
    public string? TaxCode { get; set; }
    public string? TaxName { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsInclusive { get; set; }
    public Guid? SalesTaxAccountId { get; set; }
    public bool IsFrozen { get; set; }
}

public class SalesReturnLineTaxEntity : PersistenceEntity
{
    public Guid SalesReturnId { get; set; }
    public Guid SalesReturnLineId { get; set; }
    public Guid? TaxCodeId { get; set; }
    public string? TaxCode { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid? SalesTaxAccountId { get; set; }
}
