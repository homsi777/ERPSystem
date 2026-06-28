using ERPSystem.Controls;
using ERPSystem.Core;
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
    private readonly Button _uploadButton;
    private readonly TextBlock _hint = new()
    {
        Text = "ارفع ملف Packing List من المورد (.xlsx). بعد التحليل ستنتقل تلقائياً إلى شاشة «تحليل الملف».",
        TextWrapping = TextWrapping.Wrap,
        Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!,
        Margin = new Thickness(0, 8, 0, 0)
    };

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
            ("اعتماد", false, false),
            ("تحويل للمخزن", false, false),
            ("جاهز للبيع", false, false)));

        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رقم الحاوية", _containerNumber),
            ("المورد", _supplierCombo),
            ("تاريخ الشحن", _shipmentDate),
            ("الوصول المتوقع", _expectedArrival),
            ("سعر الصرف", _exchangeRate),
            ("ملاحظات", _notes))));

        _uploadButton = new Button
        {
            Content = "رفع ملف Excel",
            Style = S("PrimaryButtonStyle"),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        stack.Children.Add(ErpUiFactory.Card(_uploadButton));
        stack.Children.Add(_hint);

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        _uploadButton.Click += async (_, _) => await UploadExcelAsync();
        Loaded += async (_, _) => await LoadSuppliersAsync();
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

        var canCreate = await ContainerUiService.Instance.CanCreateAsync();
        _uploadButton.IsEnabled = canCreate;
    }

    private async Task UploadExcelAsync()
    {
        if (_supplierCombo.SelectedValue is not Guid supplierId || supplierId == Guid.Empty)
        {
            MessageBox.Show("يرجى اختيار المورد.", "استيراد حاوية", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(_exchangeRate.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var exchangeRate) &&
            !decimal.TryParse(_exchangeRate.Text.Trim(), out exchangeRate))
        {
            MessageBox.Show("سعر الصرف غير صالح.", "استيراد حاوية", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (exchangeRate <= 0)
        {
            MessageBox.Show("سعر الصرف يجب أن يكون أكبر من صفر.", "استيراد حاوية", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "Excel Files (*.xls;*.xlsx;*.xlsm)|*.xls;*.xlsx;*.xlsm",
            Title = "اختر ملف Packing List"
        };

        if (dialog.ShowDialog() != true)
            return;

        _uploadButton.IsEnabled = false;
        _uploadButton.Content = "جاري التحليل...";
        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            var bytes = await File.ReadAllBytesAsync(dialog.FileName);
            var result = await ContainerUiService.Instance.ParseExcelAsync(dialog.SafeFileName, bytes);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null)
                return;

            ChinaImportNavigationContext.SetParseSession(
                result.Value,
                new ChinaImportHeaderDraft
                {
                    ContainerNumber = _containerNumber.Text.Trim(),
                    SupplierId = supplierId,
                    ShipmentDate = _shipmentDate.SelectedDate ?? DateTime.Today,
                    ExpectedArrival = _expectedArrival.SelectedDate,
                    ExchangeRateToLocalCurrency = exchangeRate,
                    Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
                },
                dialog.SafeFileName);

            MockInteractionService.Navigate(AppModule.ChinaImport, "FileAnalysis");
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"تعذّر استيراد الملف.\n\n{ex.Message}",
                "استيراد حاوية",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            _uploadButton.Content = "رفع ملف Excel";
            if (AppServices.IsInitialized)
            {
                var canCreate = await ContainerUiService.Instance.CanCreateAsync();
                _uploadButton.IsEnabled = canCreate;
            }
        }
    }

    private static Style S(string key) => (Style)WpfApplication.Current.Resources[key]!;
}
