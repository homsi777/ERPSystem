namespace ERPSystem.Domain.ValueObjects;

public sealed record AuditInfo
{
    public Guid UserId { get; }
    public string UserName { get; }
    public DateTime Timestamp { get; }

    public AuditInfo(Guid userId, string userName, DateTime timestamp)
    {
        UserId = userId;
        UserName = userName;
        Timestamp = timestamp;
    }

    public static AuditInfo Create(Guid userId, string userName) =>
        new(userId, userName, DateTime.UtcNow);

    public static AuditInfo CreateSystem() =>
        new(Guid.Empty, "System", DateTime.UtcNow);
}

public sealed record DateRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public DateRange(DateTime start, DateTime end)
    {
        if (end < start)
            throw new Exceptions.ValidationException("Date range end must be on or after start.");
        Start = start.Date;
        End = end.Date;
    }

    public bool Contains(DateTime date) => date.Date >= Start && date.Date <= End;
}
