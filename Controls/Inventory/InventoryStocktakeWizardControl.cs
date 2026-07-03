using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryStocktakeWizardControl : UserControl
{
    private int _phase; // 0=draft, 1=counting, 2=review, 3=posted
    private Guid? _sessionId;
    private List<WarehouseListExtendedDto> _warehouses = [];

    private readonly ComboBox _warehouse = new() { MinWidth = 240, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly TextBox _responsible = ErpUiFactory.FormField("");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly TextBlock _statusBanner = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 8, 0, 8), Padding = new Thickness(12), Background = Br("SurfaceAltBrush") };
    private readonly TextBlock _kpiBlock = new() { Margin = new Thickness(0, 0, 0, 12), FontWeight = FontWeights.SemiBold };
    private readonly DataGrid _linesGrid = new() { AutoGenerateColumns = false, MinHeight = 280 };
    private readonly ObservableCollection<CountRow> _lines = [];

    private readonly Button _primaryBtn = new() { Margin = new Thickness(0, 16, 8, 0), MinWidth = 140 };
    private readonly Button _postBtn = new() { Content = "ترحيل الجرد", Margin = new Thickness(0, 16, 0, 0), MinWidth = 140, Visibility = Visibility.Collapsed };

    public InventoryStocktakeWizardControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("جرد مخزني"));
        stack.Children.Add(ErpUxFactory.InfoBanner("مسودة → تجميد/عد → مراجعة الفروقات → ترحيل تلقائي للتعديلات."));
        stack.Children.Add(_statusBanner);
        stack.Children.Add(ErpUiFactory.BuildFormGrid(
            ("المستودع *", _warehouse),
            ("المسؤول *", _responsible),
            ("ملاحظات", _notes)));
        stack.Children.Add(_kpiBlock);
        stack.Children.Add(_linesGrid);

        _primaryBtn.Style = (Style)Application.Current.Resources["PrimaryButtonStyle"]!;
        _postBtn.Style = (Style)Application.Current.Resources["SecondaryButtonStyle"]!;
        _primaryBtn.Click += async (_, _) => await OnPrimaryAsync();
        _postBtn.Click += async (_, _) => await PostAsync();

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(_primaryBtn);
        actions.Children.Add(_postBtn);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Loaded += async (_, _) => await InitAsync();
        UpdatePhaseUi();
    }

    private async Task InitAsync()
    {
        if (!AppServices.IsInitialized) return;
        _sessionId = InventoryNavigationContext.EditStocktakeId;

        var wh = await InventoryUiService.Instance.GetWarehousesAsync();
        if (wh.IsSuccess && wh.Value is not null)
        {
            _warehouses = wh.Value.ToList();
            _warehouse.ItemsSource = _warehouses;
            var pre = InventoryNavigationContext.TakePreselectedStocktakeWarehouse();
            if (pre.HasValue)
                _warehouse.SelectedItem = _warehouses.FirstOrDefault(w => w.Id == pre.Value);
        }

        if (_sessionId.HasValue)
            await LoadSessionAsync(_sessionId.Value);
    }

    private async Task LoadSessionAsync(Guid sessionId)
    {
        var detail = await InventoryUiService.Instance.GetStocktakeDetailAsync(sessionId);
        if (!detail.IsSuccess || detail.Value is null) return;

        var d = detail.Value;
        _sessionId = d.Id;
        _warehouse.SelectedItem = _warehouses.FirstOrDefault(w => w.Id == d.WarehouseId);
        _responsible.Text = d.Responsible;
        _notes.Text = d.Notes ?? "";
        _warehouse.IsEnabled = false;
        _responsible.IsEnabled = false;

        _lines.Clear();
        foreach (var line in d.Lines)
            _lines.Add(new CountRow(line));

        BuildLinesGrid();
        _phase = d.Status is "Posted" or "Completed" or "Closed" ? 3
            : d.Lines.Count > 0 ? 2 : 1;
        UpdatePhaseUi();
        RefreshKpis();
    }

    private void BuildLinesGrid()
    {
        _linesGrid.Columns.Clear();
        _linesGrid.ItemsSource = _lines;
        _linesGrid.IsReadOnly = _phase >= 3;
        ErpUiFactory.AddGridColumn(_linesGrid, "Roll", nameof(CountRow.RollNumber), 60, null);
        ErpUiFactory.AddGridColumn(_linesGrid, "القماش", nameof(CountRow.FabricName), "*", null);
        ErpUiFactory.AddGridColumn(_linesGrid, "اللون", nameof(CountRow.ColorName), 90, null);
        ErpUiFactory.AddGridColumn(_linesGrid, "نظام (م)", nameof(CountRow.SystemMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(_linesGrid, "عد (م)", nameof(CountRow.CountedMeters), 90, "N2");
        ErpUiFactory.AddGridColumn(_linesGrid, "فرق", nameof(CountRow.DifferenceMeters), 80, "N2");
    }

    private void UpdatePhaseUi()
    {
        _statusBanner.Text = _phase switch
        {
            0 => "المرحلة: مسودة — أنشئ الجلسة لتجميد قائمة Rolls للعد.",
            1 => "المرحلة: عد — أدخل الكميات المعدودة لكل Roll.",
            2 => "المرحلة: مراجعة — راجع الفروقات ثم احفظ أو رحّل.",
            _ => "المرحلة: مُرحّل — تم تحديث المخزون."
        };

        _primaryBtn.Content = _phase switch
        {
            0 => "بدء الجرد (تجميد)",
            1 => "حفظ العد",
            2 => "مراجعة محفوظة",
            _ => "إغلاق"
        };

        _primaryBtn.IsEnabled = _phase < 3;
        _postBtn.Visibility = _phase == 2 ? Visibility.Visible : Visibility.Collapsed;
        _warehouse.IsEnabled = _phase == 0;
        _responsible.IsEnabled = _phase == 0;
        _linesGrid.IsReadOnly = _phase != 1 && _phase != 2;
    }

    private void RefreshKpis()
    {
        var variance = _lines.Sum(l => l.DifferenceMeters);
        var withVar = _lines.Count(l => l.DifferenceMeters != 0);
        _kpiBlock.Text = $"أسطر: {_lines.Count}  •  نظام: {_lines.Sum(l => l.SystemMeters):N2} م  •  عد: {_lines.Sum(l => l.CountedMeters):N2} م  •  فرق: {variance:N2} م  •  فروقات: {withVar}";
    }

    private async Task OnPrimaryAsync()
    {
        if (_phase == 0)
            await CreateSessionAsync();
        else if (_phase == 1)
            await SaveCountsAsync();
        else if (_phase == 2)
            RefreshKpis();
        else
            InventoryPopupService.CompleteSuccess();
    }

    private async Task CreateSessionAsync()
    {
        if (_warehouse.SelectedItem is not WarehouseListExtendedDto wh)
        {
            MessageBox.Show("اختر المستودع.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(_responsible.Text))
        {
            MessageBox.Show("اسم المسؤول مطلوب.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = await InventoryUiService.Instance.CreateStocktakeAsync(new CreateStocktakeCommand(
            Guid.Empty, wh.Id, _responsible.Text.Trim(), Notes: _notes.Text.Trim()));

        if (!ApplicationResultPresenter.Present(result) || !result.IsSuccess)
            return;

        _sessionId = result.Value;
        _warehouse.IsEnabled = false;
        _responsible.IsEnabled = false;
        await LoadSessionAsync(_sessionId.Value);
        _phase = 1;
        BuildLinesGrid();
        UpdatePhaseUi();
        InventoryListRefreshHub.RequestRefresh();
    }

    private async Task SaveCountsAsync()
    {
        if (!_sessionId.HasValue) return;
        _linesGrid.CommitEdit(DataGridEditingUnit.Row, true);

        var cmd = new UpdateStocktakeLinesCommand(
            _sessionId.Value,
            _lines.Select(l => new StocktakeLineCountCommand(l.LineId, l.CountedMeters)).ToList());

        var result = await InventoryUiService.Instance.UpdateStocktakeLinesAsync(cmd);
        if (!ApplicationResultPresenter.Present(result) || !result.IsSuccess)
            return;

        foreach (var row in _lines)
            row.RefreshDifference();

        _phase = 2;
        UpdatePhaseUi();
        RefreshKpis();
        InventoryListRefreshHub.RequestRefresh();
    }

    private async Task PostAsync()
    {
        if (!_sessionId.HasValue) return;

        if (_lines.Any(l => l.DifferenceMeters != 0) &&
            MessageBox.Show(
                $"سيتم ترحيل {_lines.Count(l => l.DifferenceMeters != 0)} فرق(ات) كحركات تعديل. متابعة؟",
                "ترحيل الجرد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        var result = await InventoryUiService.Instance.PostStocktakeAsync(_sessionId.Value);
        if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
        {
            _phase = 3;
            UpdatePhaseUi();
            InventoryListRefreshHub.RequestRefresh();
            MessageBox.Show("تم ترحيل الجرد وتحديث المخزون.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private static Brush Br(string key) =>
        (Brush)Application.Current.Resources[key]!;

    private sealed class CountRow : INotifyPropertyChanged
    {
        public CountRow(StocktakeLineDto line)
        {
            LineId = line.Id;
            RollNumber = line.RollNumber;
            FabricName = line.FabricName;
            ColorName = line.ColorName;
            SystemMeters = line.SystemMeters;
            _countedMeters = line.CountedMeters;
        }

        public Guid LineId { get; }
        public int RollNumber { get; }
        public string FabricName { get; }
        public string ColorName { get; }
        public decimal SystemMeters { get; }

        private decimal _countedMeters;
        public decimal CountedMeters
        {
            get => _countedMeters;
            set
            {
                _countedMeters = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CountedMeters)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DifferenceMeters)));
            }
        }

        public decimal DifferenceMeters => CountedMeters - SystemMeters;

        public void RefreshDifference() =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DifferenceMeters)));

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
