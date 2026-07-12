using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Controls.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

/// <summary>Carries a preselected customer into the receipt voucher form (e.g. from the customer right-click «تسجيل قبض»).</summary>
public static class ReceiptVoucherNavigationContext
{
    public static Guid? PreselectCustomerId { get; set; }

    public static Guid? TakeCustomerId()
    {
        var id = PreselectCustomerId;
        PreselectCustomerId = null;
        return id;
    }
}

/// <summary>سند قبض — إنشاء مسودة وترحيل.</summary>
public sealed class ReceiptVoucherPageControl : UserControl
{
    private readonly ComboBox _paymentMethod = new() { MinWidth = 220, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly ComboBox _customer = new() { MinWidth = 280, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly ComboBox _cashbox = new() { MinWidth = 220, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly ComboBox _bankAccount = new() { MinWidth = 220, IsEditable = false, Style = S("EnterpriseComboBoxStyle"), Visibility = Visibility.Collapsed };
    private readonly TextBox _reference = ErpUiFactory.FormField("");
    private readonly Border _referenceRow = new() { Visibility = Visibility.Collapsed };
    private readonly Border _cashboxRow = new();
    private readonly Border _bankRow = new() { Visibility = Visibility.Collapsed };
    private readonly TextBlock _glAccount = new() { FontSize = 11, Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!, Margin = new Thickness(0, 4, 0, 0) };
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
    private readonly Button _print = new() { Content = "طباعة", Style = S("GhostButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0), IsEnabled = false };

    private readonly DataGrid _allocationsGrid = new()
    {
        AutoGenerateColumns = false,
        IsReadOnly = false,
        CanUserAddRows = false,
        MinHeight = 140,
        MaxHeight = 260,
        HeadersVisibility = DataGridHeadersVisibility.Column
    };
    private readonly Border _allocationSection = new() { Visibility = Visibility.Collapsed, Margin = new Thickness(0, 12, 0, 0) };
    private readonly TextBlock _allocationHint = new() { FontSize = 11, Margin = new Thickness(0, 6, 0, 0), Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]! };
    private List<OpenInvoiceOption> _openInvoices = new();

    private Guid? _draftId;
    private Guid? _lastVoucherId;
    private bool _busy;

    public ReceiptVoucherPageControl()
    {
        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("سند قبض"));
        stack.Children.Add(ErpUxFactory.InfoBanner("تحصيل نقدي من عميل — احفظ مسودة ثم رحّل السند.", "info"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("وسيلة الدفع *", _paymentMethod),
            ("العميل *", _customer),
            ("الصندوق *", _cashboxRow),
            ("البنك *", _bankRow),
            ("المرجع *", _referenceRow),
            ("المبلغ *", _amount),
            ("التاريخ", _date))));
        _cashboxRow.Child = WrapCashboxField();
        _bankRow.Child = _bankAccount;
        _referenceRow.Child = _reference;
        stack.Children.Add(_glAccount);
        stack.Children.Add(_status);

        BuildAllocationGrid();
        stack.Children.Add(_allocationSection);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_createDraft);
        actions.Children.Add(_post);
        actions.Children.Add(_print);
        stack.Children.Add(actions);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack, MaxWidth = 720 };
        CashboxDropdownBinder.WireRefresh(_cashbox);
        Loaded += OnLoaded;
        _createDraft.Click += async (_, _) => await CreateDraftAsync();
        _post.Click += async (_, _) => await PostAsync();
        _print.Click += async (_, _) => await PrintAsync();
        _customer.SelectionChanged += async (_, _) => await LoadOpenInvoicesAsync();
        _paymentMethod.SelectionChanged += (_, _) => UpdatePaymentMethodUi();
        _cashbox.SelectionChanged += (_, _) => UpdateGlAccountDisplay();
        _bankAccount.SelectionChanged += (_, _) => UpdateGlAccountDisplay();
    }

    private UIElement WrapCashboxField()
    {
        var panel = new StackPanel();
        panel.Children.Add(_cashbox);
        return panel;
    }

