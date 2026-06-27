using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Events.Accounting;

public sealed record JournalEntryPosted(Guid EntryId, string EntryNumber, decimal DebitTotal) : DomainEvent;
