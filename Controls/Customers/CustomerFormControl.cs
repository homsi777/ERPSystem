using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Customers;

public sealed class CustomerFormControl : UserControl
{
    private readonly TextBox _txtCode = ErpUiFactory.FormField("");
    private readonly TextBox _txtNameAr = ErpUiFactory.FormField("");
    private readonly TextBox _txtNameEn = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbType = ErpUiFactory.FilterCombo(["نقدي", "آجل"]);
    private readonly TextBox _txtCreditLimit = ErpUiFactory.FormField("0");
    private readonly TextBox _txtPaymentTerms = ErpUiFactory.FormField("0");
    private readonly Button _btnSave = new() { Content = "حفظ", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private readonly TextBlock _txtTitle = ErpUiFactory.SectionTitle("إضافة / تعديل عميل");

    private Guid? _editId;
    private bool _isSaving;

    public CustomerFormControl()
    {
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnCancel);

        var stack = new StackPanel();
        stack.Children.Add(_txtTitle);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("كود العميل", _txtCode),
            ("الاسم (عربي)", _txtNameAr),
            ("الاسم (إنجليزي)", _txtNameEn),
            ("نوع العميل", _cmbType),
            ("حد الائتمان", _txtCreditLimit),
            ("أيام السداد", _txtPaymentTerms))));
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack };

        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveAsync();
        _btnCancel.Click += (_, _) => MockInteractionService.Navigate(AppModule.Customers, "List");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _editId = CustomerNavigationContext.EditCustomerId;
        _txtCode.IsReadOnly = _editId.HasValue;

        if (_editId is Guid id)
        {
            _txtTitle.Text = "تعديل عميل";
            var result = await CustomerUiService.Instance.GetDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result))
                return;

            var c = result.Value!;
            _txtCode.Text = c.Code;
            _txtNameAr.Text = c.NameAr;
            _txtNameEn.Text = c.NameEn;
            _cmbType.SelectedIndex = c.Type == CustomerType.Credit ? 1 : 0;
            _txtCreditLimit.Text = c.CreditLimit.ToString("0.##");
            _txtPaymentTerms.Text = c.PaymentTermsDays.ToString();
        }
        else
        {
            _txtTitle.Text = "إضافة عميل";
            if (!await CustomerUiService.Instance.CanCreateAsync())
            {
                _btnSave.IsEnabled = false;
                MockInteractionService.ShowWarning("لا تملك صلاحية إضافة عملاء.", "صلاحية");
                return;
            }

            _txtCode.Text = await CustomerUiService.Instance.NextCustomerCodeAsync();
            _cmbType.SelectedIndex = 1;
        }
    }

    private async Task SaveAsync()
    {
        if (_isSaving)
            return;

        if (!decimal.TryParse(_txtCreditLimit.Text, out var creditLimit))
            creditLimit = 0;
        if (!int.TryParse(_txtPaymentTerms.Text, out var paymentTerms))
            paymentTerms = 0;

        var type = _cmbType.SelectedIndex == 1 ? CustomerType.Credit : CustomerType.Cash;
        _isSaving = true;
        _btnSave.IsEnabled = false;

        try
        {
            if (_editId is Guid id)
            {
                var result = await CustomerUiService.Instance.UpdateAsync(
                    id, _txtNameAr.Text.Trim(), _txtNameEn.Text.Trim(), creditLimit, paymentTerms);
                if (ApplicationResultPresenter.Present(result))
                {
                    CustomerListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.Customers, "List");
                }
            }
            else
            {
                var result = await CustomerUiService.Instance.CreateAsync(
                    _txtCode.Text.Trim(),
                    _txtNameAr.Text.Trim(),
                    _txtNameEn.Text.Trim(),
                    type,
                    creditLimit);
                if (ApplicationResultPresenter.Present(result))
                {
                    CustomerListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.Customers, "List");
                }
            }
        }
        finally
        {
            _isSaving = false;
            _btnSave.IsEnabled = true;
        }
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
