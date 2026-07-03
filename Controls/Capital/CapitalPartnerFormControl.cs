using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Capital;
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
/// نموذج شريك — الاسم، نسبة الملكية، ومبلغ الاستثمار الأولي.
/// </summary>
public sealed class CapitalPartnerFormControl : UserControl
{
    private readonly TextBlock _title = ErpUiFactory.SectionTitle("شريك جديد");
    private readonly TextBlock _hint = new()
    {
        Text = "أدخل بيانات الشريك ونسبة ملكيته في الشركة. يمكنك تسجيل استثمارات إضافية لاحقاً من «حركة رأس مال».",
        Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!,
        Margin = new Thickness(0, 0, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBox _fullName = ErpUiFactory.FormField("");
    private readonly TextBox _ownership = ErpUiFactory.FormField("0");
    private readonly TextBox _initialAmount = ErpUiFactory.FormField("0");
    private readonly TextBox _phone = ErpUiFactory.FormField("");
    private readonly TextBox _nationalId = ErpUiFactory.FormField("");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly ComboBox _currency = ErpUiFactory.FilterCombo(["SAR", "USD", "EUR", "CNY"]);
    private readonly Button _save = new() { Content = "حفظ الشريك", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private Guid? _editId;
    private bool _saving;
    private bool _popupMode;

    public CapitalPartnerFormControl()
    {
        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_hint);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الاسم الكامل *", _fullName),
            ("نسبة الملكية % *", _ownership),
            ("مبلغ الدخول الأولي", _initialAmount),
            ("العملة", _currency),
            ("الهاتف", _phone),
            ("الهوية / السجل", _nationalId),
            ("ملاحظات", _notes))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack, MaxWidth = 600 };
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) CapitalPartnerPopupService.CancelActive();
            else MockInteractionService.Navigate(AppModule.CapitalPartners, "List");
        };
    }

    public void BindPopupHost()
    {
        _popupMode = true;
        _title.Visibility = Visibility.Collapsed;
        _hint.Visibility = Visibility.Collapsed;
        _cancel.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _editId = CapitalNavigationContext.EditPartnerId;
        if (_editId is Guid id)
        {
            _title.Text = "تعديل شريك";
            var result = await CapitalPartnerUiService.Instance.GetDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
            var d = result.Value;
            _fullName.Text = d.FullName;
            _phone.Text = d.Phone ?? "";
            _nationalId.Text = d.NationalId ?? "";
            _notes.Text = d.Notes ?? "";
            SelectCombo(_currency, d.DefaultCurrency);
            var companyPct = d.Participations
                .FirstOrDefault(p => p.Scope == PartnershipScope.Company && p.IsActive)?.OwnershipPercentage;
            if (companyPct is decimal pct)
                _ownership.Text = pct.ToString("0.##", CultureInfo.InvariantCulture);
            _initialAmount.IsEnabled = false;
            _initialAmount.Text = d.CurrentCapitalBase.ToString("0.##", CultureInfo.InvariantCulture);
        }
        else if (!await CapitalPartnerUiService.Instance.CanCreateAsync())
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية إنشاء شركاء.", "صلاحية");
        }
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        if (string.IsNullOrWhiteSpace(_fullName.Text))
        {
            MockInteractionService.ShowWarning("الاسم مطلوب.", "تحقق");
            return;
        }

        if (!TryParseDecimal(_ownership.Text, out var ownership) || ownership <= 0 || ownership > 100)
        {
            MockInteractionService.ShowWarning("نسبة الملكية يجب أن تكون بين 1 و 100.", "تحقق");
            return;
        }

        TryParseDecimal(_initialAmount.Text, out var initialAmount);
        if (initialAmount < 0)
        {
            MockInteractionService.ShowWarning("مبلغ الدخول لا يمكن أن يكون سالباً.", "تحقق");
            return;
        }

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                var result = await CapitalPartnerUiService.Instance.UpdatePartnerAsync(new UpdateCapitalPartnerCommand
                {
                    PartnerId = id,
                    FullName = _fullName.Text.Trim(),
                    NationalId = NullIfEmpty(_nationalId.Text),
                    Phone = NullIfEmpty(_phone.Text),
                    Notes = NullIfEmpty(_notes.Text),
                    DefaultCurrency = _currency.Text,
                    RiskLevel = PartnerRiskLevel.Medium
                });
                if (!ApplicationResultPresenter.Present(result)) return;

                var ownResult = await CapitalPartnerUiService.Instance.SetCompanyOwnershipAsync(id, ownership);
                if (ApplicationResultPresenter.Present(ownResult))
                {
                    MockInteractionService.ShowSuccess("تم حفظ بيانات الشريك.");
                    if (_popupMode) CapitalPartnerPopupService.CompleteSuccess();
                    else
                    {
                        CapitalListRefreshHub.RequestRefresh();
                        MockInteractionService.Navigate(AppModule.CapitalPartners, "List");
                    }
                }
            }
            else
            {
                var result = await CapitalPartnerUiService.Instance.CreatePartnerWithSetupAsync(
                    _fullName.Text.Trim(),
                    ownership,
                    initialAmount,
                    _currency.Text,
                    NullIfEmpty(_nationalId.Text),
                    NullIfEmpty(_phone.Text),
                    NullIfEmpty(_notes.Text));

                if (ApplicationResultPresenter.Present(result))
                {
                    MockInteractionService.ShowSuccess("تم إضافة الشريك بنجاح.");
                    if (_popupMode) CapitalPartnerPopupService.CompleteSuccess();
                    else
                    {
                        CapitalListRefreshHub.RequestRefresh();
                        MockInteractionService.Navigate(AppModule.CapitalPartners, "List");
                    }
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

    private static string? NullIfEmpty(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static void SelectCombo(ComboBox combo, string value)
    {
        for (int i = 0; i < combo.Items.Count; i++)
        {
            if (combo.Items[i]?.ToString() == value) { combo.SelectedIndex = i; return; }
        }
        combo.SelectedIndex = 0;
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
