using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Accounting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.Accounting;

public sealed class AccountLedgerReportControl : UserControl
{
    private sealed class LedgerRow
    {
        public string EntryDateDisplay { get; init; } = "";
        public string EntryNumber { get; init; } = "";
        public string Description { get; init; } = "";
        public string LineNarrative { get; init; } = "";
        public string DebitDisplay { get; init; } = "";
        public string CreditDisplay { get; init; } = "";
        public string BalanceDisplay { get; init; } = "";
    }

    private readonly Guid? _preselectedAccountId;
    private readonly ComboBox _account = new() { MinWidth = 280, IsEditable = false, Style = S("EnterpriseComboBoxStyle") };
    private readonly DatePicker _from = ErpUiFactory.FormDate(DateTime.Today.AddMonths(-1));
    private readonly DatePicker _to = ErpUiFactory.FormDate(DateTime.Today);
    private readonly Button _load = new() { Content = "تحميل", Style = S("PrimaryButtonStyle"), Height = 32, MinWidth = 100 };
    private readonly DataGrid _grid;
    private readonly TextBlock _footer = new()
    {
        FontSize = 13,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["PrimaryBrush"]!
    };

    private IReadOnlyList<AccountListDto> _accounts = [];

    public AccountLedgerReportControl() : this(null) { }

    public AccountLedgerReportControl(Guid? preselectedAccountId)
    {
        _preselectedAccountId = preselectedAccountId;

        _grid = ErpUiFactory.BuildGrid(null, false);
        ErpUiFactory.AddGridColumn(_grid, "التاريخ", nameof(LedgerRow.EntryDateDisplay), 100);
        ErpUiFactory.AddGridColumn(_grid, "رقم القيد", nameof(LedgerRow.EntryNumber), 110);
        ErpUiFactory.AddGridColumn(_grid, "البيان", nameof(LedgerRow.Description), "*");
        ErpUiFactory.AddGridColumn(_grid, "سطر القيد", nameof(LedgerRow.LineNarrative), 140);
        ErpUiFactory.AddGridColumn(_grid, "مدين", nameof(LedgerRow.DebitDisplay), 100);
        ErpUiFactory.AddGridColumn(_grid, "دائن", nameof(LedgerRow.CreditDisplay), 100);
        ErpUiFactory.AddGridColumn(_grid, "الرصيد", nameof(LedgerRow.BalanceDisplay), 110);

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("كشف حساب"));
        stack.Children.Add(ErpUxFactory.InfoBanner("حركات الحساب ضمن فترة زمنية مع رصيد تراكمي.", "info"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFilterRow(
            ("الحساب", _account),
            ("من", _from),
            ("إلى", _to),
            ("", _load))));
        stack.Children.Add(ErpUiFactory.Card(_grid));
        stack.Children.Add(_footer);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack };
        _load.Click += async (_, _) => await LoadAsync();
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var result = await AccountingUiService.Instance.GetAccountsAsync();
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        _accounts = result.Value;
        _account.ItemsSource = _accounts;
        _account.DisplayMemberPath = nameof(AccountListDto.NameAr);
        _account.SelectedValuePath = nameof(AccountListDto.Id);

        if (_preselectedAccountId.HasValue)
            _account.SelectedValue = _preselectedAccountId.Value;
        else if (_accounts.Count > 0)
            _account.SelectedIndex = 0;

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        if (_account.SelectedValue is not Guid accountId || accountId == Guid.Empty)
        {
            MockInteractionService.ShowWarning("اختر حساباً.", "كشف حساب");
            return;
        }

        var from = _from.SelectedDate ?? DateTime.Today.AddMonths(-1);
        var to = _to.SelectedDate ?? DateTime.Today;
        var result = await AccountingUiService.Instance.GetAccountLedgerAsync(accountId, from, to);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var rows = result.Value.Select(r => new LedgerRow
        {
            EntryDateDisplay = AppFormats.Date(r.EntryDate),
            EntryNumber = r.EntryNumber,
            Description = r.Description,
            LineNarrative = string.IsNullOrWhiteSpace(r.LineNarrative) ? "—" : r.LineNarrative,
            DebitDisplay = AppFormats.AmountOrDash(r.Debit),
            CreditDisplay = AppFormats.AmountOrDash(r.Credit),
            BalanceDisplay = AppFormats.Amount(r.RunningBalance)
        }).ToList();

        _grid.ItemsSource = rows;

        var totalDebit = result.Value.Sum(r => r.Debit);
        var totalCredit = result.Value.Sum(r => r.Credit);
        var closing = rows.Count > 0 ? result.Value[^1].RunningBalance : 0m;
        _footer.Text = $"إجمالي مدين {AppFormats.Amount(totalDebit)} | إجمالي دائن {AppFormats.Amount(totalCredit)} | الرصيد الختامي {AppFormats.Amount(closing)}";
    }

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
