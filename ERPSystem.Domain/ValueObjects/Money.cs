using ERPSystem.Domain.Common;
using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.ValueObjects;

public sealed record Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = CurrencyDefaults.Code)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ValidationException("Currency is required.");
        if (amount < 0)
            throw new ValidationException("Money amount cannot be negative.");
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static Money Zero(string currency = CurrencyDefaults.Code) => new(0, currency);

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Currency mismatch.");
    }
}
