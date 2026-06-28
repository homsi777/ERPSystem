using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Dialogs;
using ERPSystem.Services.Sales;
using ERPSystem.Views.Sales;

namespace ERPSystem.Services
{
    /// <summary>Unified mock interaction layer — no DB, consistent UX responses.</summary>
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

        public static void OpenDetailingWorkspace(string? invoiceNumber = null, FabricSalesInvoiceRow? rowOverride = null)
        {
            if (rowOverride is not null)
                SalesNavigationContext.BeginDetailing(null, rowOverride.InvoiceNumber);
            else if (!string.IsNullOrWhiteSpace(invoiceNumber))
            {
                SalesNavigationContext.BeginDetailing(null, invoiceNumber);
            }

            Navigate(AppModule.Sales, "Detailing");
        }

        /// <summary>Navigate warehouse officer to detailing — not delivery (delivery module is not ready).</summary>
        public static void NavigateToWarehouseDetailing(string? invoiceNumber = null)
        {
            Navigate(AppModule.Sales, "Detailing");
            if (!string.IsNullOrWhiteSpace(invoiceNumber))
                OpenDetailingWorkspace(invoiceNumber);
        }

        public static void OpenInvoiceOperationsCenter(string invoiceNumber)
        {
            var si = SalesSampleData.Generate(20)
                .FirstOrDefault(i => i.InvoiceNumber == invoiceNumber)
                ?? SalesSampleData.Generate(1).First();
            var row = new FabricSalesInvoiceRow
            {
                Source = si,
                InvoiceNumber = si.InvoiceNumber,
                CustomerName = si.CustomerNameAr,
                RollCount = 5,
                Amount = si.GrandTotal,
                Date = si.Date,
                WorkflowStatus = FabricInvoiceWorkflowStatus.Approved
            };
            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.InvoiceView, EntityType.SalesInvoice, row, AppModule.Sales);
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

        public static void OpenContainerOperationsCenter(ImportContainerModel? container = null)
        {
            container ??= ChinaImportSampleData.Generate(1).First();
            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.OpenOperationsCenter, EntityType.ImportContainer, container, AppModule.ChinaImport);
        }

        public static void OpenLandingCostWorkspace(ImportContainerModel? container = null)
        {
            container ??= ChinaImportSampleData.Generate(1).First();
            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.ContainerCosts, EntityType.ImportContainer, container, AppModule.ChinaImport);
        }

        public static void OpenMockForm(string title) =>
            ShowInfo($"نموذج «{title}» — وضع تجريبي.\n\nسيتم ربط الحفظ الفعلي مع PostgreSQL لاحقاً.", title);
    }
}
