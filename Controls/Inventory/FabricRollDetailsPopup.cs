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
            BuildContent(rolls, WpfGeneralManagerAccess.CanViewSensitivePricing),
            "\uE7B8",
            760,
            720);
    }

    private static UIElement BuildContent(IReadOnlyList<FabricRollListDto> rolls, bool showPricing)
    {
        var panel = new StackPanel { Margin = new Thickness(16) };

        var totalRemaining = rolls.Sum(r => r.RemainingLengthMeters);
        var totalRemainingYards = rolls.Sum(r => r.RemainingLengthYards);
        var summary = showPricing
            ? $"عدد الأتواب: {rolls.Count}  •  إجمالي المتبقي: {totalRemaining:N2} م ({totalRemainingYards:N2} ي)  •  القيمة: {rolls.Sum(r => r.CurrentValue):N2} $"
            : $"عدد الأتواب: {rolls.Count}  •  إجمالي المتبقي: {totalRemaining:N2} م ({totalRemainingYards:N2} ي)";
        panel.Children.Add(new TextBlock
        {
            Text = summary,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 10),
            TextWrapping = TextWrapping.Wrap
        });

        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 320 };
        ErpUiFactory.AddGridColumn(grid, "رقم التوب", nameof(FabricRollListDto.RollNumber), 90, null);
        ErpUiFactory.AddGridColumn(grid, "اللوت", nameof(FabricRollListDto.LotCode), 90, null);
        ErpUiFactory.AddGridColumn(grid, "الباركود", nameof(FabricRollListDto.Barcode), 120, null);
        ErpUiFactory.AddGridColumn(grid, "الطول الأصلي (م)", nameof(FabricRollListDto.LengthMeters), 100, "N2");
        ErpUiFactory.AddGridColumn(grid, "الطول الأصلي (ي)", nameof(FabricRollListDto.LengthYards), 100, "N2");
        ErpUiFactory.AddGridColumn(grid, "المتبقي (م)", nameof(FabricRollListDto.RemainingLengthMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(grid, "المتبقي (ي)", nameof(FabricRollListDto.RemainingLengthYards), 90, "N2");
        if (showPricing)
        {
            ErpUiFactory.AddGridColumn(grid, "التكلفة/م", nameof(FabricRollListDto.CostPerMeter), 90, "N2");
            ErpUiFactory.AddGridColumn(grid, "التكلفة/ي", nameof(FabricRollListDto.CostPerYard), 90, "N2");
            ErpUiFactory.AddGridColumn(grid, "القيمة $", nameof(FabricRollListDto.CurrentValue), 100, "N2");
        }
        ErpUiFactory.AddGridColumn(grid, "الموقع", nameof(FabricRollListDto.LocationCode), 80, null);
        ErpUiFactory.AddGridColumn(grid, "الحالة", nameof(FabricRollListDto.Status), 90, null);
        grid.ItemsSource = rolls;
        panel.Children.Add(grid);

        return panel;
    }
}
