using ERPSystem.Domain.Exceptions;

namespace ERPSystem.Domain.ValueObjects;

public sealed record PhoneNumber
{
    public string Value { get; }

    public PhoneNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("Phone number is required.");
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length < 8)
            throw new ValidationException("Phone number is invalid.");
        Value = digits;
    }
}

public sealed record EmailAddress
{
    public string Value { get; }

    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.Contains('@'))
            throw new ValidationException("Email address is invalid.");
        Value = value.Trim().ToLowerInvariant();
    }
}

public sealed record Address
{
    public string Line1 { get; }
    public string? Line2 { get; }
    public string City { get; }
    public string? Region { get; }

    public Address(string line1, string city, string? line2 = null, string? region = null)
    {
        if (string.IsNullOrWhiteSpace(line1))
            throw new ValidationException("Address line is required.");
        if (string.IsNullOrWhiteSpace(city))
            throw new ValidationException("City is required.");
        Line1 = line1.Trim();
        Line2 = line2?.Trim();
        City = city.Trim();
        Region = region?.Trim();
    }
}
