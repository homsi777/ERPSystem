namespace ERPSystem.Infrastructure.Persistence.Models.Identity;

public class UserEntity : PersistenceEntity
{
    public string Username { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public string FullNameEn { get; set; } = "";
    public string PasswordHash { get; set; } = "";
}

public class RoleEntity : PersistenceEntity
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsSystem { get; set; }
}

public class PermissionEntity : PersistenceEntity
{
    public string Code { get; set; } = "";
    public string Module { get; set; } = "";
    public string Action { get; set; } = "";
}

public class UserRoleEntity
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class RolePermissionEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}

public class UserSessionEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = "";
    public string FullNameAr { get; set; } = "";
    public int ClientType { get; set; }
    public string? RefreshTokenHash { get; set; }
    public string? DeviceInfo { get; set; }
    public string? IpAddress { get; set; }
    public DateTime LoginAt { get; set; }
    public DateTime? LogoutAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public string? RevokedReason { get; set; }
}
