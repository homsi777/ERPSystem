using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.ValueObjects;

public sealed record Percentage
{
    public decimal Value { get; }

    public Percentage(decimal value)
    {
        if (value is < 0 or > 100)
            throw new ValidationException("Percentage must be between 0 and 100.");
        Value = decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    }
}
