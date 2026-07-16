using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Identity;

public sealed class UserSessionStatusDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public string Username { get; init; } = "";
    public string FullNameAr { get; init; } = "";
    public UserSessionClientType ClientType { get; init; }
    public string ClientTypeDisplay { get; init; } = "";
    public string? DeviceInfo { get; init; }
    public string? IpAddress { get; init; }
    public DateTime LoginAt { get; init; }
    public DateTime? LogoutAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public bool IsActive { get; init; }
    public string StatusDisplay { get; init; } = "";
}

public sealed class StartUserSessionResult
{
    public Guid SessionId { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime ExpiresAt { get; init; }
}
