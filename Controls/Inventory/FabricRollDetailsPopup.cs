using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Dialogs;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

/// <summary>Shows the physical fabric rolls (lengths + counts) behind a stock balance row.</summary>
public static class FabricRollDetailsPopup
{
    public static async Task ShowForStockRowAsync(FabricStockBalanceDto? row)
    {
        if (row is null || !AppServices.IsInitialized)
            return;

        var result = await InventoryUiService.Instance.GetFabricRollsByStockAsync(
            row.WarehouseId, row.ContainerId, row.FabricItemId, row.FabricColorId);

        if (!result.IsSuccess || result.Value is null)
        {
            MockInteractionService.ShowWarning("تعذّر تحميل تفاصيل الأتواب.");
            return;
        }

        var rolls = result.Value;
        if (rolls.Count == 0)
        {
            MockInteractionService.ShowInfo(
                $"لا توجد أتواب متبقية لهذا الصنف ({row.FabricName} — {row.ColorName}).",
                "تفاصيل الأتواب");
            return;
        }

        ErpModalWindow.Show(
            $"{row.FabricName} — {row.ColorName}",
            $"الحاوية {row.ContainerNumber} • المستودع {row.WarehouseName}",
            BuildContent(rolls),
            "\uE7B8",
            760,
            640);
    }

    private static UIElement BuildContent(IReadOnlyList<FabricRollListDto> rolls)
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        var totalRemaining = rolls.Sum(r => r.RemainingLengthMeters);
        var totalValue = rolls.Sum(r => r.CurrentValue);
        panel.Children.Add(new TextBlock
        {
            Text = $"عدد الأتواب: {rolls.Count}  •  إجمالي المتبقي: {totalRemaining:N2} م  •  القيمة: {totalValue:N2} $",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap
        });

        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 320 };
        ErpUiFactory.AddGridColumn(grid, "رقم التوب", nameof(FabricRollListDto.RollNumber), 90, null);
        ErpUiFactory.AddGridColumn(grid, "اللوت", nameof(FabricRollListDto.LotCode), 90, null);
        ErpUiFactory.AddGridColumn(grid, "الباركود", nameof(FabricRollListDto.Barcode), 120, null);
        ErpUiFactory.AddGridColumn(grid, "الطول الأصلي", nameof(FabricRollListDto.LengthMeters), 100, "N2");
        ErpUiFactory.AddGridColumn(grid, "المتبقي", nameof(FabricRollListDto.RemainingLengthMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(grid, "التكلفة/م", nameof(FabricRollListDto.CostPerMeter), 90, "N2");
        ErpUiFactory.AddGridColumn(grid, "القيمة $", nameof(FabricRollListDto.CurrentValue), 100, "N2");
        ErpUiFactory.AddGridColumn(grid, "الموقع", nameof(FabricRollListDto.LocationCode), 80, null);
        ErpUiFactory.AddGridColumn(grid, "الحالة", nameof(FabricRollListDto.Status), 90, null);
        grid.ItemsSource = rolls;
        panel.Children.Add(grid);

        return panel;
    }
}
