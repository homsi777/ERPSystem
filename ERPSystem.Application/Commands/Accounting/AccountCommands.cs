using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Accounting;

public sealed class CreateAccountCommand
{
    public Guid CompanyId { get; set; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public GlAccountType AccountType { get; init; }
    public Guid? ParentId { get; init; }
    public bool IsPostable { get; init; } = true;
}

public sealed class UpdateAccountCommand
{
    public Guid AccountId { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public GlAccountType AccountType { get; init; }
    public Guid? ParentId { get; init; }
    public bool IsPostable { get; init; } = true;
}

public sealed class DeactivateAccountCommand
{
    public Guid AccountId { get; init; }
}
