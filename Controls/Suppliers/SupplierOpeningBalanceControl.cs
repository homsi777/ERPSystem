using ERPSystem.Application.Commands.Finance;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Suppliers;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Suppliers;

public sealed class SupplierOpeningBalanceControl : UserControl
{
    private readonly ComboBox _cmbSupplier = new() { Height = 36, MinWidth = 280 };
    private readonly TextBox _txtAmount = ErpUiFactory.FormField("");
    private readonly DatePicker _dpDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _txtNote = ErpUiFactory.FormField("");
    private readonly Button _btnPost = new() { Content = "ترحيل الرصيد الافتتاحي", Style = S("PrimaryButtonStyle"), Height = 36, Margin = new Thickness(0, 12, 0, 0) };
    private readonly StackPanel _resultHost = new();

    public SupplierOpeningBalanceControl()
    {
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("أرصدة افتتاحية — الموردون"));
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "يُنشئ مستنداً في محرك الأرصدة الافتتاحية الموحّد ويرحّله محاسبياً — قيد واحد لكل مورد، مع سجل تدقيق كامل.",
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
            ("المورد", _cmbSupplier),
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

        _btnPost.IsEnabled = await OpeningBalanceUiService.Instance.CanAsync("suppliers.opening-balance");
        var result = await SupplierUiService.Instance.GetListAsync(null, pageSize: 200);
        if (!result.IsSuccess || result.Value is null)
            return;

        _cmbSupplier.Items.Clear();
        foreach (var s in result.Value.Items.Where(x => !x.OpeningBalancePosted))
        {
            _cmbSupplier.Items.Add(new ComboBoxItem
            {
                Content = $"{s.Code} — {s.NameAr}",
                Tag = s.Id
            });
        }
    }

    private async Task PostAsync()
    {
        if (_cmbSupplier.SelectedItem is not ComboBoxItem item || item.Tag is not Guid supplierId)
        {
            MockInteractionService.ShowWarning("اختر مورداً.", "تحقق");
            return;
        }

        if (!decimal.TryParse(_txtAmount.Text, out var amount) || amount <= 0)
        {
            MockInteractionService.ShowWarning("أدخل مبلغاً صحيحاً.", "تحقق");
            return;
        }

        if (!await OpeningBalanceUiService.Instance.CanAsync("suppliers.opening-balance"))
        {
            MockInteractionService.ShowWarning("ليس لديك صلاحية ترحيل أرصدة الموردين.", "صلاحيات");
            return;
        }

        var partyName = (item.Content as string) ?? "";
        var result = await OpeningBalanceUiService.Instance.PostPartyOpeningBalanceAsync(new PostPartyOpeningBalanceCommand
        {
            Type = OpeningBalanceType.SupplierPayable,
            PartyId = supplierId,
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
        SupplierListRefreshHub.RequestRefresh();
        OpeningBalanceListRefreshHub.RequestRefresh();
        _btnPost.IsEnabled = false;
        _txtAmount.IsEnabled = false;
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
