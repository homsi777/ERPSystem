using ERPSystem.Controls;
using ERPSystem.Controls.Accounting;
using ERPSystem.Controls.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Views.Finance
{
    public static class FinanceViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Chart" => DevelopmentPage("دليل الحسابات"),
            "Receipts" => Wrap(new ReceiptVoucherPageControl()),
            "Payments" => Wrap(new PaymentVoucherPageControl()),
            "Cashboxes" => new CashboxListPageControl(),
            "Transfers" => new CashboxTransferListPageControl(),
            "Receivables" => Wrap(new ReceivablesAgingControl()),
            "Payables" => Wrap(new PayablesAgingControl()),
            "TrialBalance" => DevelopmentPage("ميزان المراجعة"),
            "OpeningBalances" => Wrap(new OpeningBalanceListPageControl()),
            "OpeningBalanceDashboard" => Wrap(new OpeningBalanceDashboardControl()),
            "OpeningBalanceWorkspace" => CreateOpeningBalanceWorkspace(),
            _ => Wrap(new JournalEntryListPageControl())
        };

        private static UserControl CreateOpeningBalanceWorkspace()
        {
            var (id, tab) = OpeningBalanceNavigationContext.TakeWorkspaceContext();
            var ctrl = new OpeningBalanceOperationsCenterControl();
            if (id is Guid documentId)
                ctrl.Initialize(documentId, tab);
            return Wrap(ctrl);
        }

        private static UserControl DevelopmentPage(string title)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(PlaceholderUi.DevelopmentPhase(title));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl Wrap(UIElement content) => new() { Content = content };
    }
}
