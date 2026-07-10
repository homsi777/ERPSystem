using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Posting;

public sealed class PostingLineRequest
{
    public required Guid AccountId { get; init; }
    public decimal Debit { get; init; }
    public decimal Credit { get; init; }
    public required string Narrative { get; init; }
    public Guid? PartyId { get; init; }
    public Guid? SourceLineId { get; init; }
}

public sealed class PostingRequest
{
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required DocumentType SourceType { get; init; }
    public required Guid SourceId { get; init; }
    public required PostingKind PostingKind { get; init; }
    public required DateTime PostingDate { get; init; }
    public Guid? CurrencyId { get; init; }
    public decimal? ExchangeRate { get; init; }
    public required Guid UserId { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? CorrelationId { get; init; }
    public required string Description { get; init; }
    public required Guid JournalBookId { get; init; }
    public required IReadOnlyList<PostingLineRequest> Lines { get; init; }
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

public sealed class PostingResult
{
    public required bool Success { get; init; }
    public required bool AlreadyPosted { get; init; }
    public Guid? JournalEntryId { get; init; }
    public string? JournalEntryNumber { get; init; }
    public Guid? PostingAttemptId { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static PostingResult Succeeded(
        Guid journalEntryId,
        string journalEntryNumber,
        Guid postingAttemptId,
        bool alreadyPosted = false) =>
        new()
        {
            Success = true,
            AlreadyPosted = alreadyPosted,
            JournalEntryId = journalEntryId,
            JournalEntryNumber = journalEntryNumber,
            PostingAttemptId = postingAttemptId
        };

    public static PostingResult Failed(string errorCode, string errorMessage, Guid? postingAttemptId = null) =>
        new()
        {
            Success = false,
            AlreadyPosted = false,
            ErrorCode = errorCode,
            ErrorMessage = errorMessage,
            PostingAttemptId = postingAttemptId
        };
}

public sealed class ReversalRequest
{
    public required Guid CompanyId { get; init; }
    public required Guid BranchId { get; init; }
    public required Guid OriginalJournalEntryId { get; init; }
    public required Guid UserId { get; init; }
    public required DateTime ReversalDate { get; init; }
    public required string Reason { get; init; }
    public string? IdempotencyKey { get; init; }
    public string? CorrelationId { get; init; }
}

public sealed class ReversalResult
{
    public required bool Success { get; init; }
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }

    public static ReversalResult NotImplemented() =>
        new()
        {
            Success = false,
            ErrorCode = "reversal_not_implemented",
            ErrorMessage = "Journal reversal is planned for a later stabilization phase."
        };
}

public sealed record JournalEntryPostMetadata(
    PostingKind PostingKind,
    int PostingIdentityVersion = 2,
    string? IdempotencyKey = null,
    string? CorrelationId = null);