    private void UpdatePaymentMethodUi()
    {
        if (_paymentMethod.SelectedItem is not PaymentMethodDto method)
        {
            _cashboxRow.Visibility = Visibility.Visible;
            _bankRow.Visibility = Visibility.Collapsed;
            _referenceRow.Visibility = Visibility.Collapsed;
            return;
        }

        _cashboxRow.Visibility = method.RequiresCashbox ? Visibility.Visible : Visibility.Collapsed;
        _bankRow.Visibility = method.RequiresBankAccount ? Visibility.Visible : Visibility.Collapsed;
        _referenceRow.Visibility = method.RequiresReference ? Visibility.Visible : Visibility.Collapsed;
        UpdateGlAccountDisplay();
    }

    private void UpdateGlAccountDisplay()
    {
        if (_paymentMethod.SelectedItem is PaymentMethodDto { RequiresBankAccount: true } &&
            _bankAccount.SelectedItem is BankAccountListDto bank)
        {
            _glAccount.Text = $"الحساب المحاسبي: {bank.GlAccountId}";
            return;
        }

        if (_cashbox.SelectedItem is CashboxListDto box)
        {
            _glAccount.Text = box.AccountId is Guid aid && aid != Guid.Empty
                ? $"الحساب المحاسبي: {aid}"
                : "الحساب المحاسبي: غير مربوط — لا يمكن الترحيل";
            return;
        }

        _glAccount.Text = "";
    }

