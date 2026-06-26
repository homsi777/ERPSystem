using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class HRModule : ISubpageNavigator
    {
        public HRModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.HR;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
