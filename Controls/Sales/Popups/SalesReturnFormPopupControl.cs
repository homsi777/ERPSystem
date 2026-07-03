using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales.Popups;

public sealed class SalesReturnFormPopupControl : UserControl
{
    private readonly Guid _invoiceId;
    private readonly string _invoiceNumber;
    private readonly string _customerName;
    private readonly ObservableCollection<SalesReturnLineRow> _lines = new();
    private readonly ComboBox _cmbReason = new()
    {
        Height = 32,
        ItemsSource = new[]
        {
            new ReasonItem(SalesReturnReason.DefectiveGoods, "بضاعة معيبة"),
            new ReasonItem(SalesReturnReason.WrongOrder, "خطأ في الطلب"),
            new ReasonItem(SalesReturnReason.CustomerRequest, "طلب العميل"),
            new ReasonItem(SalesReturnReason.Other, "أخرى (يُرجى التوضيح)")
        },
        DisplayMemberPath = nameof(ReasonItem.Display),
        SelectedIndex = 0
    };
    private readonly TextBox _txtReasonNotes = new() { Height = 60, TextWrapping = TextWrapping.Wrap };
    private readonly TextBox _txtNotes = new() { Height = 60, TextWrapping = TextWrapping.Wrap };
    private readonly DatePicker _dpDate = new() { SelectedDate = DateTime.Today, Height = 32 };
    private readonly TextBlock _txtTotal = new() { FontSize = 14, FontWeight = FontWeights.Bold };
    private readonly DataGrid _grid;

    public SalesReturnFormPopupControl(Guid invoiceId, string invoiceNumber, string customerName)
    {
        _invoiceId = invoiceId;
        _invoiceNumber = invoiceNumber;
        _customerName = customerName;

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Header form
        var top = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        top.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        top.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetColumn(BuildField("سبب المرتجع", _cmbReason), 0);
        top.Children.Add(BuildField("سبب المرتجع", _cmbReason));

        var dateField = BuildField("تاريخ المرتجع", _dpDate);
        Grid.SetColumn(dateField, 1);
        top.Children.Add(dateField);

        var notesField = BuildField("توضيح السبب (اختياري)", _txtReasonNotes);
        Grid.SetColumn(notesField, 0);
        Grid.SetRow(notesField, 1);
        top.Children.Add(notesField);

        var notes2 = BuildField("ملاحظات إضافية", _txtNotes);
        Grid.SetColumn(notes2, 1);
        Grid.SetRow(notes2, 1);
        top.Children.Add(notes2);

        Grid.SetRow(top, 0);
        root.Children.Add(top);

        // Lines grid
        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = false,
            CanUserAddRows = false,
            CanUserDeleteRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
            FontSize = 12,
            RowBackground = Brushes.White,
            AlternatingRowBackground = (Brush)System.Windows.Application.Current.Resources["SurfaceAltBrush"],
            ItemsSource = _lines,
            MaxHeight = 360
        };
        _grid.Columns.Add(new DataGridTextColumn { Header = "الصنف", Binding = new Binding(nameof(SalesReturnLineRow.FabricDisplay)) { Mode = BindingMode.OneWay }, IsReadOnly = true, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        _grid.Columns.Add(new DataGridTextColumn { Header = "اللون", Binding = new Binding(nameof(SalesReturnLineRow.ColorDisplay)) { Mode = BindingMode.OneWay }, IsReadOnly = true, Width = 120 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "الأمتار الأصلية", Binding = new Binding(nameof(SalesReturnLineRow.OriginalMeters)) { StringFormat = "N2", Mode = BindingMode.OneWay }, IsReadOnly = true, Width = 130 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "أمتار المرتجع", Binding = new Binding(nameof(SalesReturnLineRow.ReturnMeters)) { UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged, StringFormat = "N2" }, Width = 130 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "سعر الوحدة", Binding = new Binding(nameof(SalesReturnLineRow.UnitPrice)) { StringFormat = "N2", Mode = BindingMode.OneWay }, IsReadOnly = true, Width = 120 });
        _grid.Columns.Add(new DataGridTextColumn { Header = "الإجمالي", Binding = new Binding(nameof(SalesReturnLineRow.LineTotal)) { StringFormat = "N2", Mode = BindingMode.OneWay }, IsReadOnly = true, Width = 130 });
        _grid.CellEditEnding += (_, _) => Dispatcher.BeginInvoke(new Action(RecalcTotal), System.Windows.Threading.DispatcherPriority.Background);

        Grid.SetRow(_grid, 1);
        root.Children.Add(_grid);

        // Bottom
        var bottom = new Grid { Margin = new Thickness(0, 12, 0, 0) };
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        bottom.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var totalPanel = new StackPanel { Orientation = Orientation.Horizontal };
        totalPanel.Children.Add(new TextBlock
        {
            Text = "إجمالي قيمة المرتجع: ",
            FontSize = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextSecondaryBrush"]
        });
        totalPanel.Children.Add(_txtTotal);
        Grid.SetColumn(totalPanel, 0);
        bottom.Children.Add(totalPanel);

        var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
        var btnCancel = new Button
        {
            Content = "تراجع",
            Width = 90,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)System.Windows.Application.Current.Resources["GhostButtonStyle"]
        };
        btnCancel.Click += (_, _) => SalesPopupService.CancelActive();
        var btnSaveDraft = new Button
        {
            Content = "حفظ مسودة",
            Width = 110,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Style = (Style)System.Windows.Application.Current.Resources["GhostButtonStyle"]
        };
        btnSaveDraft.Click += async (_, _) => await SaveAndOptionallyPostAsync(post: false);

        var btnPost = new Button
        {
            Content = "ترحيل المرتجع",
            Width = 140,
            Height = 34,
            Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]
        };
        btnPost.Click += async (_, _) => await SaveAndOptionallyPostAsync(post: true);
        btnRow.Children.Add(btnPost);
        btnRow.Children.Add(btnSaveDraft);
        btnRow.Children.Add(btnCancel);
        Grid.SetColumn(btnRow, 1);
        bottom.Children.Add(btnRow);

