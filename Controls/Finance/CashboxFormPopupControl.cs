using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Finance;

public sealed class CashboxFormPopupControl : UserControl
{
    private readonly TextBlock _title = ErpUiFactory.SectionTitle("صندوق جديد");
    private readonly TextBox _txtCode = ErpUiFactory.FormField("");
    private readonly TextBox _txtName = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbCurrency = ErpUiFactory.FilterCombo(ERPSystem.Services.Settings.CurrencyCatalog.CurrencyArray);
    private readonly Button _btnSave = new() { Content = "حفظ", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private Guid? _editId;
    private bool _saving;

    public CashboxFormPopupControl()
    {
        var stack = new StackPanel { MaxWidth = 480 };
        stack.Children.Add(_title);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("كود الصندوق", _txtCode),
            ("اسم الصندوق *", _txtName),
            ("العملة", _cmbCurrency))));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnCancel);
        stack.Children.Add(actions);
        Content = stack;
        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveAsync();
        _btnCancel.Click += (_, _) => CashboxPopupService.CancelActive();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _editId = CashboxNavigationContext.EditCashboxId;
        _txtCode.IsReadOnly = _editId.HasValue;
        if (_editId is Guid id)
        {
            _title.Text = "تعديل صندوق";
            var result = await FinanceUiService.Instance.GetCashboxDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
            var c = result.Value;
            _txtCode.Text = c.Code;
            _txtName.Text = c.Name;
            SelectCurrency(c.Currency);
        }
        else
        {
            _title.Text = "صندوق جديد";
            if (!await FinanceUiService.Instance.CanCreateCashboxAsync())
            {
                _btnSave.IsEnabled = false;
                MockInteractionService.ShowWarning("لا تملك صلاحية إضافة صناديق.", "صلاحية");
                return;
            }
            _txtCode.Text = await FinanceUiService.Instance.NextCashboxCodeAsync();
            _cmbCurrency.SelectedIndex = 0;
        }
    }

    private void SelectCurrency(string currency)
    {
        for (var i = 0; i < _cmbCurrency.Items.Count; i++)
        {
            if (_cmbCurrency.Items[i]?.ToString() == currency)
            {
                _cmbCurrency.SelectedIndex = i;
                return;
            }
        }
        _cmbCurrency.SelectedIndex = 0;
    }

    private async Task SaveAsync()
    {
        if (_saving) return;
        if (string.IsNullOrWhiteSpace(_txtName.Text))
        {
            MockInteractionService.ShowWarning("اسم الصندوق مطلوب.");
            return;
        }
        var currency = _cmbCurrency.SelectedItem?.ToString() ?? "USD";
        _saving = true;
        _btnSave.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                var result = await FinanceUiService.Instance.UpdateCashboxAsync(
                    id, _txtCode.Text.Trim(), _txtName.Text.Trim(), currency);
                if (ApplicationResultPresenter.Present(result))
                {
                    CashboxListRefreshHub.RequestRefresh();
                    CashboxPopupService.CloseActive(true);
                }
            }
            else
            {
                var result = await FinanceUiService.Instance.CreateCashboxAsync(
                    _txtCode.Text.Trim(), _txtName.Text.Trim(), currency);
                if (ApplicationResultPresenter.Present(result))
                {
                    CashboxListRefreshHub.RequestRefresh();
                    CashboxPopupService.CloseActive(true);
                }
            }
        }
        finally
        {
            _saving = false;
            _btnSave.IsEnabled = true;
        }
    }

    private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
}
