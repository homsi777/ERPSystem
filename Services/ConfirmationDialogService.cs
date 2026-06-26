using System.Windows;

namespace ERPSystem.Services
{
    public static class ConfirmationDialogService
    {
        public static bool Confirm(string message, string title = "تأكيد العملية")
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning,
                MessageBoxResult.No) == MessageBoxResult.Yes;
        }

        public static bool ConfirmDangerous(string actionLabel, string entityName)
        {
            return Confirm($"هل أنت متأكد من تنفيذ: {actionLabel}؟\n\n{entityName}",
                "تأكيد عملية حساسة");
        }
    }
}
