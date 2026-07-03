using ERPSystem.Core;
using ERPSystem.Core.Navigation;
using ERPSystem.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Reports;

/// <summary>Odoo-style reports hub grouped by category within each module.</summary>
public sealed class ModuleReportsHubControl : UserControl
{
    private readonly AppModule _module;
    private readonly Grid _root = new();
    private readonly ContentControl _contentHost = new();

    public ModuleReportsHubControl(AppModule module)
    {
        _module = module;
        Background = Br("AppBgBrush");
        HorizontalAlignment = HorizontalAlignment.Stretch;

        _root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        _contentHost.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Grid.SetRow(_contentHost, 1);
        _root.Children.Add(_contentHost);

        Content = _root;
        Loaded += (_, _) => ShowHub();
    }

    private void ShowHub()
    {
        var reports = ModuleReportRegistry.Get(_module);
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle(ModuleReportsTitle(_module)));
        stack.Children.Add(new TextBlock
        {
            Text = "اختر التقرير المناسب — كل قسم يحتوي تقاريره الخاصة كما في Odoo",
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(0, 0, 0, 16)
        });

        foreach (var group in reports.GroupBy(r => r.GroupAr))
        {
            stack.Children.Add(new TextBlock
            {
                Text = group.Key,
                FontWeight = FontWeights.SemiBold,
                FontSize = 13,
                Margin = new Thickness(0, 8, 0, 8),
                Foreground = Br("TextSecondaryBrush")
            });

            var wrap = new WrapPanel();
            foreach (var report in group)
                wrap.Children.Add(CreateReportCard(report));
            stack.Children.Add(wrap);
        }

        if (reports.Count == 0)
        {
            stack.Children.Add(ErpUxFactory.InfoBanner("لا توجد تقارير مسجلة لهذا القسم بعد.", "info"));
        }

        scroll.Content = stack;
        _contentHost.Content = scroll;
    }

    private void OpenReport(string reportKey)
    {
        var def = ModuleReportRegistry.Find(_module, reportKey);
        if (def is null)
            return;

        if (def.UsesCustomView)
        {
            var custom = ModuleReportCustomViewFactory.TryCreate(_module, reportKey);
            if (custom is not null)
            {
                _contentHost.Content = WrapWithBack(custom, () => ShowHub());
                return;
            }
        }

        var view = new ModuleReportViewControl(_module, reportKey);
        view.BackRequested += (_, _) => ShowHub();
        _contentHost.Content = view;
    }

    private Border CreateReportCard(ModuleReportDef report)
    {
        var card = ErpUiFactory.Card(new StackPanel
        {
            Width = 240,
            Children =
            {
                new TextBlock
                {
                    Text = report.IconGlyph,
                    FontFamily = new FontFamily("Segoe MDL2 Assets"),
                    FontSize = 22,
                    Foreground = Br("PrimaryBrush"),
                    Margin = new Thickness(0, 0, 0, 8)
                },
                new TextBlock { Text = report.TitleAr, FontWeight = FontWeights.SemiBold, FontSize = 14 },
                new TextBlock
                {
                    Text = report.DescriptionAr,
                    Foreground = Br("TextMutedBrush"),
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 6, 0, 0),
                    FontSize = 12
                }
            }
        }, new Thickness(0, 0, 12, 12));

        card.Cursor = Cursors.Hand;
        card.MouseLeftButtonUp += (_, _) => OpenReport(report.Key);
        return card;
    }

    private static UIElement WrapWithBack(UserControl inner, Action onBack)
    {
        var stack = new StackPanel { Margin = new Thickness(16, 8, 16, 0) };
        var backBtn = new Button
        {
            Content = "← تقارير القسم",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, 0, 8)
        };
        backBtn.Click += (_, _) => onBack();
        stack.Children.Add(backBtn);
        stack.Children.Add(inner);
        return stack;
    }

    private static string ModuleReportsTitle(AppModule module) => module switch
    {
        AppModule.Inventory => "تقارير المخزون",
        AppModule.Sales => "تقارير المبيعات",
        AppModule.Customers => "تقارير العملاء",
        AppModule.Suppliers => "تقارير الموردين",
        AppModule.Accounting => "التقارير المالية",
        AppModule.Expenses => "تقارير المصاريف",
        AppModule.CapitalPartners => "تقارير رأس المال",
        AppModule.ChinaImport => "تقارير الاستيراد",
        AppModule.Purchases => "تقارير المشتريات",
        AppModule.HR => "تقارير الموارد البشرية",
        AppModule.Reports => "تقارير الإدارة",
        _ => "التقارير"
    };

    private static Brush Br(string k) => (Brush)WpfApplication.Current.Resources[k]!;
}
