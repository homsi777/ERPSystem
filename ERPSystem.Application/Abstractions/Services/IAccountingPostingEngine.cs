using ERPSystem.Application.Posting;

namespace ERPSystem.Application.Abstractions.Services;

public interface IAccountingPostingEngine
{
    Task<PostingResult> PostAsync(PostingRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// After a unique-index violation on SaveChanges, detach pending rows and return the existing posted entry.
    /// </summary>
    Task<PostingResult> RecoverFromUniqueViolationAsync(
        PostingRequest request,
        CancellationToken cancellationToken = default);

    Task<ReversalResult> ReverseAsync(ReversalRequest request, CancellationToken cancellationToken = default);
}
