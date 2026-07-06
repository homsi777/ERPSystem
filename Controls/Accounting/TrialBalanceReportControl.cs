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

public sealed class TrialBalanceReportControl : UserControl
{
    private sealed class TrialBalanceRow
    {
        public string AccountCode { get; init; } = "";
        public string AccountName { get; init; } = "";
        public string AccountTypeDisplay { get; init; } = "";
        public decimal DebitTotal { get; init; }
        public decimal CreditTotal { get; init; }
        public decimal Balance { get; init; }
        public string DebitDisplay => AppFormats.AmountOrDash(DebitTotal);
        public string CreditDisplay => AppFormats.AmountOrDash(CreditTotal);
        public string BalanceDisplay => AppFormats.Amount(Balance);
    }

    private readonly DatePicker _asOf = ErpUiFactory.FormDate(DateTime.Today);
    private readonly Button _load = new() { Content = "تحميل", Style = S("PrimaryButtonStyle"), Height = 32, MinWidth = 100 };
    private readonly DataGrid _grid;
    private readonly TextBlock _footer = new()
    {
        FontSize = 13,
        FontWeight = FontWeights.SemiBold,
        Margin = new Thickness(0, 8, 0, 0),
        Foreground = (Brush)WpfApplication.Current.Resources["PrimaryBrush"]!
    };

    public TrialBalanceReportControl()
    {
        _grid = ErpUiFactory.BuildGrid(null, false);
        ErpUiFactory.AddGridColumn(_grid, "الكود", nameof(TrialBalanceRow.AccountCode), 90);
        ErpUiFactory.AddGridColumn(_grid, "الحساب", nameof(TrialBalanceRow.AccountName), "*");
        ErpUiFactory.AddGridColumn(_grid, "النوع", nameof(TrialBalanceRow.AccountTypeDisplay), 100);
        ErpUiFactory.AddGridColumn(_grid, "مدين", nameof(TrialBalanceRow.DebitDisplay), 110);
        ErpUiFactory.AddGridColumn(_grid, "دائن", nameof(TrialBalanceRow.CreditDisplay), 110);
        ErpUiFactory.AddGridColumn(_grid, "الرصيد", nameof(TrialBalanceRow.BalanceDisplay), 110);

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("ميزان المراجعة"));
        stack.Children.Add(ErpUxFactory.InfoBanner("عرض أرصدة الحسابات حتى تاريخ محدد.", "info"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFilterRow(
            ("حتى تاريخ", _asOf),
            ("", _load))));
        stack.Children.Add(ErpUiFactory.Card(_grid));
        stack.Children.Add(_footer);

        Content = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = stack };
        _load.Click += async (_, _) => await LoadAsync();
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var asOf = _asOf.SelectedDate ?? DateTime.Today;
        var result = await AccountingUiService.Instance.GetTrialBalanceAsync(asOf);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        var rows = result.Value
            .Select(r => new TrialBalanceRow
            {
                AccountCode = r.AccountCode,
                AccountName = r.AccountName,
                AccountTypeDisplay = r.AccountTypeDisplay,
                DebitTotal = r.DebitTotal,
                CreditTotal = r.CreditTotal,
                Balance = r.Balance
            })
            .ToList();

        _grid.ItemsSource = rows;

        var totalDebit = rows.Sum(r => r.DebitTotal);
        var totalCredit = rows.Sum(r => r.CreditTotal);
        var diff = Math.Abs(totalDebit - totalCredit);
        var balanced = diff < 0.01m;

        _footer.Text = balanced
            ? $"الإجمالي: مدين {totalDebit:N2} = دائن {totalCredit:N2} ✓"
            : $"الإجمالي: مدين {totalDebit:N2} | دائن {totalCredit:N2} | الفرق {diff:N2}";
        _footer.Foreground = (Brush)WpfApplication.Current.Resources[balanced ? "SuccessBrush" : "WarningBrush"]!;
    }

    private static Style S(string k) => (Style)WpfApplication.Current.Resources[k]!;
}
