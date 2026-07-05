using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core.Sales;
using ERPSystem.Helpers;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales;

public sealed class WarehouseDetailingPageControl : UserControl
{
    private readonly StackPanel _root = new();
    private Border? _bannerBorder;
    private readonly Border _workspaceHost;
    private readonly TextBlock _emptyText;
    private readonly ComboBox _cmbWarehouse;
    private readonly DataGrid _grid;
    private readonly WarehouseDetailingWorkspaceControl _workspace = new();
    private Dictionary<Guid, string> _containerNames = [];
    private List<WarehouseListDto> _warehouses = [];
    private List<DetailingQueueRow> _queue = [];
    private bool _isLoading;

    public WarehouseDetailingPageControl()
    {
        _root.Margin = new Thickness(16);
        _root.Children.Add(ErpUiFactory.SectionTitle("مهام المستودع — تفصيل الأطوال"));

        var warehouseRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 8) };
        warehouseRow.Children.Add(new TextBlock
        {
            Text = "المستودع:",
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            Foreground = Br("TextSecondaryBrush")
        });
        _cmbWarehouse = new ComboBox
        {
            MinWidth = 220,
            Height = 36,
            DisplayMemberPath = nameof(WarehouseListDto.NameAr)
        };
        _cmbWarehouse.SelectionChanged += (_, _) => _ = LoadQueueAsync();
        warehouseRow.Children.Add(_cmbWarehouse);
        _root.Children.Add(warehouseRow);

        _bannerBorder = ErpUxFactory.InfoBanner("جاري التحميل...", "warning");
        _root.Children.Add(_bannerBorder);

        _workspaceHost = ErpUiFactory.Card(_workspace);
        _workspaceHost.Visibility = Visibility.Collapsed;
        _root.Children.Add(_workspaceHost);

        _emptyText = new TextBlock
        {
            Text = "لا توجد فواتير بانتظار التفصيل حالياً.\nأرسل مسودة فاتورة من شاشة «فاتورة بيع جديدة» ثم اضغط «إرسال للمستودع».",
            Foreground = Br("TextSecondaryBrush"),
            Margin = new Thickness(8),
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            Visibility = Visibility.Collapsed
        };
        _root.Children.Add(ErpUiFactory.Card(_emptyText));

        _root.Children.Add(ErpUiFactory.SectionTitle("قائمة الانتظار"));

        _grid = ErpUiFactory.BuildGrid(Array.Empty<DetailingQueueRow>(), false);
        _grid.AutoGenerateColumns = false;
        _grid.IsReadOnly = true;
        _grid.SelectionChanged += OnQueueSelectionChanged;
        _grid.MouseDoubleClick += (_, _) => LoadSelectedInvoice();
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("رقم الفاتورة", nameof(DetailingQueueRow.InvoiceNumber), 120),
            ("العميل", nameof(DetailingQueueRow.CustomerName), 150),
            ("الحاوية", nameof(DetailingQueueRow.Container), 110),
            ("الأثواب", nameof(DetailingQueueRow.RollCount), 70),
            ("الحالة", nameof(DetailingQueueRow.StatusDisplay), 120)
        })
        {
            ErpUiFactory.AddGridColumn(_grid, h, p, w, null);
        }

        _root.Children.Add(ErpUiFactory.Card(_grid));

        Content = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _root
        };

        _workspace.DetailingCompleted += OnDetailingCompleted;

        Loaded += OnLoaded;
        Unloaded += (_, _) =>
        {
            DetailingQueueRefreshHub.RefreshRequested -= OnRefreshRequested;
            SalesListRefreshHub.RefreshRequested -= OnRefreshRequested;
        };
        DetailingQueueRefreshHub.RefreshRequested += OnRefreshRequested;
        SalesListRefreshHub.RefreshRequested += OnRefreshRequested;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadWarehousesAsync();
        await LoadQueueAsync();
    }

    private void OnRefreshRequested(object? sender, EventArgs e) =>
        _ = LoadQueueAsync();

    private async Task LoadWarehousesAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        var result = await SalesUiService.Instance.GetWarehousesAsync();
        if (!result.IsSuccess || result.Value is null || result.Value.Count == 0)
        {
            _warehouses =
            [
                new WarehouseListDto
                {
                    Id = DatabaseSeeder.DefaultWarehouseId,
                    Code = "WH-MAIN",
                    NameAr = "المستودع الرئيسي",
                    IsActive = true
                }
            ];
        }
        else
        {
            _warehouses = result.Value.ToList();
        }

        _cmbWarehouse.ItemsSource = _warehouses;
        if (_cmbWarehouse.SelectedItem is null && _warehouses.Count > 0)
            _cmbWarehouse.SelectedIndex = 0;
    }

    private Guid GetSelectedWarehouseId() =>
        _cmbWarehouse.SelectedItem is WarehouseListDto wh
            ? wh.Id
            : DatabaseSeeder.DefaultWarehouseId;

    private async Task LoadQueueAsync()
    {
        if (_isLoading || !AppServices.IsInitialized)
            return;

        if (_cmbWarehouse.SelectedItem is null && _warehouses.Count > 0)
            _cmbWarehouse.SelectedIndex = 0;

        _isLoading = true;
        try
        {
            await LoadContainerNamesAsync();

            var result = await SalesUiService.Instance.GetDetailingQueueAsync(GetSelectedWarehouseId());
            if (!ApplicationResultPresenter.Present(result))
                return;

            _queue = [];
            foreach (var dto in result.Value ?? [])
            {
                var unitPrice = dto.RepresentativeUnitPrice ?? 0m;
                var containerLabel = _containerNames.GetValueOrDefault(dto.ChinaContainerId, "—");
                _queue.Add(DetailingQueueRow.FromDto(dto, containerLabel, unitPrice));
            }

            _grid.ItemsSource = _queue;
            SetBanner(_queue.Count);

            if (_queue.Count == 0)
            {
                _workspaceHost.Visibility = Visibility.Collapsed;
                _emptyText.Visibility = Visibility.Visible;
                return;
            }

            _emptyText.Visibility = Visibility.Collapsed;
            _workspaceHost.Visibility = Visibility.Visible;

            var (focusId, focusNumber) = SalesNavigationContext.TakeDetailingContext();
            var selected = focusId.HasValue
                ? _queue.FirstOrDefault(q => q.InvoiceId == focusId.Value)
                : !string.IsNullOrWhiteSpace(focusNumber)
                    ? _queue.FirstOrDefault(q =>
                        q.InvoiceNumber.Equals(focusNumber, StringComparison.OrdinalIgnoreCase))
                    : null;

            _grid.SelectedItem = selected ?? _queue[0];
            LoadSelectedInvoice();
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task LoadContainerNamesAsync()
    {
        if (!AppServices.IsInitialized)
            return;

        try
        {
            using var scope = AppServices.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<ERPSystem.Application.UseCases.Queries.GetChinaContainerListHandler>();
            var containers = await handler.HandleAsync(new ERPSystem.Application.Queries.Containers.GetChinaContainerListQuery
            {
                CompanyId = DatabaseSeeder.DefaultCompanyId,
                BranchId = DatabaseSeeder.DefaultBranchId,
                Page = 1,
                PageSize = 500
            });

            if (containers.IsSuccess && containers.Value?.Items is not null)
                _containerNames = containers.Value.Items.ToDictionary(c => c.Id, c => c.ContainerNumber);
        }
        catch
        {
            // Optional display labels.
        }
    }

    private void SetBanner(int count)
    {
        if (_bannerBorder is not null)
            _root.Children.Remove(_bannerBorder);

        var warehouseName = _cmbWarehouse.SelectedItem is WarehouseListDto wh ? wh.NameAr : "المستودع";
        _bannerBorder = ErpUxFactory.InfoBanner(
            count > 0
                ? $"لديك {count} فاتورة بانتظار التفصيل في {warehouseName}. اختر فاتورة من القائمة."
                : $"لا توجد فواتير بانتظار التفصيل في {warehouseName}.",
            count > 0 ? "warning" : "info");
        _root.Children.Insert(2, _bannerBorder);
    }

    private void OnQueueSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_grid.SelectedItem is DetailingQueueRow && IsLoaded)
            LoadSelectedInvoice();
    }

    private void LoadSelectedInvoice()
    {
        if (_grid.SelectedItem is not DetailingQueueRow row)
            return;

        _workspace.LoadFromDatabase(
            row.InvoiceId,
            row.InvoiceNumber,
            row.CustomerName,
            row.Container,
            row.Dto.Rolls,
            row.UnitPrice);
    }

    private async void OnDetailingCompleted(object? sender, EventArgs e) =>
        await LoadQueueAsync();

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
}
