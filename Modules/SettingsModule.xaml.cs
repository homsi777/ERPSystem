using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class SettingsModule : ISubpageNavigator
    {
        public SettingsModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Settings;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
