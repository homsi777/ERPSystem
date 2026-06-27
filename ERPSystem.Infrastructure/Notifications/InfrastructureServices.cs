using ERPSystem.Application.Abstractions.Services;

namespace ERPSystem.Infrastructure.Notifications;

internal sealed class InMemoryNotificationService : INotificationService
{
    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : class => Task.CompletedTask;
}

internal sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
    public DateTime Today => DateTime.UtcNow.Date;
}

internal sealed class NullDocumentPreviewService : IDocumentPreviewService
{
    public Task<byte[]?> RenderPreviewAsync(string templateCode, object model, CancellationToken cancellationToken = default) =>
        Task.FromResult<byte[]?>(null);
}
