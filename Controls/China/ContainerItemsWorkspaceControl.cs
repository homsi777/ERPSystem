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

        var rows = container.Items.Select(i => new
        {
            السطر = i.LineNumber,
            الأثواب = i.RollCount,
            الأمتار = $"{i.LengthMeters:N2}",
            الحالة = i.IsValid ? "صالح" : "خطأ"
        }).Cast<object>().ToArray();

        return ErpUiFactory.Card(ErpUiFactory.BuildGrid(rows));
    }
}
