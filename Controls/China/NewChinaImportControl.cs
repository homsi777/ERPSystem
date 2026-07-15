using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using ERPSystem.Services.China;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class NewChinaImportControl : UserControl
{
    private readonly TextBox _containerNumber = ErpUiFactory.FormField("");
    private readonly ComboBox _supplierCombo = new() { MinWidth = 220, IsEditable = false };
    private readonly DatePicker _shipmentDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly DatePicker _expectedArrival = ErpUiFactory.FormDate(DateTime.Today.AddDays(20));
    private readonly TextBox _exchangeRate = ErpUiFactory.FormField("1");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly TextBlock _invoiceStatus = StatusLabel("لم يُرفع");
    private readonly TextBlock _plStatus = StatusLabel("لم يُرفع");
    private readonly TextBlock _dplStatus = StatusLabel("لم يُرفع");
    private readonly Button _continueButton;

    public NewChinaImportControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("استيراد حاوية جديدة — الخطوة 1"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", false, false),
            ("إدخال التكلفة", false, false),
            ("Landing Cost", false, false),
            ("أسعار البيع", false, false),
            ("اعتماد", false, false),
            ("جاهز للبيع", false, false)));

        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رقم الحاوية", _containerNumber),
            ("المورد", _supplierCombo),
            ("تاريخ الشحن", _shipmentDate),
            ("الوصول المتوقع", _expectedArrival),
            ("سعر الصرف", _exchangeRate),
            ("ملاحظات", _notes))));

        stack.Children.Add(ErpUiFactory.SectionTitle("ملفات الشحنة (3 ملفات من المورد)"));
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "الفاتورة + PL الملخّص اختياريان للتكلفة حسب النوع. ملف DPL (تفاصيل الأثواب) مطلوب. بدون فاتورة/PL يُطبَّق معدّل مسطح كما سابقاً.",
            "info"));

        var filesPanel = new StackPanel();
        filesPanel.Children.Add(FileUploadRow("1. فاتورة الصين (Invoice)", "رفع الفاتورة", async () => await UploadInvoiceAsync(), _invoiceStatus));
        filesPanel.Children.Add(FileUploadRow("2. Packing List ملخّص (PL)", "رفع PL", async () => await UploadPlAsync(), _plStatus));
        filesPanel.Children.Add(FileUploadRow("3. قائمة الأثواب التفصيلية (DPL)", "رفع DPL", async () => await UploadDplAsync(), _dplStatus));
        stack.Children.Add(ErpUiFactory.Card(filesPanel));

        _continueButton = new Button
        {
            Content = "التالي — تحليل الملفات",
            Style = S("PrimaryButtonStyle"),
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 12, 0, 0),
            IsEnabled = false
        };
        _continueButton.Click += (_, _) => ChinaImportNavigation.Navigate("DplUnitSelection");
        stack.Children.Add(_continueButton);

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        Loaded += async (_, _) => await LoadSuppliersAsync();
        Loaded += (_, _) => UnsavedWorkGuard.Register(this, "استيراد حاوية جديدة", HasUnsavedImportWork);
        Unloaded += (_, _) => UnsavedWorkGuard.Unregister(this);
    }

    private bool HasUnsavedImportWork()
    {
        if (ChinaImportNavigationContext.CreatedContainerId is not null)
            return false;

        if (!string.IsNullOrWhiteSpace(_containerNumber.Text)
            || !string.IsNullOrWhiteSpace(_notes.Text))
            return true;

        return ChinaImportNavigationContext.LastParseResult is not null
            || ChinaImportNavigationContext.LastInvoiceParse is not null
            || ChinaImportNavigationContext.LastPackingSummaryParse is not null
            || ChinaImportNavigationContext.HeaderDraft is not null;
    }

    private static TextBlock StatusLabel(string text) => new()
    {
        Text = text,
        Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!,
        Margin = new Thickness(12, 0, 0, 0),
        VerticalAlignment = VerticalAlignment.Center
    };

    private static UIElement FileUploadRow(string label, string buttonText, Func<Task> upload, TextBlock status)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 6) };
        row.Children.Add(new TextBlock
        {
            Text = label,
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        });
        var btn = new Button { Content = buttonText, Style = S("SecondaryButtonStyle"), MinWidth = 120 };
        btn.Click += async (_, _) => await upload();
        row.Children.Add(btn);
        row.Children.Add(status);
        return row;
    }

    private async Task LoadSuppliersAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var suppliers = await ContainerUiService.Instance.GetSuppliersAsync();
        _supplierCombo.ItemsSource = suppliers;
        _supplierCombo.DisplayMemberPath = nameof(SupplierPickItem.Display);
        _supplierCombo.SelectedValuePath = nameof(SupplierPickItem.Id);

        if (suppliers.Count > 0)
            _supplierCombo.SelectedIndex = 0;
        else
        {
            _supplierCombo.ItemsSource = new[]
            {
                new SupplierPickItem { Id = DatabaseSeeder.DefaultChinaSupplierId, Display = "مورد قوانغتشو" }
            };
            _supplierCombo.SelectedIndex = 0;
        }
    }

    private ChinaImportHeaderDraft? BuildHeaderDraft()
    {
        if (_supplierCombo.SelectedValue is not Guid supplierId || supplierId == Guid.Empty)
            return null;

        if (!decimal.TryParse(_exchangeRate.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var exchangeRate) &&
            !decimal.TryParse(_exchangeRate.Text.Trim(), out exchangeRate))
            return null;

        if (exchangeRate <= 0)
            return null;

        return new ChinaImportHeaderDraft
        {
            ContainerNumber = _containerNumber.Text.Trim(),
            SupplierId = supplierId,
            ShipmentDate = _shipmentDate.SelectedDate ?? DateTime.Today,
            ExpectedArrival = _expectedArrival.SelectedDate,
            ExchangeRateToLocalCurrency = exchangeRate,
            Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
        };
    }

    private async Task UploadInvoiceAsync()
    {
        var bytes = await PickExcelBytes("اختر ملف الفاتورة");
        if (bytes is null)
            return;

        var (fileName, content) = bytes.Value;
        var result = await ContainerUiService.Instance.ParseInvoiceAsync(fileName, content);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        ChinaImportNavigationContext.SetInvoiceParse(result.Value, fileName);
        _invoiceStatus.Text = $"✅ {fileName} ({result.Value.Lines.Count} بند)";
        _invoiceStatus.Foreground = (Brush)WpfApplication.Current.Resources["SuccessBrush"]!;
        UpdateContinueState();
    }

    private async Task UploadPlAsync()
    {
        var bytes = await PickExcelBytes("اختر ملف PL الملخّص");
        if (bytes is null)
            return;

        var (fileName, content) = bytes.Value;
        var result = await ContainerUiService.Instance.ParsePackingSummaryAsync(fileName, content);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        ChinaImportNavigationContext.SetPackingSummaryParse(result.Value, fileName);
        _plStatus.Text = $"✅ {fileName} ({result.Value.Lines.Count} بند)";
        _plStatus.Foreground = (Brush)WpfApplication.Current.Resources["SuccessBrush"]!;
        UpdateContinueState();
    }

    private async Task UploadDplAsync()
    {
        var bytes = await PickExcelBytes("اختر ملف DPL (تفاصيل الأثواب)");
        if (bytes is null)
            return;

        var (fileName, content) = bytes.Value;
        var header = BuildHeaderDraft() ?? BuildDefaultHeaderDraft();

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var result = await ContainerUiService.Instance.ParseExcelAsync(fileName, content);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null)
                return;

            ChinaImportNavigationContext.SetParseSession(result.Value, header, fileName);
            _dplStatus.Text = $"✅ {fileName} ({result.Value.Groups.Count} مجموعة، {result.Value.GrandTotal.ParsedTotalRolls} توب)";
            _dplStatus.Foreground = (Brush)WpfApplication.Current.Resources["SuccessBrush"]!;
            UpdateContinueState();

            if (BuildHeaderDraft() is null)
            {
                MessageBox.Show(
                    "تم تحليل ملف DPL بنجاح.\nأكمل رقم الحاوية والمورد وسعر الصرف قبل إدخال التكلفة.",
                    "تحليل DPL",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private ChinaImportHeaderDraft BuildDefaultHeaderDraft()
    {
        var supplierId = _supplierCombo.SelectedValue is Guid g && g != Guid.Empty
            ? g
            : DatabaseSeeder.DefaultChinaSupplierId;

        decimal.TryParse(_exchangeRate.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var rate);
        if (rate <= 0)
            decimal.TryParse(_exchangeRate.Text.Trim(), out rate);
        if (rate <= 0)
            rate = 1m;

        return new ChinaImportHeaderDraft
        {
            ContainerNumber = _containerNumber.Text.Trim(),
            SupplierId = supplierId,
            ShipmentDate = _shipmentDate.SelectedDate ?? DateTime.Today,
            ExpectedArrival = _expectedArrival.SelectedDate,
            ExchangeRateToLocalCurrency = rate,
            Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
        };
    }

    private void UpdateContinueState() =>
        _continueButton.IsEnabled = ChinaImportNavigationContext.GetParseResult() is not null;

    private static async Task<(string FileName, byte[] Content)?> PickExcelBytes(string title)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xls;*.xlsx;*.xlsm)|*.xls;*.xlsx;*.xlsm",
            Title = title
        };
        if (dialog.ShowDialog() != true)
            return null;
        return (dialog.SafeFileName, await File.ReadAllBytesAsync(dialog.FileName));
    }

    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
