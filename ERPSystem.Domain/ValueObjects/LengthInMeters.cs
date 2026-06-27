using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.ValueObjects;

public sealed record LengthInMeters
{
    public decimal Value { get; }

    public LengthInMeters(decimal value, bool allowZero = false)
    {
        if (!allowZero && value <= 0)
            throw new ValidationException("Length in meters must be greater than zero.");
        if (value < 0)
            throw new ValidationException("Length in meters cannot be negative.");
        Value = decimal.Round(value, 4, MidpointRounding.AwayFromZero);
    }

    public static LengthInMeters Zero { get; } = new(0, allowZero: true);

    public static LengthInMeters? TryCreate(decimal value) =>
        value > 0 ? new LengthInMeters(value) : null;

    public LengthInMeters Add(LengthInMeters other) =>
        new(Math.Max(0, Value + other.Value), allowZero: true);

    public LengthInMeters Subtract(LengthInMeters other) =>
        new(Math.Max(0, Value - other.Value), allowZero: true);
}
