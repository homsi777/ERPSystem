using ERPSystem.Core;
using ERPSystem.Core.Customers;
using ERPSystem.Services;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ERPSystem.Controls.Customers
{
    public class StatementLineRow
    {
        public string GoodsSummary { get; set; } = "";
        public int Quantity { get; set; }
        public string Lengths { get; set; } = "";
        public decimal AvgPrice { get; set; }
        public decimal InvoiceTotal { get; set; }
        public string InvoiceNumber { get; set; } = "";
        public string Date { get; set; } = "";
        public string Notes { get; set; } = "";
        public bool IsReturn { get; set; }
    }

    public class StatementDivider { }

    public partial class CustomerAccountStatementControl : UserControl
    {
        private readonly List<StatementLineRow> _allLines = new();
        private CustomerModel _customer = CustomerSampleData.Generate(1).First();

        public CustomerAccountStatementControl()
        {
            InitializeComponent();
            Loaded += (_, _) =>
            {
                SeedSampleData();
                CmbCustomer.SelectionChanged += (_, _) => OnCustomerChanged();
                DpFrom.SelectedDateChanged += (_, _) => ApplyFilters();
                DpTo.SelectedDateChanged += (_, _) => ApplyFilters();
                DpClaimDate.SelectedDateChanged += (_, _) => ApplyFilters();
                ApplyFilters();
            };
        }

        public void SetCustomerName(string name)
        {
            TxtTableTitle.Text = $"كشف حساب — {name}";
        }

        private void SeedSampleData()
        {
            _allLines.Clear();
            _allLines.AddRange(new[]
            {
                new StatementLineRow { GoodsSummary = "كتان F12 — رمادي", Quantity = 10, Lengths = "2,400 يارد", AvgPrice = 12.5m, InvoiceTotal = 30000, InvoiceNumber = "SINV-1026", Date = "2026-06-01", Notes = "تسليم جزئي" },
                new StatementLineRow { GoodsSummary = "كتان F12 — أبيض", Quantity = 8, Lengths = "1,920 يارد", AvgPrice = 12.0m, InvoiceTotal = 23040, InvoiceNumber = "SINV-1027", Date = "2026-06-05" },
                new StatementLineRow { GoodsSummary = "شيفون S8 — بيج", Quantity = 6, Lengths = "1,440 يارد", AvgPrice = 11.5m, InvoiceTotal = 16560, InvoiceNumber = "SINV-1028", Date = "2026-06-10", Notes = "خصم 2%" },
                new StatementLineRow { GoodsSummary = "سند مرتجع — كتان F12", Quantity = 2, Lengths = "480 يارد", AvgPrice = 12.5m, InvoiceTotal = -6000, InvoiceNumber = "SRV-1023", Date = "2026-06-12", Notes = "مرتجع جزئي", IsReturn = true },
                new StatementLineRow { GoodsSummary = "كتان F12 — أزرق", Quantity = 12, Lengths = "2,880 يارد", AvgPrice = 12.8m, InvoiceTotal = 36864, InvoiceNumber = "SINV-1029", Date = "2026-06-14" },
                new StatementLineRow { GoodsSummary = "كتان F12 — أسود", Quantity = 5, Lengths = "1,200 يارد", AvgPrice = 13.0m, InvoiceTotal = 15600, InvoiceNumber = "SINV-1030", Date = "2026-06-16", Notes = "آجل — دفعة $1,200" },
            });
        }

        private void OnCustomerChanged()
        {
            if (CmbCustomer.SelectedItem is ComboBoxItem item)
            {
                var text = item.Content?.ToString() ?? "محل الأناقة";
                var name = text.Split('—')[0].Trim();
                SetCustomerName(name);
            }
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var from = DpFrom.SelectedDate ?? DateTime.MinValue;
            var to = DpTo.SelectedDate ?? DateTime.MaxValue;

            var filtered = _allLines.Where(l =>
                DateTime.TryParse(l.Date, out var d) && d.Date >= from.Date && d.Date <= to.Date).ToList();

            var items = new ArrayList();
            StatementLineRow? prev = null;
            foreach (var row in filtered)
            {
                if (prev != null && row.IsReturn && !prev.IsReturn)
                    items.Add(new StatementDivider());
                items.Add(row);
                prev = row;
            }

            LinesList.ItemsSource = items;
        }

        private void BtnReceipt_Click(object sender, RoutedEventArgs e)
        {
            MockInteractionService.Navigate(AppModule.Accounting, "Receipts");
            MockInteractionService.ShowSuccess("تم فتح سند قبض تجريبي.", "سند قبض");
        }

        private void BtnReturn_Click(object sender, RoutedEventArgs e)
        {
            MockInteractionService.Navigate(AppModule.Sales, "NewReturn");
            MockInteractionService.ShowSuccess("تم فتح سند مرتجع تجريبي.", "سند مرتجع");
        }

        private void BtnServices_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowComingSoon("خدمات إضافية على كشف الحساب");

        private void BtnPrint_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview(TxtTableTitle.Text, "طباعة");

        private void BtnPdf_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview(TxtTableTitle.Text, "PDF");

        private void BtnExcel_Click(object sender, RoutedEventArgs e) =>
            MockInteractionService.ShowDocumentPreview(TxtTableTitle.Text, "Excel");

        private void BtnReconcile_Click(object sender, RoutedEventArgs e)
        {
            if (MockInteractionService.Confirm("مطابقة كشف الحساب مع سجلات العميل؟", "مطابقة الكشف"))
                MockInteractionService.ShowSuccess("تمت المطابقة بنجاح (تجريبي).", "مطابقة الكشف");
        }

        private void InvoiceNumber_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.Tag is string num && !num.StartsWith("SRV"))
                MockInteractionService.OpenInvoiceOperationsCenter(num);
        }
    }
}
