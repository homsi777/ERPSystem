using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Catalog;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryTransferFormControl : UserControl
{
    private readonly ComboBox _fromWarehouse = new() { MinWidth = 220, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly ComboBox _toWarehouse = new() { MinWidth = 220, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly TextBlock _status = new() { Margin = new Thickness(0, 8, 0, 0), FontWeight = FontWeights.SemiBold };
    private readonly DataGrid _lines = new() { AutoGenerateColumns = false, CanUserAddRows = false, MinHeight = 180 };
    private readonly ObservableCollection<TransferLineRow> _lineRows = [];
    private readonly ComboBox _fabric = new() { MinWidth = 180, DisplayMemberPath = nameof(FabricItem.NameAr) };
    private readonly ComboBox _color = new() { MinWidth = 120, DisplayMemberPath = nameof(FabricColor.NameAr) };
    private readonly TextBox _meters = ErpUiFactory.FormField("0");
    private readonly TextBox _rolls = ErpUiFactory.FormField("1");
    private List<FabricItem> _fabrics = [];
    private Guid? _transferId;

    public InventoryTransferFormControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("مناقلة مخزنية"));

        stack.Children.Add(ErpUiFactory.BuildFormGrid(
            ("من مستودع *", _fromWarehouse),
            ("إلى مستودع *", _toWarehouse),
            ("ملاحظات", _notes)));
        stack.Children.Add(_status);

        ErpUiFactory.AddGridColumn(_lines, "القماش", nameof(TransferLineRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_lines, "اللون", nameof(TransferLineRow.ColorName), 100, null);
        ErpUiFactory.AddGridColumn(_lines, "الأمتار", nameof(TransferLineRow.QuantityMeters), 90, null);
        ErpUiFactory.AddGridColumn(_lines, "Rolls", nameof(TransferLineRow.RollCount), 70, null);
        _lines.ItemsSource = _lineRows;
        stack.Children.Add(_lines);

        var addRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
        _fabric.SelectionChanged += async (_, _) => await LoadColorsAsync();
        addRow.Children.Add(_fabric);
        addRow.Children.Add(_color);
        addRow.Children.Add(_meters);
        addRow.Children.Add(_rolls);
        var addBtn = new Button { Content = "إضافة سطر", Margin = new Thickness(8, 0, 0, 0) };
        addBtn.Click += (_, _) => AddLine();
        addRow.Children.Add(addBtn);
        stack.Children.Add(addRow);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var saveBtn = new Button { Content = "حفظ المناقلة", Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!, Margin = new Thickness(0, 0, 8, 0) };
        saveBtn.Click += async (_, _) => await SaveAsync();
        var completeBtn = new Button { Content = "إكمال المناقلة", Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!, Margin = new Thickness(0, 0, 8, 0) };
        completeBtn.Click += async (_, _) => await CompleteAsync();
        actions.Children.Add(saveBtn);
        actions.Children.Add(completeBtn);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        if (!AppServices.IsInitialized) return;
        _transferId = InventoryNavigationContext.EditTransferId;

        var wh = await InventoryUiService.Instance.GetWarehousesAsync();
        if (wh.IsSuccess && wh.Value is not null)
        {
            _fromWarehouse.ItemsSource = wh.Value;
            _toWarehouse.ItemsSource = wh.Value;
            var pre = InventoryNavigationContext.TakePreselectedFromWarehouse();
            if (pre.HasValue)
                _fromWarehouse.SelectedItem = wh.Value.FirstOrDefault(w => w.Id == pre.Value);
        }

        var fabrics = await InventoryUiService.Instance.GetFabricCatalogAsync();
        if (fabrics.IsSuccess && fabrics.Value is not null)
        {
            _fabrics = fabrics.Value.ToList();
            _fabric.ItemsSource = _fabrics;
        }

        _status.Text = _transferId.HasValue
            ? $"مناقلة موجودة — يمكن إكمالها بعد الشحن"
            : "مسودة جديدة — أضف الأسطر ثم احفظ";
    }

    private async Task LoadColorsAsync()
    {
        if (_fabric.SelectedItem is not FabricItem item) { _color.ItemsSource = null; return; }
        var colors = await InventoryUiService.Instance.GetFabricColorsAsync(item.Id);
        _color.ItemsSource = colors.IsSuccess ? colors.Value : [];
    }

    private void AddLine()
    {
        if (_fabric.SelectedItem is not FabricItem fabric || _color.SelectedItem is not FabricColor color)
        {
            MessageBox.Show("اختر القماش واللون.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(_meters.Text, out var m) || m <= 0)
        {
            MessageBox.Show("أدخل كمية أمتار صحيحة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(_rolls.Text, out var r) || r <= 0) r = 1;

        _lineRows.Add(new TransferLineRow
        {
            FabricItemId = fabric.Id,
            FabricColorId = color.Id,
            FabricName = fabric.NameAr,
            ColorName = color.NameAr,
            QuantityMeters = m,
            RollCount = r
        });
    }

    private async Task SaveAsync()
    {
        if (_fromWarehouse.SelectedItem is not WarehouseListExtendedDto from ||
            _toWarehouse.SelectedItem is not WarehouseListExtendedDto to)
        {
            MessageBox.Show("اختر المستودعات.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (from.Id == to.Id)
        {
            MessageBox.Show("لا يمكن المناقلة لنفس المستودع.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_lineRows.Count == 0)
        {
            MessageBox.Show("أضف سطراً واحداً على الأقل.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var lines = _lineRows.Select(l => new StockTransferLineCommand(
            l.FabricItemId, l.FabricColorId, l.QuantityMeters, l.RollCount)).ToList();

        var result = await InventoryUiService.Instance.CreateTransferAsync(new CreateStockTransferCommand(
            Guid.Empty, from.Id, to.Id, null, null, _notes.Text.Trim(), lines));

        if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
        {
            _transferId = result.Value;
            _status.Text = "تم إنشاء المناقلة — يمكنك إكمالها الآن";
            InventoryListRefreshHub.RequestRefresh();
        }
    }

    private async Task CompleteAsync()
    {
        if (!_transferId.HasValue)
        {
            MessageBox.Show("احفظ المناقلة أولاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var result = await InventoryUiService.Instance.CompleteTransferAsync(_transferId.Value);
        if (ApplicationResultPresenter.Present(result))
        {
            InventoryListRefreshHub.RequestRefresh();
            NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "Transfers");
        }
    }

    private sealed class TransferLineRow
    {
        public Guid FabricItemId { get; init; }
        public Guid FabricColorId { get; init; }
        public string FabricName { get; init; } = "";
        public string ColorName { get; init; } = "";
        public decimal QuantityMeters { get; init; }
        public int RollCount { get; init; }
    }
}
