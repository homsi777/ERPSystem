using ERPSystem.Services;

namespace ERPSystem.Diagnostics.Performance;

/// <summary>
/// Thin helper for page controls — same pattern as Sales.InvoiceList, shorter call sites.
/// </summary>
public static class ScreenLoadProfiler
{
    public static WpfPerformanceScope? Begin(string screenName)
    {
        if (!AppServices.IsInitialized)
            return null;

        return AppServices.GetRequiredService<IWpfPerformanceProfiler>().BeginScreenLoad(screenName);
    }

    public static async Task<T> MeasureLoadAsync<T>(WpfPerformanceScope? scope, Func<Task<T>> load)
    {
        using (scope?.MeasureDataLoad())
            return await load();
    }
}
