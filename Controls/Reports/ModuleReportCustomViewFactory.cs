using ERPSystem.Controls.Accounting;
using ERPSystem.Controls.Capital;
using ERPSystem.Controls.Expenses;
using ERPSystem.Core;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Reports;

public static class ModuleReportCustomViewFactory
{
    public static UserControl? TryCreate(AppModule module, string reportKey) => (module, reportKey.ToLowerInvariant()) switch
    {
        (AppModule.Accounting, "acc.trial_balance") => Wrap(new TrialBalanceReportControl()),
        (AppModule.Accounting, "acc.account_ledger") => Wrap(new AccountLedgerReportControl()),
        (AppModule.Expenses, "exp.detailed") => Wrap(new ExpenseReportsControl()),
        (AppModule.CapitalPartners, "cap.summary") => Wrap(new CapitalReportsControl()),
        (AppModule.CapitalPartners, "cap.statement") => Wrap(new CapitalReportsControl()),
        _ => null
    };

    private static UserControl Wrap(UIElement content)
    {
        if (content is FrameworkElement fe)
            fe.HorizontalAlignment = HorizontalAlignment.Stretch;

        return new UserControl
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
    }
}
