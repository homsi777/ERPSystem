using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class SalesModule : ISubpageNavigator
    {
        public SalesModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Sales;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
