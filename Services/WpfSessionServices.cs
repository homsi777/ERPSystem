using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Infrastructure.Seed;

namespace ERPSystem.Services;

public sealed class WpfCurrentUserService : ICurrentUserService
{
    private Guid? _userId;
    private string? _username;
    private string? _fullNameAr;
    private IReadOnlyList<string> _permissions = [];

    public Guid? UserId => _userId;
    public string? Username => _username;
    public string? FullNameAr => _fullNameAr;
    public IReadOnlyList<string> Permissions => _permissions;
    public bool IsAuthenticated => _userId.HasValue;

    public void SetSession(
        Guid userId,
        string username,
        string fullNameAr,
        IReadOnlyList<string> permissions)
    {
        _userId = userId;
        _username = username;
        _fullNameAr = fullNameAr;
        _permissions = permissions;
    }

    public void ClearSession()
    {
        _userId = null;
        _username = null;
        _fullNameAr = null;
        _permissions = [];
    }
}

public sealed class WpfCurrentBranchService : ICurrentBranchService
{
    private static Guid? _selectedBranchId;

    public Guid? CompanyId => DatabaseSeeder.DefaultCompanyId;
    public Guid? BranchId => _selectedBranchId ?? DatabaseSeeder.DefaultBranchId;

    /// <summary>Updates the active branch for the current session (branch selector).</summary>
    public static void SelectBranch(Guid branchId) => _selectedBranchId = branchId;
}
