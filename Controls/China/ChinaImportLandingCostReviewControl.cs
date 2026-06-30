using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Controls;
using ERPSystem.Core;
using ERPSystem.Core.ChinaImport;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.China;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfApplication = System.Windows.Application;

namespace ERPSystem.Controls.China;

public sealed class ChinaImportLandingCostReviewControl : UserControl
{
    private readonly StackPanel _detailsHost = new();
    private readonly Button _approveButton;
    private readonly Button _warehouseButton;
    private ContainerOperationsCenterDto? _loaded;

    public ChinaImportLandingCostReviewControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 4: مراجعة Landing Cost"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", true, true),
            ("Landing Cost", true, true),
            ("اعتماد", false, false),
            ("تحويل للمخزن", false, false),
            ("جاهز للبيع", false, false)));

        stack.Children.Add(ErpUxFactory.InfoBanner(
            "تكاليف الوصول ($) تُوزَّع على الأمتار. احتياطي 2% ضريبة مالية يُرحَّل عند اعتماد الحاوية — لا يدخل سعر المتر.",
            "info"));

        stack.Children.Add(_detailsHost);

        _approveButton = new Button
        {
            Content = "اعتماد الحاوية",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 0, 0),
            IsEnabled = false
        };
        _approveButton.Click += async (_, _) => await ApproveAsync();

        _warehouseButton = new Button
        {
            Content = "التالي — تحويل للمخزن",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(8, 16, 0, 0),
            Visibility = Visibility.Collapsed
        };
        _warehouseButton.Click += (_, _) =>
        {
            if (_loaded?.Container.Status == ChinaContainerStatus.InWarehouse)
                MockInteractionService.Navigate(AppModule.ChinaImport, "ReadyForSale");
            else
                MockInteractionService.Navigate(AppModule.ChinaImport, "MoveToWarehouse");
        };

        var backButton = new Button
        {
            Content = "قائمة الحاويات",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 8, 0)
        };
        backButton.Click += (_, _) => MockInteractionService.Navigate(AppModule.ChinaImport, "Containers");

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(backButton);
        actions.Children.Add(_approveButton);
        actions.Children.Add(_warehouseButton);
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
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner("لا توجد حاوية للمراجعة. أكمل إدخال التكلفة أولاً.", "warning"));
            return;
        }

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(containerId.Value);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        _loaded = result.Value;
        RenderDetails(_loaded);
    }

    private void RenderDetails(ContainerOperationsCenterDto ops)
    {
        var c = ops.Container;
        var lc = c.LandingCost;

        _detailsHost.Children.Add(new TextBlock
        {
            Text = $"الحاوية: {c.ContainerNumber} — {c.Status.ToArabic()}",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 12)
        });

        if (lc is null)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner("لم تُحسب تكاليف الوصول بعد.", "warning"));
            return;
        }

        var reserveUsd = c.FinancialTaxReserveUsd;
        var reserveLocal = c.FinancialTaxReservePostedLocal
            ?? (c.ChinaInvoiceAmountUsd > 0 ? reserveUsd * c.ExchangeRateToLocalCurrency : 0m);

        if (c.Status == ChinaContainerStatus.Approved)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner(
                $"تم اعتماد الحاوية. احتياطي ضريبة مالية مُرحَّل: {reserveLocal:N2} (محلي)",
                "success"));
        }
        else if (c.Status == ChinaContainerStatus.InWarehouse)
        {
            _detailsHost.Children.Add(ErpUxFactory.InfoBanner(
                "الحاوية في المخزن وجاهزة للبيع.",
                "success"));
        }

        var form = ErpUiFactory.BuildFormGrid(
            ("إجمالي الأمتار", ErpUiFactory.FormField($"{lc.TotalLengthMeters:N2} م")),
            ("وزن الحاوية (كغ)", ErpUiFactory.FormField($"{lc.ContainerWeightKg:N0}")),
            ("فاتورة الصين ($)", ErpUiFactory.FormField($"{c.ChinaInvoiceAmountUsd:N2}")),
            ("سعر الصرف", ErpUiFactory.FormField($"{c.ExchangeRateToLocalCurrency:N4}")),
            ("الجمارك ($)", ErpUiFactory.FormField($"{lc.CustomsAmount:N2}")),
            ("تكلفة الجمارك/م ($)", ErpUiFactory.FormField($"{lc.CustomsCostPerMeter:N4}")),
            ("الشحن ($)", ErpUiFactory.FormField($"{lc.Shipping:N2}")),
            ("التخليص ($)", ErpUiFactory.FormField($"{lc.Clearance:N2}")),
            ("مصاريف أخرى ($)", ErpUiFactory.FormField($"{lc.OtherExpenses:N2}")),
            ("إجمالي تكاليف الوصول ($)", ErpUiFactory.FormField($"{lc.TotalImportExpenses:N2}")),
            ("تكلفة الوصول/م ($)", ErpUiFactory.FormField($"{lc.ExpenseCostPerMeter:N4}")),
            ("احتياطي 2% ($)", ErpUiFactory.FormField($"{reserveUsd:N2}")),
            ("احتياطي 2% (محلي)", ErpUiFactory.FormField($"{reserveLocal:N2}")));

        _detailsHost.Children.Add(ErpUiFactory.Card(form));
        _detailsHost.Children.Add(ErpUxFactory.KpiStrip(
            ("تكلفة الجمارك/م", $"{lc.CustomsCostPerMeter:N4} $"),
            ("تكلفة الوصول/م", $"{lc.ExpenseCostPerMeter:N4} $"),
            ("متوسط غ/م", $"{lc.AvgGramPerMeter:N2}"),
            ("احتياطي 2%", $"{reserveUsd:N2} $")));

        _approveButton.IsEnabled = ops.CanApprove;
        _approveButton.Content = c.Status switch
        {
            ChinaContainerStatus.Approved => "معتمدة ✓",
            ChinaContainerStatus.InWarehouse => "معتمدة ✓",
            _ => "اعتماد الحاوية"
        };
        _approveButton.Visibility = ops.CanApprove ? Visibility.Visible : Visibility.Collapsed;

        var showWarehouse = c.Status == ChinaContainerStatus.Approved && ops.CanMoveToWarehouse;
        var showReady = c.Status == ChinaContainerStatus.InWarehouse;
        _warehouseButton.Visibility = showWarehouse || showReady ? Visibility.Visible : Visibility.Collapsed;
        _warehouseButton.Content = showReady ? "عرض — جاهز للبيع" : "التالي — تحويل للمخزن";
    }

    private async Task ApproveAsync()
    {
        if (_loaded is null)
            return;

        _approveButton.IsEnabled = false;
        _approveButton.Content = "جاري الاعتماد...";
        try
        {
            var result = await ContainerUiService.Instance.ApproveContainerAsync(_loaded.Container.Id);
            if (!ApplicationResultPresenter.Present(result))
                return;

            MessageBox.Show(
                "تم اعتماد الحاوية.\nتم تسجيل احتياطي 2% ضريبة مالية (محلي) في سجل الاستيراد.",
                "اعتماد الحاوية",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            ChinaImportNavigationContext.SetActiveContainer(_loaded.Container.Id);
            await LoadAsync();
            if (_loaded.Container.Status == ChinaContainerStatus.Approved)
                MockInteractionService.Navigate(AppModule.ChinaImport, "MoveToWarehouse");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذّر اعتماد الحاوية.\n\n{ex.Message}", "اعتماد الحاوية",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            if (_loaded?.Container.Status is ChinaContainerStatus.Approved or ChinaContainerStatus.InWarehouse)
            {
                _approveButton.Content = "معتمدة ✓";
                _approveButton.IsEnabled = false;
            }
            else
            {
                _approveButton.Content = "اعتماد الحاوية";
                _approveButton.IsEnabled = _loaded?.CanApprove ?? false;
            }
        }
    }
}
