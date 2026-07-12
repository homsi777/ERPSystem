using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses;

/// <summary>تقارير المصاريف — بطاقات نطاق + تقرير مفصل بالتاريخ والبيانات.</summary>
public sealed class ExpenseReportsControl : UserControl
{
    private sealed record ScopeCard(ExpenseCategoryKind? Kind, string Title, string Subtitle, string Icon, string BrushKey);

    private static readonly ScopeCard[] ScopeCards =
    [
        new(null, "جميع المصاريف", "كل البطاقات والتعريفات", "\uE8FD", "PrimaryBrush"),
        new(ExpenseCategoryKind.Capital, "رأسمالية", "استثمارات وأصول", "\uE8B7", "AccentOrdersBrush"),
        new(ExpenseCategoryKind.Personal, "شخصية", "مصاريف شخصية مستمرة", "\uE716", "InfoBrush"),
        new(ExpenseCategoryKind.Operating, "تشغيلية", "مصاريف التشغيل اليومية", "\uE821", "AccentInventoryBrush")
    ];

    private readonly DatePicker _from = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-3));
    private readonly DatePicker _to = ErpUiFactory.FormDate(DateTime.Today);
    private readonly ComboBox _reportKind = ErpUiFactory.FilterCombo(
    [
        "تقرير مفصل", "المستحقة", "الدفعات القادمة", "المتأخرة", "المتكررة", "مصادر التمويل"
    ], 160);
    private readonly WrapPanel _scopeCardsHost = new() { Margin = new Thickness(0, 0, 0, 12) };
    private readonly StackPanel _kpiRow = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
    private readonly StackPanel _previewHost = new();
    private readonly TextBlock _meta = new()
    {
        FontSize = 12,
        Foreground = Br("TextSecondaryBrush"),
        Margin = new Thickness(0, 0, 0, 8),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly DataGrid _grid = new() { AutoGenerateColumns = false, IsReadOnly = true, MinHeight = 280 };

    private ExpenseCategoryKind? _selectedScope;
    private readonly List<(ExpenseCategoryKind? Kind, Border Border)> _scopeCardEntries = [];

    public ExpenseReportsControl()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        Background = Br("AppBgBrush");

        BuildScopeCards();
        BuildGridColumns();

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("تقارير المصاريف"));
        stack.Children.Add(new TextBlock
        {
            Text = "اختر بطاقة المصاريف (أو الكل) والفترة الزمنية لعرض تقرير مفصل",
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 12)
        });
        stack.Children.Add(ErpUiFactory.SectionTitle("نطاق التقرير"));
        stack.Children.Add(_scopeCardsHost);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFilterRow(
            ("نوع التقرير", _reportKind),
            ("من تاريخ", _from),
            ("إلى تاريخ", _to))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 0) };
        var runBtn = new Button { Content = "عرض التقرير", Style = S("PrimaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
        runBtn.Click += async (_, _) => await RunReportAsync("Preview");
        actions.Children.Add(runBtn);

        foreach (var (label, mode) in new[] { ("طباعة", "طباعة"), ("PDF", "PDF"), ("Excel", "Excel") })
        {
            var btn = new Button { Content = label, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 8, 0) };
            var fmt = mode;
            btn.Click += async (_, _) => await RunReportAsync(fmt);
            actions.Children.Add(btn);
        }
        stack.Children.Add(actions);
        stack.Children.Add(_kpiRow);
        stack.Children.Add(_meta);
        stack.Children.Add(_previewHost);

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Content = stack
        };

        _reportKind.SelectionChanged += async (_, _) => await RunReportAsync("Preview");
        _from.SelectedDateChanged += async (_, _) => await RunReportAsync("Preview");
        _to.SelectedDateChanged += async (_, _) => await RunReportAsync("Preview");
        Loaded += async (_, _) => await RunReportAsync("Preview");
    }

    private void BuildScopeCards()
    {
        _scopeCardsHost.Children.Clear();
        _scopeCardEntries.Clear();

        foreach (var card in ScopeCards)
        {
            var border = CreateScopeCard(card);
            _scopeCardEntries.Add((card.Kind, border));
            _scopeCardsHost.Children.Add(border);
        }

        SelectScope(null);
    }

    private Border CreateScopeCard(ScopeCard card)
    {
        var border = new Border
        {
            Width = 220,
            MinHeight = 96,
            Margin = new Thickness(0, 0, 12, 12),
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),
            BorderBrush = Br("BorderBrush"),
            Background = Br("SurfaceBrush"),
            Padding = new Thickness(14),
            Cursor = Cursors.Hand,
            Effect = (System.Windows.Media.Effects.Effect)WpfApplication.Current.Resources["CardShadow"]!
        };

        var accent = Br(card.BrushKey);
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        row.Children.Add(ErpUiFactory.IconBadge(card.Icon, accent));
        var text = new StackPanel { Margin = new Thickness(12, 0, 0, 0) };
        text.Children.Add(new TextBlock
        {
            Text = card.Title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = Br("TextPrimaryBrush")
        });
        text.Children.Add(new TextBlock
        {
            Text = card.Subtitle,
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 4, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        Grid.SetColumn(text, 1);
        row.Children.Add(text);
        border.Child = row;

        border.MouseLeftButtonUp += async (_, _) =>
        {
            SelectScope(card.Kind);
            await RunReportAsync("Preview");
        };
        return border;
    }

    private void SelectScope(ExpenseCategoryKind? kind)
    {
        _selectedScope = kind;
        foreach (var (scope, border) in _scopeCardEntries)
        {
            var selected = scope == kind;
            border.BorderBrush = selected ? Br("PrimaryBrush") : Br("BorderBrush");
            border.BorderThickness = new Thickness(selected ? 2 : 1);
            border.Background = selected ? Br("PrimaryVeryLightBrush") : Br("SurfaceBrush");
        }
    }

    private void BuildGridColumns()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_grid);
        ErpUiFactory.AddGridColumn(_grid, "الكود", nameof(ExpenseReportRowDto.Code), 80);
        ErpUiFactory.AddGridColumn(_grid, "المصروف", nameof(ExpenseReportRowDto.Name), 140);
        ErpUiFactory.AddGridColumn(_grid, "التصنيف", nameof(ExpenseReportRowDto.CategoryKindDisplay), 120);
        ErpUiFactory.AddGridColumn(_grid, "الفئة", nameof(ExpenseReportRowDto.Category), 110);
        ErpUiFactory.AddGridColumn(_grid, "الحالة", nameof(ExpenseReportRowDto.Status), 90);
        ErpUiFactory.AddGridColumn(_grid, "البداية", nameof(ExpenseReportRowDto.StartDate), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "النهاية", nameof(ExpenseReportRowDto.EndDate), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "المبلغ", nameof(ExpenseReportRowDto.OriginalAmount), 90, "N2");
        ErpUiFactory.AddGridColumn(_grid, "العملة", nameof(ExpenseReportRowDto.Currency), 55);
        ErpUiFactory.AddGridColumn(_grid, "سعر الصرف", nameof(ExpenseReportRowDto.ExchangeRate), 80, "N4");
        ErpUiFactory.AddGridColumn(_grid, "بالأساس USD", nameof(ExpenseReportRowDto.BaseAmount), 100, "N2");
        ErpUiFactory.AddGridColumn(_grid, "مدفوع", nameof(ExpenseReportRowDto.PaidAmountBase), 90, "N2");
        ErpUiFactory.AddGridColumn(_grid, "متبقي", nameof(ExpenseReportRowDto.RemainingBalanceBase), 90, "N2");
        ErpUiFactory.AddGridColumn(_grid, "المستفيد", nameof(ExpenseReportRowDto.PayeeName), 110);
        ErpUiFactory.AddGridColumn(_grid, "القسم", nameof(ExpenseReportRowDto.Department), 90);
        ErpUiFactory.AddGridColumn(_grid, "مركز التكلفة", nameof(ExpenseReportRowDto.CostCenter), 100);
        ErpUiFactory.AddGridColumn(_grid, "طريقة الدفع", nameof(ExpenseReportRowDto.PaymentMethod), 90);
        ErpUiFactory.AddGridColumn(_grid, "مصدر التمويل", nameof(ExpenseReportRowDto.FundingSource), 100);
        ErpUiFactory.AddGridColumn(_grid, "متكرر", nameof(ExpenseReportRowDto.IsRecurring), 55);
        ErpUiFactory.AddGridColumn(_grid, "الاستحقاق", nameof(ExpenseReportRowDto.NextDueDate), 95, "yyyy/MM/dd");
        ErpUiFactory.AddGridColumn(_grid, "دفعات", nameof(ExpenseReportRowDto.PaymentCount), 55);
        ErpUiFactory.AddGridColumn(_grid, "البيان", nameof(ExpenseReportRowDto.Description), "*");
        ErpUiFactory.AddGridColumn(_grid, "ملاحظات", nameof(ExpenseReportRowDto.Notes), 120);

        _grid.MouseDoubleClick += (_, _) =>
        {
            if (_grid.SelectedItem is ExpenseReportRowDto row)
                ExpensePopupService.ShowDetailsById(row.ExpenseId, row.Name, row.Code);
        };
    }

    private async Task RunReportAsync(string mode)
    {
        if (!AppServices.IsInitialized)
            return;

        var reportKey = MapReportType();
        var result = await ExpenseUiService.Instance.GetReportAsync(
            reportKey, _from.SelectedDate, _to.SelectedDate, _selectedScope);

        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        if (mode is "PDF" or "طباعة")
        {
            ExpenseReportDocumentService.ShowReportPreview(result.Value, exportPdf: mode == "PDF");
            return;
        }

        if (mode == "Excel")
        {
            MockInteractionService.ShowDocumentPreview(result.Value.Title, "Excel");
            return;
        }

        RenderPreview(result.Value);
    }

    private void RenderPreview(ExpenseReportDto report)
    {
        _previewHost.Children.Clear();
        _kpiRow.Children.Clear();

        var from = report.FromDate?.ToString("yyyy/MM/dd") ?? "—";
        var to = report.ToDate?.ToString("yyyy/MM/dd") ?? "—";
        _meta.Text =
            $"{report.Title}  •  الفترة: {from} → {to}  •  {report.ExpenseCount} مصروف  •  أُنشئ {report.GeneratedAt.ToLocalTime():yyyy/MM/dd HH:mm}";

        ErpUiFactory.SetSummaryCards(_kpiRow,
        [
            ("عدد المصاريف", report.ExpenseCount.ToString(), "\uE9D9", B("PrimaryBrush")),
            ("إجمالي USD", $"{report.TotalBase:N2}", "\uE8C1", B("AccentSalesBrush")),
            ("مدفوع USD", $"{report.TotalPaidBase:N2}", "\uE73E", B("SuccessBrush")),
            ("متبقي USD", $"{report.TotalRemainingBase:N2}", "\uE823", B("WarningBrush"))
        ]);

        if (report.Rows.Count == 0)
        {
            _previewHost.Children.Add(ErpUxFactory.InfoBanner("لا توجد مصاريف ضمن النطاق والفترة المحددة.", "warning"));
            return;
        }

        _grid.ItemsSource = report.Rows;
        ErpUiFactory.DetachFromVisualTree(_grid);
        _previewHost.Children.Add(ErpUiFactory.SectionTitle("التفاصيل"));
        _previewHost.Children.Add(ErpUiFactory.Card(_grid));
        _previewHost.Children.Add(new TextBlock
        {
            Text = "انقر مرتين على أي صف لفتح تفاصيل المصروف",
            FontSize = 11,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 8, 0, 0)
        });
    }

    private string MapReportType() => _reportKind.SelectedIndex switch
    {
        1 => "Outstanding",
        2 => "UpcomingPayments",
        3 => "OverduePayments",
        4 => "Recurring",
        5 => "FundingSource",
        _ => "Detailed"
    };

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
    private static Brush Br(string k) => (Brush)WpfApplication.Current.Resources[k]!;
    private static SolidColorBrush B(string k) => (SolidColorBrush)WpfApplication.Current.Resources[k]!;
}
