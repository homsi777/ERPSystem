using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Notifications;
using ERPSystem.Dialogs;
using System.Windows;

namespace ERPSystem.Services;

public sealed class WpfNotificationService : INotificationService
{
    public Task PublishAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : class
    {
        var message = notification switch
        {
            CustomerCreatedNotification c => ($"تم إنشاء العميل «{c.CustomerName}» ({c.CustomerCode}).", "عميل جديد"),
            CustomerUpdatedNotification u => ($"تم تحديث بيانات العميل «{u.CustomerName}».", "تحديث عميل"),
            CustomerDeactivatedNotification d => ($"تم تعطيل العميل «{d.CustomerName}».", "تعطيل عميل"),
            _ => (null, null)
        };

        if (message.Item1 is not null)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
                MockFeedbackDialog.Show(MockFeedbackKind.Success, message.Item1, message.Item2 ?? "ERP PRO"));
        }

        return Task.CompletedTask;
    }
}
