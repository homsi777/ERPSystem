using ERPSystem.Controls;
using ERPSystem.Controls.Expenses;
using ERPSystem.Core;
using ERPSystem.Views.Reports;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Views.Expenses;

public static class ExpenseViews
{
    public static UserControl Create(string key) => key switch
    {
        "Dashboard" => Wrap(new ExpenseDashboardControl()),
        "Entries" => Wrap(new ExpenseEntryListPageControl()),
        "Entry" => Wrap(new ExpenseEntryFormControl()),
        "List" => Wrap(new ExpenseListPageControl()),
        "Form" => Wrap(new ExpenseFormControl()),
        "Workspace" => CreateWorkspace(),
        "Reports" => ModuleReportsViews.CreateHub(AppModule.Expenses),
        "Categories" => Wrap(new ExpenseCategoryAdminControl()),
        _ => Wrap(new ExpenseListPageControl())
    };

    private static UserControl CreateWorkspace()
    {
        var (id, tab) = Services.Expenses.ExpenseNavigationContext.TakeWorkspaceContext();
        var ctrl = new ExpenseOperationsCenterControl();
        if (id is Guid expenseId)
            ctrl.Initialize(expenseId, tab);
        return Wrap(ctrl);
    }

    private static UserControl Wrap(UIElement content) => new() { Content = content };
}
