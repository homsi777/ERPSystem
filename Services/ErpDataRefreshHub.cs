using System.Windows;

namespace ERPSystem.Services;

[Flags]
public enum ErpDataRefreshScope
{
    Dashboard = 1,
    Inventory = 2,
    Sales = 4,
    OperationsCenter = 8,
    Reports = 16,
    All = Dashboard | Inventory | Sales | OperationsCenter | Reports
}

public static class ErpDataRefreshHub
{
    public static event Action<ErpDataRefreshScope>? DataChanged;

    public static void RequestRefresh(ErpDataRefreshScope scope = ErpDataRefreshScope.All)
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
            DataChanged?.Invoke(scope);
        else
            dispatcher.Invoke(() => DataChanged?.Invoke(scope));
    }
}
