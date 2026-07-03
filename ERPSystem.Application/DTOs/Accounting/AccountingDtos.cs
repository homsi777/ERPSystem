using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.DTOs.Accounting;

public sealed class AccountListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public GlAccountType AccountType { get; init; }
    public string AccountTypeDisplay { get; init; } = "";
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public bool IsPostable { get; init; }
    public bool IsActive { get; init; }
    public int ChildCount { get; init; }
    public int Level { get; init; }
}

public sealed class AccountDetailsDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string NameEn { get; init; } = "";
    public GlAccountType AccountType { get; init; }
    public string AccountTypeDisplay { get; init; } = "";
    public Guid? ParentId { get; init; }
    public string? ParentName { get; init; }
    public bool IsPostable { get; init; }
    public bool IsActive { get; init; }
    public IReadOnlyList<AccountListDto> Children { get; init; } = [];
}

public sealed class JournalEntryListDto
{
    public Guid Id { get; init; }
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public JournalEntryStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public int LineCount { get; init; }
    public DocumentType? SourceType { get; init; }
    public string? SourceTypeDisplay { get; init; }
}

public sealed class JournalEntryDetailsDto
{
    public Guid Id { get; init; }
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public JournalEntryStatus Status { get; init; }
    public string StatusDisplay { get; init; } = "";
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public DocumentType? SourceType { get; init; }
    public string? SourceTypeDisplay { get; init; }
    public Guid? SourceId { get; init; }
    public DateTime? PostedAt { get; init; }
    public IReadOnlyList<JournalEntryLineDetailsDto> Lines { get; init; } = [];
}

public sealed class JournalEntryLineDetailsDto
{
    public Guid Id { get; init; }
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = "";
    public string AccountName { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string Narrative { get; init; } = "";
}

public sealed class AccountLookupDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public string Display => $"{Code} — {NameAr}";
}

public sealed class JournalBookListDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = "";
    public string NameAr { get; init; } = "";
    public JournalBookType BookType { get; init; }
    public string BookTypeDisplay { get; init; } = "";
}

public sealed class TrialBalanceLineDto
{
    public Guid AccountId { get; init; }
    public string AccountCode { get; init; } = "";
    public string AccountName { get; init; } = "";
    public string AccountTypeDisplay { get; init; } = "";
    public decimal DebitTotal { get; init; }
    public decimal CreditTotal { get; init; }
    public decimal Balance { get; init; }
}

public sealed class AccountLedgerLineDto
{
    public Guid JournalEntryId { get; init; }
    public string EntryNumber { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public string Description { get; init; } = "";
    public string LineNarrative { get; init; } = "";
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public decimal RunningBalance { get; init; }
}
