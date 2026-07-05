namespace ERPSystem.Application.Abstractions.Services;

public sealed record GlobalSearchResult(
    string EntityType,
    Guid EntityId,
    string DisplayText,
    string SecondaryText,
    string Icon);

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default);
}
