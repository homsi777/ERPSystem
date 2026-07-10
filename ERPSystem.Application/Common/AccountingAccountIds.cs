namespace ERPSystem.Application.Common;

/// <summary>Well-known account IDs seeded in PostgreSQL for integrated posting.</summary>
public static class AccountingAccountIds
{
    public static readonly Guid InventoryAsset = Guid.Parse("a1000001-0001-0001-0001-000000000001");
    public static readonly Guid AccountsPayable = Guid.Parse("a1000002-0002-0002-0002-000000000002");
    public static readonly Guid CostOfGoodsSold = Guid.Parse("a1000003-0003-0003-0003-000000000003");
    public static readonly Guid SalesRevenue = Guid.Parse("a1000004-0004-0004-0004-000000000004");
    public static readonly Guid SalesDiscounts = Guid.Parse("a1000011-0011-0011-0011-000000000011");
    public static readonly Guid AccountsReceivable = Guid.Parse("a1000005-0005-0005-0005-000000000005");
    public static readonly Guid LandingCostClearing = Guid.Parse("a1000006-0006-0006-0006-000000000006");
    public static readonly Guid CashUsd = Guid.Parse("a1000007-0007-0007-0007-000000000007");
    public static readonly Guid OperatingExpenses = Guid.Parse("a1000008-0008-0008-0008-000000000008");
    public static readonly Guid RootAssets = Guid.Parse("b1000001-0001-0001-0001-000000000001");
    public static readonly Guid RootLiabilities = Guid.Parse("b1000002-0002-0002-0002-000000000002");
    public static readonly Guid RootEquity = Guid.Parse("b1000003-0003-0003-0003-000000000003");
    public static readonly Guid RootRevenue = Guid.Parse("b1000004-0004-0004-0004-000000000004");
    public static readonly Guid RootExpense = Guid.Parse("b1000005-0005-0005-0005-000000000005");
    public static readonly Guid OpeningBalanceEquity = Guid.Parse("a1000009-0009-0009-0009-000000000009");
    public static readonly Guid PartnerCapital = Guid.Parse("a1000010-0010-0010-0010-000000000010");
    public static readonly Guid VatPayable = Guid.Parse("a1000012-0012-0012-0012-000000001012");
    public static readonly Guid RoundingDifference = Guid.Parse("a1000013-0013-0013-0013-000000001013");
}

public static class SalesTaxCodeIds
{
    public static readonly Guid DefaultVat15Exclusive = Guid.Parse("c1000002-0002-0002-0002-000000000002");
}

public static class SalesPostingProfileIds
{
    public static readonly Guid Default = Guid.Parse("d1000001-0001-0001-0001-000000000001");
}
