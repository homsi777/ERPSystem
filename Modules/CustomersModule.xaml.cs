using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class CustomersModule : ISubpageNavigator
    {
        public CustomersModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Customers;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
