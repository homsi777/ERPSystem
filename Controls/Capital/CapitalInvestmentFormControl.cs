using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Capital;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Capital;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Capital;

/// <summary>
/// تسجيل حركة رأس مال — استثمار أو سحب لشريك موجود.
/// </summary>
public sealed class CapitalInvestmentFormControl : UserControl
{
    private readonly TextBlock _title = ErpUiFactory.SectionTitle("حركة رأس مال");
    private readonly ComboBox _partner = new() { MinWidth = 260, IsEditable = false };
    private readonly StackPanel _partnerRow = new();
    private readonly ComboBox _type = ErpUiFactory.FilterCombo(["استثمار", "استثمار إضافي", "سحب جزئي"]);
    private readonly TextBox _amount = ErpUiFactory.FormField("0");
    private readonly DatePicker _date = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _notes = ErpUiFactory.FormField("البيان");
    private readonly ComboBox _currency = ErpUiFactory.FilterCombo(["USD", "EUR", "CNY", "SAR"]);
    private readonly Button _save = new() { Content = "حفظ الحركة", Style = S("PrimaryButtonStyle"), MinWidth = 130, Height = 38 };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private IReadOnlyList<CapitalPartnerListDto> _partners = [];
    private bool _saving;
    private bool _popupMode;

    public CapitalInvestmentFormControl()
    {
        _partnerRow.Children.Add(_partner);
        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الشريك *", _partnerRow),
            ("نوع الحركة", _type),
            ("المبلغ *", _amount),
            ("العملة", _currency),
            ("التاريخ", _date),
            ("البيان", _notes))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack, MaxWidth = 620 };
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) CapitalPartnerPopupService.CancelActive();
            else MockInteractionService.Navigate(AppModule.CapitalPartners, "Transactions");
        };
    }

    public void BindPopupHost(bool hidePartnerPicker = false)
    {
        _popupMode = true;
        _title.Visibility = Visibility.Collapsed;
        _cancel.Visibility = Visibility.Collapsed;
        if (hidePartnerPicker)
            _partnerRow.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (CapitalNavigationContext.TransactionMode == "Withdrawal")
        {
            _title.Text = "سحب رأس مال";
            _type.SelectedIndex = 2;
            _type.IsEnabled = false;
        }
        else if (CapitalNavigationContext.TransactionMode == "Investment")
        {
            _title.Text = "استثمار رأس مال";
            _type.SelectedIndex = 1;
        }

        var result = await CapitalPartnerUiService.Instance.GetListAsync(new CapitalPartnerListFilter(), 1, 500);
        if (!ApplicationResultPresenter.Present(result)) return;

        _partners = result.Value?.Items ?? [];
        _partner.ItemsSource = _partners;
        _partner.DisplayMemberPath = nameof(CapitalPartnerListDto.FullName);

        if (CapitalNavigationContext.PreselectedPartnerId is Guid pre)
            _partner.SelectedItem = _partners.FirstOrDefault(p => p.Id == pre);
        else if (_partners.Count > 0)
            _partner.SelectedIndex = 0;

        CapitalNavigationContext.ClearTransactionContext();

        if (_partners.Count == 0)
            _save.IsEnabled = false;

        if (!await CapitalPartnerUiService.Instance.CanEditAsync())
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية تسجيل حركات.", "صلاحية");
        }
    }

    private async Task SaveAsync()
    {
        if (_saving) return;

        if (_partner.SelectedItem is not CapitalPartnerListDto partner)
        {
            MockInteractionService.ShowWarning("اختر الشريك.", "تحقق");
            return;
        }

        if (!TryParseDecimal(_amount.Text, out var amount) || amount <= 0)
        {
            MockInteractionService.ShowWarning("المبلغ يجب أن يكون أكبر من صفر.", "تحقق");
            return;
        }

        var txType = _type.SelectedIndex switch
        {
            2 => CapitalTransactionType.PartialWithdrawal,
            1 => CapitalTransactionType.AdditionalInvestment,
            _ => CapitalTransactionType.InitialInvestment
        };

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            var result = await CapitalPartnerUiService.Instance.RecordTransactionAsync(new RecordCapitalTransactionCommand
            {
                PartnerId = partner.Id,
                Type = txType,
                AmountOriginal = amount,
                Currency = _currency.Text,
                ExchangeRate = 1m,
                BaseCurrency = "USD",
                TransactionDate = _date.SelectedDate ?? DateTime.Today,
                Scope = PartnershipScope.Company,
                Notes = string.IsNullOrWhiteSpace(_notes.Text) ? null : _notes.Text.Trim()
            });

            if (ApplicationResultPresenter.Present(result))
            {
                MockInteractionService.ShowSuccess("تم تسجيل الحركة.");
                if (_popupMode) CapitalPartnerPopupService.CompleteSuccess();
                else
                {
                    CapitalListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.CapitalPartners, "Transactions");
                }
            }
        }
        finally
        {
            _saving = false;
            _save.IsEnabled = true;
        }
    }

    private static bool TryParseDecimal(string text, out decimal value) =>
        decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value)
        || decimal.TryParse(text, out value);

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
