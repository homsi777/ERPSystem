using ERPSystem.Controls;
using ERPSystem.Core;

namespace ERPSystem.Helpers
{
    public static class ErpListNavigation
    {
        public static void WirePrimaryTo(this ErpListModuleControl page, AppModule module, string subPage) =>
            page.PrimaryActionRequested += (_, _) =>
                Services.MockInteractionService.Navigate(module, subPage);
    }
}
