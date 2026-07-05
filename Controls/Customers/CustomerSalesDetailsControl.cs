using ERPSystem.Application.DTOs.Customers;
using ERPSystem.Application.Results;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Customers;

public sealed class CustomerSalesDetailsControl : UserControl
{
    private readonly Guid _customerId;
    private readonly string _customerName;
    private readonly DatePicker _dpFrom;
    private readonly DatePicker _dpTo;
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true };
    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Margin = new Thickness(0, 10, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!
    };

    public CustomerSalesDetailsControl(Guid customerId, string customerName)
    {
        _customerId = customerId;
        _customerName = customerName;
        _dpFrom = ErpUiFactory.FormDate(DateTime.Today.AddYears(-1));
        _dpTo = ErpUiFactory.FormDate(DateTime.Today);

        var root = new StackPanel { Margin = new Thickness(4) };
        root.Children.Add(ErpUxFactory.InfoBanner(
            "ما اشتراه العميل وبأي سعر — اختر فترة التاريخ ثم اضغط «عرض».",
            "info"));

        root.Children.Add(BuildFilterPanel());

        ConfigureGrid();
        root.Children.Add(_grid);
        root.Children.Add(_status);

        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private UIElement BuildFilterPanel()
    {
        var panel = new Grid { Margin = new Thickness(0, 12, 0, 10) };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(168) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var lblFrom = FilterLabel("من");
        Grid.SetColumn(lblFrom, 0);
        Grid.SetColumn(_dpFrom, 1);
        _dpFrom.Margin = new Thickness(0, 0, 16, 0);
        _dpFrom.HorizontalAlignment = HorizontalAlignment.Stretch;

        var lblTo = FilterLabel("إلى");
        Grid.SetColumn(lblTo, 2);
        Grid.SetColumn(_dpTo, 3);
        _dpTo.Margin = new Thickness(0, 0, 12, 0);
        _dpTo.HorizontalAlignment = HorizontalAlignment.Stretch;

        var btnShow = new Button
        {
            Content = "عرض",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            Height = ErpDesignTokens.ControlHeight,
            MinWidth = 88,
            Padding = new Thickness(16, 0, 16, 0),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        btnShow.Click += async (_, _) => await LoadAsync();
        Grid.SetColumn(btnShow, 4);

        panel.Children.Add(lblFrom);
        panel.Children.Add(_dpFrom);
        panel.Children.Add(lblTo);
        panel.Children.Add(_dpTo);
        panel.Children.Add(btnShow);
        return panel;
    }

    private static TextBlock FilterLabel(string text) => new()
    {
        Text = text,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(0, 0, 8, 8),
        FontSize = 12,
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!
    };

    private void ConfigureGrid()
    {
        _grid.MaxHeight = 360;
        _grid.RowHeight = 34;
        _grid.ColumnHeaderHeight = 36;
        _grid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
        _grid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
        _grid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        _grid.FlowDirection = FlowDirection.RightToLeft;

        _grid.Columns.Add(BuildTextColumn("التاريخ", nameof(CustomerSalesDetailRow.SaleDateDisplay), 118));
        ErpUiFactory.AddGridColumn(_grid, "اسم التوب", nameof(CustomerSalesDetailRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_grid, "الكود", nameof(CustomerSalesDetailRow.FabricCode), 90, null);
        ErpUiFactory.AddGridColumn(_grid, "اللون", nameof(CustomerSalesDetailRow.ColorName), 100, null);
        ErpUiFactory.AddGridColumn(_grid, "سعر المتر", nameof(CustomerSalesDetailRow.UnitPrice), 96, "N2");
    }

    private static DataGridTextColumn BuildTextColumn(string header, string path, double width)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.NoWrap));
        style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
        style.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
        style.Setters.Add(new Setter(TextBlock.PaddingProperty, new Thickness(8, 0, 8, 0)));

        return new DataGridTextColumn
        {
            Header = header,
            Binding = new Binding(path),
            Width = width,
            ElementStyle = style
        };
    }

    private async Task LoadAsync()
    {
        if (_dpFrom.SelectedDate is null || _dpTo.SelectedDate is null)
        {
            MockInteractionService.ShowWarning("اختر تاريخ البداية والنهاية.", "تفاصيل بيع");
            return;
        }

        if (_dpFrom.SelectedDate > _dpTo.SelectedDate)
        {
            MockInteractionService.ShowWarning("تاريخ البداية يجب أن يكون قبل تاريخ النهاية.", "تفاصيل بيع");
            return;
        }

        _status.Text = "جاري التحميل...";
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var result = await CustomerUiService.Instance.GetSalesDetailsAsync(
                _customerId,
                _dpFrom.SelectedDate.Value.Date,
                _dpTo.SelectedDate.Value.Date);
            if (!ApplicationResultPresenter.Present(result))
            {
                _status.Text = "تعذّر تحميل تفاصيل البيع.";
                return;
            }

            var rows = (result.Value ?? [])
                .Select(CustomerSalesDetailRow.FromDto)
                .ToList();
            _grid.ItemsSource = rows;

            var from = _dpFrom.SelectedDate.Value.ToString("yyyy/MM/dd");
            var to = _dpTo.SelectedDate.Value.ToString("yyyy/MM/dd");
            _status.Text = rows.Count == 0
                ? $"لا توجد مبيعات بين {from} و {to}."
                : $"{rows.Count} صف — {_customerName} ({from} → {to})";
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private sealed class CustomerSalesDetailRow
    {
        public string SaleDateDisplay { get; init; } = "";
        public string FabricName { get; init; } = "";
        public string FabricCode { get; init; } = "";
        public string ColorName { get; init; } = "";
        public decimal UnitPrice { get; init; }

        public static CustomerSalesDetailRow FromDto(CustomerSalesDetailDto dto) => new()
        {
            SaleDateDisplay = dto.SaleDate.ToString("yyyy/MM/dd"),
            FabricName = dto.FabricName,
            FabricCode = dto.FabricCode,
            ColorName = dto.ColorName,
            UnitPrice = dto.UnitPrice
        };
    }
}
