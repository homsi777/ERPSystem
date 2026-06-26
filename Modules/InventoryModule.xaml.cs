using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class InventoryModule : ISubpageNavigator
    {
        public InventoryModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Inventory;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
