using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Accounting;

/// <summary>نموذج حساب — كود، اسم، نوع، حساب أب، قابل للترحيل.</summary>
public sealed class AccountFormControl : UserControl
{
    private sealed record AccountTypeOption(GlAccountType Type, string Label);
    private sealed record ParentOption(Guid? Id, string Display);

    private readonly TextBlock _title = ErpUiFactory.SectionTitle("حساب جديد");
    private readonly TextBlock _hint = new()
    {
        Text = "أنشئ حساباً في دليل الحسابات. الحسابات التجميعية لا تقبل قيوداً مباشرة.",
        Foreground = (System.Windows.Media.Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]!,
        Margin = new Thickness(0, 0, 0, 12),
        TextWrapping = TextWrapping.Wrap
    };
    private readonly TextBox _code = ErpUiFactory.FormField("");
    private readonly TextBox _nameAr = ErpUiFactory.FormField("");
    private readonly TextBox _nameEn = ErpUiFactory.FormField("");
    private readonly ComboBox _accountType = new() { MinWidth = 220, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly ComboBox _parent = new() { MinWidth = 280, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly CheckBox _isPostable = new()
    {
        Content = "قابل للترحيل (يقبل قيود يومية)",
        IsChecked = true,
        Margin = new Thickness(0, 8, 0, 0)
    };
    private readonly Button _save = new() { Content = "حفظ الحساب", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private Guid? _editId;
    private bool _saving;
    private bool _popupMode;

    public AccountFormControl()
    {
        var typeOptions = Enum.GetValues(typeof(GlAccountType))
            .Cast<GlAccountType>()
            .Select(t => new AccountTypeOption(t, t.ToDisplay()))
            .ToList();
        _accountType.ItemsSource = typeOptions;
        _accountType.DisplayMemberPath = nameof(AccountTypeOption.Label);
        _accountType.SelectedIndex = 0;

        var stack = new StackPanel();
        stack.Children.Add(_title);
        stack.Children.Add(_hint);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("كود الحساب *", _code),
            ("الاسم (عربي) *", _nameAr),
            ("الاسم (إنجليزي)", _nameEn),
            ("نوع الحساب *", _accountType),
            ("الحساب الأب", _parent),
            ("", _isPostable))));

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack, MaxWidth = 620 };
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) AccountingPopupService.CancelActive();
            else MockInteractionService.Navigate(AppModule.Accounting, "Chart");
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
        _editId = AccountingNavigationContext.EditAccountId;
        var preselectedParent = AccountingNavigationContext.PreselectedParentId;

        var accounts = await AccountingUiService.Instance.GetAccountsAsync();
        if (!ApplicationResultPresenter.Present(accounts))
            return;

        var parentOptions = new List<ParentOption> { new(null, "— بدون حساب أب —") };
        foreach (var a in accounts.Value ?? [])
        {
            if (_editId is Guid edit && a.Id == edit)
                continue;
            parentOptions.Add(new(a.Id, $"{a.Code} — {a.NameAr}"));
        }

        _parent.ItemsSource = parentOptions;
        _parent.DisplayMemberPath = nameof(ParentOption.Display);
        _parent.SelectedIndex = 0;

        if (preselectedParent is Guid parentId)
        {
            _parent.SelectedItem = parentOptions.FirstOrDefault(p => p.Id == parentId);
            AccountingNavigationContext.PreselectedParentId = null;
        }

        if (_editId is Guid id)
        {
            _title.Text = "تعديل حساب";
            var result = await AccountingUiService.Instance.GetAccountDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

            var d = result.Value;
            _code.Text = d.Code;
            _nameAr.Text = d.NameAr;
            _nameEn.Text = d.NameEn;
            _isPostable.IsChecked = d.IsPostable;
            SelectAccountType(d.AccountType);
            if (d.ParentId is Guid pid)
                _parent.SelectedItem = parentOptions.FirstOrDefault(p => p.Id == pid);
        }
        else if (!await AccountingUiService.Instance.CanCreateAccountAsync())
        {
            _save.IsEnabled = false;
            MockInteractionService.ShowWarning("لا تملك صلاحية إنشاء حسابات.", "صلاحية");
        }
    }

    private void SelectAccountType(GlAccountType type)
    {
        if (_accountType.ItemsSource is IEnumerable<AccountTypeOption> options)
        {
            var match = options.FirstOrDefault(o => o.Type == type);
            if (match is not null)
                _accountType.SelectedItem = match;
        }
    }

    private GlAccountType GetSelectedAccountType() =>
        (_accountType.SelectedItem as AccountTypeOption)?.Type ?? GlAccountType.Asset;

    private async Task SaveAsync()
    {
        if (_saving) return;

        if (string.IsNullOrWhiteSpace(_code.Text))
        {
            MessageBox.Show("كود الحساب مطلوب.", "دليل الحسابات", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameAr.Text))
        {
            MessageBox.Show("الاسم العربي مطلوب.", "دليل الحسابات", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var parentId = (_parent.SelectedItem as ParentOption)?.Id;
        var isPostable = _isPostable.IsChecked == true;

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            if (_editId is Guid id)
            {
                var result = await AccountingUiService.Instance.UpdateAccountAsync(new UpdateAccountCommand
                {
                    AccountId = id,
                    Code = _code.Text.Trim(),
                    NameAr = _nameAr.Text.Trim(),
                    NameEn = NullIfEmpty(_nameEn.Text) ?? "",
                    AccountType = GetSelectedAccountType(),
                    ParentId = parentId,
                    IsPostable = isPostable
                });

                if (ApplicationResultPresenter.Present(result))
                {
                    MockInteractionService.ShowSuccess("تم حفظ الحساب.");
                    if (_popupMode) AccountingPopupService.CompleteSuccess();
                    else
                    {
                        AccountingListRefreshHub.RequestRefresh();
                        MockInteractionService.Navigate(AppModule.Accounting, "Chart");
                    }
                }
            }
            else
            {
                var result = await AccountingUiService.Instance.CreateAccountAsync(new CreateAccountCommand
                {
                    Code = _code.Text.Trim(),
                    NameAr = _nameAr.Text.Trim(),
                    NameEn = NullIfEmpty(_nameEn.Text) ?? "",
                    AccountType = GetSelectedAccountType(),
                    ParentId = parentId,
                    IsPostable = isPostable
                });

                if (ApplicationResultPresenter.Present(result))
                {
                    MockInteractionService.ShowSuccess("تم إنشاء الحساب.");
                    if (_popupMode) AccountingPopupService.CompleteSuccess();
                    else
                    {
                        AccountingListRefreshHub.RequestRefresh();
                        MockInteractionService.Navigate(AppModule.Accounting, "Chart");
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

    private static string? NullIfEmpty(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
}
