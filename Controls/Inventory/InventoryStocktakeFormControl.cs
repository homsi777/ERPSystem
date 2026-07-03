using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.DTOs.Inventory;
using ERPSystem.Core;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Inventory;
using System.Windows;
using System.Windows.Controls;

namespace ERPSystem.Controls.Inventory;

public sealed class InventoryStocktakeFormControl : UserControl
{
    private readonly ComboBox _warehouse = new() { MinWidth = 220, DisplayMemberPath = nameof(WarehouseListExtendedDto.NameAr) };
    private readonly TextBox _responsible = ErpUiFactory.FormField("");
    private readonly TextBox _notes = ErpUiFactory.FormField("");
    private readonly TextBlock _status = new() { Margin = new Thickness(0, 8, 0, 0), TextWrapping = TextWrapping.Wrap };
    private Guid? _sessionId;

    public InventoryStocktakeFormControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();
        stack.Children.Add(ErpUiFactory.SectionTitle("جلسة جرد"));
        stack.Children.Add(ErpUxFactory.InfoBanner("إنشاء جلسة جرد جديدة — بعد العد يمكن ترحيل الفروقات تلقائياً عبر محرك المخزون."));
        stack.Children.Add(ErpUiFactory.BuildFormGrid(
            ("المستودع *", _warehouse),
            ("المسؤول *", _responsible),
            ("ملاحظات", _notes)));
        stack.Children.Add(_status);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };
        var createBtn = new Button { Content = "بدء الجرد", Style = (Style)System.Windows.Application.Current.Resources["PrimaryButtonStyle"]!, Margin = new Thickness(0, 0, 8, 0) };
        createBtn.Click += async (_, _) => await CreateAsync();
        var postBtn = new Button { Content = "ترحيل الجرد", Style = (Style)System.Windows.Application.Current.Resources["SecondaryButtonStyle"]! };
        postBtn.Click += async (_, _) => await PostAsync();
        actions.Children.Add(createBtn);
        actions.Children.Add(postBtn);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Loaded += async (_, _) => await InitAsync();
    }

    private async Task InitAsync()
    {
        if (!AppServices.IsInitialized) return;
        var wh = await InventoryUiService.Instance.GetWarehousesAsync();
        if (wh.IsSuccess && wh.Value is not null)
        {
            _warehouse.ItemsSource = wh.Value;
            var pre = InventoryNavigationContext.TakePreselectedStocktakeWarehouse();
            if (pre.HasValue)
                _warehouse.SelectedItem = wh.Value.FirstOrDefault(w => w.Id == pre.Value);
        }
        _status.Text = "لم تبدأ جلسة بعد.";
    }

    private async Task CreateAsync()
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

        if (ApplicationResultPresenter.Present(result) && result.IsSuccess)
        {
            _sessionId = result.Value;
            _status.Text = "تم إنشاء جلسة الجرد — يمكنك ترحيلها بعد مراجعة الفروقات.";
            InventoryListRefreshHub.RequestRefresh();
        }
    }

    private async Task PostAsync()
    {
        if (!_sessionId.HasValue)
        {
            MessageBox.Show("أنشئ جلسة جرد أولاً.", "تحقق", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var result = await InventoryUiService.Instance.PostStocktakeAsync(_sessionId.Value);
        if (ApplicationResultPresenter.Present(result))
        {
            InventoryListRefreshHub.RequestRefresh();
            NavigationStateManager.Instance.NavigateTo(AppModule.Inventory, "Stocktake");
        }
    }
}
