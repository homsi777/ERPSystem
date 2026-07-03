using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Dialogs;
using ERPSystem.Services.China;
using ERPSystem.Services.Sales;

namespace ERPSystem.Services
{
    /// <summary>Unified interaction layer for navigation, confirmations, and workspace actions.</summary>
    public static class MockInteractionService
    {
        public static void Navigate(AppModule module, string subPage = "") =>
            NavigationStateManager.Instance.NavigateTo(module, subPage);

        public static bool Confirm(string message, string title = "تأكيد العملية") =>
            ConfirmationDialogService.Confirm(message, title);

        public static void ShowSuccess(string message, string title = "تم بنجاح") =>
            MockFeedbackDialog.Show(MockFeedbackKind.Success, message, title);

        public static void ShowWarning(string message, string title = "تنبيه") =>
            MockFeedbackDialog.Show(MockFeedbackKind.Warning, message, title);

        public static void ShowInfo(string message, string title = "ERP PRO") =>
            MockFeedbackDialog.Show(MockFeedbackKind.Info, message, title);

        public static void ShowComingSoon(string feature) =>
            MockFeedbackDialog.Show(MockFeedbackKind.ComingSoon,
                $"الميزة «{feature}» جاهزة للربط وستُفعّل مع محرك المستندات وقاعدة البيانات.",
                "قريباً");

        public static void ShowDocumentPreview(string documentTitle, string format = "معاينة")
        {
            var w = new DocumentPreviewWindow
            {
                Owner = System.Windows.Application.Current.MainWindow,
                DocumentTitle = documentTitle,
                DocumentFormat = format
            };
            w.ShowDialog();
        }

        public static void OpenDetailingWorkspace(string? invoiceNumber = null)
        {
            if (!string.IsNullOrWhiteSpace(invoiceNumber))
                SalesNavigationContext.BeginDetailing(null, invoiceNumber);
            Navigate(AppModule.Sales, "Detailing");
        }

        /// <summary>Navigate warehouse officer to detailing — not delivery (delivery module is not ready).</summary>
        public static void NavigateToWarehouseDetailing(string? invoiceNumber = null)
        {
            Navigate(AppModule.Sales, "Detailing");
            if (!string.IsNullOrWhiteSpace(invoiceNumber))
                OpenDetailingWorkspace(invoiceNumber);
        }

        public static void OpenInvoiceOperationsCenter(string invoiceNumber) =>
            _ = OpenInvoiceOperationsCenterAsync(invoiceNumber);

        private static async Task OpenInvoiceOperationsCenterAsync(string invoiceNumber)
        {
            if (!AppServices.IsInitialized)
            {
                ShowWarning("قاعدة البيانات غير متصلة.", "مركز العمليات");
                return;
            }

            var result = await SalesUiService.Instance.GetListAsync(invoiceNumber, null, 1, 50);
            var items = result.Value?.Items;
            if (items is null || items.Count == 0)
            {
                ShowWarning($"لم يتم العثور على الفاتورة {invoiceNumber}.", "مركز العمليات");
                return;
            }

            var invoice = items.FirstOrDefault(i =>
                i.InvoiceNumber.Equals(invoiceNumber, StringComparison.OrdinalIgnoreCase))
                ?? items[0];

            var row = SalesInvoiceListRow.FromDto(invoice, "—", "—");
            SalesPopupService.ShowOperationsCenter(row);
        }

        public static void OpenCustomerStatement(CustomerListRow? customer = null)
        {
            if (customer is null)
            {
                MockInteractionService.ShowWarning("اختر عميلاً من القائمة أولاً.", "كشف حساب");
                MockInteractionService.Navigate(AppModule.Customers, "List");
                return;
            }

            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.CustomerStatement, EntityType.Customer, customer, AppModule.Customers);
        }

        public static void OpenCustomerOperationsCenter(CustomerListRow? customer = null)
        {
            if (customer is null)
            {
                MockInteractionService.ShowWarning("اختر عميلاً من القائمة أولاً.", "مركز العمليات");
                MockInteractionService.Navigate(AppModule.Customers, "List");
                return;
            }

            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.OpenOperationsCenter, EntityType.Customer, customer, AppModule.Customers);
        }

        public static void OpenContainerOperationsCenter(ContainerListRow? container = null)
        {
            if (container is null)
            {
                ShowWarning("اختر حاوية من القائمة أولاً.", "مركز العمليات");
                Navigate(AppModule.ChinaImport, "Containers");
                return;
            }

            ChinaImportNavigation.OpenOperationsCenter(container);
        }

        public static void OpenLandingCostWorkspace(ContainerListRow? container = null)
        {
            if (container is null)
            {
                Navigate(AppModule.ChinaImport, "Containers");
                return;
            }

            ChinaImportNavigation.OpenLandingCostWorkspace(container);
        }

        public static void OpenMockForm(string title) =>
            ShowInfo($"نموذج «{title}» — وضع تجريبي.\n\nسيتم ربط الحفظ الفعلي مع PostgreSQL لاحقاً.", title);
    }
}
