using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.ValueObjects;

public sealed record WeightInKg
{
    public decimal Value { get; }

    public WeightInKg(decimal value)
    {
        if (value <= 0)
            throw new ValidationException("Weight in kg must be greater than zero.");
        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public WeightInGrams ToGrams() => new(Value * 1000m);
}

public sealed record WeightInGrams
{
    public decimal Value { get; }

    public WeightInGrams(decimal value)
    {
        if (value <= 0)
            throw new ValidationException("Weight in grams must be greater than zero.");
        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }
}
