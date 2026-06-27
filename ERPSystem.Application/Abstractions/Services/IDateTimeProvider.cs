namespace ERPSystem.Application.Abstractions.Services;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
    DateTime Today { get; }
}
