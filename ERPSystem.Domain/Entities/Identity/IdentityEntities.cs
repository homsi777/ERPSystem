namespace ERPSystem.Domain.Entities.Identity;

public class Company
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public string DefaultCurrency { get; private set; } = "SAR";
    public bool IsActive { get; private set; } = true;

    private Company() { }

    public static Company Create(string code, string nameAr, string nameEn) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn
    };
}

public class Branch
{
    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public string Code { get; private set; } = "";
    public string NameAr { get; private set; } = "";
    public string NameEn { get; private set; } = "";
    public bool IsActive { get; private set; } = true;

    private Branch() { }

    public static Branch Create(Guid companyId, string code, string nameAr) => new()
    {
        Id = Guid.NewGuid(),
        CompanyId = companyId,
        Code = code,
        NameAr = nameAr,
        NameEn = nameAr
    };
}

public class User
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = "";
    public string FullNameAr { get; private set; } = "";
    public string FullNameEn { get; private set; } = "";
    public bool IsActive { get; private set; } = true;

    private User() { }

    public static User Create(string username, string fullNameAr) => new()
    {
        Id = Guid.NewGuid(),
        Username = username,
        FullNameAr = fullNameAr,
        FullNameEn = fullNameAr
    };
}

public class Role
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = "";
    public string Description { get; private set; } = "";
    public bool IsSystem { get; private set; }

    private Role() { }

    public static Role Create(string name, string description, bool isSystem = false) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = description,
        IsSystem = isSystem
    };
}

public class Permission
{
    public Guid Id { get; private set; }
    public string Code { get; private set; } = "";
    public string Module { get; private set; } = "";
    public string Action { get; private set; } = "";

    private Permission() { }

    public static Permission Create(string code, string module, string action) => new()
    {
        Id = Guid.NewGuid(),
        Code = code,
        Module = module,
        Action = action
    };
}
