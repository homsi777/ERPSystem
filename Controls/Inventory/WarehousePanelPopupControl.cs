using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Documents;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public enum WarehousePopupPanel
{
    Stock,
    Movements,
    Timeline,
    Audit
}

/// <summary>عرض بيانات مستودع في نافذة منبثقة — بدون مغادرة القائمة.</summary>
public sealed class WarehousePanelPopupControl : UserControl
{
    private InventoryOperationsCenterDto? _loaded;

    public WarehousePanelPopupControl(Guid warehouseId, WarehousePopupPanel panel)
    {
        Content = new TextBlock
        {
            Text = "جاري التحميل...",
            Margin = new Thickness(24),
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextMutedBrush"]!
        };
        Loaded += async (_, _) => await LoadAsync(warehouseId, panel);
    }

    private async Task LoadAsync(Guid warehouseId, WarehousePopupPanel panel)
    {
        if (!AppServices.IsInitialized) return;
        var result = await InventoryUiService.Instance.GetOperationsCenterAsync(warehouseId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            Content = new TextBlock { Text = "تعذر تحميل البيانات.", Margin = new Thickness(24) };
            return;
        }

        var oc = result.Value;
        _loaded = oc;
        Content = panel switch
        {
            WarehousePopupPanel.Stock => BuildStock(oc),
            WarehousePopupPanel.Movements => BuildGrid("الحركات", oc.RecentMovements,
                ("الرقم", nameof(StockMovementListDto.MovementNumber)),
                ("النوع", nameof(StockMovementListDto.Type)),
                ("أمتار", nameof(StockMovementListDto.TotalMeters)),
                ("قيمة", nameof(StockMovementListDto.TotalValue)),
                ("تاريخ", nameof(StockMovementListDto.MovementDate))),
            WarehousePopupPanel.Timeline => BuildGrid("الخط الزمني", oc.Timeline,
                ("التاريخ", nameof(InventoryTimelineDto.OccurredAt)),
                ("الحدث", nameof(InventoryTimelineDto.Title)),
                ("المستخدم", nameof(InventoryTimelineDto.Username))),
            WarehousePopupPanel.Audit => BuildGrid("التدقيق", oc.RecentAudit,
                ("التاريخ", nameof(InventoryAuditDto.RecordedAt)),
                ("الإجراء", nameof(InventoryAuditDto.Action)),
                ("المستخدم", nameof(InventoryAuditDto.Username)),
                ("قبل", nameof(InventoryAuditDto.PreviousValue)),
                ("بعد", nameof(InventoryAuditDto.NewValue))),
            _ => new TextBlock { Text = "—" }
        };
    }

    private UIElement BuildStock(InventoryOperationsCenterDto oc)
    {
        var sp = new StackPanel();
        sp.Children.Add(ErpUxFactory.ExportBar($"مخزون {oc.Warehouse.NameAr}", mode =>
        {
            switch (mode)
            {
                case "print":
                    WarehouseDocumentService.ShowPreview(oc, exportPdf: false);
                    break;
                case "pdf":
                    WarehouseDocumentService.ShowPreview(oc, exportPdf: true);
                    break;
                case "excel":
                    InventoryExportService.ExportOperationsCenterStock(oc);
                    break;
            }
        }));
        sp.Children.Add(ErpUxFactory.InfoBanner(
            $"قيمة المخزون: ${oc.InventoryValue:N2}  •  {oc.Stock.Count} صنف  •  {oc.Rolls.Count} roll"));
        if (oc.Stock.Count == 0)
        {
            sp.Children.Add(new TextBlock
            {
                Text = "لا أرصدة — أضف مخزوناً عبر أول المدة أو استيراد حاوية.",
                Margin = new Thickness(0, 12, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            return sp;
        }
        var g = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, ItemsSource = oc.Stock, MaxHeight = 480 };
        ErpUiFactory.AddGridColumn(g, "القماش", nameof(FabricStockBalanceDto.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(g, "اللون", nameof(FabricStockBalanceDto.ColorName), 100, null);
        ErpUiFactory.AddGridColumn(g, "Rolls", nameof(FabricStockBalanceDto.RollCount), 70, null);
        ErpUiFactory.AddGridColumn(g, "متاح", nameof(FabricStockBalanceDto.AvailableMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(g, "قيمة", nameof(FabricStockBalanceDto.InventoryValue), 100, "N2");
        sp.Children.Add(g);
        return sp;
    }

    private static UIElement BuildGrid<T>(string title, IReadOnlyList<T> data, params (string, string)[] cols)
    {
        var sp = new StackPanel();
        sp.Children.Add(ErpUiFactory.SectionTitle(title));
        if (data.Count == 0)
        {
            sp.Children.Add(new TextBlock
            {
                Text = "لا سجلات في PostgreSQL بعد.",
                Margin = new Thickness(0, 8, 0, 0),
                TextWrapping = TextWrapping.Wrap
            });
            return sp;
        }
        var g = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, ItemsSource = data, MaxHeight = 480 };
        foreach (var (h, p) in cols)
            ErpUiFactory.AddGridColumn(g, h, p, "*", null);
        sp.Children.Add(g);
        return sp;
    }
}
