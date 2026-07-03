using ERPSystem.Application.Commands.Suppliers;
using ERPSystem.Application.Common;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Suppliers;

public sealed class SupplierFormControl : UserControl
{
    private readonly TextBox _txtCode = ErpUiFactory.FormField("");
    private readonly TextBox _txtNameAr = ErpUiFactory.FormField("");
    private readonly TextBox _txtNameEn = ErpUiFactory.FormField("");
    private readonly TextBox _txtPhone = ErpUiFactory.FormField("");
    private readonly TextBox _txtEmail = ErpUiFactory.FormField("");
    private readonly TextBox _txtAddress = ErpUiFactory.FormField("");
    private readonly TextBox _txtCountry = ErpUiFactory.FormField("");
    private readonly TextBox _txtCity = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbTerms = ErpUiFactory.FilterCombo(["فوري", "Net 15", "Net 30", "Net 60", "Net 90"]);
    private readonly TextBox _txtCreditLimit = ErpUiFactory.FormField("0");
    private readonly TextBox _txtTaxNumber = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbPayablesAccount = new() { Height = 36, MinWidth = 240 };
    private readonly TextBox _txtNotes = ErpUiFactory.FormField("");
    private readonly Button _btnSave = new() { Content = "حفظ", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private readonly TextBlock _txtTitle = ErpUiFactory.SectionTitle("إضافة / تعديل مورد");

    private Guid? _editId;
    private bool _isSaving;

    public SupplierFormControl()
    {
        _cmbTerms.SelectedIndex = 2;
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnCancel);

        var stack = new StackPanel();
        stack.Children.Add(_txtTitle);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("كود المورد", _txtCode),
            ("الاسم (عربي)", _txtNameAr),
            ("الاسم (إنجليزي)", _txtNameEn),
            ("الهاتف", _txtPhone),
            ("البريد", _txtEmail),
            ("الدولة", _txtCountry),
            ("المدينة", _txtCity),
            ("العنوان", _txtAddress),
            ("شروط السداد", _cmbTerms),
            ("حد الائتمان", _txtCreditLimit),
            ("الرقم الضريبي", _txtTaxNumber),
            ("حساب الذمم الدائنة", _cmbPayablesAccount),
            ("ملاحظات", _txtNotes))));
        stack.Children.Add(actions);
        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack };

        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveAsync();
        _btnCancel.Click += (_, _) => MockInteractionService.Navigate(AppModule.Suppliers, "List");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadPayablesAccountsAsync();
        _editId = SupplierNavigationContext.EditSupplierId;
        _txtCode.IsReadOnly = _editId.HasValue;

        if (_editId is Guid id)
        {
            _txtTitle.Text = "تعديل مورد";
            var result = await SupplierUiService.Instance.GetDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result))
                return;

            var s = result.Value!;
            _txtCode.Text = s.Code;
            _txtNameAr.Text = s.NameAr;
            _txtNameEn.Text = s.NameEn;
            _txtPhone.Text = s.Phone ?? "";
            _txtEmail.Text = s.Email ?? "";
            _txtCountry.Text = s.Country ?? "";
            _txtCity.Text = s.City ?? "";
            _txtAddress.Text = s.Address ?? "";
            _txtCreditLimit.Text = s.CreditLimit.ToString("0.##");
            _txtTaxNumber.Text = s.TaxNumber ?? "";
            _txtNotes.Text = s.Notes ?? "";
            _cmbTerms.SelectedIndex = TermsIndex(s.PaymentTermsDays);
            SelectPayablesAccount(s.PayablesAccountId);
        }
        else
        {
            _txtTitle.Text = "إضافة مورد";
            if (!await SupplierUiService.Instance.CanCreateAsync())
            {
                _btnSave.IsEnabled = false;
                MockInteractionService.ShowWarning("لا تملك صلاحية إضافة موردين.", "صلاحية");
                return;
            }

            _txtCode.Text = await SupplierUiService.Instance.NextSupplierCodeAsync();
        }
    }

    private async Task LoadPayablesAccountsAsync()
    {
        _cmbPayablesAccount.Items.Clear();
        var result = await AccountingUiService.Instance.GetAccountsAsync();
        if (!result.IsSuccess || result.Value is null)
            return;

        foreach (var a in result.Value.Where(x => x.AccountType == GlAccountType.Liability && x.IsPostable))
        {
            _cmbPayablesAccount.Items.Add(new ComboBoxItem
            {
                Content = $"{a.Code} — {a.NameAr}",
                Tag = a.Id
            });
        }
    }

    private void SelectPayablesAccount(Guid accountId)
    {
        foreach (ComboBoxItem item in _cmbPayablesAccount.Items)
        {
            if (item.Tag is Guid id && id == accountId)
            {
                _cmbPayablesAccount.SelectedItem = item;
                return;
            }
        }
    }

    private async Task SaveAsync()
    {
        if (_isSaving || string.IsNullOrWhiteSpace(_txtNameAr.Text))
        {
            MockInteractionService.ShowWarning("اسم المورد مطلوب.", "تحقق");
            return;
        }

        if (!decimal.TryParse(_txtCreditLimit.Text, out var creditLimit))
            creditLimit = 0;

        var terms = TermsDays(_cmbTerms.SelectedIndex);
        var payablesId = (_cmbPayablesAccount.SelectedItem as ComboBoxItem)?.Tag as Guid?;

        _isSaving = true;
        _btnSave.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                if (!payablesId.HasValue)
                {
                    MockInteractionService.ShowWarning("اختر حساب الذمم الدائنة.", "تحقق");
                    return;
                }

                var result = await SupplierUiService.Instance.UpdateAsync(new UpdateSupplierCommand
                {
                    SupplierId = id,
                    NameAr = _txtNameAr.Text.Trim(),
                    NameEn = _txtNameEn.Text.Trim(),
                    Phone = _txtPhone.Text.Trim(),
                    Email = _txtEmail.Text.Trim(),
                    Address = _txtAddress.Text.Trim(),
                    Country = _txtCountry.Text.Trim(),
                    City = _txtCity.Text.Trim(),
                    PaymentTermsDays = terms,
                    CreditLimit = creditLimit,
                    TaxNumber = _txtTaxNumber.Text.Trim(),
                    PayablesAccountId = payablesId.Value,
                    Notes = _txtNotes.Text.Trim()
                });
                if (ApplicationResultPresenter.Present(result))
                {
                    SupplierListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.Suppliers, "List");
                }
            }
            else
            {
                var result = await SupplierUiService.Instance.CreateAsync(new CreateSupplierCommand
                {
                    Code = _txtCode.Text.Trim(),
                    NameAr = _txtNameAr.Text.Trim(),
                    NameEn = _txtNameEn.Text.Trim(),
                    Phone = _txtPhone.Text.Trim(),
                    Email = _txtEmail.Text.Trim(),
                    Address = _txtAddress.Text.Trim(),
                    Country = _txtCountry.Text.Trim(),
                    City = _txtCity.Text.Trim(),
                    PaymentTermsDays = terms,
                    CreditLimit = creditLimit,
                    TaxNumber = _txtTaxNumber.Text.Trim(),
                    PayablesAccountId = payablesId,
                    Notes = _txtNotes.Text.Trim()
                });
                if (ApplicationResultPresenter.Present(result))
                {
                    SupplierListRefreshHub.RequestRefresh();
                    MockInteractionService.Navigate(AppModule.Suppliers, "List");
                }
            }
        }
        finally
        {
            _isSaving = false;
            _btnSave.IsEnabled = true;
        }
    }

    private static int TermsDays(int index) => index switch
    {
        0 => 0,
        1 => 15,
        2 => 30,
        3 => 60,
        4 => 90,
        _ => 30
    };

    private static int TermsIndex(int days) => days switch
    {
        0 => 0,
        15 => 1,
        30 => 2,
        60 => 3,
        90 => 4,
        _ => 2
    };

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
