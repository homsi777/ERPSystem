using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Suppliers;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseReturnFormControl : UserControl
{
    private readonly TextBox _txtNumber = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbSupplier = new() { Height = 36, MinWidth = 220, Style = S("EnterpriseComboBoxStyle") };
    private readonly ComboBox _cmbInvoice = new() { Height = 36, MinWidth = 280, Style = S("EnterpriseComboBoxStyle") };
    private readonly DatePicker _dpDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly TextBox _txtReason = ErpUiFactory.FormField("");
    private readonly DataGrid _lines = new() { AutoGenerateColumns = false, MinHeight = 160, CanUserAddRows = false };
    private readonly TextBlock _txtSummary = new() { Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button _btnSave = new() { Content = "حفظ مسودة", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36 };
    private readonly Button _btnPost = new() { Content = "ترحيل", Style = S("PrimaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };

    private Guid? _editId;
    private Guid? _originalInvoiceId;
    private readonly List<ReturnLineVm> _lineItems = [];

    public PurchaseReturnFormControl()
    {
        ErpDataGridHelper.ApplyEnterpriseStyle(_lines);
        ErpUiFactory.AddGridColumn(_lines, "الصنف", nameof(ReturnLineVm.ItemName), "*", null);
        ErpUiFactory.AddGridColumn(_lines, "الكمية الأصلية", nameof(ReturnLineVm.MaxQuantity), 100, "N2");
        ErpUiFactory.AddGridColumn(_lines, "كمية المرتجع", nameof(ReturnLineVm.ReturnQuantity), 100, "N2");
        ErpUiFactory.AddGridColumn(_lines, "سعر الوحدة", nameof(ReturnLineVm.UnitPrice), 90, "N2");
        ErpUiFactory.AddGridColumn(_lines, "الإجمالي", nameof(ReturnLineVm.LineTotal), 90, "N2");
        _lines.CellEditEnding += (_, _) => RefreshLines();

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnPost);
        actions.Children.Add(_btnCancel);

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("مرتجع شراء"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رقم المرتجع", _txtNumber),
            ("المورد", _cmbSupplier),
            ("فاتورة الشراء الأصلية", _cmbInvoice),
            ("تاريخ المرتجع", _dpDate),
            ("سبب المرتجع *", _txtReason))));
        stack.Children.Add(ErpUiFactory.Card(_lines));
        stack.Children.Add(_txtSummary);
        stack.Children.Add(actions);
        Content = new ScrollViewer { Content = stack };

        Loaded += OnLoaded;
        _cmbSupplier.SelectionChanged += async (_, _) => await LoadInvoicesForSupplierAsync();
        _cmbInvoice.SelectionChanged += async (_, _) => await LoadInvoiceLinesAsync();
        _btnSave.Click += async (_, _) => await SaveDraftAsync();
        _btnPost.Click += async (_, _) => await PostAsync();
        _btnCancel.Click += (_, _) => MockInteractionService.Navigate(AppModule.Purchases, "Returns");
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadSuppliersAsync();
        _editId = PurchaseNavigationContext.EditReturnId;
        if (_editId is Guid id)
        {
            var result = await PurchaseUiService.Instance.GetReturnDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
            var ret = result.Value;
            _txtNumber.Text = ret.ReturnNumber;
            _txtNumber.IsReadOnly = true;
            _dpDate.SelectedDate = ret.ReturnDate;
            _txtReason.Text = ret.Notes ?? "";
            _originalInvoiceId = ret.OriginalInvoiceId;
            SelectSupplier(ret.SupplierId);
            await LoadInvoicesForSupplierAsync();
            _cmbInvoice.SelectedValue = ret.OriginalInvoiceId;
            _lineItems.Clear();
            foreach (var l in ret.Lines)
                _lineItems.Add(new ReturnLineVm
                {
                    OriginalInvoiceItemId = l.OriginalInvoiceItemId,
                    LineType = (int)l.LineType,
                    FabricItemId = l.FabricItemId,
                    FabricColorId = l.FabricColorId,
                    ItemName = l.FabricItemName ?? l.Description,
                    MaxQuantity = l.MaxQuantityMeters,
                    ReturnQuantity = l.QuantityMeters,
                    UnitPrice = l.UnitPrice
                });
            RefreshLines();
            var readOnly = ret.IsReadOnly;
            _btnSave.IsEnabled = !readOnly;
            _btnPost.IsEnabled = !readOnly;
            _lines.IsReadOnly = readOnly;
            _cmbSupplier.IsEnabled = !readOnly;
            _cmbInvoice.IsEnabled = !readOnly;
            _txtReason.IsReadOnly = readOnly;
        }
        else
        {
            _txtNumber.Text = await PurchaseUiService.Instance.NextReturnNumberAsync();
            if (PurchaseNavigationContext.ReturnSourceInvoiceId is Guid sourceId)
            {
                var inv = await PurchaseUiService.Instance.GetInvoiceDetailsAsync(sourceId);
                if (inv.IsSuccess && inv.Value is not null)
                {
                    SelectSupplier(inv.Value.SupplierId);
                    await LoadInvoicesForSupplierAsync();
                    _cmbInvoice.SelectedValue = sourceId;
                    await LoadInvoiceLinesAsync();
                }
            }
        }
    }

    private async Task LoadSuppliersAsync()
    {
        var suppliers = await SupplierUiService.Instance.GetListAsync(null, pageSize: 200);
        if (!suppliers.IsSuccess) return;
        _cmbSupplier.Items.Clear();
        foreach (var s in suppliers.Value!.Items)
            _cmbSupplier.Items.Add(new ComboBoxItem { Content = s.NameAr, Tag = s });
    }

    private async Task LoadInvoicesForSupplierAsync()
    {
        _cmbInvoice.ItemsSource = null;
        _lineItems.Clear();
        RefreshLines();
        if (_cmbSupplier.SelectedItem is not ComboBoxItem item || item.Tag is not SupplierListDto supplier)
            return;

        var result = await PurchaseUiService.Instance.GetPostedInvoicesForSupplierAsync(supplier.Id);
        if (ApplicationResultPresenter.Present(result))
        {
            _cmbInvoice.ItemsSource = result.Value;
            _cmbInvoice.DisplayMemberPath = nameof(PurchaseInvoicePickDto.Display);
            _cmbInvoice.SelectedValuePath = nameof(PurchaseInvoicePickDto.Id);
        }
    }

    private async Task LoadInvoiceLinesAsync()
    {
        if (_cmbInvoice.SelectedValue is not Guid invoiceId)
            return;

        _originalInvoiceId = invoiceId;
        var result = await PurchaseUiService.Instance.GetInvoiceDetailsAsync(invoiceId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;

        _lineItems.Clear();
        foreach (var l in result.Value.Lines.Where(x => x.LineType == PurchaseLineType.Inventory))
        {
            _lineItems.Add(new ReturnLineVm
            {
                OriginalInvoiceItemId = l.Id,
                LineType = (int)l.LineType,
                FabricItemId = l.FabricItemId,
                FabricColorId = l.FabricColorId,
                ItemName = l.FabricItemName ?? l.Description,
                MaxQuantity = l.QuantityMeters,
                ReturnQuantity = 0,
                UnitPrice = l.UnitPrice
            });
        }
        RefreshLines();
    }

    private void RefreshLines()
    {
        _lines.ItemsSource = null;
        _lines.ItemsSource = _lineItems.ToList();
        _txtSummary.Text = $"إجمالي المرتجع: {_lineItems.Sum(l => l.LineTotal):N2} ر.س";
    }

    private async Task SaveDraftAsync()
    {
        if (!TryBuildCommand(out var cmd)) return;
        if (_editId is Guid id)
        {
            var result = await PurchaseUiService.Instance.UpdateReturnDraftAsync(new UpdatePurchaseReturnDraftCommand
            {
                ReturnId = id,
                Notes = cmd.Notes,
                Lines = cmd.Lines
            });
            if (ApplicationResultPresenter.Present(result))
            {
                MockInteractionService.ShowSuccess("تم حفظ مسودة المرتجع.");
                PurchaseListRefreshHub.RequestRefresh();
            }
            return;
        }

        var create = await PurchaseUiService.Instance.CreateReturnAsync(cmd);
        if (ApplicationResultPresenter.Present(create))
        {
            _editId = create.Value;
            PurchaseNavigationContext.BeginReturnEdit(_editId.Value);
            MockInteractionService.ShowSuccess("تم حفظ مسودة المرتجع.");
            PurchaseListRefreshHub.RequestRefresh();
        }
    }

    private async Task PostAsync()
    {
        if (_editId is null)
        {
            await SaveDraftAsync();
            if (_editId is null) return;
        }
        var result = await PurchaseUiService.Instance.PostReturnAsync(_editId.Value);
        if (ApplicationResultPresenter.Present(result))
        {
            MockInteractionService.ShowSuccess($"تم ترحيل المرتجع.\nقيد اليومية: {result.Value}");
            PurchaseListRefreshHub.RequestRefresh();
            MockInteractionService.Navigate(AppModule.Purchases, "Returns");
        }
    }

    private bool TryBuildCommand(out CreatePurchaseReturnCommand cmd)
    {
        cmd = null!;
        if (string.IsNullOrWhiteSpace(_txtReason.Text))
        {
            MockInteractionService.ShowWarning("سبب المرتجع مطلوب.");
            return false;
        }
        if (_originalInvoiceId is not Guid invoiceId)
        {
            MockInteractionService.ShowWarning("اختر فاتورة الشراء الأصلية.");
            return false;
        }

        var lines = _lineItems
            .Where(l => l.ReturnQuantity > 0)
            .ToList();
        if (lines.Count == 0)
        {
            MockInteractionService.ShowWarning("حدد كمية مرتجع لسطر واحد على الأقل.");
            return false;
        }
        foreach (var l in lines)
        {
            if (l.ReturnQuantity > l.MaxQuantity)
            {
                MockInteractionService.ShowWarning($"كمية المرتجع للصنف {l.ItemName} تتجاوز كمية الفاتورة.");
                return false;
            }
        }

        cmd = new CreatePurchaseReturnCommand
        {
            OriginalInvoiceId = invoiceId,
            ReturnDate = _dpDate.SelectedDate ?? DateTime.Today,
            Notes = _txtReason.Text.Trim(),
            Lines = lines.Select(l => new PurchaseReturnLineInput
            {
                OriginalInvoiceItemId = l.OriginalInvoiceItemId,
                LineType = l.LineType,
                FabricItemId = l.FabricItemId,
                FabricColorId = l.FabricColorId,
                QuantityMeters = l.ReturnQuantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
        return true;
    }

    private void SelectSupplier(Guid id)
    {
        foreach (ComboBoxItem item in _cmbSupplier.Items)
            if (item.Tag is SupplierListDto s && s.Id == id)
                _cmbSupplier.SelectedItem = item;
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;

    private sealed class ReturnLineVm
    {
        public Guid OriginalInvoiceItemId { get; set; }
        public int LineType { get; set; }
        public Guid? FabricItemId { get; set; }
        public Guid? FabricColorId { get; set; }
        public string ItemName { get; set; } = "";
        public decimal MaxQuantity { get; set; }
        public decimal ReturnQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal => ReturnQuantity * UnitPrice;
    }
}
