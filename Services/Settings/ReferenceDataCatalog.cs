using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Services;
using ERPSystem.Services.Sales;

namespace ERPSystem.Services.Settings;

/// <summary>
/// Cached reference lists for forms (warehouses, branches, tax codes).
/// Refreshed at startup and invalidated after related save paths.
/// </summary>
public static class ReferenceDataCatalog
{
    private static IReadOnlyList<WarehouseListDto> _warehouses = [];
    private static IReadOnlyList<TaxCodeDto> _taxCodes = [];
    private static Guid? _branchId;

    public static IReadOnlyList<WarehouseListDto> Warehouses => _warehouses;
    public static IReadOnlyList<TaxCodeDto> TaxCodes => _taxCodes;
    public static Guid? BranchId => _branchId;

    public static async Task RefreshAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            var branch = AppServices.GetRequiredService<ICurrentBranchService>();
            _branchId = branch.BranchId;

            if (_branchId is Guid branchId)
            {
                var warehouseResult = await SalesUiService.Instance.GetWarehousesAsync();
                if (warehouseResult.IsSuccess && warehouseResult.Value is not null)
                    _warehouses = warehouseResult.Value;
            }

            var taxResult = await SalesUiService.Instance.GetTaxCodesAsync();
            if (taxResult.IsSuccess && taxResult.Value is not null)
                _taxCodes = taxResult.Value;
        }
        catch
        {
            // Keep previous cache if refresh fails.
        }
    }

    public static void InvalidateWarehouses() => _ = RefreshAsync();
    public static void InvalidateTaxCodes() => _ = RefreshAsync();
    public static void InvalidateAll() => _ = RefreshAsync();
}
