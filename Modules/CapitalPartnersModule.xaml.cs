using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules;

public partial class CapitalPartnersModule : ISubpageNavigator
{
    public CapitalPartnersModule()
    {
        InitializeComponent();
        Shell.Module = AppModule.CapitalPartners;
    }

    public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
}
