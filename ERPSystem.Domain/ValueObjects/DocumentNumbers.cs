using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.ValueObjects;

public abstract record DocumentNumber
{
    public string Value { get; }

    protected DocumentNumber(string value, string prefix)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException($"{prefix} number is required.");
        Value = value.Trim().ToUpperInvariant();
    }

    public override string ToString() => Value;
}

public sealed record BranchCode : DocumentNumber
{
    public BranchCode(string value) : base(value, "Branch") { }
}

public sealed record ContainerNumber : DocumentNumber
{
    public ContainerNumber(string value) : base(value, "Container") { }
}

public sealed record InvoiceNumber : DocumentNumber
{
    public InvoiceNumber(string value) : base(value, "Invoice") { }
}

public sealed record RollNumber
{
    public int Value { get; }

    public RollNumber(int value)
    {
        if (value <= 0)
            throw new ValidationException("Roll number must be positive.");
        Value = value;
    }
}
