using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Workspace;
using ERPSystem.Dialogs;
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

        public static void OpenDetailingWorkspace(string? invoiceNumber = null)
        {
            var si = SalesSampleData.Generate(20)
                .First(i => invoiceNumber == null || i.InvoiceNumber == invoiceNumber);
            var row = new FabricSalesInvoiceRow
            {
                Source = si,
                InvoiceNumber = si.InvoiceNumber,
                CustomerName = si.CustomerNameAr,
                RollCount = 5,
                Amount = si.GrandTotal,
                Date = si.Date,
                WorkflowStatus = FabricInvoiceWorkflowStatus.AwaitingDetailing
            };
            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.InvoiceDetailLengths, EntityType.SalesInvoice, row, AppModule.Sales);
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

        public static void OpenCustomerStatement(CustomerModel? customer = null)
        {
            customer ??= CustomerSampleData.Generate(1).First();
            WorkspaceWindowManager.Instance.OpenAction(
                EntityActionId.CustomerStatement, EntityType.Customer, customer, AppModule.Customers);
        }

        public static void OpenCustomerOperationsCenter(CustomerModel? customer = null)
        {
            customer ??= CustomerSampleData.Generate(1).First();
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
