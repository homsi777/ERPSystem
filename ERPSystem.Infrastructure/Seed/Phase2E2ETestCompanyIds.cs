namespace ERPSystem.Infrastructure.Seed;

/// <summary>Fixed IDs for the isolated Phase 2 tax E2E test company. Must not overlap production GUIDs.</summary>
public static class Phase2E2ETestCompanyIds
{
    public const string CompanyCode = "E2E-TAX-TEST";
    public const string CompanyNameEn = "ERP PRO TAX E2E TEST COMPANY";

    public static readonly Guid CompanyId = Guid.Parse("e2e00001-0001-0001-0001-000000000001");
    public static readonly Guid BranchId = Guid.Parse("e2e00002-0002-0002-0002-000000000002");
    public static readonly Guid WarehouseId = Guid.Parse("e2e00003-0003-0003-0003-000000000003");
    public static readonly Guid CustomerId = Guid.Parse("e2e00004-0004-0004-0004-000000000004");
    public static readonly Guid SupplierId = Guid.Parse("e2e00005-0005-0005-0005-000000000005");
    public static readonly Guid ContainerId = Guid.Parse("e2e00006-0006-0006-0006-000000000006");
    public static readonly Guid ContainerItemId = Guid.Parse("e2e00007-0007-0007-0007-000000000007");
    public static readonly Guid FabricCategoryId = Guid.Parse("e2e00008-0008-0008-0008-000000000008");
    public static readonly Guid FabricItemId = Guid.Parse("e2e00009-0009-0009-0009-000000000009");
    public static readonly Guid FabricColorId = Guid.Parse("e2e0000a-000a-000a-000a-00000000000a");

    public static readonly Guid RootAssets = Guid.Parse("e2e00010-0010-0010-0010-000000001010");
    public static readonly Guid RootLiabilities = Guid.Parse("e2e00011-0011-0011-0011-000000001011");
    public static readonly Guid RootRevenue = Guid.Parse("e2e00012-0012-0012-0012-000000001012");
    public static readonly Guid RootExpense = Guid.Parse("e2e00013-0013-0013-0013-000000001013");
    public static readonly Guid AccountsReceivable = Guid.Parse("e2e00014-0014-0014-0014-000000001014");
    public static readonly Guid SalesRevenue = Guid.Parse("e2e00015-0015-0015-0015-000000001015");
    public static readonly Guid SalesDiscounts = Guid.Parse("e2e00016-0016-0016-0016-000000001016");
    public static readonly Guid VatPayable = Guid.Parse("e2e00017-0017-0017-0017-000000001017");
    public static readonly Guid InventoryAsset = Guid.Parse("e2e00018-0018-0018-0018-000000001018");
    public static readonly Guid CostOfGoodsSold = Guid.Parse("e2e00019-0019-0019-0019-000000001019");
    public static readonly Guid RoundingDifference = Guid.Parse("e2e0001a-001a-001a-001a-00000000101a");
    public static readonly Guid CashTest = Guid.Parse("e2e0001b-001b-001b-001b-00000000101b");
    public static readonly Guid AccountsPayable = Guid.Parse("e2e0001c-001c-001c-001c-00000000101c");

    public static readonly Guid PostingProfileId = Guid.Parse("e2e00020-0020-0020-0020-000000002020");
    public static readonly Guid Vat15Exclusive = Guid.Parse("e2e00021-0021-0021-0021-000000002021");
    public static readonly Guid Vat15Inclusive = Guid.Parse("e2e00022-0022-0022-0022-000000002022");
    public static readonly Guid ZeroRated = Guid.Parse("e2e00023-0023-0023-0023-000000002023");
    public static readonly Guid Exempt = Guid.Parse("e2e00024-0024-0024-0024-000000002024");

    public const decimal CostPerMeter = 6.00m;
    public const decimal DefaultRollMeters = 100m;
    public const int SeedRollCount = 40;
    public const decimal MinimumAvailableMetersPerRun = 2_000m;

    public static bool IsTestCompany(Guid companyId) => companyId == CompanyId;

    public static bool IsTestCompanyName(string? nameEn) =>
        nameEn?.Contains("TEST", StringComparison.OrdinalIgnoreCase) == true;
}
