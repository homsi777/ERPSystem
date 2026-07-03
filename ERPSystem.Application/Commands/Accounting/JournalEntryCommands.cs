using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Commands.Accounting;

public sealed class CreateJournalEntryCommand
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? JournalBookId { get; set; }
    public string Description { get; init; } = "";
    public DateTime EntryDate { get; init; }
    public DocumentType? SourceType { get; init; }
    public Guid? SourceId { get; init; }
    public IReadOnlyList<JournalEntryLineCommand> Lines { get; init; } = [];
}

public sealed class JournalEntryLineCommand
{
    public Guid AccountId { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public string Narrative { get; init; } = "";
    public Guid? PartyId { get; init; }
}

public sealed class ApproveJournalEntryCommand
{
    public Guid EntryId { get; init; }
}

public sealed class PostJournalEntryCommand
{
    public Guid EntryId { get; init; }
}

public sealed class ReverseJournalEntryCommand
{
    public Guid EntryId { get; init; }
}

public sealed class CancelJournalEntryCommand
{
    public Guid EntryId { get; init; }
}
