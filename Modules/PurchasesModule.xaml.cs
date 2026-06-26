using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class PurchasesModule : ISubpageNavigator
    {
        public PurchasesModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Purchases;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
