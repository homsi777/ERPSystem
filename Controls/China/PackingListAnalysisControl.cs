using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class PackingListGroupAnalysisRow
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public int DeclaredRolls { get; init; }
    public decimal DeclaredMeters { get; init; }
    public int ParsedRolls { get; init; }
    public decimal ParsedMeters { get; init; }
    public string RollsIndicator { get; init; } = "";
    public string MetersIndicator { get; init; } = "";
    public string CatalogStatus { get; init; } = "";

    public static PackingListGroupAnalysisRow FromDto(PackingListGroupDto dto) => new()
    {
        GroupIndex = dto.GroupIndex,
        FabricCode = dto.FabricCode,
        Color = dto.Color,
        DeclaredRolls = dto.DeclaredTotalRolls,
        DeclaredMeters = dto.DeclaredTotalMeters,
        ParsedRolls = dto.ParsedTotalRolls,
        ParsedMeters = dto.ParsedTotalMeters,
        RollsIndicator = dto.RollsMatchIndicator,
        MetersIndicator = dto.MetersMatchIndicator,
        CatalogStatus = dto.FabricResolved && dto.ColorResolved
            ? "✅"
            : $"⚠️ {dto.ResolutionError}"
    };
}

public sealed class PackingListIssueRow
{
    public int GroupIndex { get; init; }
    public string FabricCode { get; init; } = "";
    public string Color { get; init; } = "";
    public string RollLabel { get; init; } = "";
    public string Reason { get; init; } = "";

    public static IEnumerable<PackingListIssueRow> FromParseResult(ContainerExcelParseResultDto result) =>
        result.Groups
            .SelectMany(g => g.ResolutionIssues.Select(i => new PackingListIssueRow
            {
                GroupIndex = i.GroupIndex,
                FabricCode = i.FabricCode,
                Color = i.Color,
                RollLabel = i.RollNumber.HasValue ? i.RollNumber.Value.ToString() : "—",
                Reason = i.Reason
            }));
}

public sealed class PackingListAnalysisControl : UserControl
{
    public PackingListAnalysisControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 2: تحليل الملف"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", false, false),
            ("Landing Cost", false, false),
            ("اعتماد", false, false),
            ("تحويل للمخزن", false, false),
            ("جاهز للبيع", false, false)));

        var parseResult = ChinaImportNavigationContext.GetParseResult();
        if (parseResult is null)
        {
            stack.Children.Add(ErpUxFactory.InfoBanner("لم يتم تحميل ملف للتحليل. ارجع إلى شاشة الاستيراد وارفع ملف Packing List.", "warning"));
            var backOnly = new Button
            {
                Content = "العودة إلى الاستيراد",
                Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 12, 0, 0)
            };
            backOnly.Click += (_, _) => MockInteractionService.Navigate(AppModule.ChinaImport, "NewImport");
            stack.Children.Add(backOnly);
            root.Content = stack;
            Content = root;
            Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
            return;
        }

        stack.Children.Add(new TextBlock
        {
            Text = $"الملف: {parseResult.FileName}",
            Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (!string.IsNullOrWhiteSpace(parseResult.SupplierNameFromFile))
        {
            stack.Children.Add(new TextBlock
            {
                Text = $"المورد (من الملف): {parseResult.SupplierNameFromFile}",
                Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!,
                Margin = new Thickness(0, 0, 0, 12)
            });
        }

        stack.Children.Add(ErpUiFactory.SectionTitle("الإجمالي الكلي للملف"));
        stack.Children.Add(ErpUiFactory.Card(BuildGrandTotalPanel(parseResult.GrandTotal)));

        stack.Children.Add(ErpUiFactory.SectionTitle("تحليل المجموعات (كود + لون)"));
        stack.Children.Add(ErpUiFactory.Card(BuildGroupsGrid(parseResult)));

        var issues = PackingListIssueRow.FromParseResult(parseResult).ToList();
        stack.Children.Add(ErpUiFactory.SectionTitle("مشاكل التحليل وربط الأكواد"));
        if (issues.Count == 0)
        {
            stack.Children.Add(ErpUxFactory.InfoBanner("لا توجد مشاكل في ربط الأكواد أو تحليل الصفوف.", "success"));
        }
        else
        {
            stack.Children.Add(ErpUiFactory.Card(BuildIssuesGrid(issues)));
        }

        stack.Children.Add(ErpUxFactory.InfoBanner(
            "هذه الشاشة للمراجعة فقط — الحفظ في قاعدة البيانات سيتم في الخطوة التالية (إدخال التكلفة ثم التأكيد).",
            "info"));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var backButton = new Button
        {
            Content = "العودة — رفع ملف آخر",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            Margin = new Thickness(0, 0, 8, 0)
        };
        backButton.Click += (_, _) => MockInteractionService.Navigate(AppModule.ChinaImport, "NewImport");
        actions.Children.Add(backButton);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
    }

    private static UIElement BuildGrandTotalPanel(PackingListGrandTotalDto grand)
    {
        var grid = new Grid { Margin = new Thickness(4) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var summary = new TextBlock
        {
            Text = grand.SummaryText,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(summary, 0);
        grid.Children.Add(summary);

        var metersBadge = new TextBlock
        {
            Text = $"الأطوال {(grand.MetersMatch ? "✅" : "⚠️")}",
            Margin = new Thickness(12, 0, 8, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(metersBadge, 1);
        grid.Children.Add(metersBadge);

        var rollsBadge = new TextBlock
        {
            Text = $"الأثواب {(grand.RollsMatch ? "✅" : "⚠️")}",
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(rollsBadge, 2);
        grid.Children.Add(rollsBadge);

        return grid;
    }

    private static DataGrid BuildGroupsGrid(ContainerExcelParseResultDto result)
    {
        var rows = result.Groups.Select(PackingListGroupAnalysisRow.FromDto).ToList();
        var g = ErpUiFactory.BuildGrid(rows, false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("#", nameof(PackingListGroupAnalysisRow.GroupIndex), 40, null),
            ("كود القماش", nameof(PackingListGroupAnalysisRow.FabricCode), 110, null),
            ("اللون", nameof(PackingListGroupAnalysisRow.Color), 90, null),
            ("أثواب معلنة", nameof(PackingListGroupAnalysisRow.DeclaredRolls), 90, null),
            ("أثواب محللة", nameof(PackingListGroupAnalysisRow.ParsedRolls), 90, null),
            ("تطابق الأثواب", nameof(PackingListGroupAnalysisRow.RollsIndicator), 90, null),
            ("أطوال معلنة", nameof(PackingListGroupAnalysisRow.DeclaredMeters), 100, "N2"),
            ("أطوال محللة", nameof(PackingListGroupAnalysisRow.ParsedMeters), 100, "N2"),
            ("تطابق الأطوال", nameof(PackingListGroupAnalysisRow.MetersIndicator), 90, null),
            ("الربط", nameof(PackingListGroupAnalysisRow.CatalogStatus), 140, null)
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
        }
        return g;
    }

    private static DataGrid BuildIssuesGrid(IReadOnlyList<PackingListIssueRow> issues)
    {
        var g = ErpUiFactory.BuildGrid(issues, false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("المجموعة", nameof(PackingListIssueRow.GroupIndex), 70),
            ("كود القماش", nameof(PackingListIssueRow.FabricCode), 110),
            ("اللون", nameof(PackingListIssueRow.Color), 90),
            ("رقم التوب", nameof(PackingListIssueRow.RollLabel), 80),
            ("السبب", nameof(PackingListIssueRow.Reason), "*")
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, null);
        }
        return g;
    }
}
