using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Controls.China;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Suppliers;
using ERPSystem.Core.Workspace;
using ERPSystem.Services.Customers;
using ERPSystem.Services.China;
using System.Windows.Controls;

namespace ERPSystem.Services
{
    public static class MockQuickActionRouter
    {
        public static void Execute(string? actionKey, OperationsCenterContext ctx, TabControl? tabs)
        {
            if (CustomerActionRouter.TryHandleQuickAction(actionKey, ctx))
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
                MockInteractionService.ShowDocumentPreview(actionKey[8..], "PDF");
                return;
            }

            if (actionKey.StartsWith("success:", StringComparison.OrdinalIgnoreCase))
            {
                MockInteractionService.ShowSuccess(actionKey[8..]);
                return;
            }

            switch (actionKey)
            {
                case "ws:SendToWarehouse":
                    if (tabs != null) SelectTab(tabs, "Detailing");
                    MockInteractionService.ShowSuccess("تم إرسال الفاتورة للمستودع.\n\nالحالة: بانتظار التفصيل", "إرسال للمستودع");
                    break;
                case "ws:ApproveInvoice":
                    MockInteractionService.ShowSuccess("تم اعتماد الفاتورة بنجاح.", "اعتماد الفاتورة");
                    break;
                case "form:EditEmployee":
                    MockInteractionService.Navigate(AppModule.HR, "Form");
                    break;
                case "form:NewTransfer":
                    MockInteractionService.OpenMockForm("مناقلة مخزنية");
                    break;
                case "form:Stocktake":
                    MockInteractionService.OpenMockForm("جرد مستودع");
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
                    else if (ctx.EntityRow is SupplierModel)
                        WorkspaceWindowManager.Instance.OpenAction(
                            EntityActionId.SupplierStatement, ctx.EntityType, ctx.EntityRow, ctx.SourceModule);
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
