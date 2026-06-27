namespace ERPSystem.Application.Abstractions.Services;

public interface INotificationService
{
    Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : class;
}
