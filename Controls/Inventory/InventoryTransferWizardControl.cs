using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryTransferWizardControl : UserControl
{
    private int _step = 1;
    private Guid? _transferId;
    private List<WarehouseListExtendedDto> _warehouses = [];

    private readonly TextBlock _stepTitle = new() { FontSize = 16, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 8) };
    private readonly TextBlock _stepHint = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 12), Foreground = Br("TextSecondaryBrush") };
    private readonly StackPanel _stepHost = new();
    private readonly TextBlock _footer = new() { Margin = new Thickness(0, 12, 0, 0), FontSize = 12, Foreground = Br("TextMutedBrush") };

    private readonly ComboBox _fromWarehouse = new() { MinWidth = 240, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly ComboBox _toWarehouse = new() { MinWidth = 240, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly TextBox _notes = ErpUiFactory.FormField("");

    private readonly DataGrid _rollsGrid = new() { AutoGenerateColumns = false, CanUserAddRows = false, MinHeight = 220, SelectionMode = DataGridSelectionMode.Extended };
    private readonly ObservableCollection<RollPickRow> _rollRows = [];

    private readonly DataGrid _qtyGrid = new() { AutoGenerateColumns = false, CanUserAddRows = false, MinHeight = 200 };
    private readonly ObservableCollection<QtyRow> _qtyRows = [];

    private readonly StackPanel _previewPanel = new();

    private readonly Button _backBtn = new() { Content = "السابق", Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button _nextBtn = new() { Content = "التالي", Margin = new Thickness(0, 0, 8, 0) };
    private readonly Button _finishBtn = new() { Content = "إكمال المناقلة", Visibility = Visibility.Collapsed };

    public InventoryTransferWizardControl()
    {
        var root = new DockPanel();
        var header = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        header.Children.Add(BuildStepIndicator());
        header.Children.Add(_stepTitle);
        header.Children.Add(_stepHint);
        DockPanel.SetDock(header, Dock.Top);
        root.Children.Add(header);

        root.Children.Add(_stepHost);
        DockPanel.SetDock(_footer, Dock.Bottom);
        root.Children.Add(_footer);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        _backBtn.Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!;
        _nextBtn.Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!;
        _finishBtn.Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!;
        _backBtn.Click += (_, _) => GoBack();
        _nextBtn.Click += async (_, _) => await GoNextAsync();
        _finishBtn.Click += async (_, _) => await FinishAsync();
        actions.Children.Add(_backBtn);
        actions.Children.Add(_nextBtn);
        actions.Children.Add(_finishBtn);
        DockPanel.SetDock(actions, Dock.Bottom);
        root.Children.Add(actions);

        Content = root;
        Loaded += async (_, _) => await InitAsync();
        RenderStep();
    }

    private UIElement BuildStepIndicator()
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        for (var i = 1; i <= 5; i++)
        {
            var n = i;
            var pill = new Border
            {
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(10, 4, 10, 4),
                Margin = new Thickness(0, 0, 6, 0),
                Background = Br("SurfaceAltBrush"),
                Child = new TextBlock { Text = $"خطوة {n}", FontSize = 11 }
            };
            pill.Tag = n;
            sp.Children.Add(pill);
        }
        return sp;
    }

    private async Task InitAsync()
    {
        if (!AppServices.IsInitialized) return;
        _transferId = InventoryNavigationContext.EditTransferId;

        var wh = await InventoryUiService.Instance.GetWarehousesAsync();
        if (wh.IsSuccess && wh.Value is not null)
        {
            _warehouses = wh.Value.ToList();
            _fromWarehouse.ItemsSource = _warehouses;
            _toWarehouse.ItemsSource = _warehouses;
            var pre = InventoryNavigationContext.TakePreselectedFromWarehouse();
            if (pre.HasValue)
                _fromWarehouse.SelectedItem = _warehouses.FirstOrDefault(w => w.Id == pre.Value);
        }

        if (_transferId.HasValue)
        {
            var detail = await InventoryUiService.Instance.GetTransferDetailAsync(_transferId.Value);
            if (detail.IsSuccess && detail.Value is not null)
            {
                _fromWarehouse.SelectedItem = _warehouses.FirstOrDefault(w => w.Id == detail.Value.FromWarehouseId);
                _toWarehouse.SelectedItem = _warehouses.FirstOrDefault(w => w.Id == detail.Value.ToWarehouseId);
                _notes.Text = detail.Value.Notes ?? "";
                _step = 5;
                RenderStep();
            }
        }
    }

    private void RenderStep()
    {
        _stepHost.Children.Clear();
        _backBtn.IsEnabled = _step > 1 && _step < 5;
        _nextBtn.Visibility = _step < 5 ? Visibility.Visible : Visibility.Collapsed;
        _finishBtn.Visibility = _step >= 5 ? Visibility.Visible : Visibility.Collapsed;

        switch (_step)
        {
            case 1:
                _stepTitle.Text = "١ — اختيار المستودعات";
                _stepHint.Text = "حدد مستودع المصدر والوجهة. يجب أن يكونا مختلفين.";
                _stepHost.Children.Add(ErpUiFactory.BuildFormGrid(
                    ("من مستودع *", _fromWarehouse),
                    ("إلى مستودع *", _toWarehouse),
                    ("ملاحظات", _notes)));
                break;
            case 2:
                _stepTitle.Text = "٢ — المخزون المتاح في المصدر";
                _stepHint.Text = "يُعرض فقط Rolls المتاحة فعلياً في مستودع المصدر.";
                BuildRollsGrid();
                _stepHost.Children.Add(_rollsGrid);
                break;
            case 3:
                _stepTitle.Text = "٣ — تحديد الكميات";
                _stepHint.Text = "عدّل الأمتار المراد نقلها. لا يمكن تجاوز المتاح.";
                BuildQtyGrid();
                _stepHost.Children.Add(_qtyGrid);
                break;
            case 4:
                _stepTitle.Text = "٤ — معاينة المناقلة";
                _stepHint.Text = "راجع Rolls والأمتار والقيمة قبل الاعتماد.";
                BuildPreview();
                _stepHost.Children.Add(_previewPanel);
                break;
            case 5:
                _stepTitle.Text = "٥ — اعتماد وإكمال";
                _stepHint.Text = _transferId.HasValue
                    ? "المناقلة جاهزة — اعتمد ثم أكمل لتحديث المخزون."
                    : "احفظ المناقلة ثم اعتمد وأكمل.";
                BuildPreview();
                _stepHost.Children.Add(_previewPanel);
                break;
        }

        _footer.Text = $"الخطوة {_step} من 5";
    }

    private void BuildRollsGrid()
    {
        _rollsGrid.Columns.Clear();
        _rollsGrid.ItemsSource = _rollRows;
        ErpUiFactory.AddGridColumn(_rollsGrid, "Roll", nameof(RollPickRow.RollNumber), 60, null);
        ErpUiFactory.AddGridColumn(_rollsGrid, "القماش", nameof(RollPickRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_rollsGrid, "اللون", nameof(RollPickRow.ColorName), 90, null);
        ErpUiFactory.AddGridColumn(_rollsGrid, "Batch", nameof(RollPickRow.BatchNumber), 80, null);
        ErpUiFactory.AddGridColumn(_rollsGrid, "الموقع", nameof(RollPickRow.LocationCode), 80, null);
        ErpUiFactory.AddGridColumn(_rollsGrid, "متاح (م)", nameof(RollPickRow.RemainingLengthMeters), 90, null);
        ErpUiFactory.AddGridColumn(_rollsGrid, "القيمة $", nameof(RollPickRow.CurrentValue), 90, null);
    }

    private void BuildQtyGrid()
    {
        _qtyGrid.Columns.Clear();
        _qtyGrid.IsReadOnly = false;
        _qtyGrid.ItemsSource = _qtyRows;
        ErpUiFactory.AddGridColumn(_qtyGrid, "Roll", nameof(QtyRow.RollNumber), 60, null);
        ErpUiFactory.AddGridColumn(_qtyGrid, "القماش", nameof(QtyRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_qtyGrid, "متاح", nameof(QtyRow.MaxMeters), 80, "N2");
        ErpUiFactory.AddGridColumn(_qtyGrid, "نقل (م)", nameof(QtyRow.TransferMeters), 100, "N2");
    }

    private void BuildPreview()
    {
        _previewPanel.Children.Clear();
        var from = _fromWarehouse.SelectedItem as WarehouseListExtendedDto;
        var to = _toWarehouse.SelectedItem as WarehouseListExtendedDto;
        var lines = _qtyRows.Where(r => r.TransferMeters > 0).ToList();
        var totalM = lines.Sum(l => l.TransferMeters);
        var totalV = WpfGeneralManagerAccess.CanViewSensitivePricing
            ? lines.Sum(l => l.TransferMeters * l.CostPerMeter)
            : 0m;
        var previewSummary = WpfGeneralManagerAccess.CanViewSensitivePricing
            ? $"Rolls: {lines.Count}  •  الأمتار: {totalM:N2}  •  القيمة: ${totalV:N2}"
            : $"Rolls: {lines.Count}  •  الأمتار: {totalM:N2}";

        _previewPanel.Children.Add(new TextBlock
        {
            Text = $"من: {from?.NameAr ?? "—"}  →  إلى: {to?.NameAr ?? "—"}",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });
        _previewPanel.Children.Add(new TextBlock
        {
            Text = previewSummary,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var grid = new DataGrid { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 240, ItemsSource = lines };
        ErpUiFactory.AddGridColumn(grid, "Roll", nameof(QtyRow.RollNumber), 60, null);
        ErpUiFactory.AddGridColumn(grid, "القماش", nameof(QtyRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(grid, "أمتار", nameof(QtyRow.TransferMeters), 90, null);
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
            ErpUiFactory.AddGridColumn(grid, "قيمة $", nameof(QtyRow.LineValue), 90, null);
        _previewPanel.Children.Add(grid);
    }

    private async Task LoadRollsAsync()
    {
        if (_fromWarehouse.SelectedItem is not WarehouseListExtendedDto from) return;
        _rollRows.Clear();
        var result = await InventoryUiService.Instance.GetTransferableRollsAsync(from.Id);
        if (!result.IsSuccess || result.Value is null) return;
        foreach (var r in result.Value)
            _rollRows.Add(new RollPickRow(r));
        _rollsGrid.ItemsSource = _rollRows;
    }

    private void GoBack()
    {
        if (_step <= 1) return;
        _step--;
        RenderStep();
    }

    private async Task GoNextAsync()
    {
        if (_step == 1)
        {
            if (_fromWarehouse.SelectedItem is not WarehouseListExtendedDto from ||
                _toWarehouse.SelectedItem is not WarehouseListExtendedDto to)
            {
                MessageBox.Show("اختر المستودعات.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (from.Id == to.Id)
            {
                MessageBox.Show("المستودع المصدر والوجهة يجب أن يكونا مختلفين.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            await LoadRollsAsync();
            if (_rollRows.Count == 0)
            {
                MessageBox.Show("لا يوجد مخزون متاح في مستودع المصدر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
        }
        else if (_step == 2)
        {
            var selected = _rollsGrid.SelectedItems.Cast<RollPickRow>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("اختر Roll واحداً على الأقل.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            _qtyRows.Clear();
            foreach (var s in selected)
                _qtyRows.Add(new QtyRow(s));
        }
        else if (_step == 3)
        {
            _qtyGrid.CommitEdit(DataGridEditingUnit.Row, true);
            if (_qtyRows.All(r => r.TransferMeters <= 0))
            {
                MessageBox.Show("حدد كمية نقل لسطر واحد على الأقل.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            foreach (var row in _qtyRows.Where(r => r.TransferMeters > 0))
            {
                if (row.TransferMeters > row.MaxMeters)
                {
                    MessageBox.Show($"Roll {row.RollNumber}: الكمية تتجاوز المتاح.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
        }
        else if (_step == 4)
        {
            if (!_transferId.HasValue)
            {
                var saved = await SaveTransferAsync();
                if (!saved) return;
            }
        }

        _step++;
        RenderStep();
    }

    private async Task<bool> SaveTransferAsync()
    {
        if (_fromWarehouse.SelectedItem is not WarehouseListExtendedDto from ||
            _toWarehouse.SelectedItem is not WarehouseListExtendedDto to)
            return false;

        var lines = _qtyRows
            .Where(r => r.TransferMeters > 0)
            .Select(r => new StockTransferLineCommand(
                r.FabricItemId, r.FabricColorId, r.TransferMeters, 1, r.RollId))
            .ToList();

        if (lines.Count == 0)
        {
            MessageBox.Show("لا توجد أسطر للحفظ.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        var result = await InventoryUiService.Instance.CreateTransferAsync(new CreateStockTransferCommand(
            Guid.Empty, from.Id, to.Id, null, null, _notes.Text.Trim(), lines));

        if (!ApplicationResultPresenter.Present(result) || !result.IsSuccess)
            return false;

        _transferId = result.Value;
        InventoryListRefreshHub.RequestRefresh();
        return true;
    }

    private async Task FinishAsync()
    {
        if (!_transferId.HasValue)
        {
            if (!await SaveTransferAsync()) return;
        }

        var approve = await InventoryUiService.Instance.ApproveTransferAsync(_transferId!.Value);
        if (!ApplicationResultPresenter.Present(approve) || !approve.IsSuccess)
            return;

        var complete = await InventoryUiService.Instance.CompleteTransferAsync(_transferId.Value);
        if (ApplicationResultPresenter.Present(complete) && complete.IsSuccess)
        {
            InventoryListRefreshHub.RequestRefresh();
            InventoryPopupService.CompleteSuccess();
        }
    }

    private static Brush Br(string key) =>
        (Brush)System.Windows.Application.Current.Resources[key]!;

    private sealed class RollPickRow
    {
        public RollPickRow(WarehouseTransferRollDto r)
        {
            RollId = r.Id;
            FabricItemId = r.FabricItemId;
            FabricColorId = r.FabricColorId;
            RollNumber = r.RollNumber;
            FabricName = r.FabricName;
            ColorName = r.ColorName;
            BatchNumber = r.BatchNumber ?? "—";
            LocationCode = r.LocationCode ?? "—";
            RemainingLengthMeters = r.RemainingLengthMeters;
            CurrentValue = r.CurrentValue;
            CostPerMeter = r.CostPerMeter;
        }

        public Guid RollId { get; }
        public Guid FabricItemId { get; }
        public Guid FabricColorId { get; }
        public int RollNumber { get; }
        public string FabricName { get; }
        public string ColorName { get; }
        public string BatchNumber { get; }
        public string LocationCode { get; }
        public decimal RemainingLengthMeters { get; }
        public decimal CurrentValue { get; }
        public decimal CostPerMeter { get; }
    }

    private sealed class QtyRow : INotifyPropertyChanged
    {
        public QtyRow(RollPickRow r)
        {
            RollId = r.RollId;
            FabricItemId = r.FabricItemId;
            FabricColorId = r.FabricColorId;
            RollNumber = r.RollNumber;
            FabricName = r.FabricName;
            MaxMeters = r.RemainingLengthMeters;
            TransferMeters = r.RemainingLengthMeters;
            CostPerMeter = r.CostPerMeter;
        }

        public Guid RollId { get; }
        public Guid FabricItemId { get; }
        public Guid FabricColorId { get; }
        public int RollNumber { get; }
        public string FabricName { get; }
        public decimal MaxMeters { get; }
        public decimal CostPerMeter { get; }

        private decimal _transferMeters;
        public decimal TransferMeters
        {
            get => _transferMeters;
            set
            {
                _transferMeters = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TransferMeters)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LineValue)));
            }
        }

        public decimal LineValue => TransferMeters * CostPerMeter;
        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
