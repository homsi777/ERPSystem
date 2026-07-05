using ERPSystem.Application.Abstractions.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Search;

public sealed class GlobalSearchUiService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public GlobalSearchUiService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public static GlobalSearchUiService Instance => AppServices.GetRequiredService<GlobalSearchUiService>();

    public async Task<IReadOnlyList<GlobalSearchResult>> SearchAsync(string query, int limit = 20, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<IGlobalSearchService>();
        return await svc.SearchAsync(query, limit, cancellationToken);
    }
}
