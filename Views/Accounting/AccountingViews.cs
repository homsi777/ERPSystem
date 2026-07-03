using ERPSystem.Controls.Accounting;
using ERPSystem.Core;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Views.Accounting;

public static class AccountingViews
{
    public static UserControl Create(string key) => key switch
    {
        "Chart" => Wrap(new ChartOfAccountsListPageControl()),
        "Journal" => Wrap(new JournalEntryListPageControl()),
        "JournalBooks" => Wrap(new JournalBookListPageControl()),
        "TrialBalance" => Wrap(new TrialBalanceReportControl()),
        "AccountLedger" => Wrap(new AccountLedgerReportControl()),
        "Receipts" => Wrap(new ReceiptVoucherPageControl()),
        "Payments" => Wrap(new PaymentVoucherPageControl()),
        "AccountForm" => Wrap(new AccountFormControl()),
        "JournalForm" => Wrap(new JournalEntryFormControl()),
        "Reports" => ModuleReportsViews.CreateHub(AppModule.Accounting),
        _ => Wrap(new ChartOfAccountsListPageControl())
    };

    private static UserControl Wrap(UIElement content)
    {
        if (content is FrameworkElement fe)
        {
            fe.HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        return new UserControl
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }
}
