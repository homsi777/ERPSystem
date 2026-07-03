using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ERPSystem.Infrastructure.Persistence;

/// <summary>
/// Npgsql requires UTC for <c>timestamp with time zone</c>. WPF DatePicker values are often Local/Unspecified.
/// </summary>
internal sealed class UtcDateTimeSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        NormalizeTrackedDateTimes(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        NormalizeTrackedDateTimes(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void NormalizeTrackedDateTimes(DbContext? context)
    {
        if (context is not null)
            UtcDateTimeNormalizer.NormalizeTrackedEntities(context);
    }
}

internal static class UtcDateTimeNormalizer
{
    public static void NormalizeTrackedEntities(DbContext context)
    {
        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is not DateTime value || value.Kind == DateTimeKind.Utc)
                    continue;

                property.CurrentValue = ToUtc(value);
            }
        }
    }

    public static DateTime ToUtc(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        // Calendar dates from UI pickers arrive at midnight — keep the same calendar day in UTC.
        if (value.TimeOfDay == TimeSpan.Zero)
            return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);

        return value.Kind switch
        {
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
    }
}
