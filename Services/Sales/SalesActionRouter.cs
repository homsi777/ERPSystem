using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Sales;

/// <summary>
/// موجّه إجراءات فاتورة المبيعات — يستلم إجراء + سجل ثم يفتح popup مناسب أو يستدعي عملية حقيقية.
/// </summary>
public static class SalesActionRouter
{
    public static void Handle(EntityActionId actionId, SalesInvoiceListRow row)
    {
        switch (actionId)
        {
            case EntityActionId.OpenOperationsCenter:
            case EntityActionId.InvoiceView:
                SalesPopupService.ShowOperationsCenter(row);
                break;

            case EntityActionId.InvoiceEdit:
                SalesPopupService.ShowEdit(row);
                break;

            case EntityActionId.InvoiceSendToWarehouse:
                _ = SalesPopupService.SendToWarehouseAsync(row);
                break;

            case EntityActionId.InvoiceApprove:
                _ = SalesPopupService.ApproveAsync(row);
                break;

            case EntityActionId.InvoiceApproveAndDeliver:
                _ = SalesPopupService.ApproveAndDeliverAsync(row);
                break;

            case EntityActionId.InvoiceCancel:
                _ = SalesPopupService.CancelAsync(row);
                break;

            case EntityActionId.InvoiceDetailLengths:
                SalesPopupService.NavigateToDetailing(row);
                break;

            case EntityActionId.InvoiceDeliver:
                SalesPopupService.ShowDelivery(row);
                break;

            case EntityActionId.InvoiceReturn:
                SalesPopupService.ShowReturn(row);
                break;

            case EntityActionId.InvoicePrint:
            case EntityActionId.InvoiceExportPdf:
                _ = SalesPopupService.PrintAsync(row, actionId == EntityActionId.InvoiceExportPdf);
                break;

            case EntityActionId.InvoiceCallCustomer:
                SalesPopupService.CallCustomer(row);
                break;

            case EntityActionId.InvoiceViewReturns:
                SalesPopupService.ShowReturnsForInvoice(row);
                break;

            default:
                MessageBox.Show(
                    $"إجراء غير معروف: {actionId}",
                    "قائمة المهام",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                break;
        }
    }

    /// <summary>
    /// يتلقى مفاتيح الإجراءات السريعة القادمة من OperationsCenterShell (زر مركز العمليات)
    /// ويحوّلها إلى إجراء حقيقي (وليس ws:mock).
    /// </summary>
    public static bool TryHandleQuickAction(string? actionKey, OperationsCenterContext ctx)
    {
        if (string.IsNullOrEmpty(actionKey) || ctx.EntityRow is not SalesInvoiceListRow row)
            return false;

        switch (actionKey)
        {
            case "sales:edit":
                SalesPopupService.ShowEdit(row);
                return true;
            case "sales:send-to-warehouse":
                _ = SalesPopupService.SendToWarehouseAsync(row);
                return true;
            case "sales:approve":
            case "sales:approve-deliver":
                _ = SalesPopupService.ApproveAndDeliverAsync(row);
                return true;
            case "sales:cancel":
                _ = SalesPopupService.CancelAsync(row);
                return true;
            case "sales:detail":
                SalesPopupService.NavigateToDetailing(row);
                return true;
            case "sales:deliver":
                SalesPopupService.ShowDelivery(row);
                return true;
            case "sales:return":
                SalesPopupService.ShowReturn(row);
                return true;
            case "sales:print":
                _ = SalesPopupService.PrintAsync(row, exportPdf: false);
                return true;
            case "sales:pdf":
                _ = SalesPopupService.PrintAsync(row, exportPdf: true);
                return true;
            case "sales:call-customer":
                SalesPopupService.CallCustomer(row);
                return true;
            default:
                return false;
        }
    }
}
