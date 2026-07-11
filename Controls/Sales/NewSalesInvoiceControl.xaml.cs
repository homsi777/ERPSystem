using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Queries.Containers;
using ERPSystem.Application.Results;
using ERPSystem.Application.Services;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Controls.Finance;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using ERPSystem.Services.Customers;
using ERPSystem.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales
{
    public sealed class ContainerPickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

    public sealed class CustomerPickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

    public sealed class WarehousePickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
    }

    public sealed class TaxCodePickItem
    {
        public Guid Id { get; init; }
        public string Display { get; init; } = "";
        public decimal Rate { get; init; }
        public bool IsInclusive { get; init; }
    }

    public class SalesInvoiceLineRow : INotifyPropertyChanged
    {
        private string _goodsType = "";
        private string _boltCode = "";
        private string _color = "";
        private int _rollCount;
        private string _rollCountText = "";
        private string _lengthStatus = "—";
        private string _unit = "متر";
        private decimal _unitPrice;
        private string _unitPriceText = "";
        private decimal _originalUnitPrice;
        private string _discountReason = "";
        private string _notes = "";
        private SalesWarehouseStockOptionDto? _selectedStock;
        private ContainerPickItem? _selectedContainer;
        private bool _missingSalePrice;
        private TaxCodePickItem? _selectedTaxCode;
        private decimal _previewTaxAmount;
        private decimal _previewTaxableAmount;
        private string _taxRateDisplay = "—";
        private string _isTaxInclusiveDisplay = "—";

        public ObservableCollection<SalesWarehouseStockOptionDto> StockOptions { get; } = new();
        public Guid LineId { get; set; }
        internal decimal LoadedNetLineAmount { get; set; }
        internal decimal LoadedLineDiscount { get; set; }
        public Guid FabricItemId { get; set; }
        public Guid FabricColorId { get; set; }
        public Guid ChinaContainerId => SelectedContainer?.Id ?? Guid.Empty;
        public int AvailableRollCount { get; set; }
        public decimal AvailableMeters { get; set; }

        public ContainerPickItem? SelectedContainer
        {
            get => _selectedContainer;
            set
            {
                if (_selectedContainer?.Id == value?.Id)
                    return;

                if (_selectedContainer is not null && FabricItemId != Guid.Empty &&
                    !MockInteractionService.Confirm(
                        "تغيير حاوية السطر سيمسح التوب المختار والسعر. هل تريد المتابعة؟",
                        "تغيير حاوية السطر"))
                    return;

                _selectedContainer = value;
                ClearStockSelection();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedContainer)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ChinaContainerId)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ContainerDisplay)));
            }
        }

        public string ContainerDisplay => SelectedContainer?.Display ?? "اختر الحاوية...";

        public bool MissingSalePrice
        {
            get => _missingSalePrice;
            set => SetField(ref _missingSalePrice, value);
        }

        public SalesWarehouseStockOptionDto? SelectedStock
        {
            get => _selectedStock;
            set
            {
                if (!SetField(ref _selectedStock, value))
                    return;

                if (value is not null)
                    ApplyStockSelection(value);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StockSelectionDisplay)));
            }
        }

        public string GoodsType
        {
            get => _goodsType;
            set => SetField(ref _goodsType, value);
        }

        public string BoltCode
        {
            get => _boltCode;
            set => SetField(ref _boltCode, value);
        }

        public string Color
        {
            get => _color;
            set => SetField(ref _color, value);
        }

        public int RollCount
        {
            get => _rollCount;
            set
            {
                if (!SetField(ref _rollCount, value))
                    return;
                var text = value.ToString(CultureInfo.CurrentCulture);
                if (_rollCountText != text)
                {
                    _rollCountText = text;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(RollCountText)));
                }
            }
        }

        /// <summary>Editable text for grid cell — avoids WPF int-binding blocking keystrokes.</summary>
        public string RollCountText
        {
            get => _rollCountText;
            set => SetField(ref _rollCountText, value ?? "");
        }

        public string LengthStatus
        {
            get => _lengthStatus;
            set => SetField(ref _lengthStatus, value);
        }

        public string Unit
        {
            get => _unit;
            set => SetField(ref _unit, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (!SetField(ref _unitPrice, value))
                    return;
                var text = FormatUnitPriceDisplay(value);
                if (_unitPriceText != text)
                {
                    _unitPriceText = text;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UnitPriceText)));
                }
                RaiseDiscountDisplayChanged();
            }
        }

        /// <summary>Editable text for grid cell — avoids WPF decimal-binding blocking keystrokes.</summary>
        public string UnitPriceText
        {
            get => _unitPriceText;
            set => SetField(ref _unitPriceText, value ?? "");
        }

        /// <summary>Catalog (card) price captured when the stock was selected; baseline for the discount.</summary>
        public decimal OriginalUnitPrice
        {
            get => _originalUnitPrice;
            set
            {
                if (!SetField(ref _originalUnitPrice, value))
                    return;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OriginalUnitPriceText)));
                RaiseDiscountDisplayChanged();
            }
        }

        public string OriginalUnitPriceText =>
            _originalUnitPrice > 0 ? FormatUnitPriceDisplay(_originalUnitPrice) : "—";

        /// <summary>Per-meter discount (original − applied), zero when no discount was given.</summary>
        public decimal PerMeterDiscount =>
            _originalUnitPrice > 0 && _originalUnitPrice > _unitPrice
                ? _originalUnitPrice - _unitPrice
                : 0m;

        public bool HasDiscount => PerMeterDiscount > 0;

        public string DiscountHint =>
            HasDiscount ? $"-{PerMeterDiscount.ToString("N2", CultureInfo.CurrentCulture)} $/م" : "—";

        public string DiscountReason
        {
            get => _discountReason;
            set => SetField(ref _discountReason, value ?? "");
        }

        private void RaiseDiscountDisplayChanged()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PerMeterDiscount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasDiscount)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DiscountHint)));
        }

        public string Notes
        {
            get => _notes;
            set => SetField(ref _notes, value ?? "");
        }

        public TaxCodePickItem? SelectedTaxCode
        {
            get => _selectedTaxCode;
            set
            {
                if (!SetField(ref _selectedTaxCode, value))
                    return;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TaxCodeId)));
            }
        }

        public Guid? TaxCodeId => SelectedTaxCode?.Id;

        public decimal PreviewTaxAmount
        {
            get => _previewTaxAmount;
            set
            {
                if (!SetField(ref _previewTaxAmount, value))
                    return;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PreviewTaxAmountText)));
            }
        }

        public decimal PreviewTaxableAmount
        {
            get => _previewTaxableAmount;
            set => SetField(ref _previewTaxableAmount, value);
        }

        public string TaxRateDisplay
        {
            get => _taxRateDisplay;
            set => SetField(ref _taxRateDisplay, value ?? "—");
        }

        public string IsTaxInclusiveDisplay
        {
            get => _isTaxInclusiveDisplay;
            set => SetField(ref _isTaxInclusiveDisplay, value ?? "—");
        }

        public string PreviewTaxAmountText =>
            PreviewTaxAmount > 0 ? FormatUnitPriceDisplay(PreviewTaxAmount) : "—";

        internal void ApplyTaxPreview(SalesInvoiceTaxPreviewLineDto preview)
        {
            PreviewTaxAmount = preview.TaxAmount;
            PreviewTaxableAmount = preview.TaxableAmount;
            TaxRateDisplay = preview.TaxRate > 0
                ? $"{preview.TaxRate * 100m:N1}%"
                : "—";
            IsTaxInclusiveDisplay = preview.TaxCodeId is null
                ? "—"
                : preview.IsInclusive ? "شامل" : "غير شامل";
        }

        internal void ClearTaxPreview()
        {
            PreviewTaxAmount = 0;
            PreviewTaxableAmount = 0;
            TaxRateDisplay = "—";
            IsTaxInclusiveDisplay = "—";
        }

        internal static string FormatUnitPriceDisplay(decimal value) =>
            value.ToString("N2", CultureInfo.CurrentCulture);

        public event PropertyChangedEventHandler? PropertyChanged;

        private void ApplyStockSelection(SalesWarehouseStockOptionDto stock)
        {
            FabricItemId = stock.FabricItemId;
            FabricColorId = stock.FabricColorId;
            GoodsType = stock.FabricDisplayName;
            BoltCode = stock.FabricCode;
            Color = stock.ColorDisplayName;
            AvailableRollCount = stock.AvailableRollCount;
            AvailableMeters = stock.AvailableMeters;

            if (stock.SalePricePerMeter is > 0)
            {
                OriginalUnitPrice = stock.SalePricePerMeter.Value;
                UnitPrice = stock.SalePricePerMeter.Value;
                MissingSalePrice = false;
            }
            else
            {
                OriginalUnitPrice = 0;
                UnitPrice = 0;
                MissingSalePrice = true;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StockSelectionDisplay)));
        }

        public string StockSelectionDisplay
        {
            get
            {
                if (SelectedStock is not null)
                    return SelectedStock.Display;

                if (FabricItemId != Guid.Empty)
                    return BuildSavedLineDisplay();

                return "اضغط لاختيار التوب...";
            }
        }

        internal void AttachStockOption(SalesWarehouseStockOptionDto stock)
        {
            _selectedStock = stock;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StockSelectionDisplay)));
        }

        internal void ClearStockSelection()
        {
            _selectedStock = null;
            FabricItemId = Guid.Empty;
            FabricColorId = Guid.Empty;
            GoodsType = "";
            BoltCode = "";
            Color = "";
            AvailableRollCount = 0;
            AvailableMeters = 0;
            OriginalUnitPrice = 0;
            UnitPrice = 0;
            MissingSalePrice = false;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StockSelectionDisplay)));
        }

        internal void RefreshStockSelectionDisplay() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StockSelectionDisplay)));

        private string BuildSavedLineDisplay()
        {
            var name = !string.IsNullOrWhiteSpace(GoodsType)
                ? GoodsType
                : !string.IsNullOrWhiteSpace(BoltCode) ? BoltCode : "—";

            return !string.IsNullOrWhiteSpace(Color)
                ? $"{name} / {Color}"
                : name;
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }

    public partial class NewSalesInvoiceControl : UserControl
    {
        private readonly ObservableCollection<SalesInvoiceLineRow> _lines = new();
        public ObservableCollection<SalesWarehouseStockOptionDto> StockOptions { get; } = new();
        public ObservableCollection<ContainerPickItem> ContainerOptions { get; } = new();
        public ObservableCollection<TaxCodePickItem> TaxCodeOptions { get; } = new();
        private Guid? _invoiceId;
        private SalesInvoiceStatus _domainStatus = SalesInvoiceStatus.Draft;
        private decimal _loadedGrandTotal;
        private decimal _loadedSubTotal;
        private decimal _loadedDiscountTotal;
        private decimal _loadedTaxTotal;
        private decimal _loadedTaxableAmount;
        private decimal _loadedLineDiscountTotal;
        private decimal _loadedRoundingDifference;
        private decimal _loadedTotalMeters;
        private bool _isLegacyUntaxed;
        private bool _hasTaxPreview;
        private decimal _previewTaxTotal;
        private decimal _previewTaxableAmount;
        private decimal _previewLineDiscountTotal;
        private decimal _previewRoundingDifference;
        private decimal _previewGrandTotal;
        private bool _taxPreviewRefreshScheduled;
        private readonly Dictionary<int, string> _lineDisplayByNumber = new();
        private bool _cellEditFailed;
        private bool _isSaving;
        private bool _initialized;
        private bool _isFormDirty;

        public NewSalesInvoiceControl()
        {
            InitializeComponent();
            DataContext = this;
            Loaded += OnLoaded;
            CustomerListRefreshHub.RefreshRequested += OnCustomersRefreshRequested;
            SalesListRefreshHub.RefreshRequested += OnSalesListRefreshRequested;
            Unloaded += (_, _) =>
            {
                CustomerListRefreshHub.RefreshRequested -= OnCustomersRefreshRequested;
                SalesListRefreshHub.RefreshRequested -= OnSalesListRefreshRequested;
            };
            CmbContainer.SelectionChanged += async (_, _) => await SyncWarehouseForContainerAsync();
            CmbWarehouse.SelectionChanged += async (_, _) => await ReloadAllLineStockOptionsAsync();
            _lines.CollectionChanged += OnLinesCollectionChanged;
            CashboxDropdownBinder.WireRefresh(CmbCashbox);
            Loaded += (_, _) => UnsavedWorkGuard.Register(this, "فاتورة بيع جديدة", HasUnsavedWork);
            Unloaded += (_, _) => UnsavedWorkGuard.Unregister(this);
            CmbCustomer.SelectionChanged += (_, _) => MarkFormDirty();
            CmbWarehouse.SelectionChanged += (_, _) => MarkFormDirty();
            CmbContainer.SelectionChanged += (_, _) => MarkFormDirty();
        }

        private void MarkFormDirty()
        {
            if (_domainStatus == SalesInvoiceStatus.Draft)
                _isFormDirty = true;
        }

        private void MarkFormClean() => _isFormDirty = false;

        private bool HasUnsavedWork() =>
            _domainStatus == SalesInvoiceStatus.Draft && _isFormDirty;

        private void OnLinesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems is not null)
            {
                foreach (SalesInvoiceLineRow row in e.NewItems)
                    row.PropertyChanged += OnLinePropertyChanged;
            }

            if (e.OldItems is not null)
            {
                foreach (SalesInvoiceLineRow row in e.OldItems)
                    row.PropertyChanged -= OnLinePropertyChanged;
            }
        }

        private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(SalesInvoiceLineRow.RollCount)
                or nameof(SalesInvoiceLineRow.RollCountText)
                or nameof(SalesInvoiceLineRow.UnitPrice)
                or nameof(SalesInvoiceLineRow.UnitPriceText)
                or nameof(SalesInvoiceLineRow.OriginalUnitPrice)
                or nameof(SalesInvoiceLineRow.SelectedTaxCode)
                or nameof(SalesInvoiceLineRow.TaxCodeId)
                or nameof(SalesInvoiceLineRow.SelectedStock)
                or nameof(SalesInvoiceLineRow.FabricItemId)
                or nameof(SalesInvoiceLineRow.SelectedContainer))
            {
                MarkFormDirty();
                Dispatcher.BeginInvoke(RefreshSummary);
                ScheduleTaxPreviewRefresh();
            }

            if (sender is SalesInvoiceLineRow row &&
                e.PropertyName is nameof(SalesInvoiceLineRow.SelectedContainer))
            {
                _ = ReloadStockOptionsForRowAsync(row);
            }
        }

        public Guid? SelectedContainerId =>
            CmbContainer.SelectedItem is ContainerPickItem item ? item.Id : null;

        private void ItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is SalesInvoiceLineRow row)
            {
                TxtLineNotes.Text = row.Notes;
                UpdateDiscountReasonEditor(row);
            }
            else
            {
                TxtLineNotes.Text = "";
                UpdateDiscountReasonEditor(null);
            }
        }

        private void TxtLineNotes_LostFocus(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is SalesInvoiceLineRow row)
                row.Notes = TxtLineNotes.Text.Trim();
        }

        /// <summary>Shows the discount-reason editor only when the selected line carries a discount.</summary>
        private void UpdateDiscountReasonEditor(SalesInvoiceLineRow? row)
        {
            var showReason = row is { HasDiscount: true } && _domainStatus == SalesInvoiceStatus.Draft;
            DiscountReasonPanel.Visibility = showReason ? Visibility.Visible : Visibility.Collapsed;

            if (row is null)
            {
                CmbDiscountReason.Text = "";
                TxtDiscountHint.Text = "";
                return;
            }

            CmbDiscountReason.Text = row.DiscountReason;
            TxtDiscountHint.Text = row.HasDiscount
                ? $"خصم {row.PerMeterDiscount:N2} $/م عن السعر الأصلي {row.OriginalUnitPriceText} $"
                : "";
        }

        private void CmbDiscountReason_TextChanged(object sender, RoutedEventArgs e)
        {
            if (ItemsGrid.SelectedItem is SalesInvoiceLineRow row)
                row.DiscountReason = CmbDiscountReason.Text?.Trim() ?? "";
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                _initialized = true;
                TxtInvoiceNumber.Text = "";
                DpDate.SelectedDate = DateTime.Today;
                ItemsGrid.ItemsSource = _lines;
                ItemsGrid.PreviewMouseLeftButtonDown += ItemsGrid_PreviewMouseLeftButtonDown;
                ItemsGrid.SelectionChanged += ItemsGrid_SelectionChanged;
                TxtLineNotes.LostFocus += TxtLineNotes_LostFocus;
                CmbDiscountReason.LostFocus += CmbDiscountReason_TextChanged;
                CmbDiscountReason.SelectionChanged += (s, _) => CmbDiscountReason_TextChanged(s, new RoutedEventArgs());
                foreach (var row in _lines)
                    row.PropertyChanged += OnLinePropertyChanged;
            }

            await LoadLookupsAsync();

            var editId = SalesNavigationContext.EditInvoiceId;
            if (editId.HasValue)
            {
                await LoadInvoiceAsync(editId.Value);
            }
            else if (_lines.Count == 0)
            {
                EnsureDefaultLine();
            }

            UpdateStatusBadge();
            UpdateWorkflowUi();
            ScheduleTaxPreviewRefresh();
        }

        private void OnSalesListRefreshRequested(object? sender, EventArgs e)
        {
            if (!IsLoaded || _invoiceId is null)
                return;

            _ = ReloadInvoiceAsync();
        }

        private async void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is not true || !IsLoaded || !_initialized || _invoiceId.HasValue)
                return;

            await LoadCustomersAsync();
        }

        private void OnCustomersRefreshRequested(object? sender, EventArgs e)
        {
            if (!IsLoaded || _invoiceId.HasValue)
                return;

            _ = LoadCustomersAsync();
        }

        private void EnsureDefaultLine()
        {
            if (_lines.Count == 0)
            {
                _lines.Add(new SalesInvoiceLineRow());
            }
            RefreshSummary();
        }

        private async Task LoadLookupsAsync()
        {
            await LoadCustomersAsync();
            await LoadWarehousesAsync();
            await LoadContainersAsync();
            await LoadCashboxesAsync();
            await LoadTaxCodesAsync();
        }

        private async Task LoadTaxCodesAsync()
        {
            TaxCodeOptions.Clear();
            if (!AppServices.IsInitialized)
                return;

            var effectiveOn = DpDate.SelectedDate ?? DateTime.Today;
            var result = await SalesUiService.Instance.GetTaxCodesAsync(effectiveOn);
            if (!result.IsSuccess || result.Value is null)
                return;

            foreach (var code in result.Value)
            {
                TaxCodeOptions.Add(new TaxCodePickItem
                {
                    Id = code.Id,
                    Display = $"{code.Code} — {code.Name}",
                    Rate = code.Rate,
                    IsInclusive = code.IsInclusive
                });
            }
        }

        private async Task LoadCashboxesAsync()
        {
            if (!AppServices.IsInitialized)
                return;

            await CashboxDropdownBinder.LoadAsync(CmbCashbox);
        }

        private async Task LoadCustomersAsync()
        {
            var previousId = CmbCustomer.SelectedItem is CustomerPickItem selected ? selected.Id : (Guid?)null;
            var items = new List<CustomerPickItem>();

            if (AppServices.IsInitialized)
            {
                var result = await CustomerUiService.Instance.GetListAsync("", 1, 500);
                if (!result.IsSuccess)
                {
                    ApplicationResultPresenter.Present(result);
                    CmbCustomer.ItemsSource = null;
                    CmbCustomer.SelectedIndex = -1;
                    return;
                }

                if (result.Value?.Items.Count > 0)
                {
                    items.AddRange(result.Value.Items.Select(c => new CustomerPickItem
                    {
                        Id = c.Id,
                        Display = string.IsNullOrWhiteSpace(c.NameEn) ? c.NameAr : $"{c.NameAr} — {c.NameEn}"
                    }));
                }
            }

            CmbCustomer.ItemsSource = null;
            CmbCustomer.ItemsSource = items;
            CmbCustomer.DisplayMemberPath = nameof(CustomerPickItem.Display);
            if (items.Count == 0)
            {
                CmbCustomer.SelectedIndex = -1;
                return;
            }

            var restore = previousId.HasValue
                ? items.FindIndex(i => i.Id == previousId.Value)
                : -1;
            CmbCustomer.SelectedIndex = restore >= 0 ? restore : 0;
        }

        private async Task LoadWarehousesAsync()
        {
            var items = new List<WarehousePickItem>();
            if (AppServices.IsInitialized)
            {
                var result = await SalesUiService.Instance.GetWarehousesAsync();
                if (result.IsSuccess && result.Value?.Count > 0)
                {
                    items.AddRange(result.Value.Select(w => new WarehousePickItem
                    {
                        Id = w.Id,
                        Display = w.NameAr
                    }));
                }
            }

            if (items.Count == 0)
            {
                items.Add(new WarehousePickItem
                {
                    Id = DatabaseSeeder.DefaultWarehouseId,
                    Display = "المستودع الرئيسي"
                });
            }

            CmbWarehouse.ItemsSource = items;
            CmbWarehouse.DisplayMemberPath = nameof(WarehousePickItem.Display);
            if (items.Count > 0)
                CmbWarehouse.SelectedIndex = 0;
        }

        private async Task LoadContainersAsync()
        {
            var items = new List<ContainerPickItem>();
            var seenIds = new HashSet<Guid>();

            if (AppServices.IsInitialized)
            {
                try
                {
                    using var scope = AppServices.CreateScope();
                    var handler = scope.ServiceProvider.GetRequiredService<GetChinaContainerListHandler>();
                    var inventoryRepo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();

                    var sellableIds = (await inventoryRepo.GetSellableContainerIdsAsync()).ToHashSet();

                    var result = await handler.HandleAsync(new GetChinaContainerListQuery
                    {
                        CompanyId = DatabaseSeeder.DefaultCompanyId,
                        BranchId = DatabaseSeeder.DefaultBranchId,
                        Status = ChinaContainerStatus.InWarehouse,
                        Page = 1,
                        PageSize = 100
                    });

                    if (result.IsSuccess && result.Value?.Items.Count > 0)
                    {
                        foreach (var c in result.Value.Items)
                        {
                            if (!seenIds.Add(c.Id))
                                continue;

                            items.Add(new ContainerPickItem
                            {
                                Id = c.Id,
                                Display = c.ContainerNumber
                            });
                        }
                    }

                    // Include any container with available rolls even if status filter missed it.
                    if (sellableIds.Count > 0)
                    {
                        var allContainers = await handler.HandleAsync(new GetChinaContainerListQuery
                        {
                            CompanyId = DatabaseSeeder.DefaultCompanyId,
                            BranchId = DatabaseSeeder.DefaultBranchId,
                            Page = 1,
                            PageSize = 500
                        });

                        if (allContainers.IsSuccess && allContainers.Value?.Items is not null)
                        {
                            foreach (var c in allContainers.Value.Items.Where(c => sellableIds.Contains(c.Id)))
                            {
                                if (!seenIds.Add(c.Id))
                                    continue;

                                items.Add(new ContainerPickItem
                                {
                                    Id = c.Id,
                                    Display = c.ContainerNumber
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Keep empty list when database query fails.
                }
            }

            ContainerOptions.Clear();
            foreach (var item in items)
                ContainerOptions.Add(item);

            CmbContainer.ItemsSource = ContainerOptions;
            CmbContainer.SelectedIndex = -1;
        }

        private async Task SyncWarehouseForContainerAsync()
        {
            if (!AppServices.IsInitialized ||
                CmbContainer.SelectedItem is not ContainerPickItem container)
                return;

            try
            {
                using var scope = AppServices.CreateScope();
                var inventoryRepo = scope.ServiceProvider.GetRequiredService<IInventoryRepository>();
                var warehouseIds = await inventoryRepo.GetWarehousesWithContainerStockAsync(container.Id);
                if (warehouseIds.Count == 0)
                    return;

                if (CmbWarehouse.SelectedItem is WarehousePickItem current &&
                    warehouseIds.Contains(current.Id))
                    return;

                SelectWarehouse(warehouseIds[0]);
            }
            catch
            {
                // Keep current warehouse selection.
            }
        }

        private async Task LoadInvoiceAsync(Guid invoiceId)
        {
            if (!AppServices.IsInitialized)
                return;

            var result = await SalesUiService.Instance.GetOperationsCenterAsync(invoiceId);
            if (!ApplicationResultPresenter.Present(result) || result.Value?.Invoice is null)
                return;

            await ApplyOperationsCenterAsync(result.Value);
        }

        private async Task ApplyOperationsCenterAsync(SalesInvoiceOperationsCenterDto data)
        {
            var invoice = data.Invoice;
            _invoiceId = invoice.Id;
            _domainStatus = invoice.Status;
            _loadedSubTotal = invoice.SubTotal;
            _loadedGrandTotal = invoice.GrandTotal;
            _loadedDiscountTotal = invoice.DiscountTotal;
            _loadedTaxTotal = invoice.TaxTotal;
            _loadedTaxableAmount = invoice.Lines.Sum(l => l.TaxableAmount);
            _loadedLineDiscountTotal = invoice.Lines.Sum(l => l.DiscountAmount);
            _loadedRoundingDifference = invoice.RoundingDifference;
            _isLegacyUntaxed = invoice.IsLegacyUntaxed;
            _loadedTotalMeters = invoice.Lines.Sum(l => l.TotalLengthMeters);

            TxtInvoiceNumber.Text = invoice.InvoiceNumber;
            DpDate.SelectedDate = invoice.InvoiceDate;

            SelectCustomer(invoice.CustomerId);
            SelectWarehouse(invoice.WarehouseId);
            SelectContainer(invoice.ChinaContainerId);
            BtnCash.IsChecked = invoice.PaymentType == PaymentType.Cash;
            BtnCredit.IsChecked = invoice.PaymentType == PaymentType.Credit;
            TxtPartialPayment.Text = invoice.PartialPaymentAmount > 0
                ? invoice.PartialPaymentAmount.ToString("0.##")
                : "0";
            if (invoice.CashboxId is Guid cashboxId && cashboxId != Guid.Empty)
                CmbCashbox.SelectedValue = cashboxId;

            _lineDisplayByNumber.Clear();
            _lines.Clear();
            foreach (var line in invoice.Lines.OrderBy(l => l.LineNumber))
            {
                _lineDisplayByNumber[line.LineNumber] = string.IsNullOrWhiteSpace(line.ColorDisplayName)
                    ? line.FabricDisplayName
                    : $"{line.FabricDisplayName} / {line.ColorDisplayName}";

                var row = new SalesInvoiceLineRow
                {
                    LineId = line.Id,
                    SelectedContainer = ContainerOptions.FirstOrDefault(c => c.Id == line.ChinaContainerId),
                    FabricItemId = line.FabricItemId,
                    FabricColorId = line.FabricColorId,
                    GoodsType = line.FabricDisplayName,
                    BoltCode = line.FabricCode,
                    Color = line.ColorDisplayName,
                    RollCount = line.RollCount,
                    OriginalUnitPrice = line.OriginalUnitPrice > 0 ? line.OriginalUnitPrice : line.UnitPrice,
                    UnitPrice = line.UnitPrice,
                    MissingSalePrice = line.UnitPrice <= 0,
                    LengthStatus = BuildLengthStatus(line, _domainStatus),
                    DiscountReason = line.DiscountReason ?? "",
                    Notes = line.Notes ?? "",
                    SelectedTaxCode = TaxCodeOptions.FirstOrDefault(t => t.Id == line.TaxCodeId),
                    PreviewTaxAmount = line.TaxAmount,
                    PreviewTaxableAmount = line.TaxableAmount,
                    TaxRateDisplay = line.TaxRate > 0 ? $"{line.TaxRate * 100m:N1}%" : "—",
                    IsTaxInclusiveDisplay = line.TaxCodeId is null
                        ? "—"
                        : line.IsTaxInclusive ? "شامل" : "غير شامل"
                };

                if (line.LineTotal > 0 || line.TotalLengthMeters > 0)
                {
                    row.LoadedNetLineAmount = line.LineTotal;
                    row.LoadedLineDiscount = line.DiscountAmount;
                }
                row.RefreshStockSelectionDisplay();
                _lines.Add(row);
            }

            if (_lines.Count == 0)
                EnsureDefaultLine();
            else
                RefreshSummary();

            await ReloadAllLineStockOptionsAsync();
            RestoreLineStockSelections();

            UpdateStatusBadge();
            UpdateWorkflowUi();
            MarkFormClean();
            ScheduleTaxPreviewRefresh();
        }

        private void RestoreLineStockSelections()
        {
            foreach (var row in _lines.Where(r => r.FabricItemId != Guid.Empty))
            {
                var match = row.StockOptions.FirstOrDefault(o =>
                    o.FabricItemId == row.FabricItemId && o.FabricColorId == row.FabricColorId);
                if (match is not null)
                    row.AttachStockOption(match);
                else
                    row.RefreshStockSelectionDisplay();
            }
        }

        private static string BuildLengthStatus(SalesInvoiceLineDto line, SalesInvoiceStatus status)
        {
            if (line.TotalLengthMeters > 0)
                return $"{line.TotalLengthMeters:N1} م / {line.RollCount} ثوب";

            return status switch
            {
                SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
                SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval
                    or SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed
                    or SalesInvoiceStatus.Delivered => "مُدخل",
                _ => "—"
            };
        }

        private async Task ReloadAllLineStockOptionsAsync()
        {
            var groups = _lines
                .Where(row => row.SelectedContainer is ContainerPickItem)
                .GroupBy(row => (
                    Container: ((ContainerPickItem)row.SelectedContainer!).Id,
                    Warehouse: (CmbWarehouse.SelectedItem as WarehousePickItem)?.Id ?? Guid.Empty));

            foreach (var group in groups)
            {
                if (group.Key.Warehouse == Guid.Empty) continue;
                var result = await SalesUiService.Instance.GetWarehouseStockAsync(
                    group.Key.Container, group.Key.Warehouse);
                foreach (var row in group)
                {
                    row.StockOptions.Clear();
                    if (!result.IsSuccess || result.Value is null) continue;
                    foreach (var option in result.Value)
                        row.StockOptions.Add(option);
                }
            }
        }

        private async Task ReloadStockOptionsForRowAsync(SalesInvoiceLineRow row)
        {
            row.StockOptions.Clear();
            UpdateStockEmptyBanner(false, null);

            if (!AppServices.IsInitialized)
                return;

            if (row.SelectedContainer is not ContainerPickItem container ||
                CmbWarehouse.SelectedItem is not WarehousePickItem warehouse)
                return;

            var result = await SalesUiService.Instance.GetWarehouseStockAsync(container.Id, warehouse.Id);
            if (!result.IsSuccess || result.Value is null)
            {
                UpdateStockEmptyBanner(true, result.ErrorMessage ?? "تعذّر تحميل مخزون الحاوية.");
                return;
            }

            if (result.Value.Count == 0)
            {
                var warehouseName = warehouse.Display;
                UpdateStockEmptyBanner(true,
                    $"لا يوجد مخزون متاح للحاوية {container.Display} في {warehouseName}. " +
                    "تأكد أن الحاوية رُحّلت لهذا المستودع.");
                return;
            }

            UpdateStockEmptyBanner(false, null);
            foreach (var option in result.Value)
                row.StockOptions.Add(option);
        }

        private void UpdateStockEmptyBanner(bool visible, string? message)
        {
            StockEmptyBanner.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            if (!string.IsNullOrWhiteSpace(message))
                TxtStockEmptyMessage.Text = message;
        }

        private void SelectCustomer(Guid customerId)
        {
            if (CmbCustomer.ItemsSource is IEnumerable<CustomerPickItem> items)
            {
                var match = items.FirstOrDefault(i => i.Id == customerId);
                if (match is not null)
                    CmbCustomer.SelectedItem = match;
            }
        }

        private void SelectWarehouse(Guid warehouseId)
        {
            if (CmbWarehouse.ItemsSource is IEnumerable<WarehousePickItem> items)
            {
                var match = items.FirstOrDefault(i => i.Id == warehouseId);
                if (match is not null)
                    CmbWarehouse.SelectedItem = match;
            }
        }

        private void SelectContainer(Guid containerId)
        {
            if (CmbContainer.ItemsSource is IEnumerable<ContainerPickItem> items)
            {
                var match = items.FirstOrDefault(i => i.Id == containerId);
                if (match is not null)
                    CmbContainer.SelectedItem = match;
            }
        }

        private void BtnCash_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded) return;
            TxtPartialPayment.Text = "0";
        }

        private async void BtnSaveDraft_Click(object sender, RoutedEventArgs e) =>
            await SaveDraftAsync(sendToWarehouse: false);

        private async void BtnSendToWarehouse_Click(object sender, RoutedEventArgs e) =>
            await SendToWarehouseAsync();

        private async Task SaveDraftAsync(bool sendToWarehouse = false)
        {
            if (_isSaving)
                return;

            if (!TryGetHeader(out var customerId, out var warehouseId, out var containerId, out var paymentType, out var cashboxId))
                return;

            if (_lines.Count == 0 || _lines.All(l => l.RollCount <= 0))
            {
                MockInteractionService.ShowWarning("أضف صنفاً واحداً على الأقل قبل الحفظ.");
                return;
            }

            foreach (var row in _lines.Where(l => l.RollCount > 0))
            {
                if (row.FabricItemId == Guid.Empty)
                {
                    MockInteractionService.ShowWarning("اختر صنفاً من المخزون لكل سطر قبل الحفظ.");
                    return;
                }

                if (row.ChinaContainerId == Guid.Empty)
                {
                    MockInteractionService.ShowWarning("اختر حاوية لكل سطر قبل الحفظ.");
                    return;
                }

                if (row.RollCount > row.AvailableRollCount)
                {
                    MockInteractionService.ShowWarning(
                        $"عدد الأثواب المطلوب ({row.RollCount}) يتجاوز المتاح ({row.AvailableRollCount}) لـ {row.GoodsType}.");
                    return;
                }

                if (row.UnitPrice <= 0)
                {
                    MockInteractionService.ShowWarning(
                        $"أدخل سعر بيع أكبر من صفر لـ {row.GoodsType} — {row.Color}.");
                    return;
                }
            }

            _isSaving = true;
            try
            {
                var lines = BuildLineCommands();
                containerId = lines.OrderBy(l => l.LineNumber).First().ChinaContainerId;
                SelectContainer(containerId);
                var partialPayment = GetPartialPaymentAmount(paymentType);

                if (_invoiceId is null)
                {
                    var manualNumber = TxtInvoiceNumber.Text.Trim();
                    if (manualNumber is "جديد")
                        manualNumber = "";

                    var createResult = await SalesUiService.Instance.CreateDraftAsync(
                        customerId, warehouseId, containerId, paymentType, lines,
                        string.IsNullOrWhiteSpace(manualNumber) ? null : manualNumber,
                        GetDiscountAmount(),
                        cashboxId,
                        partialPayment);
                    if (!ApplicationResultPresenter.Present(createResult))
                        return;

                    _invoiceId = createResult.Value;
                }
                else if (_domainStatus == SalesInvoiceStatus.Draft)
                {
                    var updateResult = await SalesUiService.Instance.UpdateDraftAsync(
                        _invoiceId.Value, customerId, warehouseId, containerId, paymentType, lines,
                        GetDiscountAmount(),
                        cashboxId,
                        partialPayment);
                    if (!ApplicationResultPresenter.Present(updateResult))
                        return;
                }
                else
                {
                    MockInteractionService.ShowWarning("لا يمكن تعديل الفاتورة إلا وهي في حالة مسودة.");
                    return;
                }

                await ReloadInvoiceAsync();
                SalesListRefreshHub.RequestRefresh();

                MockInteractionService.ShowSuccess(
                    $"تم حفظ المسودة {TxtInvoiceNumber.Text}.",
                    "حفظ مسودة");
                MarkFormClean();

                if (sendToWarehouse)
                    await SendToWarehouseAsync(skipSave: true);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private async Task SendToWarehouseAsync(bool skipSave = false)
        {
            if (_invoiceId is null)
            {
                if (!skipSave)
                {
                    await SaveDraftAsync(sendToWarehouse: false);
                    if (_invoiceId is null)
                        return;
                }
                else
                {
                    MockInteractionService.ShowWarning("احفظ الفاتورة كمسودة أولاً.");
                    return;
                }
            }

            if (_domainStatus != SalesInvoiceStatus.Draft)
            {
                MockInteractionService.ShowWarning("يمكن إرسال الفاتورة للمستودع فقط وهي في حالة مسودة.");
                return;
            }

            if (!MockInteractionService.Confirm(
                    "إرسال الفاتورة للمستودع لتنفيذ الأطوال؟\nسيظهر الطلب في شاشة «تفصيل المستودع».",
                    "إرسال للمستودع"))
                return;

            if (!await SalesUiService.Instance.CanSendToWarehouseAsync())
            {
                MockInteractionService.ShowWarning("لا تملك صلاحية إرسال الفاتورة للمستودع.");
                return;
            }

            var sendResult = await SalesUiService.Instance.SendToWarehouseAsync(_invoiceId!.Value);
            if (!ApplicationResultPresenter.Present(sendResult))
                return;

            await ReloadInvoiceAsync();
            SalesListRefreshHub.RequestRefresh();
            DetailingQueueRefreshHub.RequestRefresh();
            SalesNavigationContext.BeginDetailing(_invoiceId, TxtInvoiceNumber.Text);

            MockInteractionService.ShowSuccess(
                $"تم إرسال الفاتورة {TxtInvoiceNumber.Text} للمستودع.\n" +
                $"الحالة: {StatusDisplay(_domainStatus)}\n" +
                "انتقل إلى «تفصيل المستودع» لإدخال الأطوال.",
                "تم الإرسال للمستودع");

            if (MockInteractionService.Confirm("فتح شاشة تفصيل الأطوال الآن؟", "تفصيل المستودع"))
                MockInteractionService.NavigateToWarehouseDetailing(TxtInvoiceNumber.Text);
        }

        private async Task ReloadInvoiceAsync()
        {
            if (_invoiceId is null)
                return;

            var result = await SalesUiService.Instance.GetOperationsCenterAsync(_invoiceId.Value);
            if (!result.IsSuccess || result.Value?.Invoice is null)
                return;

            await ApplyOperationsCenterAsync(result.Value);
        }

        private bool HasActionableLine() =>
            _lines.Any(l => l.FabricItemId != Guid.Empty && l.RollCount > 0);

        private void UpdateWorkflowUi()
        {
            var isDraft = _domainStatus == SalesInvoiceStatus.Draft;
            var canApprove = _domainStatus is SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval;
            var isDetailed = _domainStatus >= SalesInvoiceStatus.Detailed
                && _domainStatus is not SalesInvoiceStatus.Cancelled;
            var awaitingDetailing = _domainStatus == SalesInvoiceStatus.AwaitingDetailing;

            var hasLine = HasActionableLine();
            BtnSaveDraft.IsEnabled = isDraft && hasLine;
            BtnSendToWarehouse.IsEnabled = isDraft && hasLine;
            BtnApprove.IsEnabled = canApprove;

            TxtInvoiceNumber.IsReadOnly = !isDraft;

            CmbCustomer.IsEnabled = isDraft;
            CmbWarehouse.IsEnabled = isDraft;
            CmbContainer.IsEnabled = isDraft;
            BtnCash.IsEnabled = isDraft;
            BtnCredit.IsEnabled = isDraft;
            DpDate.IsEnabled = isDraft;
            ItemsGrid.IsReadOnly = !isDraft;

            DetailingCompleteBanner.Visibility = isDetailed ? Visibility.Visible : Visibility.Collapsed;
            if (isDetailed)
            {
                TxtDetailingCompleteMessage.Text =
                    $"تم حساب الأطوال والمبلغ. الإجمالي بعد الخصم: $ {_loadedGrandTotal:N2} — يمكنك اعتماد الفاتورة.";
            }

            AwaitingDetailingBanner.Visibility = awaitingDetailing ? Visibility.Visible : Visibility.Collapsed;
            if (awaitingDetailing)
            {
                TxtAwaitingDetailingMessage.Text =
                    $"الفاتورة {TxtInvoiceNumber.Text} بانتظار تفنيد الأطوال في المستودع. " +
                    "بعد الإكمال ستظهر الأطوال والمجموع هنا تلقائياً.";
            }

            var canEditDiscount = CanEditDiscount();
            DiscountEntryPanel.Visibility = canEditDiscount && _invoiceId.HasValue
                ? Visibility.Visible
                : Visibility.Collapsed;
            TxtDiscount.IsEnabled = canEditDiscount;
            BtnApplyDiscount.IsEnabled = canEditDiscount && _invoiceId.HasValue;

            FinancialSummaryPanel.Visibility = isDetailed || HasTaxSummaryToShow()
                ? Visibility.Visible
                : Visibility.Collapsed;
            TaxSummaryPanel.Visibility = isDetailed || HasTaxSummaryToShow()
                ? Visibility.Visible
                : Visibility.Collapsed;
            TxtPendingLengthsMessage.Visibility = isDetailed ? Visibility.Collapsed : Visibility.Visible;

            UpdateTaxSummaryUi(isDetailed);

            if (canEditDiscount || isDetailed)
                TxtDiscount.Text = FormatDiscountEntryText(_loadedDiscountTotal);

            if (isDetailed)
            {
                TxtTotalMeters.Text = _loadedTotalMeters.ToString("N2", CultureInfo.CurrentCulture);
                TxtSubTotal.Text = _loadedSubTotal.ToString("N2", CultureInfo.CurrentCulture);
                TxtDiscountApplied.Text = FormatDiscountSummaryText(_loadedDiscountTotal);
                TxtGrandTotal.Text = _loadedGrandTotal.ToString("N2", CultureInfo.CurrentCulture);
                var currencyLabel = GetSelectedCurrencyLabel();
                TxtSubTotalCurrency.Text = currencyLabel;
                TxtGrandTotalCurrency.Text = currencyLabel;
            }
            else if (HasTaxSummaryToShow())
            {
                TxtSubTotal.Text = (_previewTaxableAmount + _previewLineDiscountTotal)
                    .ToString("N2", CultureInfo.CurrentCulture);
                TxtGrandTotal.Text = (_previewGrandTotal > 0 ? _previewGrandTotal : _loadedGrandTotal)
                    .ToString("N2", CultureInfo.CurrentCulture);
            }
        }

        private bool HasTaxSummaryToShow() =>
            _hasTaxPreview && (_previewTaxTotal > 0 || _lines.Any(l => l.TaxCodeId is not null));

        private void UpdateTaxSummaryUi(bool isDetailed)
        {
            var taxable = isDetailed ? _loadedTaxableAmount : _previewTaxableAmount;
            var taxTotal = isDetailed ? _loadedTaxTotal : _previewTaxTotal;
            var lineDiscount = isDetailed ? _loadedLineDiscountTotal : _previewLineDiscountTotal;
            var rounding = isDetailed ? _loadedRoundingDifference : _previewRoundingDifference;

            TxtTaxableAmount.Text = taxable > 0
                ? taxable.ToString("N2", CultureInfo.CurrentCulture)
                : "—";
            TxtTaxTotal.Text = taxTotal > 0
                ? taxTotal.ToString("N2", CultureInfo.CurrentCulture)
                : "—";
            TxtLineDiscountSummary.Text = lineDiscount > 0
                ? lineDiscount.ToString("N2", CultureInfo.CurrentCulture)
                : "—";
            TxtRoundingDifference.Text = rounding != 0
                ? rounding.ToString("N2", CultureInfo.CurrentCulture)
                : "—";

            TxtLegacyTaxBanner.Visibility = _isLegacyUntaxed ? Visibility.Visible : Visibility.Collapsed;
            if (_isLegacyUntaxed)
            {
                TxtLegacyTaxBanner.Text =
                    "فاتورة قديمة بدون ضريبة — لا تُطبَّق قواعد الضريبة الجديدة على هذا المستند.";
            }
        }

        private void ScheduleTaxPreviewRefresh()
        {
            if (_taxPreviewRefreshScheduled)
                return;

            _taxPreviewRefreshScheduled = true;
            Dispatcher.BeginInvoke(() =>
            {
                _taxPreviewRefreshScheduled = false;
                _ = RefreshTaxPreviewAsync();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private async Task RefreshTaxPreviewAsync()
        {
            if (!AppServices.IsInitialized)
                return;

            var actionableLines = _lines
                .Where(l => l.RollCount > 0 && l.FabricItemId != Guid.Empty)
                .ToList();

            if (actionableLines.Count == 0)
            {
                _hasTaxPreview = false;
                _previewTaxTotal = 0;
                _previewTaxableAmount = 0;
                _previewLineDiscountTotal = 0;
                _previewRoundingDifference = 0;
                _previewGrandTotal = 0;
                foreach (var row in _lines)
                    row.ClearTaxPreview();
                UpdateTaxSummaryUi(_domainStatus >= SalesInvoiceStatus.Detailed
                    && _domainStatus is not SalesInvoiceStatus.Cancelled);
                UpdateWorkflowUi();
                return;
            }

            var lineNumber = 1;
            var requests = new List<SalesInvoiceTaxPreviewLineRequest>();
            foreach (var row in actionableLines)
            {
                var (net, discount) = BuildPreviewLineAmounts(row);
                requests.Add(new SalesInvoiceTaxPreviewLineRequest
                {
                    LineId = row.LineId != Guid.Empty ? row.LineId : Guid.NewGuid(),
                    LineNumber = lineNumber++,
                    NetLineAmount = net,
                    LineDiscountTotal = discount,
                    TaxCodeId = row.TaxCodeId
                });
            }

            var invoiceDate = DpDate.SelectedDate ?? DateTime.Today;
            var result = await SalesUiService.Instance.CalculateTaxPreviewAsync(
                invoiceDate,
                GetDiscountAmount(),
                requests);

            if (!result.IsSuccess || result.Value is null)
            {
                _hasTaxPreview = false;
                UpdateTaxSummaryUi(_domainStatus >= SalesInvoiceStatus.Detailed
                    && _domainStatus is not SalesInvoiceStatus.Cancelled);
                UpdateWorkflowUi();
                return;
            }

            var preview = result.Value;
            _hasTaxPreview = true;
            _previewTaxTotal = preview.TaxTotal;
            _previewTaxableAmount = preview.TaxableAmount;
            _previewLineDiscountTotal = preview.LineDiscountTotal;
            _previewRoundingDifference = preview.RoundingDifference;
            _previewGrandTotal = preview.GrandTotal;

            foreach (var row in _lines)
                row.ClearTaxPreview();

            foreach (var previewLine in preview.Lines)
            {
                var index = previewLine.LineNumber - 1;
                if (index < 0 || index >= actionableLines.Count)
                    continue;
                actionableLines[index].ApplyTaxPreview(previewLine);
            }

            var isDetailed = _domainStatus >= SalesInvoiceStatus.Detailed
                && _domainStatus is not SalesInvoiceStatus.Cancelled;
            UpdateTaxSummaryUi(isDetailed);
            UpdateWorkflowUi();
        }

        private static (decimal NetLineAmount, decimal LineDiscountTotal) BuildPreviewLineAmounts(SalesInvoiceLineRow row)
        {
            if (row.LoadedNetLineAmount > 0)
                return (row.LoadedNetLineAmount, row.LoadedLineDiscount);

            if (row.RollCount <= 0 || row.UnitPrice <= 0)
                return (0m, 0m);

            var avgMeters = row.AvailableRollCount > 0
                ? row.AvailableMeters / row.AvailableRollCount
                : 0m;
            var estimatedMeters = row.RollCount * avgMeters;
            var net = estimatedMeters * row.UnitPrice;
            var discount = row.HasDiscount ? row.PerMeterDiscount * estimatedMeters : 0m;
            return (net, discount);
        }

        // Header-level discount is retired in favour of per-line price overrides
        // (discounts are now tracked as contra-revenue on each line).
        private bool CanEditDiscount() => false;

        private static string FormatDiscountEntryText(decimal amount) =>
            amount > 0 ? amount.ToString("N2", CultureInfo.CurrentCulture) : string.Empty;

        private static string FormatDiscountSummaryText(decimal amount) =>
            amount > 0 ? amount.ToString("N2", CultureInfo.CurrentCulture) : "—";

        private decimal GetDiscountAmount()
        {
            var text = TxtDiscount.Text.Trim();
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount))
                return Math.Max(0, amount);
            if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out amount))
                return Math.Max(0, amount);
            return 0;
        }

        private async void BtnApplyDiscount_Click(object sender, RoutedEventArgs e) =>
            await ApplyDiscountAsync();

        private async Task ApplyDiscountAsync()
        {
            if (_invoiceId is null)
            {
                MockInteractionService.ShowWarning("احفظ الفاتورة كمسودة أولاً قبل تطبيق الخصم.");
                return;
            }

            var discount = GetDiscountAmount();
            if (_loadedSubTotal > 0 && discount > _loadedSubTotal)
            {
                MockInteractionService.ShowWarning("مبلغ الخصم لا يمكن أن يتجاوز مجموع الفاتورة.");
                return;
            }

            ApplicationResult result;
            if (_domainStatus == SalesInvoiceStatus.Draft)
            {
                if (!TryGetHeader(out var customerId, out var warehouseId, out var containerId, out var paymentType, out var cashboxId))
                    return;

                result = await SalesUiService.Instance.UpdateDraftAsync(
                    _invoiceId.Value, customerId, warehouseId, containerId, paymentType,
                    BuildLineCommands(), discount, cashboxId, GetPartialPaymentAmount(paymentType));
            }
            else
            {
                result = await SalesUiService.Instance.ApplyDiscountAsync(_invoiceId.Value, discount);
            }

            if (!ApplicationResultPresenter.Present(result))
                return;

            await ReloadInvoiceAsync();
            SalesListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess(
                discount > 0
                    ? $"تم تطبيق خصم $ {discount:N2} على الفاتورة."
                    : "تم إلغاء الخصم.",
                "خصم الفاتورة");
        }

        private string GetSelectedCurrencyLabel()
        {
            if (CmbCurrency.SelectedItem is ComboBoxItem { Content: string content })
            {
                if (content.Contains("USD", StringComparison.OrdinalIgnoreCase))
                    return "USD — دولار أمريكي $";
                if (content.Contains("SYP", StringComparison.OrdinalIgnoreCase))
                    return "SYP — ليرة سورية";
                if (content.Contains("SAR", StringComparison.OrdinalIgnoreCase))
                    return "SAR — ريال سعودي";
            }

            return "USD — دولار أمريكي $";
        }

        private async void BtnApprove_Click(object sender, RoutedEventArgs e)
        {
            if (_invoiceId is null)
            {
                MockInteractionService.ShowWarning("احفظ الفاتورة كمسودة أولاً قبل الاعتماد.");
                return;
            }

            if (_domainStatus is not (SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval))
            {
                MockInteractionService.ShowWarning("لا يمكن اعتماد الفاتورة قبل إكمال تفصيل الأطوال في المستودع.");
                return;
            }

            if (!await ConfirmBelowCostAsync())
                return;

            if (!MockInteractionService.Confirm("اعتماد وتسليم الفاتورة نهائياً؟", "اعتماد وتسليم"))
                return;

            var customerName = CmbCustomer.SelectedItem is CustomerPickItem customerPick
                ? customerPick.Display
                : null;
            var result = await SalesUiService.Instance.ApproveAndDeliverAsync(_invoiceId.Value, customerName);
            if (!ApplicationResultPresenter.Present(result))
                return;

            await ReloadInvoiceAsync();
            SalesListRefreshHub.RequestRefresh();
            MockInteractionService.ShowSuccess("تم اعتماد وتسليم الفاتورة بنجاح.", "اعتماد وتسليم");
            UpdateWorkflowUi();
        }

        /// <summary>
        /// Warns (without blocking) when any line's applied price is below its roll cost.
        /// Returns false only if the manager chooses to cancel the approval.
        /// </summary>
        private async Task<bool> ConfirmBelowCostAsync()
        {
            if (_invoiceId is null)
                return true;

            var check = await SalesUiService.Instance.CheckBelowCostAsync(_invoiceId.Value);
            if (!check.IsSuccess || check.Value is null || check.Value.Count == 0)
                return true;

            var details = string.Join("\n", check.Value.Select(l =>
            {
                var name = _lineDisplayByNumber.GetValueOrDefault(l.LineNumber, $"سطر {l.LineNumber}");
                return $"• {name}: البيع {l.AppliedPrice:N2} $ < التكلفة {l.CostPerMeter:N2} $ " +
                       $"— خسارة {l.LossPerMeter:N2} $/م ({l.TotalLoss:N2} $)";
            }));

            var totalLoss = check.Value.Sum(l => l.TotalLoss);
            return MockInteractionService.Confirm(
                "تنبيه: بعض الأسطر تُباع بأقل من التكلفة:\n\n" + details +
                $"\n\nإجمالي الخسارة المتوقعة: {totalLoss:N2} $\n\nهل تريد المتابعة والاعتماد؟",
                "بيع أقل من التكلفة");
        }

        private async void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (!MockInteractionService.Confirm("إلغاء الفاتورة والعودة؟", "إلغاء"))
                return;

            if (_invoiceId is not null &&
                _domainStatus is not (SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered or SalesInvoiceStatus.Cancelled))
            {
                var result = await SalesUiService.Instance.CancelAsync(_invoiceId.Value, "إلغاء من شاشة الفاتورة");
                if (!ApplicationResultPresenter.Present(result))
                    return;

                SalesListRefreshHub.RequestRefresh();
            }

            MockInteractionService.Navigate(AppModule.Sales, "Invoices");
        }

        private bool TryGetHeader(
            out Guid customerId,
            out Guid warehouseId,
            out Guid containerId,
            out PaymentType paymentType,
            out Guid? cashboxId)
        {
            customerId = Guid.Empty;
            warehouseId = Guid.Empty;
            containerId = Guid.Empty;
            paymentType = PaymentType.Cash;
            cashboxId = null;

            if (CmbCustomer.SelectedItem is not CustomerPickItem customer || customer.Id == Guid.Empty)
            {
                MockInteractionService.ShowWarning("اختر العميل.", "فاتورة بيع");
                return false;
            }

            if (CmbWarehouse.SelectedItem is not WarehousePickItem warehouse || warehouse.Id == Guid.Empty)
            {
                MockInteractionService.ShowWarning("اختر المستودع.", "فاتورة بيع");
                return false;
            }

            if (CmbCashbox.SelectedValue is not Guid selectedCashboxId || selectedCashboxId == Guid.Empty)
            {
                MockInteractionService.ShowWarning("اختر الصندوق.", "فاتورة بيع");
                return false;
            }

            customerId = customer.Id;
            warehouseId = warehouse.Id;
            containerId = CmbContainer.SelectedItem is ContainerPickItem selectedContainer ? selectedContainer.Id : Guid.Empty;
            paymentType = BtnCredit.IsChecked == true ? PaymentType.Credit : PaymentType.Cash;
            cashboxId = selectedCashboxId;
            return true;
        }

        private decimal? GetPartialPaymentAmount(PaymentType paymentType)
        {
            if (paymentType != PaymentType.Credit)
                return null;

            var raw = TxtPartialPayment.Text?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(raw) || raw == "0")
                return null;

            if (!decimal.TryParse(raw.Replace(',', '.'), System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.InvariantCulture, out var amount) || amount <= 0)
                return null;

            return amount;
        }

        private List<SalesInvoiceLineCommand> BuildLineCommands()
        {
            var commands = new List<SalesInvoiceLineCommand>();
            var lineNumber = 1;
            foreach (var row in _lines.Where(l => l.RollCount > 0))
            {
                commands.Add(new SalesInvoiceLineCommand
                {
                    LineNumber = lineNumber++,
                    ChinaContainerId = row.ChinaContainerId,
                    FabricItemId = row.FabricItemId,
                    FabricColorId = row.FabricColorId,
                    RollCount = row.RollCount,
                    UnitPrice = row.UnitPrice,
                    OriginalUnitPrice = row.OriginalUnitPrice > 0 ? row.OriginalUnitPrice : row.UnitPrice,
                    DiscountReason = row.HasDiscount && !string.IsNullOrWhiteSpace(row.DiscountReason)
                        ? row.DiscountReason.Trim()
                        : null,
                    Notes = string.IsNullOrWhiteSpace(row.Notes) ? null : row.Notes.Trim(),
                    TaxCodeId = row.TaxCodeId
                });
            }
            return commands;
        }

        private async void BtnPrint_Click(object sender, RoutedEventArgs e) =>
            await ShowInvoicePreviewAsync(exportPdf: false);

        private async void BtnPdf_Click(object sender, RoutedEventArgs e) =>
            await ShowInvoicePreviewAsync(exportPdf: true);

        private async void BtnPreview_Click(object sender, RoutedEventArgs e) =>
            await ShowInvoicePreviewAsync(exportPdf: false);

        private async Task ShowInvoicePreviewAsync(bool exportPdf)
        {
            if (_invoiceId is null || _invoiceId == Guid.Empty)
            {
                MockInteractionService.ShowWarning(
                    "احفظ الفاتورة أولاً قبل الطباعة أو التصدير.",
                    "طباعة الفاتورة");
                return;
            }
            if (!AppServices.IsInitialized) return;

            var oc = await SalesUiService.Instance.GetOperationsCenterAsync(_invoiceId.Value);
            if (!ApplicationResultPresenter.Present(oc) || oc.Value?.Invoice is null) return;
            SalesDocumentService.ShowInvoicePreview(
                oc.Value.Invoice,
                oc.Value.Invoice.CustomerName,
                exportPdf,
                oc.Value.CustomerBalance);
        }

        private void UpdateStatusBadge()
        {
            TxtStatusBadge.Text = StatusDisplay(_domainStatus);
            TxtStatusBadge.Foreground = _domainStatus switch
            {
                SalesInvoiceStatus.AwaitingDetailing => Br("WarningBrush"),
                SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed or SalesInvoiceStatus.Delivered => Br("SuccessBrush"),
                SalesInvoiceStatus.Cancelled => Br("DangerBrush"),
                _ => Br("PrimaryBrush")
            };
        }

        private static string StatusDisplay(SalesInvoiceStatus status) => status switch
        {
            SalesInvoiceStatus.Draft => "مسودة",
            SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
            SalesInvoiceStatus.Detailed => "مفصلة",
            SalesInvoiceStatus.ReadyForApproval => "جاهزة للاعتماد",
            SalesInvoiceStatus.Approved => "معتمدة",
            SalesInvoiceStatus.Printed => "مطبوعة",
            SalesInvoiceStatus.Delivered => "مسلمة",
            SalesInvoiceStatus.Cancelled => "ملغاة",
            _ => status.ToString()
        };

        private void BtnAddLine_Click(object sender, RoutedEventArgs e)
        {
            _lines.Add(new SalesInvoiceLineRow());
            RefreshSummary();
        }

        private void BtnRemoveLine_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: SalesInvoiceLineRow row })
            {
                _lines.Remove(row);
                RefreshSummary();
            }
        }

        private void ItemsGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_domainStatus != SalesInvoiceStatus.Draft)
                return;

            var dep = e.OriginalSource as DependencyObject;
            while (dep is not null and not DataGridCell)
                dep = VisualTreeHelper.GetParent(dep);

            if (dep is not DataGridCell { IsReadOnly: false, IsEditing: false } cell)
                return;

            var header = cell.Column.Header?.ToString();

            if (cell.Column is DataGridTemplateColumn && cell.Column.DisplayIndex == 0)
            {
                ItemsGrid.CurrentCell = new DataGridCellInfo(cell);
                if (!cell.IsFocused)
                    cell.Focus();
                ItemsGrid.BeginEdit(e);
                return;
            }

            if (cell.Column is DataGridComboBoxColumn ||
                (cell.Column is DataGridTemplateColumn && header is "اختر التوب" or "كود الضريبة"))
            {
                if (cell.DataContext is SalesInvoiceLineRow { StockOptions.Count: 0 })
                {
                    MockInteractionService.ShowWarning(
                        "لا يوجد مخزون لهذه الحاوية في المستودع المختار.\nتأكد من اختيار الحاوية والمستودع أولاً.",
                        "اختيار التوب");
                    return;
                }

                ItemsGrid.CurrentCell = new DataGridCellInfo(cell);
                if (!cell.IsFocused)
                    cell.Focus();
                ItemsGrid.BeginEdit(e);
                return;
            }

            if (cell.Column is not DataGridTextColumn)
                return;

            if (header is not ("عدد الأثواب" or "سعر الوحدة"))
                return;

            ItemsGrid.CurrentCell = new DataGridCellInfo(cell);
            if (!cell.IsFocused)
                cell.Focus();
            ItemsGrid.BeginEdit(e);
        }

        private void ItemsGrid_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            if (_domainStatus != SalesInvoiceStatus.Draft)
            {
                e.Cancel = true;
                return;
            }

            if (e.Column.IsReadOnly)
                e.Cancel = true;
        }

        private void ItemsGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is FabricStockAutocompleteEditor fabricEditor)
            {
                fabricEditor.AdvanceRequested -= FabricEditor_AdvanceRequested;
                fabricEditor.AdvanceRequested += FabricEditor_AdvanceRequested;
                Dispatcher.BeginInvoke(fabricEditor.FocusEditor, System.Windows.Threading.DispatcherPriority.Loaded);
                return;
            }

            if (e.EditingElement is not TextBox textBox)
                return;

            textBox.AcceptsReturn = false;
            textBox.Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            textBox.CaretBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A));
            textBox.PreviewKeyDown -= ItemsGrid_EditingTextBox_PreviewKeyDown;
            textBox.PreviewKeyDown += ItemsGrid_EditingTextBox_PreviewKeyDown;
        }

        private void FabricEditor_AdvanceRequested(object? sender, EventArgs e)
        {
            _cellEditFailed = false;
            ItemsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ItemsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            Dispatcher.BeginInvoke(MoveToNextEditableCell);
        }

        private void ItemsGrid_EditingTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;

            e.Handled = true;
            _cellEditFailed = false;
            ItemsGrid.CommitEdit(DataGridEditingUnit.Cell, true);
            ItemsGrid.CommitEdit(DataGridEditingUnit.Row, true);
            Dispatcher.BeginInvoke(MoveToNextEditableCell);
        }

        private void MoveToNextEditableCell()
        {
            if (_cellEditFailed || ItemsGrid.CurrentItem == null)
                return;

            var rowIndex = ItemsGrid.Items.IndexOf(ItemsGrid.CurrentItem);
            if (rowIndex < 0)
                return;

            var editableColumns = GetEditableColumnsForRow(ItemsGrid.CurrentItem as SalesInvoiceLineRow);
            if (editableColumns.Count == 0)
                return;

            var currentColumn = ItemsGrid.CurrentColumn;
            var currentIndex = currentColumn == null
                ? -1
                : editableColumns.FindIndex(c => c == currentColumn);

            var nextIndex = currentIndex + 1;
            if (nextIndex >= editableColumns.Count)
            {
                if (rowIndex >= _lines.Count - 1)
                {
                    _lines.Add(new SalesInvoiceLineRow());
                    RefreshSummary();
                }

                rowIndex++;
                nextIndex = 0;
            }

            var nextColumn = editableColumns[nextIndex];
            ItemsGrid.CurrentCell = new DataGridCellInfo(ItemsGrid.Items[rowIndex], nextColumn);
            ItemsGrid.Focus();
            ItemsGrid.BeginEdit();
        }

        private List<DataGridColumn> GetEditableColumnsForRow(SalesInvoiceLineRow? row)
        {
            var order = new List<string> { "الحاوية", "اختر التوب", "عدد الأثواب", "سعر الوحدة", "كود الضريبة" };

            return ItemsGrid.Columns
                .Where(c => !c.IsReadOnly && order.Contains(c.Header?.ToString() ?? ""))
                .OrderBy(c => order.IndexOf(c.Header?.ToString() ?? ""))
                .ToList();
        }

        private void ItemsGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not SalesInvoiceLineRow row)
                return;

            MarkFormDirty();
            _cellEditFailed = false;
            var header = e.Column.Header?.ToString();
            if (e.EditingElement is FabricStockAutocompleteEditor)
            {
                RefreshSummary();
                return;
            }

            if (e.EditingElement is not TextBox textBox)
            {
                RefreshSummary();
                return;
            }

            var text = textBox.Text.Trim();

            switch (header)
            {
                case "عدد الأثواب":
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var rolls) || rolls < 0)
                    {
                        e.Cancel = true;
                        _cellEditFailed = true;
                        MockInteractionService.ShowWarning("أدخل عدداً صحيحاً موجباً لعدد الأثواب.");
                        row.RollCountText = row.RollCount.ToString(CultureInfo.CurrentCulture);
                        textBox.Text = row.RollCountText;
                        return;
                    }
                    row.RollCount = rolls;
                    row.RollCountText = rolls.ToString(CultureInfo.CurrentCulture);
                    break;

                case "سعر الوحدة":
                    if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price) || price <= 0)
                    {
                        e.Cancel = true;
                        _cellEditFailed = true;
                        MockInteractionService.ShowWarning("أدخل سعر بيع أكبر من صفر.");
                        row.UnitPriceText = SalesInvoiceLineRow.FormatUnitPriceDisplay(row.UnitPrice);
                        textBox.Text = row.UnitPriceText;
                        return;
                    }
                    row.UnitPrice = price;
                    row.UnitPriceText = SalesInvoiceLineRow.FormatUnitPriceDisplay(price);
                    row.MissingSalePrice = false;
                    if (ItemsGrid.SelectedItem == row)
                        UpdateDiscountReasonEditor(row);
                    break;
            }

            Dispatcher.BeginInvoke(RefreshSummary);
        }

        private void RefreshSummary()
        {
            SummaryPills.Children.Clear();
            var groups = _lines
                .Where(l => !string.IsNullOrWhiteSpace(l.GoodsType))
                .GroupBy(l => l.GoodsType)
                .ToList();

            foreach (var g in groups)
            {
                SummaryPills.Children.Add(CreatePill(
                    $"مجموع {g.Key}: {g.Sum(x => x.RollCount)} ثوب",
                    Br("SurfaceAltBrush"), Br("TextSecondaryBrush")));
            }

            var total = _lines.Sum(l => l.RollCount);
            SummaryPills.Children.Add(CreatePill(
                $"إجمالي الأثواب: {total} ثوب",
                Br("PrimaryVeryLightBrush"), Br("PrimaryBrush"), bold: true));

            if (_loadedGrandTotal > 0)
            {
                SummaryPills.Children.Add(CreatePill(
                    $"$ {_loadedGrandTotal:N2} USD",
                    Br("SuccessBrush"), Br("White"), bold: true));
            }

            var showWarning = _lines.Any(l => l.MissingSalePrice && l.FabricItemId != Guid.Empty);
            SalePriceWarningBanner.Visibility = showWarning ? Visibility.Visible : Visibility.Collapsed;
            ScheduleTaxPreviewRefresh();
            UpdateWorkflowUi();
        }

        private static Border CreatePill(string text, System.Windows.Media.Brush bg, System.Windows.Media.Brush fg, bool bold = false)
        {
            return new Border
            {
                Background = bg,
                CornerRadius = new CornerRadius(100),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 11,
                    FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                    Foreground = fg,
                    FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Tahoma, Arial")
                }
            };
        }

        private static System.Windows.Media.Brush Br(string key) =>
            (System.Windows.Media.Brush)System.Windows.Application.Current.Resources[key]!;
    }
}
