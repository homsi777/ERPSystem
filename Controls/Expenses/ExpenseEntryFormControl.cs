using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Expenses;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Expenses;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Expenses;

/// <summary>
/// قيد مصروف يومي — المصروف، الصندوق، العملة، المبلغ، والبيان.
/// </summary>
public sealed class ExpenseEntryFormControl : UserControl
{
    private const string BaseCurrency = "USD";

    private sealed record CurrencyOption(string Code, string Label);

    private static readonly CurrencyOption[] CurrencyOptions =
    [
        new("USD", "دولار أمريكي (USD)"),
        new("SYP", "ليرة سورية (SYP)"),
        new("SAR", "ريال سعودي (SAR)"),
        new("EUR", "يورو (EUR)"),
        new("CNY", "يوان صيني (CNY)")
    ];

    private readonly TextBlock _title = ErpUiFactory.SectionTitle("قيد مصروف جديد");
    private readonly TextBlock _hint = new()
    {
        Text = "سجّل حركة صرف على تعريف موجود. العملة الأساسية للمشروع هي الدولار (USD).",
        Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
        Margin = new Thickness(0, 0, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly ComboBox _expense = new() { MinWidth = 280, IsEditable = false };
    private readonly StackPanel _expenseRow = new();
    private readonly ComboBox _cashbox = new() { MinWidth = 220, IsEditable = false };
    private readonly ComboBox _currency = new()
    {
        MinWidth = 280,
        IsEditable = false,
        ItemsSource = CurrencyOptions,
        DisplayMemberPath = nameof(CurrencyOption.Label)
    };
    private readonly TextBlock _amountLabel = new()
    {
        FontSize = 11,
        Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!
    };
    private readonly TextBox _description = ErpUiFactory.FormField("البيان — وصف الحركة");
    private readonly TextBox _amount = ErpUiFactory.FormField("0");
    private readonly TextBox _exchangeRate = ErpUiFactory.FormField("15000");
    private readonly TextBlock _exchangeRateLabel = new()
    {
        FontSize = 11,
        Foreground = (Brush)WpfApplication.Current.Resources["TextMutedBrush"]!
    };
    private readonly TextBlock _convertedPreview = new()
    {
        FontSize = 13,
        FontWeight = FontWeights.SemiBold,
        Foreground = (Brush)WpfApplication.Current.Resources["PrimaryBrush"]!,
        Margin = new Thickness(0, 4, 0, 0)
    };
    private readonly StackPanel _exchangeRateRow = new();
    private readonly StackPanel _convertedRow = new();
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly Button _save = new() { Content = "حفظ القيد", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 38 };
    private readonly Button _define = new() { Content = "تعريف مصروف جديد", Style = S("SecondaryButtonStyle"), MinWidth = 150, Height = 38, Margin = new Thickness(8, 0, 0, 0) };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private IReadOnlyList<ExpenseListDto> _expenses = [];
    private bool _saving;
    private bool _popupMode;

    public ExpenseEntryFormControl()
    {
        _expenseRow.Children.Add(_expense);
        _amountLabel.Text = "المبلغ *";
        _exchangeRateLabel.Text = "سعر الصرف (ليرة سورية لكل 1 دولار) *";
        _exchangeRateRow.Children.Add(Labeled(
            _exchangeRateLabel,
            _exchangeRate,
            "مثال: إذا كان 1$ = 15,000 ل.س اكتب 15000"));
        _convertedRow.Children.Add(_convertedPreview);
        _exchangeRateRow.Visibility = Visibility.Collapsed;
        _convertedRow.Visibility = Visibility.Collapsed;

        var amountPanel = new StackPanel();
        amountPanel.Children.Add(_amountLabel);
        amountPanel.Children.Add(_amount);

        var form = ErpUiFactory.BuildFormGrid(
            ("المصروف *", _expenseRow),
            ("الصندوق *", _cashbox),
            ("البيان *", _description),
            ("العملة *", _currency),
            ("", amountPanel),
            ("", _exchangeRateRow),
            ("", _convertedRow),
            ("التاريخ", _date));

        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_hint);
        stack.Children.Add(ErpUiFactory.Card(form));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_define);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack, MaxWidth = 640 };
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
        _define.Click += (_, _) => ExpensePopupService.ShowCreate();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) ExpensePopupService.CancelActive();
            else MockInteractionService.Navigate(AppModule.Expenses, "Entries");
        };

        _currency.SelectionChanged += (_, _) => UpdateCurrencyUi();
        _amount.TextChanged += (_, _) => UpdateConvertedPreview();
        _exchangeRate.TextChanged += (_, _) => UpdateConvertedPreview();
    }

    public void BindPopupHost(bool hideExpensePicker = false)
    {
        _popupMode = true;
        _title.Visibility = Visibility.Collapsed;
        _hint.Visibility = Visibility.Collapsed;
        _cancel.Visibility = Visibility.Collapsed;
        _define.Visibility = Visibility.Collapsed;
        if (hideExpensePicker)
            _expenseRow.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        SelectCurrency(BaseCurrency);

        var expenses = await ExpenseUiService.Instance.GetDefinitionsAsync();
        if (!ApplicationResultPresenter.Present(expenses))
            return;

        _expenses = expenses.Value?.Items ?? [];
        _expense.ItemsSource = _expenses;
        _expense.DisplayMemberPath = nameof(ExpenseListDto.Name);

        if (ExpenseNavigationContext.PreselectedExpenseId is Guid pre)
        {
            _expense.SelectedItem = _expenses.FirstOrDefault(x => x.Id == pre);
            ExpenseNavigationContext.ClearPreselection();
        }
        else if (_expenses.Count > 0)
            _expense.SelectedIndex = 0;

        if (_expenses.Count == 0)
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا يوجد مصروف معرّف. أنشئ تعريفاً أولاً.", "المصاريف");
        }

        var boxes = await ExpenseUiService.Instance.GetCashboxesAsync();
        if (ApplicationResultPresenter.Present(boxes))
        {
            var list = boxes.Value ?? [];
            _cashbox.ItemsSource = list;
            _cashbox.DisplayMemberPath = nameof(CashboxOptionDto.Name);
            if (list.Count > 0) _cashbox.SelectedIndex = 0;
            if (list.Count == 0) _save.IsEnabled = false;
        }

        if (!await ExpenseUiService.Instance.CanEditAsync())
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية تسجيل قيود.", "صلاحية");
        }

        UpdateCurrencyUi();
    }

    private void UpdateCurrencyUi()
    {
        var currency = GetSelectedCurrency();
        var isForeign = !string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase);
        _exchangeRateRow.Visibility = isForeign ? Visibility.Visible : Visibility.Collapsed;
        _convertedRow.Visibility = isForeign ? Visibility.Visible : Visibility.Collapsed;

        _amountLabel.Text = isForeign
            ? $"المبلغ بـ{GetCurrencyArabicName(currency)} *"
            : "المبلغ بالدولار (USD) *";

        _exchangeRateLabel.Text = isForeign
            ? $"سعر الصرف ({GetCurrencyArabicName(currency)} لكل 1 دولار) *"
            : "سعر الصرف";

        UpdateConvertedPreview();
    }

    private void UpdateConvertedPreview()
    {
        if (!IsForeignCurrency())
        {
            _convertedPreview.Text = "";
            return;
        }

        if (TryParseDecimal(_amount.Text, out var amount) &&
            TryParseDecimal(_exchangeRate.Text, out var rate) &&
            rate > 0)
        {
            var usd = Math.Round(amount / rate, 4);
            _convertedPreview.Text = $"المعادل بالدولار: {usd:N2} {BaseCurrency}";
        }
        else
        {
            _convertedPreview.Text = "المعادل بالدولار: —";
        }
    }

    private async Task SaveAsync()
    {
        if (_saving) return;

        if (_expense.SelectedItem is not ExpenseListDto exp)
        {
            MessageBox.Show("اختر المصروف.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_cashbox.SelectedItem is not CashboxOptionDto box)
        {
            MessageBox.Show("اختر الصندوق.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_description.Text) ||
            _description.Text.Trim() == "البيان - وصف الحركة")
        {
            MessageBox.Show("البيان مطلوب.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!TryParseDecimal(_amount.Text, out var amountOriginal) || amountOriginal <= 0)
        {
            MessageBox.Show("المبلغ يجب أن يكون أكبر من صفر.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var currency = GetSelectedCurrency();
        decimal exchangeRate = 1m;
        decimal amountBase;

        if (IsForeignCurrency())
        {
            if (!TryParseDecimal(_exchangeRate.Text, out exchangeRate) || exchangeRate <= 0)
            {
                MessageBox.Show("سعر الصرف يجب أن يكون أكبر من صفر.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            amountBase = Math.Round(amountOriginal / exchangeRate, 4);
            if (amountBase <= 0)
            {
                MessageBox.Show("المعادل بالدولار غير صالح. تحقق من المبلغ وسعر الصرف.", "المصاريف", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            amountBase = amountOriginal;
            currency = BaseCurrency;
        }

        var payDate = _date.SelectedDate ?? DateTime.Today;

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            var result = await ExpenseUiService.Instance.RecordPaymentAsync(new RecordExpensePaymentCommand
            {
                ExpenseId = exp.Id,
                PaymentDate = payDate,
                AmountOriginal = amountOriginal,
                AmountBase = amountBase,
                Currency = currency,
                ExchangeRateSnapshot = exchangeRate,
                PaymentMethod = ExpensePaymentMethod.Cash,
                FundingSource = ExpenseFundingSource.Cash,
                Notes = _description.Text.Trim(),
                CashboxId = box.Id
            });

            if (ApplicationResultPresenter.Present(result))
            {
                var savedMsg = IsForeignCurrency()
                    ? $"تم تسجيل القيد: {amountOriginal:N0} {currency} = {amountBase:N2} {BaseCurrency}"
                    : "تم تسجيل القيد.";
                MockInteractionService.ShowSuccess(savedMsg);
                _description.Text = "";
                _amount.Text = "0";
                if (_popupMode)
                    ExpensePopupService.CompleteSuccess();
                else
                {
                    ExpenseListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.Expenses, "Entries");
                }
            }
        }
        finally
        {
            _saving = false;
            _save.IsEnabled = true;
        }
    }

    private string GetSelectedCurrency() =>
        (_currency.SelectedItem as CurrencyOption)?.Code ?? BaseCurrency;

    private bool IsForeignCurrency() =>
        !string.Equals(GetSelectedCurrency(), BaseCurrency, StringComparison.OrdinalIgnoreCase);

    private void SelectCurrency(string code)
    {
        for (int i = 0; i < CurrencyOptions.Length; i++)
        {
            if (CurrencyOptions[i].Code == code)
            {
                _currency.SelectedIndex = i;
                return;
            }
        }
        _currency.SelectedIndex = 0;
    }

    private static string GetCurrencyArabicName(string code) => code switch
    {
        "SYP" => "الليرة السورية",
        "SAR" => "الريال السعودي",
        "EUR" => "اليورو",
        "CNY" => "اليوان الصيني",
        _ => "الدولار الأمريكي"
    };

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(text, out value);

    private static StackPanel Labeled(TextBlock label, UIElement control, string? hint = null)
    {
        var sp = new StackPanel();
        sp.Children.Add(label);
        if (!string.IsNullOrWhiteSpace(hint))
        {
            sp.Children.Add(new TextBlock
            {
                Text = hint,
                FontSize = 10,
                Foreground = (Brush)WpfApplication.Current.Resources["TextSecondaryBrush"]!,
                Margin = new Thickness(0, 2, 0, 4),
                TextWrapping = TextWrapping.Wrap
            });
        }
        sp.Children.Add(control);
        return sp;
    }

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
