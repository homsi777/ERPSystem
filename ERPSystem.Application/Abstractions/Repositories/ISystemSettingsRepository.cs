namespace ERPSystem.Application.Abstractions.Repositories;

/// <summary>
/// Key/value persistence for system-wide configuration (company profile,
/// finance defaults, numbering prefixes, exchange rates, ...).
/// Values are stored as strings in the <c>settings.system_settings</c> table.
/// </summary>
public interface ISystemSettingsRepository
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
    Task SetManyAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default);
}
