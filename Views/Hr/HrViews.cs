using ERPSystem.Controls;
using ERPSystem.Controls.Hr;
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
            "Departments" => new UserControl { Content = new DepartmentListPageControl() },
            "Attendance" => DevelopmentTab("الحضور والانصراف"),
            "Leaves" => DevelopmentTab("الإجازات"),
            "Shifts" => DevelopmentTab("الورديات"),
            "Contracts" => DevelopmentTab("العقود"),
            "Payroll" => DevelopmentTab("الرواتب"),
            "Advances" => DevelopmentTab("السلف والخصومات"),
            "Reports" => ModuleReportsViews.CreateHub(AppModule.HR),
            _ => new UserControl { Content = new EmployeeListPageControl() }
        };

        private static UserControl DevelopmentTab(string title)
        {
            var root = new ScrollViewer { Padding = new Thickness(16) };
            var stack = new StackPanel();
            stack.Children.Add(ErpUiFactory.SectionTitle(title));
            stack.Children.Add(PlaceholderUi.DatabasePhase(title));
            root.Content = stack;
            return new UserControl { Content = root };
        }
    }
}
