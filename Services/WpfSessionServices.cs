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
    private static Guid? _selectedBranchId;

    public Guid? CompanyId => DatabaseSeeder.DefaultCompanyId;
    public Guid? BranchId => _selectedBranchId ?? DatabaseSeeder.DefaultBranchId;

    /// <summary>Updates the active branch for the current session (branch selector).</summary>
    public static void SelectBranch(Guid branchId) => _selectedBranchId = branchId;
}
