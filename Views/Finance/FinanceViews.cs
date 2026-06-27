using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Accounting;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Domain;
using ERPSystem.Helpers;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.Finance
{
    public static class FinanceViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Chart" => SimpleList("دليل الحسابات", new[] { new { الكود = "1010", الاسم = "الصندوق", النوع = "أصول", الحالة = "نشط" } }),
            "Receipts" => VoucherPage("سند قبض", true),
            "Payments" => VoucherPage("سند صرف", false),
            "Cashboxes" => BuildCashboxList(),
            "Transfers" => SimpleList("تحويل بين الصناديق", new CashboxTransfer[] { new() { Number = "CT-001", FromCashbox = "رئيسي", ToCashbox = "جدة", Amount = 10000, Date = DateTime.Today, Status = "مرحّل" } }),
            "Receivables" => SimpleList("الذمم المدينة", new[] { new { العميل = "أحمد الحمصي", الرصيد = "15,700 ر.س" } }),
            "Payables" => SimpleList("الذمم الدائنة", new[] { new { المورد = "مورد شنتشن", الرصيد = "95,000 ر.س" } }),
            "TrialBalance" => SimpleList("ميزان المراجعة", new[] { new { الحساب = "الصندوق", مدين = "125,000", دائن = "0" } }),
            _ => JournalList()
        };

        private static UserControl JournalList()
        {
            var data = AccountingSampleData.Generate(25);
            var page = new ErpListModuleControl();
            page.Configure(EntityType.JournalEntry, AppModule.Accounting);
            page.SetHeader("القيود اليومية", "الأستاذ العام — قيود اليومية والسندات", "\uE8C1", B("PrimaryBrush"));
            page.SetPrimaryButton("قيد يومية جديد");
            page.SetEmptyState("لا توجد قيود يومية", "قيد يومية جديد", "\uE8C1");
            page.PrimaryActionRequested += (_, _) => Services.MockInteractionService.OpenMockForm("قيد يومية جديد");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("رقم القيد","EntryNumber",120),("التاريخ","EntryDate",100),("الوصف","Description","*"),
                ("مدين","DebitTotal",100),("دائن","CreditTotal",100),("بواسطة","CreatedBy",100),("الحالة","StatusDisplay",80)
            }) AddCol(g, h, p, w, p.Contains("Total") ? "N2" : p == "EntryDate" ? "yyyy/MM/dd" : null);
            page.BindData(data);
            return page;
        }

        private static UserControl VoucherPage(string title, bool receipt)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            foreach (var a in new[] { "جديد", "حفظ مسودة", "اعتماد", "ترحيل", "طباعة", "PDF" })
            {
                var captured = a;
                var btn = new Button { Content = a, Style = S("SecondaryButtonStyle"), Margin = new Thickness(0, 0, 6, 0) };
                btn.Click += (_, _) =>
                {
                    if (captured is "طباعة" or "PDF")
                        Services.MockInteractionService.ShowDocumentPreview(title, captured);
                    else if (captured == "اعتماد" && Services.MockInteractionService.Confirm($"اعتماد {title}؟"))
                        Services.MockInteractionService.ShowSuccess($"تم اعتماد {title}.");
                    else
                        Services.MockInteractionService.ShowSuccess($"تم تنفيذ «{captured}» على {title} (تجريبي).");
                };
                actions.Children.Add(btn);
            }
            stack.Children.Add(actions);
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                ("رقم السند", ErpUiFactory.FormField(receipt ? "RCP-001" : "PAY-001")),
                ("التاريخ", ErpUiFactory.FormDate()),
                (receipt ? "العميل" : "المورد", ErpUiFactory.FormField("أحمد الحمصي")),
                ("المبلغ", ErpUiFactory.FormField("10,000")),
                ("الصندوق", ErpUiFactory.FilterCombo(["الصندوق الرئيسي"])),
                ("البيان", ErpUiFactory.FormField("تحصيل / دفع أقمشة")))));
            root.Content = stack;
            return Wrap(root);
        }

        private static UserControl BuildCashboxList()
        {
            var data = new Cashbox[]
            {
                new() { Code = "CB-01", Name = "الصندوق الرئيسي", Balance = 125000, Currency = "ر.س" },
                new() { Code = "CB-02", Name = "صندوق جدة", Balance = 48200, Currency = "ر.س" },
                new() { Code = "CB-03", Name = "صندوق التحصيل", Balance = 31800, Currency = "ر.س" },
            };
            var page = new ErpListModuleControl();
            page.Configure(EntityType.Cashbox, AppModule.Accounting);
            page.SetHeader("الصناديق", "مراكز عمليات الصناديق — تحصيل وصرف", "\uE8C1", B("AccentReceivableBrush"));
            page.SetPrimaryButton("صندوق جديد");
            page.SetEmptyState("لا توجد صناديق", "صندوق جديد", "\uE8C1");
            page.PrimaryActionRequested += (_, _) => Services.MockInteractionService.OpenMockForm("صندوق جديد");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[]
            {
                ("الكود", "Code", 90), ("الاسم", "Name", "*"), ("الرصيد", "Balance", 120), ("العملة", "Currency", 80)
            }) AddCol(g, h, p, w, p == "Balance" ? "N2" : null);
            page.BindData(data);
            return page;
        }

        private static UserControl SimpleList(string title, IEnumerable data)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(data)));
            root.Content = stack;
            return Wrap(root);
        }

        private static void AddCol(DataGrid g, string h, string p, object w, string? fmt)
            => ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
        private static Style S(string k) => (Style)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}
