using ERPSystem.Controls.Sales;
using ERPSystem.Controls.Sales.Popups;
using ERPSystem.Core;
using ERPSystem.Core.Sales;
using ERPSystem.Dialogs;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;

namespace ERPSystem.Services.Sales;

/// <summary>
/// Popups لجميع عمليات فاتورة البيع (نفس فلسفة ExpensePopupService).
/// </summary>
public static class SalesPopupService
{
    private static ErpModalWindow? _active;

    public static void ShowOperationsCenter(SalesInvoiceListRow row)
    {
        var oc = new SalesInvoiceOperationsCenterControl();
        oc.Initialize(row.Id, initialTab: null);
        var host = new ErpModalWindow();
        host.Configure(
            "مركز عمليات فاتورة البيع",
            $"{row.InvoiceNumber} — {row.CustomerName}",
            "\uE8A7",
            width: 1180,
            maxHeight: 860);
        host.SetBody(oc);
        _active = host;
        host.ShowDialog();
        _active = null;
        SalesListRefreshHub.RequestRefresh();
    }

    public static void ShowEdit(SalesInvoiceListRow row)
    {
        if (row.Status != SalesInvoiceStatus.Draft)
        {
            MockInteractionService.ShowWarning(
                "لا يمكن تعديل الفاتورة إلا في حالة المسودة.",
                "تعديل الفاتورة");
            return;
        }

        SalesNavigationContext.BeginEdit(row.Id);
        var form = new NewSalesInvoiceControl();
        var host = new ErpModalWindow();
        host.Configure(
            "تعديل فاتورة بيع",
            $"{row.InvoiceNumber} — {row.CustomerName}",
            "\uE70F",
            width: 1120,
            maxHeight: 820);
        host.SetBody(form);
        _active = host;
        host.ShowDialog();
        _active = null;
        SalesNavigationContext.EditInvoiceId = null;
        SalesListRefreshHub.RequestRefresh();
    }

    public static async Task ApproveAsync(SalesInvoiceListRow row)
    {
        if (row.Status is not (SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
        {
            MockInteractionService.ShowWarning(
                "يجب إكمال تفصيل الأطوال أولاً قبل الاعتماد.",
                "اعتماد الفاتورة");
            return;
        }

        if (!ConfirmationDialogService.ConfirmDangerous(
            "اعتماد الفاتورة (تسجيل قيود GL + خصم مخزون)",
            $"{row.InvoiceNumber} — {row.CustomerName}"))
            return;

        if (!AppServices.IsInitialized) return;
        if (!await SalesUiService.Instance.CanApproveAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية الاعتماد.", "صلاحية");
            return;
        }

        var result = await SalesUiService.Instance.ApproveAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess(
                $"تم اعتماد الفاتورة {row.InvoiceNumber} بنجاح — تم تسجيل القيود المحاسبية.",
                "اعتماد الفاتورة");
            SalesListRefreshHub.RequestRefresh();
        }
    }

    public static async Task SendToWarehouseAsync(SalesInvoiceListRow row)
    {
        if (row.Status != SalesInvoiceStatus.Draft)
        {
            MockInteractionService.ShowWarning(
                "يمكن إرسال الفواتير في حالة المسودة فقط.",
                "إرسال للمستودع");
            return;
        }

        if (!AppServices.IsInitialized) return;

        var warehousesResult = await SalesUiService.Instance.GetWarehousesAsync();
        var warehouses = warehousesResult.Value ?? new List<Application.DTOs.Warehouses.WarehouseListDto>();
        Guid? chosenWarehouseId = null;

        if (warehouses.Count > 1)
        {
            var picker = new SalesWarehousePickerPopupControl(warehouses, row);
            var host = new ErpModalWindow();
            host.Configure(
                "اختر المستودع",
                $"إرسال {row.InvoiceNumber} للمستودع",
                "\uE72A",
                width: 480,
                maxHeight: 320);
            host.SetBody(picker);
            _active = host;
            var ok = host.ShowDialog() == true;
            _active = null;
            if (!ok) return;
            chosenWarehouseId = picker.SelectedWarehouseId;
        }

        if (chosenWarehouseId.HasValue && chosenWarehouseId.Value != Guid.Empty)
        {
            var updated = await SalesUiService.Instance.UpdateWarehouseAsync(row.Id, chosenWarehouseId.Value);
            if (!ApplicationResultPresenter.Present(updated)) return;
        }

        if (!await SalesUiService.Instance.CanSendToWarehouseAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية إرسال الفاتورة للمستودع.", "صلاحية");
            return;
        }

        var result = await SalesUiService.Instance.SendToWarehouseAsync(row.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess(
                $"تم إرسال الفاتورة {row.InvoiceNumber} للمستودع (بانتظار التفصيل).",
                "إرسال للمستودع");
            SalesListRefreshHub.RequestRefresh();
            DetailingQueueRefreshHub.RequestRefresh();
        }
    }

    public static async Task CancelAsync(SalesInvoiceListRow row)
    {
        if (row.Status is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered)
        {
            MockInteractionService.ShowWarning(
                "الفواتير المعتمدة/المسلمة يجب أن تُعكس بمرتجع (وليس إلغاء).",
                "إلغاء الفاتورة");
            return;
        }

        var reasonPopup = new SalesCancelPopupControl(row);
        var host = new ErpModalWindow();
        host.Configure(
            "إلغاء فاتورة بيع",
            $"{row.InvoiceNumber} — {row.CustomerName}",
            "\uE711",
            width: 500,
            maxHeight: 380);
        host.SetBody(reasonPopup);
        _active = host;
        var ok = host.ShowDialog() == true;
        _active = null;
        if (!ok || string.IsNullOrWhiteSpace(reasonPopup.Reason)) return;

        if (!AppServices.IsInitialized) return;
        if (!await SalesUiService.Instance.CanCancelAsync())
        {
            MockInteractionService.ShowWarning("لا تملك صلاحية إلغاء الفواتير.", "صلاحية");
            return;
        }

        var result = await SalesUiService.Instance.CancelAsync(row.Id, reasonPopup.Reason);
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess($"تم إلغاء الفاتورة {row.InvoiceNumber}.", "إلغاء");
            SalesListRefreshHub.RequestRefresh();
        }
    }

