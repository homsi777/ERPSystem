using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
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
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBlock _status = new()
    {
        FontSize = 12,
        Margin = new Thickness(0, 8, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!
    };
    private readonly Button _createDraft = new() { Content = "حفظ مسودة", Style = S("SecondaryButtonStyle"), MinWidth = 120, Height = 38 };
    private readonly Button _post = new() { Content = "ترحيل", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 38, Margin = new Thickness(8, 0, 0, 0), IsEnabled = false };

    private Guid? _draftId;
    private bool _busy;

    public PaymentVoucherPageControl()
    {
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("سند صرف"));
        stack.Children.Add(ErpUxFactory.InfoBanner("دفع نقدي لمورد — احفظ مسودة ثم رحّل السند.", "info"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("المورد *", _supplier),
            ("الصندوق *", _cashbox),
            ("المبلغ *", _amount),
            ("التاريخ", _date))));
        stack.Children.Add(_status);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_createDraft);
        actions.Children.Add(_post);
        stack.Children.Add(actions);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack, MaxWidth = 640 };
        Loaded += OnLoaded;
        _createDraft.Click += async (_, _) => await CreateDraftAsync();
        _post.Click += async (_, _) => await PostAsync();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var suppliers = await FinanceUiService.Instance.GetSuppliersAsync();
        if (ApplicationResultPresenter.Present(suppliers) && suppliers.Value is { Count: > 0 })
        {
            _supplier.ItemsSource = suppliers.Value;
            _supplier.DisplayMemberPath = nameof(FinancePartyOption.Display);
            _supplier.SelectedValuePath = nameof(FinancePartyOption.Id);
            _supplier.SelectedIndex = 0;
        }

        var boxes = await FinanceUiService.Instance.GetCashboxesAsync();
        if (ApplicationResultPresenter.Present(boxes) && boxes.Value is { Count: > 0 })
        {
            _cashbox.ItemsSource = boxes.Value;
            _cashbox.DisplayMemberPath = nameof(CashboxOptionDto.Name);
            _cashbox.SelectedValuePath = nameof(CashboxOptionDto.Id);
            _cashbox.SelectedIndex = 0;
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
            _post.IsEnabled = true;
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
            var result = await FinanceUiService.Instance.PostPaymentVoucherAsync(voucherId);
            if (!ApplicationResultPresenter.Present(result))
                return;

            MockInteractionService.ShowSuccess("تم ترحيل سند الصرف.");
            _draftId = null;
            _post.IsEnabled = false;
            _status.Text = "تم الترحيل بنجاح.";
            _amount.Text = "0";
        }
        finally
        {
            _busy = false;
            _post.IsEnabled = _draftId.HasValue;
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
