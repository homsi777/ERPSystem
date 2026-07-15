using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

/// <summary>
/// Step 1b — explicit DPL length unit selection (meter vs yard) before file analysis.
/// </summary>
public sealed class ChinaImportDplUnitSelectionControl : UserControl
{
    private readonly RadioButton _meterOption = null!;
    private readonly RadioButton _yardOption = null!;
    private readonly TextBlock _hintText = null!;
    private readonly Button _continueButton = null!;

    public ChinaImportDplUnitSelectionControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("تحديد وحدة أطوال DPL"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("وحدة DPL", true, true),
            ("تحليل الملف", false, false),
            ("إدخال التكلفة", false, false),
            ("Landing Cost", false, false),
            ("اعتماد", false, false),
            ("جاهز للبيع", false, false)));

        var parse = ChinaImportNavigationContext.GetParseResult();
        if (parse is null)
        {
            stack.Children.Add(ErpUxFactory.InfoBanner("ارفع ملف DPL أولاً من شاشة الاستيراد.", "warning"));
            root.Content = stack;
            Content = root;
            Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
            return;
        }

        stack.Children.Add(ErpUxFactory.InfoBanner(
            "يجب اختيار وحدة عمود الأطوال في ملف DPL يدوياً لكل شحنة. التخزين والتكلفة دائماً بالمتر؛ التحويل من يارد يستخدم المعامل الدولي 0.9144 بالضبط.",
            "info"));

        _hintText = new TextBlock
        {
            Text = $"اقتراح من الملف (للمرجع فقط): {parse.DetectedQuantityUnitDisplay}",
            Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
            Margin = new Thickness(0, 0, 0, 12),
            TextWrapping = TextWrapping.Wrap
        };
        stack.Children.Add(_hintText);

        stack.Children.Add(ErpUiFactory.SectionTitle("وحدة عمود الأطوال في DPL"));
        var unitPanel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };

        _meterOption = new RadioButton
        {
            Content = "متر (M / MTS) — القيم في الملف بالمتر",
            GroupName = "DplUnit",
            Margin = new Thickness(0, 4, 0, 4),
            IsChecked = false
        };
        _yardOption = new RadioButton
        {
            Content = "ياردة (YDS / YARD) — سيتم تحويل كل طول × 0.9144 إلى متر للمخزون والتكلفة",
            GroupName = "DplUnit",
            Margin = new Thickness(0, 4, 0, 4),
            IsChecked = false
        };

        unitPanel.Children.Add(_yardOption);
        unitPanel.Children.Add(_meterOption);
        stack.Children.Add(ErpUiFactory.Card(unitPanel));

        stack.Children.Add(ErpUxFactory.InfoBanner(
            "سعر الوحدة في الفاتورة يبقى «لكل متر» ولا يُحوَّل — فقط أطوال الأثواب تُحوَّل عند اختيار يارد.",
            "warning"));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var back = new Button
        {
            Content = "العودة — رفع الملفات",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            Margin = new Thickness(0, 0, 8, 0)
        };
        back.Click += (_, _) => ChinaImportNavigation.Navigate("NewImport");
        actions.Children.Add(back);

        _continueButton = new Button
        {
            Content = "تأكيد والمتابعة — تحليل الملف",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            IsEnabled = false
        };
        _continueButton.Click += (_, _) => ConfirmAndContinue();
        actions.Children.Add(_continueButton);

        void UpdateContinue()
        {
            _continueButton.IsEnabled = _meterOption.IsChecked == true || _yardOption.IsChecked == true;
        }

        _meterOption.Checked += (_, _) => UpdateContinue();
        _yardOption.Checked += (_, _) => UpdateContinue();

        stack.Children.Add(actions);
        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
    }

    private void ConfirmAndContinue()
    {
        var unit = _yardOption.IsChecked == true ? DplQuantityUnit.Yards : DplQuantityUnit.Meters;
        ChinaImportNavigationContext.ConfirmDplQuantityUnit(unit);
        ChinaImportNavigation.Navigate("FileAnalysis");
    }
}
