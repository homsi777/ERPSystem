namespace ERPSystem.Application.Common;

/// <summary>Well-known account IDs seeded in PostgreSQL for integrated posting.</summary>
public static class AccountingAccountIds
{
    public static readonly Guid InventoryAsset = Guid.Parse("a1000001-0001-0001-0001-000000000001");
    public static readonly Guid AccountsPayable = Guid.Parse("a1000002-0002-0002-0002-000000000002");
    public static readonly Guid CostOfGoodsSold = Guid.Parse("a1000003-0003-0003-0003-000000000003");
    public static readonly Guid SalesRevenue = Guid.Parse("a1000004-0004-0004-0004-000000000004");
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
}
