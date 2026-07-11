using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using ERPSystem.Diagnostics.Performance;

namespace ERPSystem.Controls.China;

public sealed class ContainerListRow
{
    public Guid Id { get; init; }
    public string ContainerNumber { get; init; } = "";
    public string SupplierName { get; init; } = "";
    public ChinaContainerStatus Status { get; init; }
    public string OrderNumber { get; init; } = "—";
    public DateTime ShipmentDate { get; init; }
    public DateTime? ExpectedArrival { get; init; }
    public string StatusDisplay { get; init; } = "";
    public int CodeCount { get; init; }
    public int ColorCount { get; init; }
    public int TotalRolls { get; init; }
    public decimal TotalMeters { get; init; }
    public decimal TotalWeightKg { get; init; }
    public decimal WastePercent { get; init; }
    public string LinkedCustomers { get; init; } = "—";
    public DateTime LastUpdated { get; init; }

    public static ContainerListRow FromDto(ContainerListDto dto) => new()
    {
        Id = dto.Id,
        ContainerNumber = dto.ContainerNumber,
        SupplierName = dto.SupplierName,
        Status = dto.Status,
        ShipmentDate = dto.ShipmentDate,
        ExpectedArrival = dto.ExpectedArrival,
        StatusDisplay = dto.Status.ToArabic(),
        CodeCount = dto.CodeCount,
        ColorCount = dto.ColorCount,
        TotalRolls = dto.TotalRolls,
        TotalMeters = dto.TotalMeters,
        TotalWeightKg = dto.TotalWeightKg ?? 0,
        LastUpdated = dto.ShipmentDate
    };

    public static ContainerListRow FromDetails(ContainerDetailsDto dto) => new()
    {
        Id = dto.Id,
        ContainerNumber = dto.ContainerNumber,
        SupplierName = dto.SupplierName,
        Status = dto.Status,
        ShipmentDate = dto.ShipmentDate,
        ExpectedArrival = dto.ArrivalDate,
        StatusDisplay = dto.Status.ToArabic(),
        CodeCount = dto.Items.Select(i => i.FabricItemId).Distinct().Count(),
        ColorCount = dto.Items.Select(i => i.FabricColorId).Distinct().Count(),
        TotalRolls = dto.TotalRolls,
        TotalMeters = dto.TotalMeters,
        TotalWeightKg = dto.TotalWeightKg ?? 0,
        LastUpdated = dto.ShipmentDate
    };
}

public sealed class ContainerListPageControl : UserControl
{
    private readonly ErpListModuleControl _page = new();
    private readonly ComboBox _statusFilter = ErpUiFactory.FilterCombo(
        ["الكل", "بالطريق", "واصلة", "قيد المراجعة", "مراجعة التكلفة", "معتمدة", "في المخزن", "مغلقة", "مؤرشفة"], 130);
    private readonly DispatcherTimer _searchTimer = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private string _pendingSearch = "";
    private int _totalCount;
    private bool _isLoading;

    public ContainerListPageControl()
    {
        Content = _page;
        ConfigureList();
        Loaded += OnLoaded;
        Unloaded += (_, _) => ContainerListRefreshHub.RefreshRequested -= OnRefreshRequested;
        ContainerListRefreshHub.RefreshRequested += OnRefreshRequested;
        _searchTimer.Tick += async (_, _) =>
        {
            _searchTimer.Stop();
            await LoadContainersAsync(_pendingSearch);
        };
    }

