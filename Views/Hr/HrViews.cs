using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Views.Hr
{
    public static class HrViews
    {
        public static UserControl Create(string key) => key switch
        {
            "Departments" => DevelopmentTab("الأقسام"),
            "Attendance" => DevelopmentTab("الحضور والانصراف"),
            "Leaves" => DevelopmentTab("الإجازات"),
            "Shifts" => DevelopmentTab("الورديات"),
            "Contracts" => DevelopmentTab("العقود"),
            "Payroll" => DevelopmentTab("الرواتب"),
            "Advances" => DevelopmentTab("السلف والخصومات"),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.HR),
            _ => EmployeeList()
        };

        private static UserControl EmployeeList()
        {
            var page = new ErpListModuleControl();
            page.Configure(EntityType.Employee, AppModule.HR);
            page.SetHeader("الموظفون", "الموارد البشرية", "\uE716", B("AccentCustomersBrush"));
            page.SetPrimaryButton("إضافة موظف");
            page.SetEmptyState("لا يوجد موظفون مضافون بعد", "إضافة موظف", "\uE716");
            page.PrimaryActionRequested += (_, _) => MockInteractionService.ShowComingSoon("إضافة موظف");
            page.BindData([]);
            return page;
        }

        private static UserControl DevelopmentTab(string title)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(PlaceholderUi.DatabasePhase(title));
            root.Content = stack;
            return new UserControl { Content = root };
        }

        private static SolidColorBrush B(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
    }
}
