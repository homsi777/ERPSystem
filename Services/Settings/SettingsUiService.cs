using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Common;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Services.Settings;

/// <summary>
/// UI bridge for reading/writing <c>system_settings</c> and managing branches.
/// Backed by <see cref="ISystemSettingsRepository"/> and <see cref="IBranchRepository"/>.
/// </summary>
public sealed class SettingsUiService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SettingsUiService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

    public static SettingsUiService Instance => AppServices.GetRequiredService<SettingsUiService>();

    public async Task<IReadOnlyDictionary<string, string>> LoadAllAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();
        return await repo.GetAllAsync(cancellationToken);
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();
        return await repo.GetAsync(key, cancellationToken);
    }

    public async Task SaveAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISystemSettingsRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repo.SetManyAsync(values, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
    }

    // ---- Branches ----

    public async Task<IReadOnlyList<BranchListItem>> GetBranchesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var branch = scope.ServiceProvider.GetRequiredService<ICurrentBranchService>();
        var repo = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var companyId = branch.CompanyId ?? Guid.Empty;
        return await repo.GetAllAsync(companyId, cancellationToken);
    }

    public async Task AddBranchAsync(string code, string nameAr, string nameEn, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var branch = scope.ServiceProvider.GetRequiredService<ICurrentBranchService>();
        var repo = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repo.AddAsync(branch.CompanyId ?? Guid.Empty, code, nameAr, nameEn, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBranchAsync(Guid id, string code, string nameAr, string nameEn, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IBranchRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repo.UpdateAsync(id, code, nameAr, nameEn, cancellationToken);
        await uow.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Reads the configured default exchange rate, falling back to a sane default.</summary>
    public async Task<decimal> GetDefaultExchangeRateAsync(CancellationToken cancellationToken = default)
    {
        var raw = await GetAsync(SystemSettingKeys.DefaultExchangeRate, cancellationToken);
        return decimal.TryParse(raw, out var value) && value > 0
            ? value
            : SystemSettingKeys.DefaultExchangeRateFallback;
    }
}
