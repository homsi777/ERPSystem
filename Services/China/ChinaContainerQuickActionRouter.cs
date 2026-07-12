using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Controls.China;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Services.China;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Reports;
using System.Windows.Controls;

namespace ERPSystem.Services;

public static class ChinaContainerQuickActionRouter
{
    public static bool TryHandle(string? actionKey, OperationsCenterContext ctx, TabControl? tabs)
    {
        if (ctx.EntityType != EntityType.ImportContainer || ctx.EntityRow is not ContainerListRow row)
            return false;

        if (string.IsNullOrEmpty(actionKey))
            return false;

        if (actionKey.StartsWith("tab:", StringComparison.OrdinalIgnoreCase))
        {
            SelectTab(tabs, actionKey[4..]);
            return true;
        }

        switch (actionKey)
        {
            case "china:Approve":
                _ = ApproveAsync(row.Id);
                return true;
            case "china:Archive":
                _ = ArchiveAsync(row.Id);
                return true;
            case "china:MoveToWarehouse":
                ChinaImportNavigationContext.SetActiveContainer(row.Id);
                ChinaImportNavigation.Navigate("MoveToWarehouse", row.Status);
                return true;
            case "china:SalePrice":
                ChinaImportNavigationContext.SetActiveContainer(row.Id);
                ChinaImportNavigation.Navigate("SalePrice", row.Status);
                return true;
            case "china:Documentation":
                ContainerDocumentationPopupService.Show(row);
                return true;
            case "china:PurchaseInvoice":
                _ = OpenLinkedPurchaseInvoiceAsync(row.Id);
                return true;
            case "nav:ChinaImport:NewImport":
                ChinaImportNavigation.Navigate("NewImport");
                return true;
            case "ws:LandingCost":
                SelectTab(tabs, "LandingCost");
                return true;
            case "preview:ملخص الحاوية":
                _ = ExportContainerSummaryAsync(row);
                return true;
            default:
                return false;
        }
    }

    private static async Task ExportContainerSummaryAsync(ContainerListRow row)
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(row.Id);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var oc = result.Value;
        var container = oc.Container;
        var report = new ModuleReportResultDto
        {
            Title = $"ملخص الحاوية {container.ContainerNumber}",
            GeneratedAt = DateTime.UtcNow,
            Kpis =
            [
                new ModuleReportKpiDto { Label = "الأمتار", Value = $"{container.TotalMeters:N2}" },
                new ModuleReportKpiDto { Label = "الأثواب", Value = container.TotalRolls.ToString() },
                new ModuleReportKpiDto { Label = "فاتورة الصين", Value = $"{container.ChinaInvoiceAmountUsd:N2} USD" }
            ],
            Columns =
            [
                new ModuleReportColumnDto { Key = "label", HeaderAr = "البند" },
                new ModuleReportColumnDto { Key = "value", HeaderAr = "القيمة", IsStar = true }
            ],
            Rows =
            [
                new Dictionary<string, object?> { ["label"] = "رقم الحاوية", ["value"] = container.ContainerNumber },
                new Dictionary<string, object?> { ["label"] = "المورد", ["value"] = container.SupplierName ?? "—" },
                new Dictionary<string, object?> { ["label"] = "تاريخ الشحن", ["value"] = container.ShipmentDate },
                new Dictionary<string, object?> { ["label"] = "الحالة", ["value"] = container.Status.ToString() },
                new Dictionary<string, object?> { ["label"] = "جاهزة للبيع", ["value"] = oc.IsReadyForSale ? "نعم" : "لا" },
                new Dictionary<string, object?> { ["label"] = "تكلفة الاستيراد", ["value"] = container.LandingCost?.TotalImportExpenses },
                new Dictionary<string, object?> { ["label"] = "عدد الأصناف", ["value"] = container.Items.Count }
            ]
        };

        ModuleReportDocumentService.ShowPreview(report, exportPdf: false);
    }

    private static async Task ApproveAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            var result = await ContainerUiService.Instance.ApproveContainerAsync(containerId);
            if (ApplicationResultPresenter.Present(result))
            {
                ContainerListRefreshHub.RequestRefresh();
                ErpDataRefreshHub.RequestRefresh(ErpDataRefreshScope.OperationsCenter);
                MockInteractionService.ShowSuccess(
                    "تم اعتماد الحاوية وإنشاء فاتورة الشراء في المشتريات.",
                    "اعتماد الحاوية");
            }
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning($"تعذّر اعتماد الحاوية.\n\n{ex.Message}", "اعتماد الحاوية");
        }
    }

    private static async Task OpenLinkedPurchaseInvoiceAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await PurchaseUiService.Instance.GetInvoiceBySourceContainerAsync(containerId);
        if (!ApplicationResultPresenter.Present(result))
            return;

        if (result.Value is null)
        {
            MockInteractionService.ShowWarning(
                "لا توجد فاتورة شراء مرتبطة بهذه الحاوية بعد.\n\n" +
                "تُنشأ تلقائياً عند الاعتماد، أو عبر «ربط حاويات معتمدة» من قائمة المشتريات.",
                "فاتورة الشراء");
            return;
        }

        PurchaseActionRouter.OpenOperationsCenter(PurchaseListRow.FromDto(result.Value));
    }

    private static async Task ArchiveAsync(Guid containerId)
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            var result = await ContainerUiService.Instance.ArchiveContainerAsync(containerId);
            if (ApplicationResultPresenter.Present(result))
            {
                ContainerListRefreshHub.RequestRefresh();
                ErpDataRefreshHub.RequestRefresh(ErpDataRefreshScope.OperationsCenter);
                MockInteractionService.ShowSuccess("تم أرشفة الحاوية.", "أرشفة");
            }
        }
        catch (Exception ex)
        {
            MockInteractionService.ShowWarning($"تعذّر أرشفة الحاوية.\n\n{ex.Message}", "أرشفة");
        }
    }

    private static void SelectTab(TabControl? tabs, string tabKey)
    {
        if (tabs is null)
            return;

        for (var i = 0; i < tabs.Items.Count; i++)
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
