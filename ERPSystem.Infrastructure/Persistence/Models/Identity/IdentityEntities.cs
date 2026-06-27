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
