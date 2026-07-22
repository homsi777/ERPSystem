using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Domain.Entities.Finance;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Finance;
using ERPSystem.Services.Inventory;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryOpeningStockFormControl : UserControl
{
    private readonly ComboBox _warehouse = new() { MinWidth = 220, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly TextBox _containerNumber = ErpUiFactory.FormField("");
    private readonly DatePicker _date = new() { SelectedDate = DateTime.Today };
    private readonly TextBox _reference = ErpUiFactory.FormField("");
    private readonly TextBox _currency = ErpUiFactory.FormField("USD");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly DataGrid _lines = new()
    {
        AutoGenerateColumns = false,
        CanUserAddRows = false,
        CanUserDeleteRows = true,
        IsReadOnly = false,
        MinHeight = 200,
        SelectionUnit = DataGridSelectionUnit.FullRow
    };
    private readonly ObservableCollection<OpeningLineRow> _lineRows = [];
    private readonly TextBox _fabricCode = ErpUiFactory.FormField("");
    private readonly TextBox _fabricName = ErpUiFactory.FormField("");
    private readonly TextBox _colorName = ErpUiFactory.FormField("");
    private readonly TextBox _meters = ErpUiFactory.FormField("");
    private readonly TextBox _rolls = ErpUiFactory.FormField("");
    private readonly TextBox _unitCost = ErpUiFactory.FormField("");
    private readonly TextBox _lineNote = ErpUiFactory.FormField("");
    private readonly Button _saveButton = new();
    private readonly Button _postButton = new();
    private bool _isSaving;
    private bool _isPosting;
    private Guid? _documentId;

    public InventoryOpeningStockFormControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("مواد أول المدة"));
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "حدد المستودع ورقم الحاوية أولاً، ثم أضف الأسطر. انقر مرتين على أي خانة في الجدول لتعديلها (مثل التكلفة)."));

        stack.Children.Add(ErpUiFactory.BuildFormGrid(
            ("المستودع *", _warehouse),
            ("رقم الحاوية *", _containerNumber),
            ("تاريخ الافتتاح", Wrap(_date)),
            ("مرجع", _reference),
            ("العملة", _currency),
            ("ملاحظات", _notes)));

        _lines.Columns.Add(EditableColumn("اسم التوب", nameof(OpeningLineRow.FabricName), "*"));
        _lines.Columns.Add(EditableColumn("كود التوب", nameof(OpeningLineRow.FabricCode), 100));
        _lines.Columns.Add(EditableColumn("اللون", nameof(OpeningLineRow.ColorName), 100));
        _lines.Columns.Add(EditableColumn("الأمتار", nameof(OpeningLineRow.QuantityMeters), 80, "N2"));
        _lines.Columns.Add(EditableColumn("عدد الأتواب", nameof(OpeningLineRow.RollCount), 80));
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
        {
            _lines.Columns.Add(EditableColumn("التكلفة/م", nameof(OpeningLineRow.UnitCost), 80, "N4"));
            _lines.Columns.Add(EditableColumn("القيمة", nameof(OpeningLineRow.TotalValue), 90, "N2", readOnly: true));
        }
        _lines.Columns.Add(EditableColumn("ملاحظة", nameof(OpeningLineRow.LineNote), 140));
        _lines.CellEditEnding += OnLineCellEditEnding;
        _lines.ItemsSource = _lineRows;
        stack.Children.Add(_lines);

        var addRow = new Grid { Margin = new Thickness(0, 8, 0, 0) };
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 140 });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
            addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 100 });
        addRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

        AddEntryField(addRow, "اسم التوب *", _fabricName, 0);
        AddEntryField(addRow, "كود التوب *", _fabricCode, 1);
        AddEntryField(addRow, "اللون *", _colorName, 2);
        AddEntryField(addRow, "الأمتار *", _meters, 3);
        AddEntryField(addRow, "عدد الأتواب *", _rolls, 4);

        var noteColumn = 5;
        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
        {
            AddEntryField(addRow, "التكلفة/م", _unitCost, 5);
            noteColumn = 6;
        }

        AddEntryField(addRow, "ملاحظة", _lineNote, noteColumn);

        var buttonColumn = noteColumn + 1;

        var addBtn = new Button
        {
            Content = "إضافة",
            Margin = new Thickness(8, 20, 0, 0),
            VerticalAlignment = VerticalAlignment.Bottom
        };
        addBtn.Click += (_, _) => AddLine();
        Grid.SetColumn(addBtn, buttonColumn);
        addRow.Children.Add(addBtn);
        stack.Children.Add(addRow);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        _saveButton.Content = "حفظ المستند";
        _saveButton.Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!;
        _saveButton.Margin = new Thickness(0, 0, 8, 0);
        _saveButton.Click += async (_, _) => await SaveAsync();
        _postButton.Content = "ترحيل للمخزون";
        _postButton.Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]!;
        _postButton.Click += async (_, _) => await PostAsync();
        actions.Children.Add(_saveButton);
        actions.Children.Add(_postButton);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        WireEnterNavigation();
        Loaded += async (_, _) => await InitAsync();
    }

    private void WireEnterNavigation()
    {
        EnterFocusNavigation.WireChain(
            [_warehouse, _containerNumber, _date, _reference, _currency, _notes],
            onLastEnter: () => EnterFocusNavigation.FocusNext(_fabricName));

        if (WpfGeneralManagerAccess.CanViewSensitivePricing)
        {
            EnterFocusNavigation.WireChain(
                [_fabricName, _fabricCode, _colorName, _meters, _rolls, _unitCost, _lineNote],
                onLastEnter: AddLineAndRefocusFabric);
        }
        else
        {
            EnterFocusNavigation.WireChain(
                [_fabricName, _fabricCode, _colorName, _meters, _rolls, _lineNote],
                onLastEnter: AddLineAndRefocusFabric);
        }
    }

    private void AddLineAndRefocusFabric()
    {
        AddLine();
        EnterFocusNavigation.FocusNext(_fabricName);
    }

    private static UIElement Wrap(UIElement c) => new Border { Child = c, Padding = new Thickness(0, 4, 0, 0) };

    private static DataGridTextColumn EditableColumn(
        string header, string path, object width, string? format = null, bool readOnly = false)
    {
        var binding = new Binding(path)
        {
            Mode = readOnly ? BindingMode.OneWay : BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            StringFormat = format,
            ConverterCulture = CultureInfo.CurrentCulture
        };
        return new DataGridTextColumn
        {
            Header = header,
            Binding = binding,
            IsReadOnly = readOnly,
            Width = width is string
                ? new DataGridLength(1, DataGridLengthUnitType.Star)
                : new DataGridLength(Convert.ToDouble(width))
        };
    }

    private void OnLineCellEditEnding(object? sender, DataGridCellEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit || e.Row.Item is not OpeningLineRow row)
            return;
        if (e.EditingElement is not TextBox editor)
            return;

        var header = e.Column.Header?.ToString() ?? "";
        var text = editor.Text.Trim();

        if (header is "الأمتار")
        {
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var meters) || meters <= 0)
            {
                e.Cancel = true;
                MessageBox.Show("الأمتار يجب أن تكون أكبر من صفر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else if (header is "عدد الأتواب")
        {
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var rolls) || rolls <= 0)
            {
                e.Cancel = true;
                MessageBox.Show("عدد الأتواب يجب أن يكون أكبر من صفر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else if (header is "التكلفة/م")
        {
            if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var cost) || cost < 0)
            {
                e.Cancel = true;
                MessageBox.Show("التكلفة يجب أن تكون صفراً أو أكبر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else if (header is "اسم التوب" or "كود التوب" or "اللون")
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                e.Cancel = true;
                MessageBox.Show("لا يمكن ترك هذه الخانة فارغة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Refresh computed value after edit commits.
        Dispatcher.BeginInvoke(() => row.NotifyTotalsChanged(), System.Windows.Threading.DispatcherPriority.Background);
    }

    private static void AddEntryField(Grid row, string label, FrameworkElement field, int column)
    {
        field.HorizontalAlignment = HorizontalAlignment.Stretch;
        var container = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
        container.Children.Add(new TextBlock
        {
            Text = label,
            Margin = new Thickness(0, 0, 0, 4)
        });
        container.Children.Add(field);
        Grid.SetColumn(container, column);
        row.Children.Add(container);
    }

    private async Task InitAsync()
    {
        if (!AppServices.IsInitialized) return;
        var wh = await InventoryUiService.Instance.GetWarehousesAsync();
        if (wh.IsSuccess && wh.Value is not null)
        {
            _warehouse.ItemsSource = wh.Value;
            var pre = InventoryNavigationContext.TakePreselectedOpeningWarehouse();
            if (pre.HasValue)
                _warehouse.SelectedItem = wh.Value.FirstOrDefault(w => w.Id == pre.Value);
        }
    }

    private string? TryGetDocumentContainerNumber()
    {
        var containerNumber = _containerNumber.Text.Trim();
        return string.IsNullOrWhiteSpace(containerNumber) ? null : containerNumber;
    }

    private void AddLine()
    {
        if (_warehouse.SelectedItem is not WarehouseListExtendedDto)
        {
            MessageBox.Show("اختر المستودع أولاً من أعلى المستند.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var containerNumber = TryGetDocumentContainerNumber();
        if (containerNumber is null)
        {
            MessageBox.Show("اكتب رقم الحاوية في أعلى المستند أولاً (أرقام أو أحرف).", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var fabricCode = _fabricCode.Text.Trim();
        var fabricName = _fabricName.Text.Trim();
        var colorName = _colorName.Text.Trim();
        if (string.IsNullOrWhiteSpace(fabricCode))
        {
            MessageBox.Show("اكتب كود التوب.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(fabricName) || string.IsNullOrWhiteSpace(colorName))
        {
            MessageBox.Show("اكتب اسم التوب ولون التوب.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!decimal.TryParse(_meters.Text, out var m) || m <= 0)
        {
            MessageBox.Show("اكتب إجمالي الأمتار بقيمة أكبر من صفر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (!int.TryParse(_rolls.Text, out var r) || r <= 0)
        {
            MessageBox.Show("اكتب عدد الرولات بقيمة صحيحة أكبر من صفر.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var cost = 0m;
        if (!string.IsNullOrWhiteSpace(_unitCost.Text) &&
            (!decimal.TryParse(_unitCost.Text, out cost) || cost < 0))
        {
            MessageBox.Show("اكتب تكلفة صحيحة أو اتركها فارغة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _lineRows.Add(new OpeningLineRow
        {
            FabricCode = fabricCode,
            FabricName = fabricName,
            ColorName = colorName,
            QuantityMeters = m,
            RollCount = r,
            UnitCost = cost,
            LineNote = _lineNote.Text.Trim()
        });

        _fabricCode.Clear();
        _fabricName.Clear();
        _colorName.Clear();
        _meters.Clear();
        _rolls.Clear();
        _unitCost.Clear();
        _lineNote.Clear();
        _fabricName.Focus();
    }

    private async Task SaveAsync()
    {
        if (_isSaving)
            return;

        if (_warehouse.SelectedItem is not WarehouseListExtendedDto wh)
        {
            MessageBox.Show("اختر المستودع.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var containerNumber = TryGetDocumentContainerNumber();
        if (containerNumber is null)
        {
            MessageBox.Show("اكتب رقم الحاوية في أعلى المستند.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_lineRows.Count == 0)
        {
            MessageBox.Show("أضف سطراً واحداً على الأقل.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var lines = _lineRows.Select(l => new OpeningBalanceLineInput
        {
            WarehouseId = wh.Id,
            WarehouseName = wh.NameAr,
            FabricItemId = l.FabricItemId,
            FabricColorId = l.FabricColorId,
            ItemCode = l.FabricCode,
            ItemName = l.FabricName,
            ColorName = l.ColorName,
            ContainerNumber = containerNumber,
            Quantity = l.QuantityMeters,
            RollCount = l.RollCount,
            UnitCost = l.UnitCost,
            Debit = l.TotalValue,
            Credit = 0m,
            Notes = string.IsNullOrWhiteSpace(l.LineNote) ? null : l.LineNote
        }).ToList();

        _isSaving = true;
        _saveButton.IsEnabled = false;
        try
        {
            var result = await OpeningBalanceUiService.Instance.CreateAsync(new CreateOpeningBalanceCommand
            {
                Type = OpeningBalanceType.OpeningStock,
                Source = OpeningBalanceSource.Manual,
                OpeningDate = ApplicationDateNormalizer.ToUtcDate(_date.SelectedDate) ?? DateTime.UtcNow,
                CurrencyCode = _currency.Text.Trim().ToUpperInvariant(),
                ExchangeRate = 1m,
                Reference = _reference.Text.Trim(),
                Notes = _notes.Text.Trim(),
                Lines = lines
            });

            if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
            {
                _documentId = result.Value?.Id;
                OpeningBalanceListRefreshHub.RequestRefresh();
                InventoryListRefreshHub.RequestRefresh();
                MockInteractionService.ShowInfo(
                    $"تم حفظ مستند مواد أول المدة رقم {result.Value?.Number ?? "—"}.\nيمكنك الآن الضغط على «ترحيل للمخزون».",
                    "مواد أول المدة");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر حفظ مستند مواد أول المدة.\n\n{ex.Message}",
                "مواد أول المدة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isSaving = false;
            _saveButton.IsEnabled = true;
        }
    }

    private async Task PostAsync()
    {
        if (_isPosting)
            return;

        if (!_documentId.HasValue)
        {
            MessageBox.Show("احفظ المستند أولاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _isPosting = true;
        _postButton.IsEnabled = false;
        try
        {
            var submit = await OpeningBalanceUiService.Instance.SubmitAsync(_documentId.Value);
            if (!ApplicationResultPresenter.Present(submit))
                return;

            var approve = await OpeningBalanceUiService.Instance.ApproveAsync(
                _documentId.Value, "اعتماد رصيد مخزون من شاشة مواد أول المدة");
            if (!ApplicationResultPresenter.Present(approve))
                return;

            var post = await OpeningBalanceUiService.Instance.PostAsync(_documentId.Value, lockAfterPost: true);
            if (ApplicationResultPresenter.Present(post))
            {
                OpeningBalanceListRefreshHub.RequestRefresh();
                InventoryListRefreshHub.RequestRefresh();
                InventoryPopupService.CompleteSuccess();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر ترحيل مستند مواد أول المدة.\n\n{ex.Message}",
                "مواد أول المدة", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _isPosting = false;
            _postButton.IsEnabled = true;
        }
    }

    private sealed class OpeningLineRow : INotifyPropertyChanged
    {
        private string _fabricCode = "";
        private string _fabricName = "";
        private string _colorName = "";
        private decimal _quantityMeters;
        private int _rollCount;
        private decimal _unitCost;
        private string _lineNote = "";

        public Guid? FabricItemId { get; init; }
        public Guid? FabricColorId { get; init; }

        public string FabricCode
        {
            get => _fabricCode;
            set => SetField(ref _fabricCode, value);
        }

        public string FabricName
        {
            get => _fabricName;
            set => SetField(ref _fabricName, value);
        }

        public string ColorName
        {
            get => _colorName;
            set => SetField(ref _colorName, value);
        }

        public decimal QuantityMeters
        {
            get => _quantityMeters;
            set
            {
                if (SetField(ref _quantityMeters, value))
                    OnPropertyChanged(nameof(TotalValue));
            }
        }

        public int RollCount
        {
            get => _rollCount;
            set => SetField(ref _rollCount, value);
        }

        public decimal UnitCost
        {
            get => _unitCost;
            set
            {
                if (SetField(ref _unitCost, value))
                    OnPropertyChanged(nameof(TotalValue));
            }
        }

        public string LineNote
        {
            get => _lineNote;
            set => SetField(ref _lineNote, value);
        }

        public decimal TotalValue => QuantityMeters * UnitCost;

        public event PropertyChangedEventHandler? PropertyChanged;

        public void NotifyTotalsChanged() => OnPropertyChanged(nameof(TotalValue));

        private void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(name);
            return true;
        }
    }
}

