using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Expenses;
using ERPSystem.Infrastructure.Persistence.Models.Identity;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Seed;

internal static class ExpenseModuleSeeder
{
    public static readonly Guid CapitalFactoryId = Guid.Parse("e1000001-0000-0000-0000-000000000001");
    public static readonly Guid CapitalBuildingId = Guid.Parse("e1000001-0000-0000-0000-000000000002");
    public static readonly Guid CapitalWarehouseId = Guid.Parse("e1000001-0000-0000-0000-000000000003");
    public static readonly Guid CapitalMachineryId = Guid.Parse("e1000001-0000-0000-0000-000000000004");
    public static readonly Guid PersonalSalaryId = Guid.Parse("e2000001-0000-0000-0000-000000000001");
    public static readonly Guid PersonalAllowanceId = Guid.Parse("e2000001-0000-0000-0000-000000000002");
    public static readonly Guid OperatingElectricityId = Guid.Parse("e3000001-0000-0000-0000-000000000001");
    public static readonly Guid OperatingRentId = Guid.Parse("e3000001-0000-0000-0000-000000000002");
    public static readonly Guid OperatingMaintenanceId = Guid.Parse("e3000001-0000-0000-0000-000000000003");
    public static readonly Guid OperatingOtherId = Guid.Parse("e3000001-0000-0000-0000-000000000099");

    public static readonly Guid CostCenterAdminId = Guid.Parse("f1000001-0000-0000-0000-000000000001");
    public static readonly Guid CostCenterSalesId = Guid.Parse("f1000001-0000-0000-0000-000000000002");
    public static readonly Guid CostCenterWarehouseId = Guid.Parse("f1000001-0000-0000-0000-000000000003");
    public static readonly Guid CostCenterManufacturingId = Guid.Parse("f1000001-0000-0000-0000-000000000004");

  public static async Task EnsureAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCategoriesAsync(context, companyId, cancellationToken);
        await EnsureCostCentersAsync(context, companyId, cancellationToken);
    }

    private static async Task EnsureCategoriesAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (await context.ExpenseCategories.AnyAsync(c => c.CompanyId == companyId, cancellationToken))
            return;

        context.ExpenseCategories.AddRange(
            Cat(CapitalFactoryId, companyId, ExpenseCategoryKind.Capital, "CAP-FACTORY", "إنشاء مصنع", "Factory construction"),
            Cat(CapitalBuildingId, companyId, ExpenseCategoryKind.Capital, "CAP-BUILDING", "إنشاء مبنى", "Building construction"),
            Cat(CapitalWarehouseId, companyId, ExpenseCategoryKind.Capital, "CAP-WAREHOUSE", "إنشاء مستودع", "Warehouse construction"),
            Cat(CapitalMachineryId, companyId, ExpenseCategoryKind.Capital, "CAP-MACHINERY", "تركيب آلات", "Machinery installation"),
            Cat(PersonalSalaryId, companyId, ExpenseCategoryKind.Personal, "PER-SALARY", "راتب إداري", "Manager salary"),
            Cat(PersonalAllowanceId, companyId, ExpenseCategoryKind.Personal, "PER-ALLOWANCE", "بدل شخصي شهري", "Personal allowance"),
            Cat(OperatingElectricityId, companyId, ExpenseCategoryKind.Operating, "OP-ELECTRICITY", "كهرباء", "Electricity"),
            Cat(OperatingRentId, companyId, ExpenseCategoryKind.Operating, "OP-RENT", "إيجار", "Rent"),
            Cat(OperatingMaintenanceId, companyId, ExpenseCategoryKind.Operating, "OP-MAINTENANCE", "صيانة", "Maintenance"),
            Cat(OperatingOtherId, companyId, ExpenseCategoryKind.Operating, "OP-OTHER", "مصاريف تشغيلية أخرى", "Other operating"));

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCostCentersAsync(
        ErpDbContext context,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (await context.CostCenters.AnyAsync(c => c.CompanyId == companyId, cancellationToken))
            return;

        context.CostCenters.AddRange(
            Cc(CostCenterAdminId, companyId, "CC-ADMIN", "الإدارة العامة", "مصاريف الإدارة والمكاتب"),
            Cc(CostCenterSalesId, companyId, "CC-SALES", "المبيعات", "مصاريف قسم المبيعات"),
            Cc(CostCenterWarehouseId, companyId, "CC-WH", "المستودعات", "مصاريف المستودعات واللوجستيات"),
            Cc(CostCenterManufacturingId, companyId, "CC-MFG", "التصنيع", "مصاريف الإنتاج والتصنيع"));

        await context.SaveChangesAsync(cancellationToken);
    }

    private static ERPSystem.Infrastructure.Persistence.Models.Finance.CostCenterEntity Cc(
        Guid id, Guid companyId, string code, string name, string? description) => new()
    {
        Id = id,
        CompanyId = companyId,
        Code = code,
        Name = name,
        Description = description,
        Status = 0,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static ExpenseCategoryEntity Cat(
        Guid id,
        Guid companyId,
        ExpenseCategoryKind kind,
        string code,
        string nameAr,
        string nameEn) => new()
    {
        Id = id,
        CompanyId = companyId,
        Kind = (int)kind,
        Code = code,
        NameAr = nameAr,
        NameEn = nameEn,
        IsSystem = true,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };
}
