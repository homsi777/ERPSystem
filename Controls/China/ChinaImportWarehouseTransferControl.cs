using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Application.DTOs.Warehouses;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Infrastructure.Seed;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class ChinaImportWarehouseTransferControl : UserControl
{
    private readonly StackPanel _detailsHost = new();
    private readonly ComboBox _warehouseCombo = new() { MinWidth = 280, IsEditable = false };
    private readonly Button _transferButton;
    private ContainerOperationsCenterDto? _loaded;

    public ChinaImportWarehouseTransferControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 6: تحويل للمخزن"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", true, true),
            ("Landing Cost", true, true),
            ("اعتماد", true, true),
            ("تحويل للمخزن", true, true),
            ("جاهز للبيع", false, false)));

        stack.Children.Add(ErpUxFactory.InfoBanner(
            "اختر المستودع لترحيل أرصدة الحاوية. سيتم إنشاء حركة مخزون (استيراد) وإتاحة الأقمشة للبيع.",
            "info"));

        stack.Children.Add(_detailsHost);

        var warehouseRow = ErpUiFactory.BuildFormGrid(("المستودع", _warehouseCombo));
        stack.Children.Add(ErpUiFactory.Card(warehouseRow));

        _transferButton = new Button
        {
            Content = "ترحيل للمخزن",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 0, 0),
            IsEnabled = false
        };
        _transferButton.Click += async (_, _) => await TransferAsync();

        var backButton = new Button
        {
            Content = "مراجعة Landing Cost",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 8, 0)
        };
        backButton.Click += (_, _) =>
            ChinaImportNavigation.Navigate("LandingCost", _loaded?.Container.Status);

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(backButton);
        actions.Children.Add(_transferButton);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _detailsHost.Children.Clear();

        if (!AppServices.IsInitialized)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner("الخدمات غير مهيأة.", "warning"));
            return;
        }

        var containerId = ChinaImportNavigationContext.ResolveContainerId();
        if (containerId is null || containerId == Guid.Empty)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner("لا توجد حاوية. أكمل الاعتماد أولاً.", "warning"));
            return;
        }

        var opsResult = await ContainerUiService.Instance.GetOperationsCenterAsync(containerId.Value);
        if (!ApplicationResultPresenter.Present(opsResult) || opsResult.Value is null)
            return;

        _loaded = opsResult.Value;
        var c = _loaded.Container;

        if (c.Status == ChinaContainerStatus.InWarehouse)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner("تم ترحيل الحاوية للمخزن مسبقاً.", "success"));
            ChinaImportNavigation.Navigate("ReadyForSale", c.Status);
            return;
        }

        if (c.Status != ChinaContainerStatus.Approved)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner(
                $"الحاوية في حالة «{c.Status.ToArabic()}». يجب اعتمادها قبل الترحيل للمخزن.",
                "warning"));
            _transferButton.IsEnabled = false;
            return;
        }

        if (!string.IsNullOrWhiteSpace(_loaded.MoveToWarehouseBlockReason))
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner(_loaded.MoveToWarehouseBlockReason, "warning"));
            _transferButton.IsEnabled = false;
            return;
        }

        _detailsHost.Children.Add(new TextBlock
        {
            Text = $"الحاوية: {c.ContainerNumber} — {c.TotalRolls} توب / {c.TotalMeters:N2} م",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        var warehouses = await ContainerUiService.Instance.GetWarehousesAsync();
        _warehouseCombo.ItemsSource = warehouses;
        _warehouseCombo.DisplayMemberPath = nameof(WarehouseListDto.NameAr);
        _warehouseCombo.SelectedValuePath = nameof(WarehouseListDto.Id);

        if (warehouses.Count > 0)
        {
            var defaultWh = warehouses.FirstOrDefault(w => w.Id == DatabaseSeeder.DefaultWarehouseId);
            _warehouseCombo.SelectedItem = defaultWh ?? warehouses[0];
        }

        _transferButton.IsEnabled = _loaded.CanMoveToWarehouse && warehouses.Count > 0;
        if (warehouses.Count == 0)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner(
                "لا توجد مستودعات نشطة. أضف مستودعاً من «المخزون › المستودعات» ثم أعد فتح هذه الشاشة.",
                "warning"));
        }
    }

    private async Task TransferAsync()
    {
        if (_loaded is null || _warehouseCombo.SelectedValue is not Guid warehouseId || warehouseId == Guid.Empty)
        {
            MessageBox.Show("يرجى اختيار المستودع.", "تحويل للمخزن",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _transferButton.IsEnabled = false;
        _transferButton.Content = "جاري الترحيل...";
        try
        {
            var result = await ContainerUiService.Instance.MoveToWarehouseAsync(
                _loaded.Container.Id,
                warehouseId);

            if (!ApplicationResultPresenter.Present(result))
                return;

            ChinaImportNavigationContext.SetActiveContainer(_loaded.Container.Id);
            MessageBox.Show(
                "تم ترحيل الحاوية للمخزن بنجاح.\nالأقمشة متاحة الآن في المخزون.",
                "تحويل للمخزن",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ChinaImportNavigation.Navigate("ReadyForSale", ChinaContainerStatus.InWarehouse);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر ترحيل الحاوية.\n\n{ex.Message}", "تحويل للمخزن",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _transferButton.Content = "ترحيل للمخزن";
            if (_loaded?.CanMoveToWarehouse == true)
                _transferButton.IsEnabled = true;
        }
    }
}
