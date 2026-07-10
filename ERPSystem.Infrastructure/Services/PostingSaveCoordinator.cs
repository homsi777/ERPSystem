using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Posting;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ERPSystem.Infrastructure.Services;

internal sealed class PostingSaveCoordinator(
    IUnitOfWork unitOfWork,
    IAccountingPostingEngine postingEngine) : IPostingSaveCoordinator
{
    public async Task SaveChangesWithPostingRecoveryAsync(
        IReadOnlyList<PostingRequest> recoveryRequests,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (IsUniquePostingViolation(ex) && recoveryRequests.Count > 0)
        {
            foreach (var request in recoveryRequests)
                await postingEngine.RecoverFromUniqueViolationAsync(request, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static bool IsUniquePostingViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pg)
            return pg.SqlState == PostgresErrorCodes.UniqueViolation;

        return ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;
    }
}
