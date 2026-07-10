namespace ERPSystem.Infrastructure.Seed;

/// <summary>Fixed IDs for the isolated Phase 3 finance E2E test company.</summary>
public static class Phase3FinanceE2ETestCompanyIds
{
    public const string CompanyCode = "E2E-FIN-TEST";
    public const string CompanyNameEn = "ERP PRO FINANCE E2E TEST COMPANY";

    public static readonly Guid CompanyId = Guid.Parse("e3f00001-0001-0001-0001-000000000001");
    public static readonly Guid BranchId = Guid.Parse("e3f00002-0002-0002-0002-000000000002");
    public static readonly Guid WarehouseId = Guid.Parse("e3f00003-0003-0003-0003-000000000003");
    public static readonly Guid CustomerId = Guid.Parse("e3f00004-0004-0004-0004-000000000004");
    public static readonly Guid CashCustomerId = Guid.Parse("e3f00005-0005-0005-0005-000000000005");
    public static readonly Guid SupplierId = Guid.Parse("e3f00006-0006-0006-0006-000000000006");
    public static readonly Guid ContainerId = Guid.Parse("e3f00007-0007-0007-0007-000000000007");
    public static readonly Guid ContainerItemId = Guid.Parse("e3f00008-0008-0008-0008-000000000008");
    public static readonly Guid FabricCategoryId = Guid.Parse("e3f00009-0009-0009-0009-000000000009");
    public static readonly Guid FabricItemId = Guid.Parse("e3f0000a-000a-000a-000a-00000000000a");
    public static readonly Guid FabricColorId = Guid.Parse("e3f0000b-000b-000b-000b-00000000000b");

    public static readonly Guid RootAssets = Guid.Parse("e3f00010-0010-0010-0010-000000001010");
    public static readonly Guid RootLiabilities = Guid.Parse("e3f00011-0011-0011-0011-000000001011");
    public static readonly Guid RootRevenue = Guid.Parse("e3f00012-0012-0012-0012-000000001012");
    public static readonly Guid RootExpense = Guid.Parse("e3f00013-0013-0013-0013-000000001013");
    public static readonly Guid AccountsReceivable = Guid.Parse("e3f00014-0014-0014-0014-000000001014");
    public static readonly Guid SalesRevenue = Guid.Parse("e3f00015-0015-0015-0015-000000001015");
    public static readonly Guid VatPayable = Guid.Parse("e3f00017-0017-0017-0017-000000001017");
    public static readonly Guid InventoryAsset = Guid.Parse("e3f00018-0018-0018-0018-000000001018");
    public static readonly Guid CostOfGoodsSold = Guid.Parse("e3f00019-0019-0019-0019-000000001019");
    public static readonly Guid CashAccountA = Guid.Parse("e3f0001a-001a-001a-001a-00000000101a");
    public static readonly Guid CashAccountB = Guid.Parse("e3f0001b-001b-001b-001b-00000000101b");
    public static readonly Guid BankGlAccount = Guid.Parse("e3f0001c-001c-001c-001c-00000000101c");
    public static readonly Guid CustomerAdvances = Guid.Parse("e3f0001d-001d-001d-001d-00000000101d");
    public static readonly Guid RoundingDifference = Guid.Parse("e3f0001e-001e-001e-001e-00000000101e");

    public static readonly Guid PostingProfileId = Guid.Parse("e3f00020-0020-0020-0020-000000002020");
    public static readonly Guid Vat15Exclusive = Guid.Parse("e3f00021-0021-0021-0021-000000002021");

    public static readonly Guid CashboxA = Guid.Parse("e3f00030-0030-0030-0030-000000003030");
    public static readonly Guid CashboxB = Guid.Parse("e3f00031-0031-0031-0031-000000003031");
    public static readonly Guid CashboxNoAccount = Guid.Parse("e3f00032-0032-0032-0032-000000003032");
    public static readonly Guid CashboxInactive = Guid.Parse("e3f00033-0033-0033-0033-000000003033");

    public static readonly Guid BankAccount = Guid.Parse("e3f00040-0040-0040-0040-000000004040");

    public static readonly Guid CreditInvoiceId = Guid.Parse("e3f00050-0050-0050-0050-000000005050");

    public const decimal CreditInvoiceTotal = 1000m;
    public const decimal DefaultRollMeters = 50m;
    public const int SeedRollCount = 20;
    public const decimal CostPerMeter = 5m;

    public static bool IsTestCompany(Guid companyId) => companyId == CompanyId;

    public static bool IsTestCompanyName(string? nameEn) =>
        nameEn?.Contains("TEST", StringComparison.OrdinalIgnoreCase) == true;
}
