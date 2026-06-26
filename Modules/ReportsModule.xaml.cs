using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class ReportsModule : ISubpageNavigator
    {
        public ReportsModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Reports;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
