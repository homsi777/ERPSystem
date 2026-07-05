using ERPSystem.Application.Abstractions.Services;

namespace ERPSystem.Api.Services;

internal sealed class ApiCurrentUserService : ICurrentUserService
{
    public Guid? UserId => null;
    public string? Username => null;
    public bool IsAuthenticated => false;
}

internal sealed class ApiCurrentBranchService : ICurrentBranchService
{
    public Guid? CompanyId => null;
    public Guid? BranchId => null;
}