    public static void NavigateToDetailing(SalesInvoiceListRow row)
    {
        SalesNavigationContext.BeginDetailing(row.Id, row.InvoiceNumber);
        MockInteractionService.Navigate(AppModule.Sales, "Detailing");
    }

    public static void ShowDelivery(SalesInvoiceListRow row)
    {
        if (row.Status is not (SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed))
        {
            MockInteractionService.ShowWarning(
                "لا يمكن تسليم الفاتورة قبل الاعتماد.",
                "تسليم");
            return;
        }

        var body = new SalesDeliveryPopupControl(row);
        var host = new ErpModalWindow();
        host.Configure(
            "تأكيد التسليم",
            $"{row.InvoiceNumber} — {row.CustomerName}",
            "\uE7C1",
            width: 540,
            maxHeight: 520);
        host.SetBody(body);
        _active = host;
        var ok = host.ShowDialog() == true;
        _active = null;
        if (ok) SalesListRefreshHub.RequestRefresh();
    }

    public static void ShowReturn(SalesInvoiceListRow row)
    {
        if (row.Status is not (SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed
            or SalesInvoiceStatus.Delivered or SalesInvoiceStatus.PartiallyReturned))
        {
            MockInteractionService.ShowWarning(
                "المرتجع متاح للفواتير المعتمدة/المسلمة فقط.",
                "مرتجع بيع");
            return;
        }

        var body = new SalesReturnFormPopupControl(row.Id, row.InvoiceNumber, row.CustomerName);
        var host = new ErpModalWindow();
        host.Configure(
            "مرتجع بيع",
            $"{row.InvoiceNumber} — {row.CustomerName}",
            "\uE72C",
            width: 900,
            maxHeight: 720);
        host.SetBody(body);
        _active = host;
        var ok = host.ShowDialog() == true;
        _active = null;
        if (ok)
        {
            SalesListRefreshHub.RequestRefresh();
            SalesReturnListRefreshHub.RequestRefresh();
        }
    }

    public static void ShowReturnsForInvoice(SalesInvoiceListRow row)
    {
        MockInteractionService.Navigate(AppModule.Sales, "Returns");
    }

    public static async Task PrintAsync(SalesInvoiceListRow row, bool exportPdf)
    {
        if (!AppServices.IsInitialized) return;
        var oc = await SalesUiService.Instance.GetOperationsCenterAsync(row.Id);
        if (!ApplicationResultPresenter.Present(oc) || oc.Value?.Invoice is null) return;
        SalesDocumentService.ShowInvoicePreview(oc.Value.Invoice, row.CustomerName, exportPdf);
    }

    public static void CallCustomer(SalesInvoiceListRow row)
    {
        MockInteractionService.ShowInfo(
            $"العميل: {row.CustomerName}\nرقم الفاتورة: {row.InvoiceNumber}\n\n(استخدم بيانات العميل من مركز عمليات العميل)",
            "اتصل بالعميل");
    }

    public static void CompleteSuccess()
    {
        SalesListRefreshHub.RequestRefresh();
        if (_active is null) return;
        _active.DialogResult = true;
        _active.Close();
    }

    public static void CancelActive()
    {
        if (_active is null) return;
        _active.DialogResult = false;
        _active.Close();
    }
}
