using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Controls;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Capital;

public sealed class CapitalReportsControl : UserControl
{
    private readonly ComboBox _reportType = ErpUiFactory.FilterCombo(
        ["ملخص رأس المال", "كشف حساب شريك", "دفتر الاستثمار", "تحليل العائد"]);
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, Margin = new Thickness(0, 12, 0, 0) };
    private readonly TextBlock _summary = new() { Margin = new Thickness(0, 12, 0, 0), FontWeight = FontWeights.SemiBold };

    public CapitalReportsControl()
    {
        var root = new StackPanel { Margin = new Thickness(16) };
        root.Children.Add(ErpUiFactory.SectionTitle("تقارير رأس المال والشركاء"));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var loadBtn = new Button { Content = "عرض التقرير", Style = S("PrimaryButtonStyle"), MinWidth = 120 };
        loadBtn.Click += async (_, _) => await LoadAsync();
        var printBtn = new Button { Content = "طباعة", Style = S("SecondaryButtonStyle"), Margin = new Thickness(8, 0, 0, 0) };
        printBtn.Click += (_, _) => MockInteractionService.ShowDocumentPreview("تقرير رأس المال", "PDF");
        actions.Children.Add(_reportType);
        actions.Children.Add(loadBtn);
        actions.Children.Add(printBtn);
        root.Children.Add(actions);

        ErpUiFactory.AddGridColumn(_grid, "المفتاح", nameof(CapitalReportRowDto.Key), 100);
        ErpUiFactory.AddGridColumn(_grid, "الوصف", nameof(CapitalReportRowDto.Label), "*");
        ErpUiFactory.AddGridColumn(_grid, "تفاصيل", nameof(CapitalReportRowDto.SubLabel), 120);
        ErpUiFactory.AddGridColumn(_grid, "المبلغ", nameof(CapitalReportRowDto.Amount), 120, "N2");
        root.Children.Add(ErpUiFactory.Card(_grid));
        root.Children.Add(_summary);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = root };
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;
        var type = _reportType.SelectedIndex switch
        {
            1 => "Statement",
            2 => "Ledger",
            3 => "ROI",
            _ => "Summary"
        };

        var result = await CapitalPartnerUiService.Instance.GetReportAsync(type);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        _grid.ItemsSource = result.Value.Rows;
        _summary.Text = $"{result.Value.Title} — الإجمالي: {result.Value.TotalBase:N2} {result.Value.BaseCurrency}";
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
