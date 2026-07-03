using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.Results;
using ERPSystem.Core;
using ERPSystem.Core.Navigation;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Reports;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Reports;

/// <summary>Generic module report runner with date filters, KPIs, and grid.</summary>
public sealed class ModuleReportViewControl : UserControl
{
    private readonly AppModule _module;
    private readonly string _reportKey;
    private readonly ModuleReportDef _definition;
    private readonly DatePicker _from = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-1));
    private readonly DatePicker _to = ErpUiFactory.FormDate(DateTime.Today);
    private readonly StackPanel _kpiRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
    private readonly TextBlock _meta = new()
    {
        FontSize = 12,
        Foreground = Br("TextSecondaryBrush"),
        Margin = new Thickness(0, 0, 0, 8),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 280 };
    private readonly StackPanel _body = new();

    public event EventHandler? BackRequested;

    public ModuleReportViewControl(AppModule module, string reportKey)
    {
        _module = module;
        _reportKey = reportKey;
        _definition = ModuleReportRegistry.Find(module, reportKey)
            ?? new ModuleReportDef(reportKey, reportKey, "", "\uE9D2", "");

        Background = Br("AppBgBrush");
        HorizontalAlignment = HorizontalAlignment.Stretch;
        BuildUi();
        Loaded += async (_, _) => await RunAsync();
    }

    private void BuildUi()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_grid);

        var stack = new StackPanel { Margin = new Thickness(16) };

        var headerRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        var backBtn = new Button
        {
            Content = "← تقارير القسم",
            Style = S("SecondaryButtonStyle"),
            Margin = new Thickness(0, 0, 12, 0)
        };
        backBtn.Click += (_, _) => BackRequested?.Invoke(this, EventArgs.Empty);
        headerRow.Children.Add(backBtn);
        headerRow.Children.Add(ErpUiFactory.SectionTitle(_definition.TitleAr));
        stack.Children.Add(headerRow);

        if (!string.IsNullOrWhiteSpace(_definition.DescriptionAr))
        {
            stack.Children.Add(new TextBlock
            {
                Text = _definition.DescriptionAr,
                Foreground = Br("TextSecondaryBrush"),
                Margin = new Thickness(0, 0, 0, 12)
            });
        }

        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFilterRow(
            ("من تاريخ", _from),
            ("إلى تاريخ", _to))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var runBtn = new Button { Content = "عرض التقرير", Style = S("PrimaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
        runBtn.Click += async (_, _) => await RunAsync();
        actions.Children.Add(runBtn);

        foreach (var label in new[] { "طباعة", "PDF", "Excel" })
        {
            var btn = new Button { Content = label, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
            btn.Click += (_, _) => MockInteractionService.ShowDocumentPreview(_definition.TitleAr, label == "Excel" ? "Excel" : "PDF");
            actions.Children.Add(btn);
        }

        stack.Children.Add(actions);
        stack.Children.Add(_meta);
        stack.Children.Add(_kpiRow);
        stack.Children.Add(_body);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack
        };
    }

    private async Task RunAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await ModuleReportUiService.Instance.GetReportAsync(
            _reportKey, _from.SelectedDate, _to.SelectedDate);

        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        Render(result.Value);
    }

    private void Render(ModuleReportResultDto report)
    {
        _body.Children.Clear();
        _kpiRow.Children.Clear();
        _grid.Columns.Clear();

        var from = report.FromDate?.ToString("yyyy/MM/dd") ?? "—";
        var to = report.ToDate?.ToString("yyyy/MM/dd") ?? "—";
        _meta.Text =
            $"{report.Title}  •  الفترة: {from} → {to}  •  {report.Rows.Count} سجل  •  {report.GeneratedAt.ToLocalTime():yyyy/MM/dd HH:mm}";

        if (report.Kpis.Count > 0)
        {
            ErpUiFactory.SetSummaryCards(_kpiRow, report.Kpis.Select(k =>
                (k.Label, k.Value, k.IconGlyph, B("PrimaryBrush"))).ToArray());
        }

        if (report.Rows.Count == 0)
        {
            _body.Children.Add(ErpUxFactory.InfoBanner("لا توجد بيانات ضمن الفترة المحددة.", "warning"));
            return;
        }

        foreach (var col in report.Columns)
        {
            var binding = new Binding($"[{col.Key}]")
            {
                StringFormat = col.Format
            };

            var column = new DataGridTextColumn
            {
                Header = col.HeaderAr,
                Binding = binding
            };

            if (col.IsStar)
                column.Width = new DataGridLength(1, DataGridLengthUnitType.Star);
            else
                column.Width = new DataGridLength(col.Width > 0 ? col.Width : 100);

            _grid.Columns.Add(column);
        }

        _grid.ItemsSource = report.Rows.Select(r => new ModuleReportRowBinder(r)).ToList();
        ErpUiFactory.DetachFromVisualTree(_grid);
        _body.Children.Add(ErpUiFactory.SectionTitle("النتائج"));
        _body.Children.Add(ErpUiFactory.Card(_grid));
    }

    private sealed class ModuleReportRowBinder
    {
        private readonly Dictionary<string, object?> _cells;

        public ModuleReportRowBinder(Dictionary<string, object?> cells) => _cells = cells;

        public object? this[string key]
        {
            get
            {
                if (!_cells.TryGetValue(key, out var value) || value is null)
                    return null;

                return value switch
                {
                    DateTime dt => dt.ToLocalTime(),
                    decimal d => d,
                    double d => d,
                    int i => i,
                    _ => value.ToString()
                };
            }
        }
    }

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
    private static Brush Br(string k) => (Brush)WpfApplication.Current.Resources[k]!;
    private static SolidColorBrush B(string k) => (SolidColorBrush)WpfApplication.Current.Resources[k]!;
}
