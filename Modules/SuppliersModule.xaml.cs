using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class SuppliersModule : ISubpageNavigator
    {
        public SuppliersModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Suppliers;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
