using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Suppliers;
using ERPSystem.Services.Purchases;
using ERPSystem.Core.Workspace;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Suppliers;
using ERPSystem.Services.Finance;
using ERPSystem.Services.Inventory;
using ERPSystem.Services.Sales;
using System.Windows.Controls;

namespace ERPSystem.Services
{
    public static class MockQuickActionRouter
    {
        public static void Execute(string? actionKey, OperationsCenterContext ctx, TabControl? tabs)
        {
            if (CustomerActionRouter.TryHandleQuickAction(actionKey, ctx))
                return;

            if (SupplierActionRouter.TryHandleQuickAction(actionKey, ctx))
                return;

            if (InventoryActionRouter.TryHandleQuickAction(actionKey, ctx))
                return;

            if (SalesActionRouter.TryHandleQuickAction(actionKey, ctx))
                return;

            if (OpeningBalanceQuickActionRouter.TryHandleQuickAction(actionKey, ctx))
                return;

            if (string.IsNullOrEmpty(actionKey))
            {
                MockInteractionService.ShowComingSoon("هذا الإجراء");
                return;
            }

            if (actionKey.StartsWith("tab:", StringComparison.OrdinalIgnoreCase))
            {
                var tabKey = actionKey[4..];
                if (tabs != null) SelectTab(tabs, tabKey);
                return;
            }

            if (actionKey.StartsWith("nav:", StringComparison.OrdinalIgnoreCase))
            {
                var parts = actionKey[4..].Split(':', 2);
                if (parts.Length == 2 && Enum.TryParse<AppModule>(parts[0], out var mod))
                    MockInteractionService.Navigate(mod, parts[1]);
                return;
            }

            if (actionKey.StartsWith("preview:", StringComparison.OrdinalIgnoreCase))
            {
                var kind = actionKey[8..];
                if (kind.Equals("PurchaseInvoice", StringComparison.OrdinalIgnoreCase) &&
                    ctx.EntityRow is PurchaseListRow prow)
                {
                    _ = PurchaseActionRouter.PrintAsync(prow, exportPdf: false);
                    return;
                }
                MockInteractionService.ShowDocumentPreview(kind, "PDF");
                return;
            }

            if (actionKey.Equals("purchase:pdf", StringComparison.OrdinalIgnoreCase) &&
                ctx.EntityRow is PurchaseListRow pdfRow)
            {
                _ = PurchaseActionRouter.PrintAsync(pdfRow, exportPdf: true);
                return;
            }

            if (actionKey.StartsWith("success:", StringComparison.OrdinalIgnoreCase))
            {
                MockInteractionService.ShowSuccess(actionKey[8..]);
                return;
            }

            switch (actionKey)
            {
                case "form:EditEmployee":
                    MockInteractionService.Navigate(AppModule.HR, "Form");
                    break;
                case "form:NewTransfer":
                    InventoryNavigationContext.BeginCreateTransfer();
                    MockInteractionService.Navigate(AppModule.Inventory, "TransferForm");
                    break;
                case "form:Stocktake":
                    InventoryNavigationContext.BeginCreateStocktake();
                    MockInteractionService.Navigate(AppModule.Inventory, "StocktakeForm");
                    break;
                case "form:EditPrice":
                    MockInteractionService.OpenMockForm("تعديل سعر الصنف");
                    break;
                case "ws:NewInvoice":
                    MockInteractionService.Navigate(AppModule.Sales, "NewInvoice");
                    break;
                case "ws:Receipt":
                    MockInteractionService.Navigate(AppModule.Accounting, "Receipts");
                    break;
                case "ws:Return":
                    MockInteractionService.Navigate(AppModule.Sales, "NewReturn");
                    break;
                case "ws:Statement":
                    if (ctx.EntityRow is CustomerListRow c)
                        MockInteractionService.OpenCustomerStatement(c);
                    else if (ctx.EntityRow is SupplierListRow s)
                    {
                        SupplierNavigationContext.BeginStatement(s.Id, s.NameAr);
                        MockInteractionService.Navigate(AppModule.Suppliers, "Statement");
                    }
                    break;
                case "ws:Detailing":
                    MockInteractionService.OpenDetailingWorkspace();
                    break;
                case "ws:LandingCost":
                    if (ctx.EntityRow is ContainerListRow cont)
                        ChinaImportNavigation.OpenLandingCostWorkspace(cont);
                    else if (tabs != null)
                        SelectTab(tabs, "LandingCost");
                    break;
                case "form:EditCustomer":
                    MockInteractionService.Navigate(AppModule.Customers, "Form");
                    break;
                case "ws:PurchasePayment":
                    if (ctx.EntityRow is PurchaseListRow pr)
                    {
                        _ = PurchaseUiService.Instance.GetInvoiceDetailsAsync(pr.Id).ContinueWith(t =>
                        {
                            if (t.Result.IsSuccess && t.Result.Value is not null)
                                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                                    PurchaseActionRouter.OpenPayment(t.Result.Value));
                        });
                    }
                    break;
                case "form:EditPurchaseInvoice":
                    if (ctx.EntityRow is PurchaseListRow editRow)
                    {
                        PurchaseNavigationContext.BeginEdit(editRow.Id);
                        MockInteractionService.Navigate(AppModule.Purchases, "Form");
                    }
                    break;
                case "form:EditSupplier":
                    MockInteractionService.Navigate(AppModule.Suppliers, "Form");
                    break;
                case "comingsoon:Call":
                    MockInteractionService.ShowComingSoon("الاتصال الهاتفي");
                    break;
                default:
                    MockInteractionService.ShowComingSoon(actionKey);
                    break;
            }
        }

        private static void SelectTab(TabControl tabs, string tabKey)
        {
            for (int i = 0; i < tabs.Items.Count; i++)
            {
                if (tabs.Items[i] is TabItem ti &&
                    ti.Tag is string key &&
                    key.Equals(tabKey, StringComparison.OrdinalIgnoreCase))
                {
                    tabs.SelectedIndex = i;
                    return;
                }
            }
        }
    }

    public sealed class OperationsCenterContext
    {
        public EntityType EntityType { get; init; }
        public object? EntityRow { get; init; }
        public AppModule SourceModule { get; init; }
        public string Title { get; init; } = "";
    }
}
