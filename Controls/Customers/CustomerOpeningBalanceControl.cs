using ERPSystem.Application.Commands.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Customers;

public sealed class CustomerOpeningBalanceControl : UserControl
{
    private readonly ComboBox _cmbCustomer = new() { Height = 36, MinWidth = 280 };
    private readonly TextBox _txtAmount = ErpUiFactory.FormField("");
    private readonly DatePicker _dpDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _txtNote = ErpUiFactory.FormField("");
    private readonly Button _btnPost = new() { Content = "ترحيل الرصيد الافتتاحي", Style = S("PrimaryButtonStyle"), Height = 36, Margin = new Thickness(0, 12, 0, 0) };
    private readonly StackPanel _resultHost = new();

    public CustomerOpeningBalanceControl()
    {
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("أرصدة افتتاحية — العملاء"));
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "يُنشئ مستنداً في محرك الأرصدة الافتتاحية الموحّد ويرحّله محاسبياً — قيد واحد لكل عميل، مع سجل تدقيق كامل.",
            "info"));
        var navBtn = new Button
        {
            Content = "فتح مركز الأرصدة الافتتاحية",
            Style = S("SecondaryButtonStyle"),
            Margin = new Thickness(0, 0, 0, 8),
            HorizontalAlignment = HorizontalAlignment.Left
        };
        navBtn.Click += (_, _) => MockInteractionService.Navigate(AppModule.Accounting, "OpeningBalances");
        stack.Children.Add(navBtn);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("العميل", _cmbCustomer),
            ("المبلغ", _txtAmount),
            ("التاريخ", _dpDate),
            ("ملاحظة", _txtNote))));
        stack.Children.Add(_btnPost);
        stack.Children.Add(_resultHost);
        Content = new ScrollViewer { Padding = new Thickness(16), Content = stack };
        Loaded += OnLoaded;
        _btnPost.Click += async (_, _) => await PostAsync();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (!AppServices.IsInitialized)
            return;

        _btnPost.IsEnabled = await OpeningBalanceUiService.Instance.CanAsync("customers.opening-balance");
        var result = await CustomerUiService.Instance.GetListAsync(null, pageSize: 200);
        if (!result.IsSuccess || result.Value is null)
            return;

        _cmbCustomer.Items.Clear();
        foreach (var c in result.Value.Items.Where(x => !x.OpeningBalancePosted))
        {
            _cmbCustomer.Items.Add(new ComboBoxItem
            {
                Content = $"{c.Code} — {c.NameAr}",
                Tag = c.Id
            });
        }
    }

    private async Task PostAsync()
    {
        if (_cmbCustomer.SelectedItem is not ComboBoxItem item || item.Tag is not Guid customerId)
        {
            MockInteractionService.ShowWarning("اختر عميلاً.", "تحقق");
            return;
        }

        if (!decimal.TryParse(_txtAmount.Text, out var amount) || amount <= 0)
        {
            MockInteractionService.ShowWarning("أدخل مبلغاً صحيحاً.", "تحقق");
            return;
        }

        if (!await OpeningBalanceUiService.Instance.CanAsync("customers.opening-balance"))
        {
            MockInteractionService.ShowWarning("ليس لديك صلاحية ترحيل أرصدة العملاء.", "صلاحيات");
            return;
        }

        var partyName = (item.Content as string) ?? "";
        var result = await OpeningBalanceUiService.Instance.PostPartyOpeningBalanceAsync(new PostPartyOpeningBalanceCommand
        {
            Type = OpeningBalanceType.CustomerReceivable,
            PartyId = customerId,
            PartyName = partyName,
            Amount = amount,
            OpeningDate = _dpDate.SelectedDate ?? DateTime.Today,
            ReferenceNote = _txtNote.Text.Trim()
        });

        if (!ApplicationResultPresenter.Present(result))
            return;

        _resultHost.Children.Clear();
        _resultHost.Children.Add(ErpUxFactory.InfoBanner(
            $"تم الترحيل عبر المحرك الموحّد — {amount:N2} $ — قيد {result.Value?.JournalEntryNumber}",
            "success"));
        CustomerListRefreshHub.RequestRefresh();
        OpeningBalanceListRefreshHub.RequestRefresh();
        _btnPost.IsEnabled = false;
        _txtAmount.IsEnabled = false;
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
