using ERPSystem.Application.Abstractions;
using ERPSystem.Domain.Common;

namespace ERPSystem.Application.Common;

public static class DomainEventSaveExtensions
{
    public static async Task<int> SaveAndDispatchAsync(
        this IUnitOfWork unitOfWork,
        IDomainEventDispatcher dispatcher,
        IEnumerable<AggregateRoot> aggregates,
        CancellationToken cancellationToken = default)
    {
        var roots = aggregates.ToList();
        var events = roots.SelectMany(a => a.DomainEvents).ToList();
        foreach (var root in roots)
            root.ClearDomainEvents();

        var count = await unitOfWork.SaveChangesAsync(cancellationToken);
        if (events.Count > 0)
            await dispatcher.DispatchAsync(events, cancellationToken);
        return count;
    }
}
