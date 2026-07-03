using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalDistributionsControl : UserControl
{
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };
    private readonly TextBlock _sectionTitle = ErpUiFactory.SectionTitle("توزيعات الأرباح والخسائر");
    private readonly TextBlock _sectionHint = new()
    {
        Text = "متابعة دورة توزيع الأرباح من المسودة حتى الإغلاق",
        Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!,
        Margin = new Thickness(0, 0, 0, 12)
    };

    public CapitalDistributionsControl()
    {
        var root = new StackPanel();
        root.Children.Add(_sectionTitle);
        root.Children.Add(_sectionHint);

        ErpUiFactory.AddGridColumn(_grid, "الكود", nameof(ProfitDistributionListDto.Code), 120);
        ErpUiFactory.AddGridColumn(_grid, "النطاق", nameof(ProfitDistributionListDto.ScopeDisplay), 90);
        ErpUiFactory.AddGridColumn(_grid, "من", nameof(ProfitDistributionListDto.PeriodStart), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "إلى", nameof(ProfitDistributionListDto.PeriodEnd), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "ربح", nameof(ProfitDistributionListDto.NetProfit), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "خسارة", nameof(ProfitDistributionListDto.NetLoss), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "الحالة", nameof(ProfitDistributionListDto.StatusDisplay), 120);
        root.Children.Add(ErpUiFactory.Card(_grid));

        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    public void BindPopupHost()
    {
        _sectionTitle.Visibility = Visibility.Collapsed;
        _sectionHint.Visibility = Visibility.Collapsed;
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var result = await CapitalPartnerUiService.Instance.GetDistributionsAsync();
        if (ApplicationResultPresenter.Present(result))
            _grid.ItemsSource = result.Value ?? Array.Empty<ProfitDistributionListDto>();
    }
}
