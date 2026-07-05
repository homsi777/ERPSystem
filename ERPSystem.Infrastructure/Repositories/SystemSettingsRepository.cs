using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Settings;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class SystemSettingsRepository(ErpDbContext context) : ISystemSettingsRepository
{
    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var entity = await context.SystemSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        return entity?.Value;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await context.SystemSettings.AsNoTracking().ToListAsync(cancellationToken);
        return rows
            .GroupBy(r => r.Key)
            .ToDictionary(g => g.Key, g => g.Last().Value);
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var entity = await context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (entity is null)
        {
            entity = new SystemSettingEntity { Id = Guid.NewGuid(), Key = key, Value = value };
            await context.SystemSettings.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Value = value;
        }
    }

    public async Task SetManyAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken = default)
    {
        if (values.Count == 0) return;
        var keys = values.Keys.ToList();
        var existing = await context.SystemSettings
            .Where(s => keys.Contains(s.Key))
            .ToListAsync(cancellationToken);

        foreach (var (key, value) in values)
        {
            var entity = existing.FirstOrDefault(e => e.Key == key);
            if (entity is null)
                await context.SystemSettings.AddAsync(
                    new SystemSettingEntity { Id = Guid.NewGuid(), Key = key, Value = value },
                    cancellationToken);
            else
                entity.Value = value;
        }
    }
}
