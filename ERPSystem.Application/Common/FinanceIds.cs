namespace ERPSystem.Application.Common;

/// <summary>Well-known payment method IDs seeded in PostgreSQL.</summary>
public static class PaymentMethodIds
{
    public static readonly Guid Cash = Guid.Parse("f1000001-0001-0001-0001-000000000001");
    public static readonly Guid BankTransfer = Guid.Parse("f1000002-0002-0002-0002-000000000002");
    public static readonly Guid Card = Guid.Parse("f1000003-0003-0003-0003-000000000003");
    public static readonly Guid Cheque = Guid.Parse("f1000004-0004-0004-0004-000000000004");
    public static readonly Guid CustomerCredit = Guid.Parse("f1000005-0005-0005-0005-000000000005");
    public static readonly Guid Advance = Guid.Parse("f1000006-0006-0006-0006-000000000006");
    public static readonly Guid Other = Guid.Parse("f1000099-0099-0099-0099-000000000099");
}

/// <summary>Customer advances liability account for unallocated receipt amounts (Phase 3 design hook).</summary>
public static class FinanceAccountIds
{
    public static readonly Guid CustomerAdvances = Guid.Parse("a1000014-0014-0014-0014-000000001014");
}
