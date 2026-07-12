using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Accounting;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Inventory;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Suppliers;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using ERPSystem.Services.Capital;
using ERPSystem.Services.Finance;
using ERPSystem.Services.Expenses;
using ERPSystem.Services.Inventory;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.OperationsCenters;

/// <summary>Real print/PDF export bar for Operations Center «معاينة الطباعة» tabs.</summary>
public static class OperationsCenterPrintPreviewFactory
{
    public static UIElement Build(OperationsCenterContext? context)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };

        if (context is null)
        {
            stack.Children.Add(UnavailableMessage("لم يتم تحديد سجل."));
            return stack;
        }

        if (!SupportsPrintPreview(context.EntityType, context.EntityRow))
        {
            stack.Children.Add(UnavailableMessage(GetUnavailableLabel(context.EntityType)));
            return stack;
        }

        stack.Children.Add(ErpUxFactory.ExportBar("معاينة الطباعة", mode => _ = ExportAsync(context, mode)));
        stack.Children.Add(new TextBlock
        {
            Text = "استخدم «طباعة» للمعاينة أو «PDF» لحفظ الملف — نفس المولّد المستخدم في القائمة ومركز العمليات.",
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 8, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        return stack;
    }

    public static bool SupportsPrintPreview(EntityType entityType, object? entityRow) =>
        entityType switch
        {
            EntityType.SalesInvoice when entityRow is SalesInvoiceListRow => true,
            EntityType.PurchaseInvoice when entityRow is PurchaseListRow => true,
            EntityType.JournalEntry when entityRow is JournalEntryListDto => true,
            EntityType.Warehouse when entityRow is WarehouseListExtendedDto => true,
            EntityType.Expense when entityRow is ExpenseListDto or ExpenseDetailsDto => true,
            EntityType.CapitalPartner when entityRow is CapitalPartnerListDto => true,
            _ => false
        };

    private static async Task ExportAsync(OperationsCenterContext context, string mode)
    {
        if (!AppServices.IsInitialized)
            return;

        var normalized = mode.Trim().ToLowerInvariant();
        var exportPdf = normalized == "pdf";

        switch (context.EntityType)
        {
            case EntityType.SalesInvoice when context.EntityRow is SalesInvoiceListRow salesRow:
                if (normalized is "print" or "pdf")
                    await SalesPopupService.PrintAsync(salesRow, exportPdf);
                else if (normalized == "excel")
                    MockInteractionService.ShowInfo("تصدير Excel غير متاح من معاينة الطباعة.", "معاينة الطباعة");
                break;

            case EntityType.PurchaseInvoice when context.EntityRow is PurchaseListRow purchaseRow:
                if (normalized is "print" or "pdf")
                    await PurchaseActionRouter.PrintAsync(purchaseRow, exportPdf);
                else if (normalized == "excel")
                    MockInteractionService.ShowInfo("تصدير Excel غير متاح من معاينة الطباعة.", "معاينة الطباعة");
                break;

            case EntityType.JournalEntry when context.EntityRow is JournalEntryListDto journalRow:
                if (normalized is "print" or "pdf")
                    await ExportJournalAsync(journalRow.Id, exportPdf);
                else if (normalized == "excel")
                    MockInteractionService.ShowInfo("تصدير Excel غير متاح من معاينة الطباعة.", "معاينة الطباعة");
                break;

            case EntityType.Warehouse when context.EntityRow is WarehouseListExtendedDto warehouse:
                if (normalized is "print" or "pdf")
                    await WarehouseDocumentService.ShowStockPreviewAsync(warehouse.Id, exportPdf);
                else if (normalized == "excel")
                    InventoryExportService.ExportWarehouseStock(warehouse);
                break;

            case EntityType.Expense when context.EntityRow is ExpenseListDto expenseRow:
                if (normalized is "print" or "pdf")
                    await ExpenseDocumentService.HandleExportAsync(
                        exportPdf ? EntityActionId.ExpenseExportPdf : EntityActionId.ExpensePrint,
                        expenseRow);
                else if (normalized == "excel")
                    await ExpenseDocumentService.HandleExportAsync(EntityActionId.ExpenseExportExcel, expenseRow);
                break;

            case EntityType.Expense when context.EntityRow is ExpenseDetailsDto expenseDetails:
                if (normalized is "print" or "pdf")
                    await ExpenseDocumentService.HandleExportAsync(
                        exportPdf ? EntityActionId.ExpenseExportPdf : EntityActionId.ExpensePrint,
                        new ExpenseListDto { Id = expenseDetails.Id, Code = expenseDetails.Code, Name = expenseDetails.Name });
                else if (normalized == "excel")
                    await ExpenseDocumentService.HandleExportAsync(
                        EntityActionId.ExpenseExportExcel,
                        new ExpenseListDto { Id = expenseDetails.Id, Code = expenseDetails.Code, Name = expenseDetails.Name });
                break;

            case EntityType.CapitalPartner when context.EntityRow is CapitalPartnerListDto partner:
                if (normalized is "print" or "pdf")
                    await ExportCapitalPartnerAsync(partner.Id, exportPdf);
                else if (normalized == "excel")
                    await ExportCapitalPartnerExcelAsync(partner.Id);
                break;

            default:
                MockInteractionService.ShowInfo(GetUnavailableLabel(context.EntityType), "معاينة الطباعة");
                break;
        }
    }

    private static async Task ExportJournalAsync(Guid entryId, bool exportPdf)
    {
        var result = await AccountingUiService.Instance.GetJournalDetailsAsync(entryId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        AccountingJournalDocumentService.ShowPreview(result.Value, exportPdf);
    }

    private static async Task ExportCapitalPartnerAsync(Guid partnerId, bool exportPdf)
    {
        var result = await CapitalPartnerUiService.Instance.GetOperationsCenterAsync(partnerId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        CapitalPartnerDocumentService.ShowPreview(result.Value, exportPdf);
    }

    private static async Task ExportCapitalPartnerExcelAsync(Guid partnerId)
    {
        var result = await CapitalPartnerUiService.Instance.GetOperationsCenterAsync(partnerId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        CapitalPartnerDocumentService.ExportExcel(result.Value);
    }

    private static string GetUnavailableLabel(EntityType entityType) => entityType switch
    {
        EntityType.Customer => "معاينة الطباعة — كشف حساب العميل متاح من تبويب «كشف حساب» (Phase B/C).",
        EntityType.Supplier => "معاينة الطباعة — كشف حساب المورد متاح من تبويب «كشف حساب» (Phase B/C).",
        EntityType.ImportContainer => "معاينة الطباعة — ملخص الحاوية من الإجراء السريع «طباعة» (Phase B/C لتبويب مخصص).",
        EntityType.FabricItem => "معاينة الطباعة — غير متاحة لبطاقة الصنف بعد.",
        EntityType.Employee => "معاينة الطباعة — غير متاحة لملف الموظف بعد.",
        EntityType.Cashbox => "معاينة الطباعة — غير متاحة للصندوق بعد.",
        EntityType.JournalEntry => "معاينة الطباعة — افتح القيد من قائمة اليومية (سجل يحمل معرفاً).",
        _ => "معاينة الطباعة — غير متاحة لهذا النوع بعد (Phase B/C)."
    };

    private static UIElement UnavailableMessage(string text) =>
        ErpUxFactory.InfoBanner(text, "info");

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
}
