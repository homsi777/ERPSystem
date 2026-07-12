using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Services;
using ERPSystem.Services.Documents;
using ERPSystem.Services.Inventory;
using System.Windows;

namespace ERPSystem.Services.Inventory;

public static class InventoryExportService
{
    public static async void ExportWarehouseStock(WarehouseListExtendedDto warehouse)
    {
        if (!AppServices.IsInitialized) return;

        var oc = await InventoryUiService.Instance.GetOperationsCenterAsync(warehouse.Id);
        if (!oc.IsSuccess || oc.Value is null)
        {
            MessageBox.Show("تعذر تحميل بيانات المستودع.", "تصدير", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        ExportOperationsCenterStock(oc.Value);
    }

    public static void ExportOperationsCenterStock(InventoryOperationsCenterDto data)
    {
        ArgumentNullException.ThrowIfNull(data);
        ExportFabricStockRows(data.Stock, $"مخزون - {data.Warehouse.NameAr}");
    }

    public static void ExportFabricStockRows(IReadOnlyList<FabricStockBalanceDto> stock, string title)
    {
        ListExportService.ExportRecords(
            stock,
            title,
            ("المستودع", s => s.WarehouseName),
            ("القماش", s => s.FabricName),
            ("اللون", s => s.ColorName),
            ("الحاوية", s => s.ContainerNumber),
            ("Rolls", s => s.RollCount),
            ("أمتار", s => s.TotalMeters),
            ("محجوز", s => s.ReservedMeters),
            ("متاح", s => s.AvailableMeters),
            ("قيمة", s => s.InventoryValue));
    }
}