    private void ConfigureList()
    {
        _page.Configure(EntityType.ImportContainer, AppModule.ChinaImport);
        _page.SetHeader("طلبات الصين", "قائمة حاويات استيراد الأقمشة من الصين", "\uE7BF", B("AccentOrdersBrush"));
        _page.SetPrimaryButton("استيراد حاوية");
        _page.SetEmptyState("لا توجد حاويات مستوردة", "استيراد حاوية", "\uE7BF");
        _page.EnableServerSideSearch();
        _page.SetFilterExtras(_statusFilter);
        _statusFilter.SelectionChanged += async (_, _) => await LoadContainersAsync(_pendingSearch);

            _page.PrimaryActionRequested += (_, _) =>
            ChinaImportNavigation.Navigate("NewImport");

        _page.SearchChanged += (_, term) =>
        {
            _pendingSearch = term;
            _searchTimer.Stop();
            _searchTimer.Start();
        };

        _page.SetSearchMatcher((o, term) => o is ContainerListRow m &&
            (m.ContainerNumber.Contains(term, StringComparison.OrdinalIgnoreCase) ||
             m.SupplierName.Contains(term, StringComparison.OrdinalIgnoreCase)));

        SetupContainerGrid(_page.Grid);
        _page.Grid.MouseDoubleClick += OnGridDoubleClick;
    }

    private void OnGridDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_page.Grid.SelectedItem is ContainerListRow row)
            ChinaImportNavigation.OpenOperationsCenter(row);
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (AppServices.IsInitialized)
        {
            var canCreate = await ContainerUiService.Instance.CanCreateAsync();
            _page.SetPrimaryButtonEnabled(canCreate);
        }
        await LoadContainersAsync("");
    }

    private void OnRefreshRequested(object? sender, EventArgs e) =>
        _ = LoadContainersAsync(_pendingSearch);

    private async Task LoadContainersAsync(string search)
    {
        using var perfScope = ScreenLoadProfiler.Begin("China.Containers");
        if (_isLoading || !AppServices.IsInitialized)
            return;

        _isLoading = true;
        _page.SetLoadingState(true);

        try
        {
            var status = ChinaContainerStatusDisplay.FromArabicFilter(
                (_statusFilter.SelectedItem as ComboBoxItem)?.Content?.ToString());
            var result = await ScreenLoadProfiler.MeasureLoadAsync(perfScope, () => ContainerUiService.Instance.GetListAsync(search, status, 1, 100));
        perfScope?.IncrementServiceCalls();
            if (!ApplicationResultPresenter.Present(result))
                return;

            _totalCount = result.Value!.TotalCount;
            var rows = result.Value.Items
                .Select(ContainerListRow.FromDto)
                .Cast<object>()
                .ToList();

            _page.BindData(rows);
            _page.UpdateRecordCount(rows.Count, _totalCount);
        }
        finally
        {
            _page.SetLoadingState(false);
            _isLoading = false;
        }
    }

    private static void SetupContainerGrid(DataGrid g)
    {
        g.Columns.Clear();
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        g.CanUserAddRows = false;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("رقم الحاوية", nameof(ContainerListRow.ContainerNumber), 120, null),
            ("المورد", nameof(ContainerListRow.SupplierName), "*", null),
            ("رقم الطلب", nameof(ContainerListRow.OrderNumber), 110, null),
            ("تاريخ الشحن", nameof(ContainerListRow.ShipmentDate), 100, "yyyy/MM/dd"),
            ("الوصول المتوقع", nameof(ContainerListRow.ExpectedArrival), 110, "yyyy/MM/dd"),
            ("الحالة", nameof(ContainerListRow.StatusDisplay), 100, null),
            ("الأكواد", nameof(ContainerListRow.CodeCount), 65, null),
            ("الألوان", nameof(ContainerListRow.ColorCount), 65, null),
            ("الأثواب", nameof(ContainerListRow.TotalRolls), 70, null),
            ("الأطوال", nameof(ContainerListRow.TotalMeters), 90, "N0"),
            ("الوزن", nameof(ContainerListRow.TotalWeightKg), 80, "N0"),
            ("الهالك %", nameof(ContainerListRow.WastePercent), 70, null),
            ("العملاء", nameof(ContainerListRow.LinkedCustomers), 120, null),
            ("آخر تحديث", nameof(ContainerListRow.LastUpdated), 100, "yyyy/MM/dd")
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
        }
    }

    private static SolidColorBrush B(string key) => (SolidColorBrush)System.Windows.Application.Current.Resources[key]!;
}