        Grid.SetRow(bottom, 2);
        root.Children.Add(bottom);

        Content = root;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!AppServices.IsInitialized) return;

        var oc = await SalesUiService.Instance.GetOperationsCenterAsync(_invoiceId);
        if (!ApplicationResultPresenter.Present(oc) || oc.Value?.Invoice is null) return;

        _lines.Clear();
        foreach (var line in oc.Value.Invoice.Lines.OrderBy(l => l.LineNumber))
        {
            var meters = line.LineTotal > 0 && line.UnitPrice > 0 ? line.LineTotal / line.UnitPrice : 0m;
            var row = new SalesReturnLineRow
            {
                OriginalInvoiceItemId = line.Id,
                LineNumber = line.LineNumber,
                FabricDisplay = $"{line.FabricDisplayName} ({line.FabricCode})",
                ColorDisplay = line.ColorDisplayName,
                OriginalMeters = meters,
                ReturnMeters = 0m,
                UnitPrice = line.UnitPrice
            };
            row.PropertyChanged += (_, _) => RecalcTotal();
            _lines.Add(row);
        }
        RecalcTotal();
    }

    private void RecalcTotal()
    {
        var total = _lines.Sum(l => l.LineTotal);
        _txtTotal.Text = $"${total:N2}";
    }

    private async Task SaveAndOptionallyPostAsync(bool post)
    {
        var linesToSave = _lines
            .Where(l => l.ReturnMeters > 0)
            .Select(l => new SalesReturnLineCommand
            {
                LineNumber = l.LineNumber,
                OriginalInvoiceItemId = l.OriginalInvoiceItemId,
                ReturnMeters = l.ReturnMeters
            })
            .ToList();

        if (linesToSave.Count == 0)
        {
            MessageBox.Show("أدخل كمية للمرتجع في سطر واحد على الأقل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Validate quantity constraints
        foreach (var row in _lines)
        {
            if (row.ReturnMeters > row.OriginalMeters + 0.001m)
            {
                MessageBox.Show($"لا يمكن أن يتجاوز المرتجع الكمية الأصلية ({row.OriginalMeters:N2} م).",
                    "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        if (!AppServices.IsInitialized) return;
        var reasonItem = _cmbReason.SelectedItem as ReasonItem ?? new ReasonItem(SalesReturnReason.Other, "");
        var date = _dpDate.SelectedDate ?? DateTime.UtcNow;

        var createResult = await SalesReturnUiService.Instance.CreateDraftAsync(
            _invoiceId,
            date,
            reasonItem.Reason,
            _txtReasonNotes.Text?.Trim(),
            _txtNotes.Text?.Trim(),
            linesToSave);

        if (!ApplicationResultPresenter.Present(createResult) || createResult.Value == Guid.Empty) return;

        if (post)
        {
            var postResult = await SalesReturnUiService.Instance.PostAsync(createResult.Value);
            if (!ApplicationResultPresenter.Present(postResult)) return;
            MockInteractionService.ShowSuccess("تم ترحيل المرتجع وتسجيل قيود GL وإعادة المخزون.", "مرتجع بيع");
        }
        else
        {
            MockInteractionService.ShowSuccess("تم حفظ المسودة (لم يتم الترحيل بعد).", "مرتجع بيع");
        }

        SalesReturnListRefreshHub.RequestRefresh();
        SalesListRefreshHub.RequestRefresh();
        if (Window.GetWindow(this) is Window w) { w.DialogResult = true; w.Close(); }
    }

    private static StackPanel BuildField(string label, UIElement content)
    {
        var sp = new StackPanel { Margin = new Thickness(6, 4, 6, 4) };
        sp.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            Foreground = (Brush)System.Windows.Application.Current.Resources["TextMutedBrush"],
            Margin = new Thickness(0, 0, 0, 4)
        });
        sp.Children.Add(content);
        return sp;
    }

    private sealed record ReasonItem(SalesReturnReason Reason, string Display);

    public sealed class SalesReturnLineRow : INotifyPropertyChanged
    {
        public Guid OriginalInvoiceItemId { get; init; }
        public int LineNumber { get; init; }
        public string FabricDisplay { get; init; } = "";
        public string ColorDisplay { get; init; } = "";
        public decimal OriginalMeters { get; init; }
        public decimal UnitPrice { get; init; }

        private decimal _returnMeters;
        public decimal ReturnMeters
        {
            get => _returnMeters;
            set { if (_returnMeters == value) return; _returnMeters = value; OnChanged(); OnChanged(nameof(LineTotal)); }
        }

        public decimal LineTotal => ReturnMeters * UnitPrice;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
