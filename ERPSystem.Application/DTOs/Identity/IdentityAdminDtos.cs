namespace ERPSystem.Application.DTOs.Identity;

public sealed class IdentityUserListDto
{
    public Guid Id { get; init; }
    public string Username { get; init; } = "";
    public string FullNameAr { get; init; } = "";
    public string FullNameEn { get; init; } = "";
    public bool IsActive { get; init; }
    public IReadOnlyList<string> RoleNames { get; init; } = [];
}

public sealed class IdentityRoleListDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = "";
    public string Description { get; init; } = "";
    public bool IsSystem { get; init; }
    public int PermissionCount { get; init; }
}

public sealed class PermissionTreeItemDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string LabelAr { get; init; } = "";
}

public sealed class PermissionModuleGroupDto
{
    public string ModuleKey { get; init; } = "";
    public string ModuleLabelAr { get; init; } = "";
    public IReadOnlyList<PermissionTreeItemDto> Permissions { get; init; } = [];
}

public sealed class RolePermissionsDto
{
    public Guid RoleId { get; init; }
    public string RoleName { get; init; } = "";
    public bool IsSystem { get; init; }
    public IReadOnlyList<string> PermissionCodes { get; init; } = [];
}
