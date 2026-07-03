using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules;

public partial class ExpensesModule : ISubpageNavigator
{
    public ExpensesModule()
    {
        InitializeComponent();
        Shell.Module = AppModule.Expenses;
    }

    public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
}
