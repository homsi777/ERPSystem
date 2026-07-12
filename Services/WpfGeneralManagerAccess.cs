using ERPSystem.Application.Common;

namespace ERPSystem.Services;

public static class WpfGeneralManagerAccess
{
    public static bool IsGeneralManager =>
        AppServices.IsInitialized
        && AppServices.GetRequiredService<Application.Abstractions.Services.ICurrentUserService>()
            is WpfCurrentUserService wpf
        && GeneralManagerAccess.IsGeneralManager(wpf.Permissions);

    public static bool CanViewSensitivePricing => IsGeneralManager;
}
