using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class ChinaImportCostEntryControl : UserControl
{
    private readonly TextBox _invoiceUsd = ErpUiFactory.FormField("");
    private readonly TextBox _weightKg = ErpUiFactory.FormField("");
    private readonly TextBox _customsUsd = ErpUiFactory.FormField("");
    private readonly TextBox _shippingUsd = ErpUiFactory.FormField("");
    private readonly TextBox _clearanceUsd = ErpUiFactory.FormField("");
    private readonly TextBox _otherUsd = ErpUiFactory.FormField("");
    private readonly TextBlock _preview = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button? _saveButton;
    private readonly ContainerExcelParseResultDto? _parse;
    private readonly ChinaImportHeaderDraft? _header;
    private readonly string? _fileName;
    private readonly decimal _exchangeRate;
    private readonly decimal _totalMeters;

    public ChinaImportCostEntryControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 3: إدخال التكلفة"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", true, true),
            ("Landing Cost", false, false),
            ("اعتماد", false, false),
            ("تحويل للمخزن", false, false),
            ("جاهز للبيع", false, false)));

        _parse = ChinaImportNavigationContext.GetParseResult();
        _header = ChinaImportNavigationContext.HeaderDraft;
        _fileName = ChinaImportNavigationContext.LastFileName;
        _exchangeRate = _header?.ExchangeRateToLocalCurrency ?? 1m;
        _totalMeters = _parse?.GrandTotal.ParsedTotalMeters ?? 0m;

        if (_parse is null || _header is null)
        {
            stack.Children.Add(ErpUxFactory.InfoBanner("لا توجد بيانات استيراد. ابدأ من رفع ملف Packing List.", "warning"));
            stack.Children.Add(MakeBackButton());
            root.Content = stack;
            Content = root;
            Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
            return;
        }

        stack.Children.Add(ErpUxFactory.InfoBanner(
            "جميع المبالغ بالدولار ($). احتياطي 2% ضريبة مالية يُحسب من فاتورة الصين فقط — لا يدخل سعر المتر.",
            "info"));

        stack.Children.Add(new TextBlock
        {
            Text = $"إجمالي الأمتار من التحليل: {_totalMeters:N2} م | سعر الصرف: {_exchangeRate:N4}",
            Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
            Margin = new Thickness(0, 0, 0, 12)
        });

        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("إجمالي فاتورة الصين ($)", _invoiceUsd),
            ("وزن الحاوية (كغ)", _weightKg),
            ("الجمارك ($)", _customsUsd),
            ("الشحن ($)", _shippingUsd),
            ("التخليص ($)", _clearanceUsd),
            ("مصاريف أخرى ($)", _otherUsd))));

        _preview.Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!;
        stack.Children.Add(ErpUiFactory.Card(_preview));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(MakeBackButton());

        _saveButton = new Button
        {
            Content = "حفظ والمتابعة إلى Landing Cost",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            Margin = new Thickness(8, 0, 0, 0)
        };
        _saveButton.Click += async (_, _) => await SaveAsync();
        actions.Children.Add(_saveButton);
        stack.Children.Add(actions);

        foreach (var box in new[] { _invoiceUsd, _weightKg, _customsUsd, _shippingUsd, _clearanceUsd, _otherUsd })
            box.TextChanged += (_, _) => RefreshPreview();

        RefreshPreview();

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
    }

    private Button MakeBackButton()
    {
        var back = new Button
        {
            Content = "العودة — تحليل الملف",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!
        };
        back.Click += (_, _) => MockInteractionService.Navigate(AppModule.ChinaImport, "FileAnalysis");
        return back;
    }

    private void RefreshPreview()
    {
        if (!TryReadCostInput(out var input))
        {
            _preview.Text = "أدخل فاتورة الصين ($) وباقي التكاليف لعرض المعاينة.";
            return;
        }

        var preview = ChinaImportCostPreviewDto.Create(
            input.ChinaInvoiceAmountUsd,
            _exchangeRate,
            input.ContainerWeightKg,
            input.CustomsAmountUsd,
            input.ShippingUsd,
            input.ClearanceUsd,
            input.OtherExpensesUsd,
            _totalMeters);

        _preview.Text =
            $"المكافئ المحلي للفاتورة: {preview.InvoiceLocalEquivalent:N2}\n" +
            $"احتياطي ضريبة مالية (2%): {preview.FinancialTaxReserveUsd:N2} $ (≈ {preview.FinancialTaxReserveLocal:N2} محلي)\n" +
            $"إجمالي تكاليف الوصول: {preview.TotalImportExpensesUsd:N2} $\n" +
            $"تكلفة الوصول/متر (لا تشمل الفاتورة ولا الـ 2%): {preview.ExpenseCostPerMeterUsd:N4} $/م";
    }

    private async Task SaveAsync()
    {
        if (_parse is null || _header is null)
            return;

        if (!TryReadCostInput(out var input))
        {
            MessageBox.Show("يرجى إدخال فاتورة الصين ($) ووزن الحاوية بشكل صحيح.", "إدخال التكلفة",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (input.ChinaInvoiceAmountUsd <= 0)
        {
            MessageBox.Show("إجمالي فاتورة الصين مطلوب.", "إدخال التكلفة",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _saveButton!.IsEnabled = false;
        _saveButton.Content = "جاري الحفظ...";
        try
        {
            var result = await ContainerUiService.Instance.SubmitCostEntryAsync(
                _header, _parse, _fileName, input);

            if (!ApplicationResultPresenter.Present(result) || result.Value == Guid.Empty)
                return;

            var preview = ChinaImportCostPreviewDto.Create(
                input.ChinaInvoiceAmountUsd,
                _exchangeRate,
                input.ContainerWeightKg,
                input.CustomsAmountUsd,
                input.ShippingUsd,
                input.ClearanceUsd,
                input.OtherExpensesUsd,
                _totalMeters);
            ChinaImportNavigationContext.SetCostPreview(preview);
            ChinaImportNavigationContext.SetCreatedContainer(result.Value);
            ChinaImportNavigationContext.SetActiveContainer(result.Value);
            MockInteractionService.Navigate(AppModule.ChinaImport, "LandingCost");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر حفظ التكلفة.\n\n{ex.Message}", "إدخال التكلفة",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _saveButton!.Content = "حفظ والمتابعة إلى Landing Cost";
            _saveButton.IsEnabled = true;
        }
    }

    private bool TryReadCostInput(out ChinaImportCostEntryInput input)
    {
        input = new ChinaImportCostEntryInput();
        if (!TryParseDecimal(_invoiceUsd.Text, out var invoice) || invoice <= 0)
            return false;
        if (!TryParseDecimal(_weightKg.Text, out var weight) || weight <= 0)
            return false;

        TryParseDecimal(_customsUsd.Text, out var customs);
        TryParseDecimal(_shippingUsd.Text, out var shipping);
        TryParseDecimal(_clearanceUsd.Text, out var clearance);
        TryParseDecimal(_otherUsd.Text, out var other);

        input = new ChinaImportCostEntryInput
        {
            ChinaInvoiceAmountUsd = invoice,
            ContainerWeightKg = weight,
            CustomsAmountUsd = customs,
            ShippingUsd = shipping,
            ClearanceUsd = clearance,
            OtherExpensesUsd = other
        };
        return true;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        text = text.Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
               decimal.TryParse(text, out value);
    }
}
