namespace ERPSystem.Infrastructure.Persistence.Models.Accounting;

public class AccountEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string AccountType { get; set; } = "";
    public Guid? ParentId { get; set; }
    public bool IsPostable { get; set; } = true;
}

public class JournalEntryEntity : CancellablePersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? JournalBookId { get; set; }
    public string EntryNumber { get; set; } = "";
    public DateTime EntryDate { get; set; }
    public string Description { get; set; } = "";
    public int Status { get; set; }
    public int? SourceType { get; set; }
    public Guid? SourceId { get; set; }
    public int? PostingKind { get; set; }
    public int PostingIdentityVersion { get; set; } = 1;
    public string? IdempotencyKey { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime? PostedAt { get; set; }
    public Guid? PostedByUserId { get; set; }
    public Guid? ReversalOfEntryId { get; set; }
}

public class JournalEntryLineEntity : PersistenceEntity
{
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Narrative { get; set; } = "";
    public Guid? PartyId { get; set; }
}

public class JournalBookEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = "";
    public string NameAr { get; set; } = "";
    public string NameEn { get; set; } = "";
    public int BookType { get; set; }
}

public class AccountingPostingAttemptEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public int SourceType { get; set; }
    public Guid SourceId { get; set; }
    public int PostingKind { get; set; }
    public int Status { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? CorrelationId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? JournalEntryId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AccountingIdempotencyRecordEntity : PersistenceEntity
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Operation { get; set; } = "";
    public string IdempotencyKey { get; set; } = "";
    public string RequestHash { get; set; } = "";
    public string? ResponseJson { get; set; }
    public int Status { get; set; }
    public string? FailureCode { get; set; }
    public DateTime? CompletedAt { get; set; }
}
