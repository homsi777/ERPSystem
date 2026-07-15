using ERPSystem.Application.Common;
using ERPSystem.Core;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.China;

public sealed class ContainerWorkflowSummaryControl : UserControl
{
    private readonly StackPanel _stack = new();
    private readonly string _title;
    private readonly string _subtitle;
    private readonly bool _stocktakeMode;

    public ContainerWorkflowSummaryControl(string title, string subtitle, bool stocktakeMode = false)
    {
        _title = title;
        _subtitle = subtitle;
        _stocktakeMode = stocktakeMode;
        Content = _stack;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        _stack.Children.Clear();
        _stack.Children.Add(ErpUiFactory.SectionTitle(_title));
        _stack.Children.Add(new TextBlock
        {
            Text = _subtitle,
            Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!,
            Margin = new Thickness(0, 0, 0, 16)
        });

        var containerId = ChinaImportNavigationContext.ResolveContainerId();
        if (!containerId.HasValue || !AppServices.IsInitialized)
        {
            _stack.Children.Add(ErpUxFactory.InfoBanner("اختر حاوية من القائمة أو أكمل خطوات الاستيراد.", "info"));
            return;
        }

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(containerId.Value);
        if (!result.IsSuccess || result.Value?.Container is null)
        {
            _stack.Children.Add(ErpUxFactory.InfoBanner("تعذّر تحميل بيانات الحاوية.", "warning"));
            return;
        }

        var c = result.Value.Container;
        var unit = c.DplQuantityUnit;
        if (_stocktakeMode)
        {
            _stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(new[]
            {
                new { البند = "الأثواب في النظام", القيمة = AppFormats.Number(c.TotalRolls) },
                new { البند = ChinaImportLengthDisplay.LengthInSystemLabel(unit), القيمة = ChinaImportLengthDisplay.FormatLength(c.TotalMeters, unit, "N0") },
                new { البند = "الوزن", القيمة = c.TotalWeightKg.HasValue ? $"{c.TotalWeightKg:N0} كغ" : "—" },
                new { البند = "الحالة", القيمة = c.Status.ToArabic() },
                new { البند = "بنود الحاوية", القيمة = c.Items.Count.ToString() },
            })));
            _stack.Children.Add(ErpUxFactory.InfoBanner("جرد العد الفعلي يُفعَّل مع ربط المخزون.", "info"));
            return;
        }

        if (c.Items.Count == 0)
        {
            _stack.Children.Add(ErpUxFactory.InfoBanner("لا توجد بنود لتوزيعها على العملاء.", "info"));
            return;
        }

        var displayRows = c.Items.Select(i => new
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
        _stack.Children.Add(ErpUiFactory.Card(g));
        _stack.Children.Add(ErpUxFactory.InfoBanner("توزيع العملاء والحجوزات يُفعَّل مع وحدة المبيعات.", "info"));
    }
}
