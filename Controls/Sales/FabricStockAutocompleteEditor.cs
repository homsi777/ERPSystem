using ERPSystem.Application.DTOs.Sales;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales;

/// <summary>Editable dropdown — click to open, type to filter, pick with mouse or keyboard.</summary>
public sealed class FabricStockAutocompleteEditor : UserControl
{
    public static readonly DependencyProperty StockOptionsProperty =
        DependencyProperty.Register(
            nameof(StockOptions),
            typeof(IEnumerable<SalesWarehouseStockOptionDto>),
            typeof(FabricStockAutocompleteEditor),
            new PropertyMetadata(null, OnStockOptionsChanged));

    public static readonly DependencyProperty SelectedStockProperty =
        DependencyProperty.Register(
            nameof(SelectedStock),
            typeof(SalesWarehouseStockOptionDto),
            typeof(FabricStockAutocompleteEditor),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedStockChanged));

    private readonly ComboBox _combo;
    private readonly ObservableCollection<SalesWarehouseStockOptionDto> _filtered = new();
    private List<SalesWarehouseStockOptionDto> _all = [];
    private TextBox? _innerTextBox;
    private bool _suppressSelectionSync;

    public event EventHandler? AdvanceRequested;

    public IEnumerable<SalesWarehouseStockOptionDto>? StockOptions
    {
        get => (IEnumerable<SalesWarehouseStockOptionDto>?)GetValue(StockOptionsProperty);
        set => SetValue(StockOptionsProperty, value);
    }

    public SalesWarehouseStockOptionDto? SelectedStock
    {
        get => (SalesWarehouseStockOptionDto?)GetValue(SelectedStockProperty);
        set => SetValue(SelectedStockProperty, value);
    }

    public FabricStockAutocompleteEditor()
    {
        _combo = new ComboBox
        {
            IsEditable = true,
            IsTextSearchEnabled = false,
            StaysOpenOnEdit = true,
            MaxDropDownHeight = 260,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(6, 0, 6, 0),
            VerticalContentAlignment = VerticalAlignment.Center,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            ItemsSource = _filtered,
            DisplayMemberPath = nameof(SalesWarehouseStockOptionDto.Display)
        };

        Content = _combo;

        _combo.Loaded += (_, _) => HookInnerTextBox();
        _combo.GotFocus += (_, _) => OpenDropDown();
        _combo.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (!_combo.IsDropDownOpen)
            {
                OpenDropDown();
                e.Handled = false;
            }
        };

        _combo.SelectionChanged += (_, _) =>
        {
            if (_suppressSelectionSync)
                return;

            if (_combo.SelectedItem is SalesWarehouseStockOptionDto pick)
                ApplyPick(pick, closeDropdown: true);
        };

        _combo.PreviewKeyDown += OnComboPreviewKeyDown;
    }

    public void FocusEditor()
    {
        _combo.Focus();
        if (_innerTextBox is not null)
        {
            _innerTextBox.Focus();
            _innerTextBox.SelectAll();
        }
        OpenDropDown();
    }

    private static void OnStockOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FabricStockAutocompleteEditor editor)
        {
            editor._all = editor.StockOptions?.ToList() ?? [];
            editor.Filter(editor._innerTextBox?.Text ?? editor._combo.Text);
        }
    }

    private static void OnSelectedStockChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FabricStockAutocompleteEditor editor)
            editor.SyncFromSelectedStock();
    }

    private void HookInnerTextBox()
    {
        _combo.ApplyTemplate();
        _innerTextBox = _combo.Template.FindName("PART_EditableTextBox", _combo) as TextBox;
        if (_innerTextBox is null)
            return;

        _innerTextBox.TextChanged += (_, _) =>
        {
            if (!_suppressSelectionSync)
                Filter(_innerTextBox.Text);
        };

        _innerTextBox.GotFocus += (_, _) => OpenDropDown();
    }

    private void SyncFromSelectedStock()
    {
        _suppressSelectionSync = true;
        try
        {
            if (SelectedStock is null)
            {
                _combo.SelectedItem = null;
                _combo.Text = "";
                return;
            }

            _combo.SelectedItem = SelectedStock;
            _combo.Text = SelectedStock.FabricDisplayName;
        }
        finally
        {
            _suppressSelectionSync = false;
        }
    }

    private void Filter(string? text)
    {
        _filtered.Clear();
        if (_all.Count == 0)
        {
            _combo.IsDropDownOpen = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            foreach (var option in _all.Take(40))
                _filtered.Add(option);
        }
        else
        {
            var term = text.Trim();
            foreach (var option in _all.Where(o =>
                         o.Display.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                         o.FabricDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                         o.FabricCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                         o.ColorDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase)))
                _filtered.Add(option);
        }

        if (_filtered.Count > 0 && (_combo.IsKeyboardFocusWithin || _combo.IsDropDownOpen))
            _combo.IsDropDownOpen = true;
    }

    private void OpenDropDown()
    {
        Filter(_innerTextBox?.Text ?? _combo.Text);
        if (_filtered.Count > 0)
            _combo.IsDropDownOpen = true;
    }

    private void OnComboPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitCurrent(closeDropdown: true, advance: true);
            return;
        }

        if (e.Key == Key.Escape)
        {
            _combo.IsDropDownOpen = false;
            e.Handled = true;
        }
    }

    private void CommitCurrent(bool closeDropdown, bool advance)
    {
        var pick = _combo.SelectedItem as SalesWarehouseStockOptionDto ?? FindBestMatch(_combo.Text);
        if (pick is null)
            return;

        ApplyPick(pick, closeDropdown);
        if (advance)
            AdvanceRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ApplyPick(SalesWarehouseStockOptionDto pick, bool closeDropdown)
    {
        _suppressSelectionSync = true;
        try
        {
            SelectedStock = pick;
            _combo.SelectedItem = pick;
            _combo.Text = pick.FabricDisplayName;
        }
        finally
        {
            _suppressSelectionSync = false;
        }

        if (closeDropdown)
            _combo.IsDropDownOpen = false;
    }

    private SalesWarehouseStockOptionDto? FindBestMatch(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return _filtered.FirstOrDefault();

        var term = text.Trim();
        return _all.FirstOrDefault(o =>
                   o.FabricDisplayName.Equals(term, StringComparison.OrdinalIgnoreCase) ||
                   o.Display.Equals(term, StringComparison.OrdinalIgnoreCase))
               ?? _all.FirstOrDefault(o =>
                   o.FabricDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                   o.FabricCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                   o.ColorDisplayName.Contains(term, StringComparison.OrdinalIgnoreCase))
               ?? _filtered.FirstOrDefault();
    }
}
