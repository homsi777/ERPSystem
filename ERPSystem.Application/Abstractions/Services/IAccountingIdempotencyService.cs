namespace ERPSystem.Application.Abstractions.Services;

public sealed class IdempotencyExecutionResult
{
    public required bool IsReplay { get; init; }
    public required bool IsConflict { get; init; }
    public string? StoredResponseJson { get; init; }
    public Guid? RecordId { get; init; }
}

public interface IAccountingIdempotencyService
{
    Task<IdempotencyExecutionResult> BeginAsync(
        Guid companyId,
        Guid userId,
        string operation,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken = default);

    Task CompleteAsync(
        Guid recordId,
        string responseJson,
        CancellationToken cancellationToken = default);

    Task FailAsync(
        Guid recordId,
        string failureCode,
        CancellationToken cancellationToken = default);
}
