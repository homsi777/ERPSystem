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
        switch (notification)
        {
            case CustomerCreatedNotification c:
                ShowSuccess($"تم إنشاء العميل «{c.CustomerName}» ({c.CustomerCode}).", "عميل جديد");
                break;
            case CustomerUpdatedNotification u:
                ShowSuccess($"تم تحديث بيانات العميل «{u.CustomerName}».", "تحديث عميل");
                break;
            case CustomerDeactivatedNotification d:
                ShowSuccess($"تم تعطيل العميل «{d.CustomerName}».", "تعطيل عميل");
                break;
            case InventoryChangedNotification:
            case SalesInvoiceApprovedNotification:
            case ContainerApprovedNotification:
                ErpDataRefreshHub.RequestRefresh();
                break;
        }

        return Task.CompletedTask;
    }

    private static void ShowSuccess(string message, string title)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
            MockFeedbackDialog.Show(MockFeedbackKind.Success, message, title));
    }
}
