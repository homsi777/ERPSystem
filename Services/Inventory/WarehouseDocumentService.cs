using ERPSystem.Application.Documents;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Services.Documents;

namespace ERPSystem.Services.Inventory;

public static class WarehouseDocumentService
{
    private static WarehouseStockReportPdfGenerator? _generator;

    private static WarehouseStockReportPdfGenerator Generator =>
        _generator ??= WarehouseStockReportPdfGenerator.FromContentRoot(AppContext.BaseDirectory);

    public static async Task ShowStockPreviewAsync(Guid warehouseId, bool exportPdf)
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await InventoryUiService.Instance.GetOperationsCenterAsync(warehouseId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        ShowPreview(result.Value, exportPdf);
    }

    public static void ShowPreview(InventoryOperationsCenterDto data, bool exportPdf)
    {
        ArgumentNullException.ThrowIfNull(data);
        var pdfBytes = Generator.Generate(data);
        var name = data.Warehouse.NameAr;

        if (exportPdf)
        {
            PdfPreviewWindow.SaveAndOpenFromBytes(pdfBytes, $"مخزون - {name} - {DateTime.Now:yyyy-MM-dd}.pdf");
            return;
        }

        PdfPreviewWindow.ShowFromBytes(pdfBytes, $"تقرير مخزون — {name}");
    }
}
