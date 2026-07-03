using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Application.DTOs.Suppliers;
using ERPSystem.Core;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Purchases;
using ERPSystem.Services.Suppliers;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseOrderFormControl : UserControl
{
    private readonly TextBox _txtNumber = ErpUiFactory.FormField("");
    private readonly ComboBox _cmbSupplier = new() { Height = 36, MinWidth = 240, Style = S("EnterpriseComboBoxStyle") };
    private readonly DatePicker _dpOrderDate = ErpUiFactory.FormDate(DateTime.Today);
    private readonly DatePicker _dpExpected = ErpUiFactory.FormDate(DateTime.Today.AddDays(14));
    private readonly TextBox _txtNotes = ErpUiFactory.FormField("");
    private readonly StackPanel _linesHost = new();
    private readonly TextBlock _txtSummary = new() { Margin = new Thickness(0, 8, 0, 0) };
    private readonly Button _btnSave = new() { Content = "حفظ", Style = S("PrimaryButtonStyle"), MinWidth = 100, Height = 36 };
    private readonly Button _btnSend = new() { Content = "إرسال للمورد", Style = S("PrimaryButtonStyle"), MinWidth = 120, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private readonly Button _btnConvert = new() { Content = "تحويل لفاتورة شراء", Style = S("SecondaryButtonStyle"), MinWidth = 140, Height = 36, Margin = new Thickness(8, 0, 0, 0) };
    private readonly Button _btnCancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 36, Margin = new Thickness(8, 0, 0, 0) };

    private readonly List<PurchaseLineEditors.InventoryLineEditor> _lines = [];
    private Guid? _editId;
    private PurchaseOrderStatus _status = PurchaseOrderStatus.Draft;
    private IReadOnlyList<PurchaseFabricPickDto> _fabrics = [];

    public PurchaseOrderFormControl()
    {
        var addBtn = new Button { Content = "+ سطر صنف", Style = S("GhostButtonStyle"), Height = 32, Margin = new Thickness(0, 8, 0, 0) };
        addBtn.Click += (_, _) => AddLine();

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_btnSave);
        actions.Children.Add(_btnSend);
        actions.Children.Add(_btnConvert);
        actions.Children.Add(_btnCancel);

        var stack = new StackPanel { Margin = new Thickness(16) };
        stack.Children.Add(ErpUiFactory.SectionTitle("أمر شراء"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("رقم الأمر", _txtNumber),
            ("المورد", _cmbSupplier),
            ("تاريخ الأمر", _dpOrderDate),
            ("تاريخ التسليم المتوقع", _dpExpected),
            ("ملاحظات", _txtNotes))));
        stack.Children.Add(ErpUiFactory.Card(_linesHost));
        stack.Children.Add(addBtn);
        stack.Children.Add(_txtSummary);
        stack.Children.Add(actions);
        Content = new ScrollViewer { Content = stack };

        Loaded += OnLoaded;
        _btnSave.Click += async (_, _) => await SaveAsync();
        _btnSend.Click += async (_, _) => await SendAsync();
        _btnConvert.Click += async (_, _) => await ConvertAsync();
        _btnCancel.Click += async (_, _) => await CancelOrderAsync();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        await LoadLookupsAsync();
        _editId = PurchaseNavigationContext.EditOrderId;
        if (_editId is Guid id)
        {
            var result = await PurchaseUiService.Instance.GetOrderDetailsAsync(id);
            if (!ApplicationResultPresenter.Present(result) || result.Value is null) return;
            var order = result.Value;
            _txtNumber.Text = order.OrderNumber;
            _txtNumber.IsReadOnly = true;
            _dpOrderDate.SelectedDate = order.OrderDate;
            _dpExpected.SelectedDate = order.ExpectedDeliveryDate;
            _txtNotes.Text = order.Notes ?? "";
            _status = order.Status;
            SelectSupplier(order.SupplierId);
            foreach (var line in order.Lines)
            {
                var editor = AddLine();
                await editor.LoadFromAsync(line.FabricItemId, null, line.FabricItemName ?? line.Description, "", line.Quantity, line.UnitCost);
            }
            ApplyReadOnly(order.IsReadOnly);
        }
        else
        {
            _txtNumber.Text = await PurchaseUiService.Instance.NextOrderNumberAsync();
            AddLine();
        }
    }

    private async Task LoadLookupsAsync()
    {
        var fabrics = await PurchaseUiService.Instance.GetFabricItemsAsync();
        if (fabrics.IsSuccess) _fabrics = fabrics.Value!;

        var suppliers = await SupplierUiService.Instance.GetListAsync(null, pageSize: 200);
        if (suppliers.IsSuccess)
        {
            _cmbSupplier.Items.Clear();
            foreach (var s in suppliers.Value!.Items)
                _cmbSupplier.Items.Add(new ComboBoxItem { Content = s.NameAr, Tag = s });
        }
    }

    private PurchaseLineEditors.InventoryLineEditor AddLine()
    {
        var editor = new PurchaseLineEditors.InventoryLineEditor(
            _fabrics,
            id => PurchaseUiService.Instance.GetFabricColorsAsync(id),
            RemoveLine);
        _lines.Add(editor);
        _linesHost.Children.Add(editor.Row);
        return editor;
    }

    private void RemoveLine(PurchaseLineEditors.InventoryLineEditor editor)
    {
        _lines.Remove(editor);
        _linesHost.Children.Remove(editor.Row);
    }

    private void ApplyReadOnly(bool readOnly)
    {
        _btnSave.IsEnabled = !readOnly;
        _btnSend.IsEnabled = _status == PurchaseOrderStatus.Draft;
        _btnConvert.IsEnabled = _status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Sent;
        _btnCancel.IsEnabled = _status is PurchaseOrderStatus.Draft or PurchaseOrderStatus.Sent;
        _linesHost.IsEnabled = !readOnly;
        _cmbSupplier.IsEnabled = !readOnly;
        _dpExpected.IsEnabled = !readOnly;
        _txtNotes.IsReadOnly = readOnly;
    }

    private async Task SaveAsync()
    {
        if (!TryBuild(out var supplierId, out var lines)) return;
        if (_editId is Guid id)
        {
            var result = await PurchaseUiService.Instance.UpdateOrderAsync(new UpdatePurchaseOrderCommand
            {
                OrderId = id,
                SupplierId = supplierId,
                ExpectedDeliveryDate = _dpExpected.SelectedDate,
                Notes = _txtNotes.Text.Trim(),
                Lines = lines
            });
            if (ApplicationResultPresenter.Present(result))
                MockInteractionService.ShowSuccess("تم حفظ أمر الشراء.");
        }
        else
        {
            var result = await PurchaseUiService.Instance.CreateOrderAsync(new CreatePurchaseOrderCommand
            {
                SupplierId = supplierId,
                OrderDate = _dpOrderDate.SelectedDate ?? DateTime.Today,
                ExpectedDeliveryDate = _dpExpected.SelectedDate,
                Notes = _txtNotes.Text.Trim(),
                Lines = lines
            });
            if (ApplicationResultPresenter.Present(result))
            {
                _editId = result.Value;
                PurchaseNavigationContext.BeginOrderEdit(_editId.Value);
                MockInteractionService.ShowSuccess("تم إنشاء أمر الشراء.");
            }
        }
    }

    private async Task SendAsync()
    {
        if (_editId is null)
        {
            await SaveAsync();
            if (_editId is null) return;
        }
        var result = await PurchaseUiService.Instance.SendOrderAsync(_editId.Value);
        if (ApplicationResultPresenter.Present(result))
        {
            _status = PurchaseOrderStatus.Sent;
            ApplyReadOnly(false);
            _btnSend.IsEnabled = false;
            MockInteractionService.ShowSuccess("تم إرسال الأمر للمورد.");
        }
    }

    private async Task ConvertAsync()
    {
        if (_editId is null)
        {
            await SaveAsync();
            if (_editId is null) return;
        }
        var result = await PurchaseUiService.Instance.ConvertOrderToInvoiceAsync(_editId.Value);
        if (ApplicationResultPresenter.Present(result))
        {
            PurchaseNavigationContext.BeginEdit(result.Value);
            MockInteractionService.ShowSuccess("تم إنشاء مسودة فاتورة شراء من الأمر.");
            MockInteractionService.Navigate(AppModule.Purchases, "Form");
        }
    }

    private async Task CancelOrderAsync()
    {
        if (_editId is Guid id)
        {
            var result = await PurchaseUiService.Instance.CancelOrderAsync(id);
            if (!ApplicationResultPresenter.Present(result)) return;
        }
        MockInteractionService.Navigate(AppModule.Purchases, "Orders");
    }

    private bool TryBuild(out Guid supplierId, out List<PurchaseOrderLineInput> lines)
    {
        supplierId = Guid.Empty;
        lines = [];
        if (_cmbSupplier.SelectedItem is not ComboBoxItem supItem || supItem.Tag is not SupplierListDto supplier)
        {
            MockInteractionService.ShowWarning("اختر المورد.");
            return false;
        }
        supplierId = supplier.Id;
        foreach (var editor in _lines)
        {
            if (!editor.TryReadOrderLine(out var line))
            {
                MockInteractionService.ShowWarning("أكمل بيانات السطور (صنف، كمية، تكلفة).");
                return false;
            }
            lines.Add(line);
        }
        if (lines.Count == 0)
        {
            MockInteractionService.ShowWarning("أضف سطراً واحداً على الأقل.");
            return false;
        }
        return true;
    }

    private void SelectSupplier(Guid id)
    {
        foreach (ComboBoxItem item in _cmbSupplier.Items)
            if (item.Tag is SupplierListDto s && s.Id == id)
                _cmbSupplier.SelectedItem = item;
    }

    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
