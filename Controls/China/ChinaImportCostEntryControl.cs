using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
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
    private readonly TextBox _invoiceNote = ErpUiFactory.FormNoteField();
    private readonly TextBox _weightKg = ErpUiFactory.FormField("");
    private readonly TextBox _shippingUsd = ErpUiFactory.FormField("");
    private readonly TextBox _shippingNote = ErpUiFactory.FormNoteField();
    private readonly TextBox _insuranceUsd = ErpUiFactory.FormField("");
    private readonly TextBox _insuranceNote = ErpUiFactory.FormNoteField();
    private readonly TextBox _customsClearanceUsd = ErpUiFactory.FormField("");
    private readonly TextBox _customsClearanceNote = ErpUiFactory.FormNoteField();
    private readonly TextBox _other1Usd = ErpUiFactory.FormField("0");
    private readonly TextBox _other1Note = ErpUiFactory.FormNoteField();
    private readonly TextBox _other2Usd = ErpUiFactory.FormField("0");
    private readonly TextBox _other2Note = ErpUiFactory.FormNoteField();
    private readonly TextBox _other3Usd = ErpUiFactory.FormField("0");
    private readonly TextBox _other3Note = ErpUiFactory.FormNoteField();
    private readonly TextBox _other4Usd = ErpUiFactory.FormField("0");
    private readonly TextBox _other4Note = ErpUiFactory.FormNoteField();
    private readonly TextBlock _preview = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button? _saveButton;
    private readonly ContainerExcelParseResultDto? _parse;
    private readonly ChinaImportHeaderDraft? _header;
    private readonly ChinaImportMultiFileSessionDto? _session;
    private readonly string? _fileName;
    private readonly decimal _exchangeRate;
    private readonly decimal _totalMeters;
    private readonly bool _usesWeighted;

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
            ("أسعار البيع", false, false),
            ("اعتماد", false, false),
            ("جاهز للبيع", false, false)));

        _parse = ChinaImportNavigationContext.GetParseResult();
        _header = ChinaImportNavigationContext.HeaderDraft;
        _session = ChinaImportNavigationContext.GetMultiFileSession();
        _fileName = ChinaImportNavigationContext.LastRollDetailFileName;
        _exchangeRate = _header?.ExchangeRateToLocalCurrency ?? 1m;
        _totalMeters = _parse?.GrandTotal.ParsedTotalMeters ?? 0m;
        _usesWeighted = _session?.UsesWeightedAllocation == true;

        if (_parse is null || _header is null)
        {
            stack.Children.Add(ErpUxFactory.InfoBanner("لا توجد بيانات استيراد. ابدأ من رفع ملف DPL.", "warning"));
            stack.Children.Add(MakeBackButton());
            root.Content = stack;
            Content = root;
            Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
            return;
        }

        PrefillFromParsedFiles();

        stack.Children.Add(ErpUxFactory.InfoBanner(
            _usesWeighted
                ? "وضع التكلفة حسب النوع: المصاريف المشتركة تُوزَّع بالوزن. جميع المبالغ بالدولار ($)."
                : "وضع معدّل مسطح (DPL فقط): المصاريف تُوزَّع على إجمالي الأمتار. لإتاحة التكلفة حسب النوع ارفع الفاتورة + PL.",
            _usesWeighted ? "success" : "warning"));

        stack.Children.Add(new TextBlock
        {
            Text = AppFormats.Text("إجمالي الأمتار: {0} م | سعر الصرف: {1}", _totalMeters, _exchangeRate),
            Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
            Margin = new Thickness(0, 0, 0, 12)
        });

        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildAmountNoteFormGrid(
            ("إجمالي فاتورة الصين ($)", _invoiceUsd, _invoiceNote),
            ("وزن الحاوية (كغ)", _weightKg, null),
            ("الشحن ($) *", _shippingUsd, _shippingNote),
            ("التأمين ($) *", _insuranceUsd, _insuranceNote),
            ("جمارك / تخليص جمركي ($) *", _customsClearanceUsd, _customsClearanceNote),
            ("مصاريف أخرى 1 ($)", _other1Usd, _other1Note),
            ("مصاريف أخرى 2 ($)", _other2Usd, _other2Note),
            ("مصاريف أخرى 3 ($)", _other3Usd, _other3Note),
            ("مصاريف أخرى 4 ($)", _other4Usd, _other4Note))));

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

        foreach (var box in new[] { _invoiceUsd, _weightKg, _shippingUsd, _insuranceUsd, _customsClearanceUsd,
                     _other1Usd, _other2Usd, _other3Usd, _other4Usd })
            box.TextChanged += (_, _) => RefreshPreview();

        RefreshPreview();

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;
    }

    private void PrefillFromParsedFiles()
    {
        var invoice = _session?.Invoice;
        if (invoice is not null)
        {
            if (invoice.GrandTotalUsd > 0)
                _invoiceUsd.Text = invoice.GrandTotalUsd.ToString("N2", CultureInfo.InvariantCulture);
            if (invoice.SeaFreightUsd > 0)
                _shippingUsd.Text = invoice.SeaFreightUsd.ToString("N2", CultureInfo.InvariantCulture);
            if (invoice.InsuranceUsd > 0)
                _insuranceUsd.Text = invoice.InsuranceUsd.ToString("N2", CultureInfo.InvariantCulture);
        }

        var pl = _session?.PackingSummary;
        if (pl is not null && pl.TotalNetWeightKg > 0)
            _weightKg.Text = pl.TotalNetWeightKg.ToString("N0", CultureInfo.InvariantCulture);
    }

    private Button MakeBackButton()
    {
        var back = new Button
        {
            Content = "العودة — تحليل الملف",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!
        };
        back.Click += (_, _) => ChinaImportNavigation.Navigate("FileAnalysis");
        return back;
    }

    private void RefreshPreview()
    {
        if (!TryReadCostInput(out var input))
        {
            _preview.Text = "أدخل فاتورة الصين ($) والشحن والتأمين والجمارك/التخليص ووزن الحاوية لعرض المعاينة.";
            return;
        }

        var shared = input.ShippingUsd + input.InsuranceUsd + input.CustomsClearanceUsd
            + input.OtherExpense1Usd + input.OtherExpense2Usd + input.OtherExpense3Usd + input.OtherExpense4Usd;
        var reserve = input.ChinaInvoiceAmountUsd * 0.02m;

        _preview.Text =
            AppFormats.Text("المكافئ المحلي للفاتورة: {0}", input.ChinaInvoiceAmountUsd * _exchangeRate) + "\n" +
            AppFormats.Text("احتياطي ضريبة مالية (2%): {0} $ (لا يدخل سعر المتر)", reserve) + "\n" +
            AppFormats.Text("إجمالي المصاريف المشتركة: {0} $", shared) + "\n" +
            (_usesWeighted
                ? $"التخصيص: بالوزن عبر {_session?.TypeLines.Count ?? 0} نوع قماش"
                : AppFormats.Text("تكلفة الوصول/م (مسطح): {0} $/م", _totalMeters > 0 ? shared / _totalMeters : 0));
    }

    private async Task SaveAsync()
    {
        if (_parse is null || _header is null)
            return;

        if (!TryReadCostInput(out var input))
        {
            MessageBox.Show("يرجى إدخال فاتورة الصين والشحن والتأمين والجمارك/التخليص ووزن الحاوية بشكل صحيح.",
                "إدخال التكلفة", MessageBoxButton.OK, MessageBoxImage.Warning);
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

            ChinaImportNavigationContext.SetCreatedContainer(result.Value);
            ChinaImportNavigationContext.SetActiveContainer(result.Value);
            ChinaImportNavigation.Navigate("LandingCost", ChinaContainerStatus.LandingCostReviewed);
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
        if (!TryParseDecimal(_shippingUsd.Text, out var shipping))
            return false;
        if (!TryParseDecimal(_insuranceUsd.Text, out var insurance))
            return false;
        if (!TryParseDecimal(_customsClearanceUsd.Text, out var customsClearance))
            return false;

        TryParseDecimal(_other1Usd.Text, out var other1);
        TryParseDecimal(_other2Usd.Text, out var other2);
        TryParseDecimal(_other3Usd.Text, out var other3);
        TryParseDecimal(_other4Usd.Text, out var other4);

        input = new ChinaImportCostEntryInput
        {
            ChinaInvoiceAmountUsd = invoice,
            ContainerWeightKg = weight,
            ShippingUsd = shipping,
            InsuranceUsd = insurance,
            CustomsClearanceUsd = customsClearance,
            OtherExpense1Usd = other1,
            OtherExpense2Usd = other2,
            OtherExpense3Usd = other3,
            OtherExpense4Usd = other4,
            UsesWeightedAllocation = _usesWeighted,
            ChinaInvoiceNote = ReadNote(_invoiceNote),
            ShippingNote = ReadNote(_shippingNote),
            InsuranceNote = ReadNote(_insuranceNote),
            CustomsClearanceNote = ReadNote(_customsClearanceNote),
            OtherExpense1Note = ReadNote(_other1Note),
            OtherExpense2Note = ReadNote(_other2Note),
            OtherExpense3Note = ReadNote(_other3Note),
            OtherExpense4Note = ReadNote(_other4Note)
        };
        return true;
    }

    private static string? ReadNote(TextBox box)
    {
        var text = box.Text.Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        text = text.Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value) ||
               decimal.TryParse(text, out value);
    }
}
