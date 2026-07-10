using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.Entities.Sales;

/// <summary>Configurable sales tax code for a company.</summary>
public sealed class TaxCode
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public decimal Rate { get; private set; }
    public TaxPriceMode PriceMode { get; private set; }
    public TaxCategory Category { get; private set; }
    public Guid? SalesTaxAccountId { get; private set; }
    public DateTime EffectiveFrom { get; private set; }
    public DateTime? EffectiveTo { get; private set; }
    public bool IsActive { get; private set; }

    private TaxCode() { }

    public static TaxCode Create(
        Guid companyId,
        string code,
        string name,
        decimal rate,
        TaxPriceMode priceMode,
        TaxCategory category,
        Guid? salesTaxAccountId,
        DateTime effectiveFrom,
        DateTime? effectiveTo = null)
    {
        var taxCode = CreateCore(
            Guid.NewGuid(),
            companyId,
            code,
            name,
            rate,
            priceMode,
            category,
            salesTaxAccountId,
            effectiveFrom,
            effectiveTo,
            isActive: true);
        return taxCode;
    }

    public static TaxCode FromPersistence(
        Guid id,
        Guid companyId,
        string code,
        string name,
        decimal rate,
        TaxPriceMode priceMode,
        TaxCategory category,
        Guid? salesTaxAccountId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        bool isActive) =>
        CreateCore(id, companyId, code, name, rate, priceMode, category, salesTaxAccountId, effectiveFrom, effectiveTo, isActive);

    private static TaxCode CreateCore(
        Guid id,
        Guid companyId,
        string code,
        string name,
        decimal rate,
        TaxPriceMode priceMode,
        TaxCategory category,
        Guid? salesTaxAccountId,
        DateTime effectiveFrom,
        DateTime? effectiveTo,
        bool isActive)
    {
        if (companyId == Guid.Empty)
            throw new ValidationException("Company is required.");
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidationException("Tax code is required.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("Tax name is required.");
        if (rate < 0 || rate > 1)
            throw new ValidationException("Tax rate must be between 0 and 1.");
        if (category == TaxCategory.Standard && rate <= 0)
            throw new ValidationException("Standard tax codes require a positive rate.");
        if (category is TaxCategory.Standard or TaxCategory.ZeroRated
            && (salesTaxAccountId is null || salesTaxAccountId == Guid.Empty))
            throw new ValidationException("Taxable codes require a sales tax GL account.");

        return new TaxCode
        {
            Id = id,
            CompanyId = companyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Rate = rate,
            PriceMode = priceMode,
            Category = category,
            SalesTaxAccountId = salesTaxAccountId,
            EffectiveFrom = effectiveFrom.Date,
            EffectiveTo = effectiveTo?.Date,
            IsActive = isActive
        };
    }

    public bool IsEffectiveOn(DateTime date)
    {
        var d = date.Date;
        if (!IsActive)
            return false;
        if (d < EffectiveFrom.Date)
            return false;
        if (EffectiveTo is not null && d > EffectiveTo.Value.Date)
            return false;
        return true;
    }

    public decimal EffectiveRate() =>
        Category == TaxCategory.Exempt ? 0m : Rate;
}