    private void BuildAllocationGrid()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_allocationsGrid);
        _allocationsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "رقم الفاتورة",
            Binding = new Binding(nameof(OpenInvoiceOption.InvoiceNumber)),
            IsReadOnly = true,
            Width = new DataGridLength(1.4, DataGridLengthUnitType.Star)
        });
        _allocationsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "التاريخ",
            Binding = new Binding(nameof(OpenInvoiceOption.DateDisplay)),
            IsReadOnly = true,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
        _allocationsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "الإجمالي",
            Binding = new Binding(nameof(OpenInvoiceOption.TotalDisplay)),
            IsReadOnly = true,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
        _allocationsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "المتبقي",
            Binding = new Binding(nameof(OpenInvoiceOption.RemainingDisplay)),
            IsReadOnly = true,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });
        _allocationsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "المخصص",
            Binding = new Binding(nameof(OpenInvoiceOption.Allocated)) { Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged },
            IsReadOnly = false,
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        var section = new StackPanel();
        section.Children.Add(ErpUiFactory.SectionTitle("توزيع القبض على الفواتير"));
        var autoBtn = new Button { Content = "توزيع تلقائي", Style = S("GhostButtonStyle"), Height = 30, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 0, 6) };
        autoBtn.Click += (_, _) => AutoAllocate();
        section.Children.Add(autoBtn);
        section.Children.Add(_allocationsGrid);
        section.Children.Add(_allocationHint);
        _allocationSection.Child = ErpUiFactory.Card(section);
    }

    private async Task LoadOpenInvoicesAsync()
    {
        if (_customer.SelectedValue is not Guid customerId || customerId == Guid.Empty || !AppServices.IsInitialized)
        {
            _allocationSection.Visibility = Visibility.Collapsed;
            return;
        }

        _openInvoices = (await FinanceUiService.Instance.GetOpenInvoicesForCustomerAsync(customerId)).ToList();
        _allocationsGrid.ItemsSource = null;
        _allocationsGrid.ItemsSource = _openInvoices;
        _allocationSection.Visibility = _openInvoices.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        _allocationHint.Text = _openInvoices.Count > 0
            ? "اترك الحقول فارغة لترحيل القبض كرصيد غير مخصص، أو وزّع المبلغ على الفواتير المفتوحة."
            : "";
    }

    private void AutoAllocate()
    {
        _allocationsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        if (!TryParseDecimal(_amount.Text, out var remaining) || remaining <= 0) return;

        foreach (var inv in _openInvoices)
        {
            if (remaining <= 0) { inv.Allocated = 0; continue; }
            var take = Math.Min(remaining, inv.Remaining);
            inv.Allocated = take;
            remaining -= take;
        }
        _allocationsGrid.ItemsSource = null;
        _allocationsGrid.ItemsSource = _openInvoices;
    }

    private IReadOnlyList<ReceiptInvoiceAllocationInput> ReadAllocations(decimal voucherAmount, out bool valid)
    {
        valid = true;
        _allocationsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
        var list = new List<ReceiptInvoiceAllocationInput>();
        decimal totalAllocated = 0;

        foreach (var inv in _openInvoices)
        {
            if (inv.Allocated <= 0) continue;
            if (inv.Allocated > inv.Remaining + 0.005m)
            {
                MessageBox.Show($"المبلغ المخصص للفاتورة {inv.InvoiceNumber} يتجاوز المتبقي.", "توزيع القبض", MessageBoxButton.OK, MessageBoxImage.Warning);
                valid = false;
                return [];
            }
            totalAllocated += inv.Allocated;
            list.Add(new ReceiptInvoiceAllocationInput { SalesInvoiceId = inv.Id, Amount = inv.Allocated });
        }

        if (totalAllocated > voucherAmount + 0.005m)
        {
            MessageBox.Show("إجمالي المخصص يتجاوز مبلغ السند.", "توزيع القبض", MessageBoxButton.OK, MessageBoxImage.Warning);
            valid = false;
            return [];
        }

        return list;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var methods = await FinanceUiService.Instance.GetPaymentMethodsAsync();
        if (ApplicationResultPresenter.Present(methods) && methods.Value is { Count: > 0 })
        {
            _paymentMethod.ItemsSource = methods.Value;
            _paymentMethod.DisplayMemberPath = nameof(PaymentMethodDto.Name);
            _paymentMethod.SelectedValuePath = nameof(PaymentMethodDto.Id);
            _paymentMethod.SelectedIndex = 0;
        }

        var banks = await FinanceUiService.Instance.GetBankAccountsAsync();
        if (ApplicationResultPresenter.Present(banks) && banks.Value is { Count: > 0 })
        {
            _bankAccount.ItemsSource = banks.Value;
            _bankAccount.DisplayMemberPath = nameof(BankAccountListDto.Name);
            _bankAccount.SelectedValuePath = nameof(BankAccountListDto.Id);
        }

        var customers = await FinanceUiService.Instance.GetCustomersAsync();
        if (ApplicationResultPresenter.Present(customers) && customers.Value is { Count: > 0 })
        {
            _customer.ItemsSource = customers.Value;
            _customer.DisplayMemberPath = nameof(FinancePartyOption.Display);
            _customer.SelectedValuePath = nameof(FinancePartyOption.Id);
            _customer.SelectedIndex = 0;

            var preselect = ReceiptVoucherNavigationContext.TakeCustomerId();
            if (preselect is Guid customerId && customers.Value.Any(c => c.Id == customerId))
                _customer.SelectedValue = customerId;
        }

        await LoadOpenInvoicesAsync();

        var boxes = await FinanceUiService.Instance.GetCashboxListAsync(includeInactive: false);
        if (ApplicationResultPresenter.Present(boxes) && boxes.Value is { Count: > 0 })
        {
            _cashbox.ItemsSource = boxes.Value;
            _cashbox.DisplayMemberPath = nameof(CashboxListDto.Name);
            _cashbox.SelectedValuePath = nameof(CashboxListDto.Id);
            _cashbox.SelectedIndex = 0;
        }

        UpdatePaymentMethodUi();
    }

    private async Task CreateDraftAsync()
    {
        if (_busy) return;
        if (!TryReadForm(out var customerId, out var cashboxId, out var amount))
            return;

        var allocations = ReadAllocations(amount, out var allocationsValid);
        if (!allocationsValid) return;

        _busy = true;
        _createDraft.IsEnabled = false;
        try
        {
            var result = await FinanceUiService.Instance.CreateReceiptVoucherAsync(
                customerId, cashboxId, amount, allocations,
                _paymentMethod.SelectedValue as Guid? ?? PaymentMethodIds.Cash,
                _bankAccount.SelectedValue as Guid?,
                string.IsNullOrWhiteSpace(_reference.Text) ? null : _reference.Text.Trim());
            if (!ApplicationResultPresenter.Present(result))
                return;

            _draftId = result.Value;
            _lastVoucherId = result.Value;
            _post.IsEnabled = true;
            _print.IsEnabled = true;
            _status.Text = $"تم حفظ المسودة — جاهز للترحيل.";
            _status.Foreground = (Brush)WpfApplication.Current.Resources["SuccessBrush"]!;
            MockInteractionService.ShowSuccess("تم حفظ سند القبض كمسودة.");
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
            if (!TryReadForm(out var customerId, out var cashboxId, out var amount))
                return;

            var allocations = ReadAllocations(amount, out var allocationsValid);
            if (!allocationsValid) return;

            _busy = true;
            _createDraft.IsEnabled = false;
            _post.IsEnabled = false;
            try
            {
                var create = await FinanceUiService.Instance.CreateReceiptVoucherAsync(
                    customerId, cashboxId, amount, allocations,
                    _paymentMethod.SelectedValue as Guid? ?? PaymentMethodIds.Cash,
                    _bankAccount.SelectedValue as Guid?,
                    string.IsNullOrWhiteSpace(_reference.Text) ? null : _reference.Text.Trim());
                if (!ApplicationResultPresenter.Present(create))
                    return;
                voucherId = create.Value;
                _lastVoucherId = voucherId;
                _print.IsEnabled = true;
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
            var result = await FinanceUiService.Instance.PostReceiptVoucherAsync(voucherId);
            if (!ApplicationResultPresenter.Present(result))
                return;

            MockInteractionService.ShowSuccess("تم ترحيل سند القبض.");
            _draftId = null;
            _post.IsEnabled = false;
            _status.Text = "تم الترحيل بنجاح.";
            _amount.Text = "0";
            ERPSystem.Services.Sales.SalesListRefreshHub.RequestRefresh();
            await LoadOpenInvoicesAsync();
        }
        finally
        {
            _busy = false;
            _post.IsEnabled = _draftId.HasValue;
        }
    }

    private async Task PrintAsync()
    {
        if (_lastVoucherId is not Guid voucherId || _busy) return;

        _busy = true;
        _print.IsEnabled = false;
        try
        {
            var result = await FinanceUiService.Instance.GetReceiptVoucherPrintAsync(voucherId);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null)
                return;

            ERPSystem.Services.Finance.ReceiptVoucherDocumentService.ShowVoucherPreview(result.Value, exportPdf: false);
        }
        finally
        {
            _busy = false;
            _print.IsEnabled = true;
        }
    }

    private bool TryReadForm(out Guid customerId, out Guid cashboxId, out decimal amount)
    {
        customerId = Guid.Empty;
        cashboxId = Guid.Empty;
        amount = 0;

        if (_customer.SelectedValue is not Guid cid || cid == Guid.Empty)
        {
            MessageBox.Show("اختر العميل.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        if (_paymentMethod.SelectedItem is PaymentMethodDto method)
        {
            if (method.RequiresCashbox)
            {
                if (_cashbox.SelectedValue is not Guid box || box == Guid.Empty)
                {
                    MessageBox.Show("اختر الصندوق.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
                cashboxId = box;
                if (_cashbox.SelectedItem is CashboxListDto selectedCashbox
                    && (selectedCashbox.AccountId is null || selectedCashbox.AccountId == Guid.Empty))
                {
                    MessageBox.Show("الصندوق لا يملك حساب GL — لا يمكن الترحيل.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            if (method.RequiresBankAccount && (_bankAccount.SelectedValue is not Guid bankId || bankId == Guid.Empty))
            {
                MessageBox.Show("اختر الحساب البنكي.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (method.RequiresReference && string.IsNullOrWhiteSpace(_reference.Text))
            {
                MessageBox.Show("أدخل مرجع التحويل.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }
        else if (_cashbox.SelectedValue is not Guid fallbackBox || fallbackBox == Guid.Empty)
        {
            MessageBox.Show("اختر الصندوق.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        else
        {
            cashboxId = fallbackBox;
        }

        if (cashboxId == Guid.Empty && _cashbox.SelectedValue is Guid selectedBox)
            cashboxId = selectedBox;

        if (!TryParseDecimal(_amount.Text, out amount) || amount <= 0)
        {
            MessageBox.Show("أدخل مبلغاً صحيحاً أكبر من صفر.", "سند قبض", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        customerId = cid;
        if (cashboxId == Guid.Empty && _cashbox.SelectedValue is Guid boxId)
            cashboxId = boxId;
        return true;
    }

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(text, out value);

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
