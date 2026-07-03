using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryWarehouseFormControl : UserControl
{
    private readonly TextBlock _title = ErpUiFactory.SectionTitle("مستودع جديد");
    private readonly TextBlock _hint = new()
    {
        Text = "أدخل بيانات المستودع. الكود يجب أن يكون فريداً داخل الفرع.",
        Foreground = Br("TextSecondaryBrush"),
        Margin = new Thickness(0, 0, 0, 12),
        TextWrapping = TextWrapping.Wrap,
        FontSize = 12
    };
    private readonly TextBox _code = ErpUiFactory.FormField("");
    private readonly TextBox _nameAr = ErpUiFactory.FormField("");
    private readonly TextBox _nameEn = ErpUiFactory.FormField("");
    private readonly TextBox _city = ErpUiFactory.FormField("");
    private readonly TextBox _address = ErpUiFactory.FormField("");
    private readonly TextBox _manager = ErpUiFactory.FormField("");
    private readonly TextBox _description = ErpUiFactory.FormField("");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly TextBox _capacity = ErpUiFactory.FormField("");
    private readonly CheckBox _isDefault = new() { Content = "مستودع افتراضي للفرع", Margin = new Thickness(0, 4, 0, 0) };
    private readonly Border _metaCard;
    private readonly TextBlock _meta = new() { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Br("TextSecondaryBrush") };
    private readonly StackPanel _auditSection = new() { Margin = new Thickness(0, 12, 0, 0), Visibility = Visibility.Collapsed };
    private readonly DataGrid _auditPreview = new() { AutoGenerateColumns = false, IsReadOnly = true, MaxHeight = 120 };
    private readonly Button _save = new() { Content = "حفظ المستودع", Style = S("PrimaryButtonStyle"), MinWidth = 140, Height = 38 };
    private readonly Button _cancel = new() { Content = "إلغاء", Style = S("SecondaryButtonStyle"), MinWidth = 100, Height = 38, Margin = new Thickness(8, 0, 0, 0) };

    private Guid? _editId;
    private bool _popupMode;
    private bool _saving;

    public InventoryWarehouseFormControl()
    {
        _metaCard = ErpUiFactory.Card(_meta);
        _metaCard.Visibility = Visibility.Collapsed;
        _metaCard.Margin = new Thickness(0, 12, 0, 0);

        _auditSection.Children.Add(new TextBlock
        {
            Text = "آخر التدقيق",
            FontWeight = FontWeights.SemiBold,
            FontSize = 12,
            Foreground = Br("TextMutedBrush"),
            Margin = new Thickness(0, 0, 0, 6)
        });
        ErpUiFactory.AddGridColumn(_auditPreview, "التاريخ", nameof(InventoryAuditDto.RecordedAt), 110, null);
        ErpUiFactory.AddGridColumn(_auditPreview, "الإجراء", nameof(InventoryAuditDto.Action), "*", null);
        ErpUiFactory.AddGridColumn(_auditPreview, "المستخدم", nameof(InventoryAuditDto.Username), 90, null);
        _auditSection.Children.Add(_auditPreview);

        var stack = new StackPanel { MaxWidth = 520 };
        stack.Children.Add(_title);
        stack.Children.Add(_hint);
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الكود *", _code),
            ("الاسم (عربي) *", _nameAr),
            ("الاسم (إنجليزي)", _nameEn),
            ("المدينة *", _city),
            ("العنوان", _address),
            ("المدير", _manager),
            ("الوصف", _description),
            ("السعة (rolls)", _capacity),
            ("ملاحظات", _notes),
            ("", _isDefault))));
        stack.Children.Add(_metaCard);
        stack.Children.Add(_auditSection);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        actions.Children.Add(_save);
        actions.Children.Add(_cancel);
        stack.Children.Add(actions);

        Content = stack;
        Loaded += OnLoaded;
        _save.Click += async (_, _) => await SaveAsync();
        _cancel.Click += (_, _) =>
        {
            if (_popupMode) InventoryPopupService.CancelActive();
            else NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "Warehouses");
        };
    }

    public void BindPopupHost()
    {
        _popupMode = true;
        _title.Visibility = Visibility.Collapsed;
        _hint.Visibility = Visibility.Collapsed;
        _cancel.Visibility = Visibility.Collapsed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        _editId = InventoryNavigationContext.EditWarehouseId;
        if (!_editId.HasValue)
        {
            _title.Text = "مستودع جديد";
            return;
        }

        _title.Text = "تعديل مستودع";
        _code.IsReadOnly = true;

        var result = await InventoryUiService.Instance.GetWarehouseDetailAsync(_editId.Value);
        if (!result.IsSuccess || result.Value is null) return;
        var w = result.Value;

        _code.Text = w.Code;
        _nameAr.Text = w.NameAr;
        _nameEn.Text = w.NameEn ?? "";
        _city.Text = w.City;
        _address.Text = w.Address ?? "";
        _manager.Text = w.Manager ?? "";
        _description.Text = w.Description ?? "";
        _notes.Text = w.Notes ?? "";
        _capacity.Text = w.CapacityRolls?.ToString() ?? "";
        _isDefault.IsChecked = w.IsDefault;

        _metaCard.Visibility = Visibility.Visible;
        _meta.Text =
            $"الحالة: {(w.IsActive ? "نشط" : "معطل")}  •  القيمة: ${w.InventoryValue:N2}  •  Rolls: {w.RollCount}\n" +
            $"أُنشئ: {w.CreatedAt:yyyy/MM/dd HH:mm}" +
            (w.UpdatedAt.HasValue ? $"  •  آخر تعديل: {w.UpdatedAt:yyyy/MM/dd HH:mm}" : "");

        if (w.RecentAudit.Count > 0)
        {
            _auditSection.Visibility = Visibility.Visible;
            _auditPreview.ItemsSource = w.RecentAudit;
        }
    }

    private async Task SaveAsync()
    {
        if (_saving || !AppServices.IsInitialized) return;
        if (string.IsNullOrWhiteSpace(_code.Text) || string.IsNullOrWhiteSpace(_nameAr.Text) || string.IsNullOrWhiteSpace(_city.Text))
        {
            MessageBox.Show("الكود والاسم والمدينة مطلوبة.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _saving = true;
        _save.IsEnabled = false;
        try
        {
            int? capacity = int.TryParse(_capacity.Text, out var c) ? c : null;
            if (_editId.HasValue)
            {
                var result = await InventoryUiService.Instance.UpdateWarehouseAsync(new UpdateWarehouseCommand(
                    _editId.Value, _nameAr.Text.Trim(), _city.Text.Trim(), _nameEn.Text.Trim(),
                    _description.Text.Trim(), _address.Text.Trim(), _manager.Text.Trim(),
                    Notes: _notes.Text.Trim(), CapacityRolls: capacity, IsDefault: _isDefault.IsChecked == true));

                if (!ApplicationResultPresenter.Present(result)) return;

                if (_popupMode)
                    InventoryPopupService.CompleteSuccess();
                else
                {
                    InventoryListRefreshHub.RequestRefresh();
                    InventoryNavigationContext.BeginWorkspace(_editId.Value);
                    NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "WarehouseOperationsCenter");
                }
            }
            else
            {
                var result = await InventoryUiService.Instance.CreateWarehouseAsync(new CreateWarehouseCommand(
                    Guid.Empty, _code.Text.Trim(), _nameAr.Text.Trim(), _city.Text.Trim(),
                    _nameEn.Text.Trim(), _description.Text.Trim(), _address.Text.Trim(),
                    _manager.Text.Trim(), Notes: _notes.Text.Trim(), IsDefault: _isDefault.IsChecked == true,
                    CapacityRolls: capacity));

                if (!ApplicationResultPresenter.Present(result) || !result.IsSuccess) return;

                if (_popupMode)
                    InventoryPopupService.CompleteSuccess();
                else
                {
                    InventoryListRefreshHub.RequestRefresh();
                    InventoryNavigationContext.BeginWorkspace(result.Value);
                    NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "WarehouseOperationsCenter");
                }
            }
        }
        finally
        {
            _saving = false;
            _save.IsEnabled = true;
        }
    }

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
    private static Style S(string key) => (Style)System.Windows.Application.Current.Resources[key]!;
}
