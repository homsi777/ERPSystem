namespace ERPSystem.Application.Common;

/// <summary>Seeded journal book IDs (Odoo account.journal).</summary>
public static class JournalBookIds
{
    public static readonly Guid General = Guid.Parse("c1000001-0001-0001-0001-000000000001");
    public static readonly Guid Bank = Guid.Parse("c1000002-0002-0002-0002-000000000002");
    public static readonly Guid Sales = Guid.Parse("c1000003-0003-0003-0003-000000000003");
    public static readonly Guid Purchase = Guid.Parse("c1000004-0004-0004-0004-000000000004");
    public static readonly Guid Cash = Guid.Parse("c1000005-0005-0005-0005-000000000005");
}
