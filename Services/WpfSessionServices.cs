using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Infrastructure.Seed;

namespace ERPSystem.Services;

public sealed class WpfCurrentUserService : ICurrentUserService
{
    public Guid? UserId => DatabaseSeeder.AdminUserId;
    public string? Username => "admin";
    public bool IsAuthenticated => true;
}

public sealed class WpfCurrentBranchService : ICurrentBranchService
{
    public Guid? CompanyId => DatabaseSeeder.DefaultCompanyId;
    public Guid? BranchId => DatabaseSeeder.DefaultBranchId;
}
