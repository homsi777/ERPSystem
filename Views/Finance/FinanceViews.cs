using ERPSystem.Controls;
using ERPSystem.Controls.Accounting;
using ERPSystem.Controls.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
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
            "Receivables" => DevelopmentPage("الذمم المدينة"),
            "Payables" => DevelopmentPage("الذمم الدائنة"),
            "TrialBalance" => DevelopmentPage("ميزان المراجعة"),
            _ => Wrap(new JournalEntryListPageControl())
        };

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
