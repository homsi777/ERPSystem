namespace ERPSystem.Domain.Enums;

/// <summary>How unit prices on invoice lines are interpreted for tax.</summary>
public enum TaxPriceMode
{
    Exclusive = 0,
    Inclusive = 1
}

/// <summary>Tax classification — affects reporting, not always the numeric rate.</summary>
public enum TaxCategory
{
    Standard = 0,
    ZeroRated = 1,
    Exempt = 2
}
