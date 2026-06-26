using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class ChinaImportModule : ISubpageNavigator
    {
        public ChinaImportModule()
        {
            InitializeComponent();
            Shell.Module = Core.AppModule.ChinaImport;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
