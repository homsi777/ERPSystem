using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.HR;
using ERPSystem.Helpers;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Views.Hr
{
    public static class HrViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Departments" => Placeholder("الأقسام", new[] { new { القسم = "المبيعات", الموظفون = 4 }, new { القسم = "المستودعات", الموظفون = 3 } }),
            "Attendance" => Placeholder("الحضور والانصراف", new[] { new { الموظف = "أحمد الحمصي", التاريخ = "2026/06/26", الدخول = "08:00", الخروج = "17:00" } }),
            "Leaves" => Placeholder("الإجازات", new[] { new { الموظف = "سارة القحطاني", النوع = "سنوية", من = "2026/07/01", إلى = "2026/07/05" } }),
            "Shifts" => Placeholder("الورديات", new[] { new { الوردية = "صباحي", من = "08:00", إلى = "16:00" } }),
            "Contracts" => Placeholder("العقود", new[] { new { الموظف = "محمد العتيبي", النوع = "دوام كامل", البداية = "2024/01/01" } }),
            "Payroll" => Placeholder("الرواتب", new[] { new { الشهر = "يونيو 2026", الإجمالي = "68,000 ر.س", الحالة = "مسودة" } }),
            "Advances" => Placeholder("السلف والخصومات", new[] { new { الموظف = "فهد الغامدي", النوع = "سلفة", المبلغ = "2,000 ر.س" } }),
            "Reports" => Placeholder("تقارير HR", new[] { new { التقرير = "ملخص الحضور", الفترة = "يونيو 2026" } }),
            _ => EmployeeList()
        };

        private static UserControl EmployeeList()
        {
            var data = HRSampleData.Generate(20);
            var page = new ErpListModuleControl();
            page.Configure(EntityType.Employee, AppModule.HR);
            page.SetHeader("الموظفون", "الموارد البشرية — اختياري", "\uE716", B("AccentCustomersBrush"));
            page.SetPrimaryButton("إضافة موظف");
            page.SetEmptyState("لا يوجد موظفون مسجلون", "إضافة موظف", "\uE716");
            page.PrimaryActionRequested += (_, _) => Services.MockInteractionService.OpenMockForm("إضافة موظف");
            var g = page.Grid;
            g.AutoGenerateColumns = false;
            foreach (var (h, p, w) in new (string, string, object)[] {
                ("رقم الموظف","EmployeeCode",100),("الاسم","FullName","*"),("القسم","Department",110),
                ("المسمى","JobTitle",120),("الهاتف","Phone",120),("تاريخ التعيين","HireDate",110),
                ("الحالة","StatusDisplay",80)
            }) AddCol(g, h, p, w, p == "HireDate" ? "yyyy/MM/dd" : null);
            page.BindData(data.Cast<object>().ToList());
            return page;
        }

        private static UserControl Placeholder(string title, IEnumerable data)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(new TextBlock { Text = "وحدة HR اختيارية — بيانات تجريبية", Foreground = Br("TextMutedBrush"), Margin = new Thickness(0, 0, 0, 12) });
            stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildGrid(data)));
            root.Content = stack;
            return new UserControl { Content = root };
        }

        private static void AddCol(DataGrid g, string h, string p, object w, string? fmt)
            => ErpUiFactory.AddGridColumn(g, h, p, w, fmt);

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
        private static Brush Br(string k) => (Brush)System.Windows.Application.Current.Resources[k]!;
        private static UserControl Wrap(UIElement c) => new() { Content = c };
    }
}
