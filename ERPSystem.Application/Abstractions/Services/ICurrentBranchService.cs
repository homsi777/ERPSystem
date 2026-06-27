namespace ERPSystem.Application.Abstractions.Services;

public interface ICurrentBranchService
{
    Guid? CompanyId { get; }
    Guid? BranchId { get; }
}
