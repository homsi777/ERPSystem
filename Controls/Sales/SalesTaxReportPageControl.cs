using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.Sales;

/// <summary>Sales tax report — frozen snapshot rows with filters and summary KPIs.</summary>
public sealed class SalesTaxReportPageControl : UserControl
{
    private readonly DatePicker _from = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-1));
    private readonly DatePicker _to = ErpUiFactory.FormDate(DateTime.Today);
    private readonly CheckBox _includeLegacy = new() { Content = "تضمين الفواتير التاريخية (Legacy)", IsChecked = true, VerticalAlignment = VerticalAlignment.Center };
    private readonly StackPanel _kpiRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
    private readonly TextBlock _meta = new()
    {
        FontSize = 12,
        Foreground = Br("TextSecondaryBrush"),
        Margin = new Thickness(0, 0, 0, 8),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 280 };
    private readonly DataGrid _summaryGrid = new() { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 200, Margin = new Thickness(0, 12, 0, 0) };
    private bool _running;

    public SalesTaxReportPageControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Br("AppBgBrush");
        BuildGridColumns();
        BuildSummaryColumns();

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("تقرير ضريبة المبيعات"));
        stack.Children.Add(new TextBlock
        {
            Text = "يعتمد على Tax Snapshots المجمدة — لا يُعاد حساب الضريبة من أكواد اليوم.",
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFilterRow(
            ("من تاريخ", _from),
            ("إلى تاريخ", _to),
            ("", _includeLegacy))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var runBtn = new Button { Content = "عرض التقرير", Style = S("PrimaryButtonStyle") };
        runBtn.Click += async (_, _) => await RunAsync();
        actions.Children.Add(runBtn);
        stack.Children.Add(actions);
        stack.Children.Add(_kpiRow);
        stack.Children.Add(_meta);
        stack.Children.Add(ErpUiFactory.SectionTitle("تفاصيل الفواتير"));
        stack.Children.Add(ErpUiFactory.Card(_grid));
        stack.Children.Add(ErpUiFactory.SectionTitle("ملخص حسب كود الضريبة"));
        stack.Children.Add(ErpUiFactory.Card(_summaryGrid));

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack };
        _from.SelectedDateChanged += async (_, _) => await RunAsync();
        _to.SelectedDateChanged += async (_, _) => await RunAsync();
        _includeLegacy.Checked += async (_, _) => await RunAsync();
        _includeLegacy.Unchecked += async (_, _) => await RunAsync();
        Loaded += async (_, _) => await RunAsync();
    }

    private void BuildGridColumns()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_grid);
        _grid.Columns.Add(Col("رقم الفاتورة", nameof(ReportRow.InvoiceNumber), 120));
        _grid.Columns.Add(Col("التاريخ", nameof(ReportRow.InvoiceDateDisplay), 100));
        _grid.Columns.Add(Col("العميل", nameof(ReportRow.CustomerName), star: true));
        _grid.Columns.Add(Col("كود الضريبة", nameof(ReportRow.TaxCode), 90));
        _grid.Columns.Add(Col("المبلغ الخاضع", nameof(ReportRow.TaxableAmount), 110, "N2"));
        _grid.Columns.Add(Col("ضريبة المخرجات", nameof(ReportRow.TaxAmount), 110, "N2"));
        _grid.Columns.Add(Col("الترحيل", nameof(ReportRow.PostingStatus), 90));
        _grid.Columns.Add(Col("قيد اليومية", nameof(ReportRow.JournalEntryNumber), 110));
        _grid.Columns.Add(Col("Legacy", nameof(ReportRow.LegacyDisplay), 70));
    }

    private void BuildSummaryColumns()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_summaryGrid);
        _summaryGrid.Columns.Add(Col("كود الضريبة", nameof(SummaryRow.TaxCode), star: true));
        _summaryGrid.Columns.Add(Col("المبلغ الخاضع", nameof(SummaryRow.TaxableAmount), 130, "N2"));
        _summaryGrid.Columns.Add(Col("ضريبة المخرجات", nameof(SummaryRow.TaxAmount), 130, "N2"));
    }

    private async Task RunAsync()
    {
        using var perfScope = ScreenLoadProfiler.Begin("Sales.TaxReport");
        if (!AppServices.IsInitialized || _running) return;
        try
        {
            _running = true;
            _meta.Text = "جار تحميل التقرير...";
            var from = _from.SelectedDate ?? DateTime.Today.AddMonths(-1);
            var to = _to.SelectedDate ?? DateTime.Today;
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => SalesUiService.Instance.GetTaxReportAsync(from, to, _includeLegacy.IsChecked == true));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
            Render(result.Value, from, to);
        }
        finally
        {
            _running = false;
        }
    }

    private void Render(SalesTaxReportDto report, DateTime from, DateTime to)
    {
        _kpiRow.Children.Clear();
        var outputVat = report.Rows.Where(r => !r.IsLegacyUntaxed).Sum(r => r.TaxAmount);
        var taxable = report.Rows.Where(r => !r.IsLegacyUntaxed).Sum(r => r.TaxableAmount);
        var legacyCount = report.Rows.Count(r => r.IsLegacyUntaxed);
        ErpUiFactory.SetSummaryCards(_kpiRow,
        [
            ("المبلغ الخاضع", $"{taxable:N2}", "\uE8C1", B("PrimaryBrush")),
            ("ضريبة المخرجات", $"{outputVat:N2}", "\uE8A1", B("AccentOrdersBrush")),
            ("سجلات Legacy", legacyCount.ToString(), "\uE783", B("InfoBrush"))
        ]);

        _meta.Text = $"الفترة: {from:yyyy/MM/dd} → {to:yyyy/MM/dd}  •  {report.Rows.Count} سجل";
        _grid.ItemsSource = report.Rows.Select(r => new ReportRow(r)).ToList();
        _summaryGrid.ItemsSource = report.SummaryByTaxCode.Select(s => new SummaryRow(s)).ToList();
    }

    private static DataGridTextColumn Col(string header, string path, double width = 0, string? format = null, bool star = false)
    {
        var col = new DataGridTextColumn
        {
            Header = header,
            Binding = new System.Windows.Data.Binding(path) { StringFormat = format }
        };
        col.Width = star ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(width);
        return col;
    }

    private sealed class ReportRow(SalesTaxReportRowDto r)
    {
        public string InvoiceNumber => r.InvoiceNumber;
        public string InvoiceDateDisplay => r.InvoiceDate.ToLocalTime().ToString("yyyy/MM/dd");
        public string CustomerName => r.CustomerName;
        public string TaxCode => r.IsLegacyUntaxed ? "Legacy" : r.TaxCode ?? "—";
        public decimal TaxableAmount => r.TaxableAmount;
        public decimal TaxAmount => r.TaxAmount;
        public string PostingStatus => r.PostingStatus;
        public string JournalEntryNumber => r.JournalEntryNumber ?? "—";
        public string LegacyDisplay => r.IsLegacyUntaxed ? "نعم" : "—";
    }

    private sealed class SummaryRow(SalesTaxReportSummaryDto s)
    {
        public string TaxCode => s.TaxCode ?? "—";
        public decimal TaxableAmount => s.TaxableAmount;
        public decimal TaxAmount => s.TaxAmount;
    }

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
    private static Brush Br(string k) => (Brush)WpfApplication.Current.Resources[k]!;
    private static SolidColorBrush B(string k) => (SolidColorBrush)WpfApplication.Current.Resources[k]!;
}
