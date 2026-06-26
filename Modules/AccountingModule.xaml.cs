using ERPSystem.Core;
using ERPSystem.Core.Navigation;

namespace ERPSystem.Modules
{
    public partial class AccountingModule : ISubpageNavigator
    {
        public AccountingModule()
        {
            InitializeComponent();
            Shell.Module = AppModule.Accounting;
        }

        public void NavigateSubpage(string? subPage) => Shell.NavigateSubpage(subPage);
    }
}
