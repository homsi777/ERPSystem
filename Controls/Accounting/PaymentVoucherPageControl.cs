using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using ERPSystem.Services.Purchases;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

/// <summary>سند صرف — إنشاء مسودة وترحيل.</summary>
public sealed class PaymentVoucherPageControl : UserControl
{
    private readonly ComboBox _supplier = new() { MinWidth = 280, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly ComboBox _cashbox = new() { MinWidth = 220, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly TextBox _amount = ErpUiFactory.FormField("0");
    private readonly TextBox _reference = ErpUiFactory.FormField("");
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Margin = new Thickness(0, 8, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!
    };
    private readonly Button _createDraft = new() { Content = "حفظ مسودة", Style = S("SecondaryButtonStyle"), MinWidth = 120, Height = 38 };
    private readonly Button _post = new() { Content = "ترحيل", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 38, Margin = new Thickness(8, 0, 0, 0), IsEnabled = false };
    private readonly Button _print = new() { Content = "طباعة", Style = S("GhostButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0), IsEnabled = false };
    private readonly Button _pdf = new() { Content = "PDF", Style = S("GhostButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0), IsEnabled = false };

    private Guid? _draftId;
    private Guid? _lastVoucherId;
    private Guid? _purchaseInvoiceId;
    private bool _busy;

    public PaymentVoucherPageControl()
    {
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("سند صرف"));
        stack.Children.Add(ErpUxFactory.InfoBanner("دفع نقدي لمورد — يُخصم من حساب الذمم الدائنة للمورد (وليس الحساب الرئيسي).", "info"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("المورد *", _supplier),
            ("الصندوق *", _cashbox),
            ("المبلغ *", _amount),
            ("مرجع الفاتورة", _reference),
            ("التاريخ", _date))));
        stack.Children.Add(_status);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_createDraft);
        actions.Children.Add(_post);
        actions.Children.Add(_print);
        actions.Children.Add(_pdf);
        stack.Children.Add(actions);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack, MaxWidth = 640 };
        CashboxDropdownBinder.WireRefresh(_cashbox);
        Loaded += OnLoaded;
        _createDraft.Click += async (_, _) => await CreateDraftAsync();
        _post.Click += async (_, _) => await PostAsync();
        _print.Click += async (_, _) => await PrintAsync(exportPdf: false);
        _pdf.Click += async (_, _) => await PrintAsync(exportPdf: true);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var suppliers = await FinanceUiService.Instance.GetSuppliersAsync();
        if (ApplicationResultPresenter.Present(suppliers) && suppliers.Value is { Count: > 0 })
        {
            _supplier.ItemsSource = suppliers.Value;
            _supplier.DisplayMemberPath = nameof(FinancePartyOption.Display);
            _supplier.SelectedValuePath = nameof(FinancePartyOption.Id);
        }

        await CashboxDropdownBinder.LoadAsync(_cashbox);

        ApplyPurchasePaymentContext();
    }

    private void ApplyPurchasePaymentContext()
    {
        var (invoiceId, supplierId, amount, reference) = PurchaseNavigationContext.TakePaymentContext();
        _purchaseInvoiceId = invoiceId;

        if (supplierId is Guid sid && sid != Guid.Empty)
            _supplier.SelectedValue = sid;

        if (amount is > 0)
            _amount.Text = amount.Value.ToString(CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(reference))
            _reference.Text = reference;

        if (_purchaseInvoiceId.HasValue)
        {
            _status.Text = $"دفعة مرتبطة بفاتورة شراء — سيتم تحديث حالة الفاتورة عند الترحيل.";
            _status.Foreground = (Brush)WpfApplication.Current.Resources["PrimaryBrush"]!;
        }
    }

    private async Task CreateDraftAsync()
    {
        if (_busy) return;
        if (!TryReadForm(out var supplierId, out var cashboxId, out var amount))
            return;

        _busy = true;
        _createDraft.IsEnabled = false;
        try
        {
            var result = await FinanceUiService.Instance.CreatePaymentVoucherAsync(supplierId, cashboxId, amount);
            if (!ApplicationResultPresenter.Present(result))
                return;

            _draftId = result.Value;
            _lastVoucherId = result.Value;
            _post.IsEnabled = true;
            _print.IsEnabled = true;
            _pdf.IsEnabled = true;
            _status.Text = "تم حفظ المسودة — جاهز للترحيل.";
            _status.Foreground = (Brush)WpfApplication.Current.Resources["SuccessBrush"]!;
            MockInteractionService.ShowSuccess("تم حفظ سند الصرف كمسودة.");
        }
        finally
        {
            _busy = false;
            _createDraft.IsEnabled = true;
        }
    }

    private async Task PostAsync()
    {
        if (_busy) return;

        if (_draftId is not Guid voucherId)
        {
            if (!TryReadForm(out var supplierId, out var cashboxId, out var amount))
                return;

            _busy = true;
            _createDraft.IsEnabled = false;
            _post.IsEnabled = false;
            try
            {
                var create = await FinanceUiService.Instance.CreatePaymentVoucherAsync(supplierId, cashboxId, amount);
                if (!ApplicationResultPresenter.Present(create))
                    return;
                voucherId = create.Value;
                _lastVoucherId = voucherId;
                _print.IsEnabled = true;
                _pdf.IsEnabled = true;
            }
            finally
            {
                _busy = false;
                _createDraft.IsEnabled = true;
                _post.IsEnabled = true;
            }
        }

        _busy = true;
        _post.IsEnabled = false;
        try
        {
            var result = await FinanceUiService.Instance.PostPaymentVoucherAsync(voucherId, _purchaseInvoiceId);
            if (!ApplicationResultPresenter.Present(result))
                return;

            MockInteractionService.ShowSuccess(
                _purchaseInvoiceId.HasValue
                    ? "تم ترحيل سند الصرف وتحديث حالة فاتورة الشراء."
                    : "تم ترحيل سند الصرف.");
            _draftId = null;
            _purchaseInvoiceId = null;
            _post.IsEnabled = false;
            _status.Text = "تم الترحيل بنجاح.";
            _amount.Text = "0";
            _reference.Text = "";
            PurchaseListRefreshHub.RequestRefresh();
        }
        finally
        {
            _busy = false;
            _post.IsEnabled = _draftId.HasValue;
        }
    }

    private async Task PrintAsync(bool exportPdf)
    {
        if (_lastVoucherId is not Guid voucherId || _busy) return;

        _busy = true;
        _print.IsEnabled = false;
        _pdf.IsEnabled = false;
        try
        {
            var result = await FinanceUiService.Instance.GetPaymentVoucherPrintAsync(voucherId);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null)
                return;

            PaymentVoucherDocumentService.ShowVoucherPreview(result.Value, exportPdf);
        }
        finally
        {
            _busy = false;
            _print.IsEnabled = true;
            _pdf.IsEnabled = true;
        }
    }

    private bool TryReadForm(out Guid supplierId, out Guid cashboxId, out decimal amount)
    {
        supplierId = Guid.Empty;
        cashboxId = Guid.Empty;
        amount = 0;

        if (_supplier.SelectedValue is not Guid sid || sid == Guid.Empty)
        {
            MessageBox.Show("اختر المورد.", "سند صرف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (_cashbox.SelectedValue is not Guid boxId || boxId == Guid.Empty)
        {
            MessageBox.Show("اختر الصندوق.", "سند صرف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (!TryParseDecimal(_amount.Text, out amount) || amount <= 0)
        {
            MessageBox.Show("أدخل مبلغاً صحيحاً أكبر من صفر.", "سند صرف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        supplierId = sid;
        cashboxId = boxId;
        return true;
    }

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(text, out value);

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
