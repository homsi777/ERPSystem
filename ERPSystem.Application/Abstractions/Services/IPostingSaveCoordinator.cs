using ERPSystem.Application.Posting;

namespace ERPSystem.Application.Abstractions.Services;

/// <summary>
/// Saves the unit of work and recovers idempotently from protected posting unique-index violations.
/// </summary>
public interface IPostingSaveCoordinator
{
    Task SaveChangesWithPostingRecoveryAsync(
        IReadOnlyList<PostingRequest> recoveryRequests,
        CancellationToken cancellationToken = default);
}
