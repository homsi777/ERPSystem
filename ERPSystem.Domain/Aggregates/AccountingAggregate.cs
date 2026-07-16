using ERPSystem.Domain.Common;
using ERPSystem.Domain.Entities.Accounting;
using ERPSystem.Domain.Enums;
using ERPSystem.Domain.Events.Accounting;
using ERPSystem.Domain.Exceptions;
using ERPSystem.Domain.ValueObjects;

namespace ERPSystem.Domain.Aggregates;

public sealed class AccountingAggregate : AggregateRoot
{
    public string EntryNumber { get; private set; } = "";
    public DateTime EntryDate { get; private set; }
    public string Description { get; private set; } = "";
    public JournalEntryStatus Status { get; private set; }
    public DocumentType? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime? PostedAt { get; private set; }
    public Guid? PostedByUserId { get; private set; }
    public Guid? ReversalOfEntryId { get; private set; }
    public Guid? JournalBookId { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    private readonly List<JournalEntryLine> _lines = [];

    public IReadOnlyList<JournalEntryLine> Lines => _lines.AsReadOnly();

    public Money DebitTotal => _lines.Aggregate(Money.Zero(), (s, l) => s.Add(l.Debit));
    public Money CreditTotal => _lines.Aggregate(Money.Zero(), (s, l) => s.Add(l.Credit));

    private AccountingAggregate() { }

    public static AccountingAggregate CreateDraft(
        string entryNumber,
        DateTime entryDate,
        string description,
        Guid createdByUserId,
        DocumentType? sourceType = null,
        Guid? sourceId = null,
        Guid? journalBookId = null,
        Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        EntryNumber = entryNumber,
        EntryDate = entryDate,
        Description = description,
        CreatedByUserId = createdByUserId,
        SourceType = sourceType,
        SourceId = sourceId,
        JournalBookId = journalBookId,
        Status = JournalEntryStatus.Draft
    };

    public void AddLine(JournalEntryLine line)
    {
        EnsureDraft();
        _lines.Add(line);
    }

    public void Approve()
    {
        EnsureDraft();
        ValidateBalanced();
        Status = JournalEntryStatus.Approved;
    }

    public void Post(Guid userId)
    {
        if (Status is not (JournalEntryStatus.Draft or JournalEntryStatus.Approved))
            throw new AccountingException("Only draft or approved entries can be posted.");
        ValidateBalanced();
        Status = JournalEntryStatus.Posted;
        PostedAt = DateTime.UtcNow;
        PostedByUserId = userId;
        Raise(new JournalEntryPosted(Id, EntryNumber, DebitTotal.Amount));
    }

    public AccountingAggregate CreateReversal(string reversalEntryNumber, Guid userId)
    {
        if (Status != JournalEntryStatus.Posted)
            throw new AccountingException("Only posted entries can be reversed.");

        var reversal = CreateDraft(reversalEntryNumber, DateTime.UtcNow, $"Reversal of {EntryNumber}", userId,
            SourceType, SourceId, JournalBookId);
        reversal.ReversalOfEntryId = Id;
        foreach (var line in _lines)
            reversal.AddLine(JournalEntryLine.Create(
                line.AccountId,
                line.Credit,
                line.Debit,
                $"Reversal: {line.Narrative}",
                line.PartyId));
        reversal.Post(userId);
        return reversal;
    }

    public void KeepPostedAfterReversal()
    {
        if (Status == JournalEntryStatus.Reversed)
            Status = JournalEntryStatus.Posted;
    }

    public void Cancel()
    {
        if (Status == JournalEntryStatus.Posted)
            throw new AccountingException("Posted entries cannot be cancelled. Use reversal.");
        Status = JournalEntryStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    private void ValidateBalanced()
    {
        if (Math.Abs(DebitTotal.Amount - CreditTotal.Amount) > 0.01m)
            throw new AccountingException("Journal entry is not balanced.");
    }

    private void EnsureDraft()
    {
        if (Status != JournalEntryStatus.Draft)
            throw new AccountingException("Only draft entries can be modified.");
    }
}
