using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using Microsoft.Win32;
using System.IO;
using System.Text;
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

        var data = oc.Value;
        var dlg = new SaveFileDialog
        {
            Filter = "CSV|*.csv",
            FileName = $"warehouse-{warehouse.Code}-stock.csv"
        };
        if (dlg.ShowDialog() != true) return;

        var sb = new StringBuilder();
        sb.AppendLine($"مستودع,{warehouse.NameAr},{warehouse.Code}");
        sb.AppendLine($"قيمة المخزون,{data.InventoryValue:N2}");
        sb.AppendLine($"Rolls,{data.Warehouse.RollCount}");
        sb.AppendLine($"أمتار,{data.Warehouse.TotalMeters:N2}");
        sb.AppendLine();
        sb.AppendLine("القماش,اللون,Rolls,أمتار,محجوز,متاح,قيمة");

        foreach (var s in data.Stock)
            sb.AppendLine($"{Csv(s.FabricName)},{Csv(s.ColorName)},{s.RollCount},{s.TotalMeters:N2},{s.ReservedMeters:N2},{s.AvailableMeters:N2},{s.InventoryValue:N2}");

        sb.AppendLine();
        sb.AppendLine("Roll,باركود,قماش,لون,متبقي,قيمة,حالة,موقع");
        foreach (var r in data.Rolls)
            sb.AppendLine($"{r.RollNumber},{Csv(r.Barcode)},{Csv(r.FabricName)},{Csv(r.ColorName)},{r.RemainingLengthMeters:N2},{r.CurrentValue:N2},{Csv(r.Status)},{Csv(r.LocationCode)}");

        await File.WriteAllTextAsync(dlg.FileName, sb.ToString(), Encoding.UTF8);
        MessageBox.Show($"تم التصدير: {dlg.FileName}", "Excel/CSV", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private static string Csv(string? v)
    {
        if (string.IsNullOrEmpty(v)) return "";
        return v.Contains(',') || v.Contains('"') ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
    }
}
