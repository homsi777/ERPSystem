using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.China;

public sealed class ContainerItemsWorkspaceControl : UserControl
{
    private readonly ContentPresenter _host = new();
    private readonly TextBlock _loading = new()
    {
        Text = "جاري تحميل أصناف الحاوية...",
        Margin = new Thickness(12),
        FontSize = 13
    };

    private Guid _containerId;

    public ContainerItemsWorkspaceControl()
    {
        Content = _loading;
    }

    public void Initialize(Guid containerId)
    {
        _containerId = containerId;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        if (!AppServices.IsInitialized)
            return;

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(_containerId);
        if (!result.IsSuccess || result.Value?.Container is null)
        {
            Content = ErpUxFactory.InfoBanner("تعذّر تحميل بنود الحاوية.", "warning");
            return;
        }

        Content = BuildItemsGrid(result.Value.Container);
    }

    private static UIElement BuildItemsGrid(ContainerDetailsDto container)
    {
        if (container.Items.Count == 0)
            return ErpUxFactory.InfoBanner("لا توجد بنود مسجّلة لهذه الحاوية.", "info");

        var unit = container.DplQuantityUnit;
        var displayRows = container.Items.Select(i => new
        {
            i.LineNumber,
            i.RollCount,
            LengthDisplay = ChinaImportLengthDisplay.FormatLength(i.LengthMeters, unit),
            Status = i.IsValid ? "صالح" : "خطأ"
        }).ToList();

        var g = ErpUiFactory.BuildGrid(displayRows, false);
        g.AutoGenerateColumns = false;
        ErpUiFactory.AddGridColumn(g, "السطر", "LineNumber", 60, null);
        ErpUiFactory.AddGridColumn(g, "الأثواب", "RollCount", 80, null);
        ErpUiFactory.AddGridColumn(g, ChinaImportLengthDisplay.LengthColumnHeader(unit), "LengthDisplay", 100, null);
        ErpUiFactory.AddGridColumn(g, "الحالة", "Status", 80, null);
        return ErpUiFactory.Card(g);
    }
}
