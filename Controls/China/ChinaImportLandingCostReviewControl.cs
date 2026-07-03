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
    private readonly Button _salePriceButton;
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

        _salePriceButton = new Button
        {
            Content = "إدخال أسعار البيع",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(8, 16, 0, 0),
            Visibility = Visibility.Collapsed
        };
        _salePriceButton.Click += (_, _) =>
            ChinaImportNavigation.Navigate("SalePrice", _loaded?.Container.Status);

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
                ChinaImportNavigation.Navigate("ReadyForSale", _loaded.Container.Status);
            else
                ChinaImportNavigation.Navigate("MoveToWarehouse", _loaded?.Container.Status);
        };

        var backButton = new Button
        {
            Content = "قائمة الحاويات",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 8, 0)
        };
        backButton.Click += (_, _) => ChinaImportNavigation.Navigate("Containers");

        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(backButton);
        actions.Children.Add(_salePriceButton);
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
            ("وضع التخصيص", ErpUiFactory.FormField(lc.UsesWeightedAllocation ? "بالوزن (حسب النوع)" : "مسطح (DPL)")),
            ("جمارك / تخليص ($)", ErpUiFactory.FormField($"{lc.CustomsAmount:N2}")),
            ("الشحن ($)", ErpUiFactory.FormField($"{lc.Shipping:N2}")),
            ("التأمين ($)", ErpUiFactory.FormField($"{lc.Insurance:N2}")),
            ("مصاريف أخرى 1 ($)", ErpUiFactory.FormField($"{lc.OtherExpense1:N2}")),
            ("مصاريف أخرى 2 ($)", ErpUiFactory.FormField($"{lc.OtherExpense2:N2}")),
            ("مصاريف أخرى 3 ($)", ErpUiFactory.FormField($"{lc.OtherExpense3:N2}")),
            ("مصاريف أخرى 4 ($)", ErpUiFactory.FormField($"{lc.OtherExpense4:N2}")),
            ("إجمالي المصاريف المشتركة ($)", ErpUiFactory.FormField($"{lc.TotalImportExpenses:N2}")),
            ("تكلفة الوصول/م ($)", ErpUiFactory.FormField($"{lc.ExpenseCostPerMeter:N4}")),
            ("احتياطي 2% ($)", ErpUiFactory.FormField($"{reserveUsd:N2}")),
            ("احتياطي 2% (محلي)", ErpUiFactory.FormField($"{reserveLocal:N2}")));

        _detailsHost.Children.Add(ErpUiFactory.Card(form));

        if (c.FabricTypeLines.Count > 0)
        {
            _detailsHost.Children.Add(ErpUiFactory.SectionTitle("تكلفة كل نوع قماش"));
            _detailsHost.Children.Add(ErpUiFactory.Card(BuildTypeLinesGrid(c.FabricTypeLines)));
        }

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
        _approveButton.Visibility = ops.CanApprove || c.Status is ChinaContainerStatus.Approved or ChinaContainerStatus.InWarehouse
            ? Visibility.Visible
            : Visibility.Collapsed;

        _salePriceButton.Visibility = ops.CanSetSalePrices ? Visibility.Visible : Visibility.Collapsed;
        if (ops.CanSetSalePrices && !ops.CanApprove)
            _salePriceButton.Content = "إدخال أسعار البيع (مطلوب قبل الاعتماد)";
        else if (c.FabricTypeLines.All(l => l.HasSalePrice))
            _salePriceButton.Content = "تعديل أسعار البيع";
        else
            _salePriceButton.Content = "إدخال أسعار البيع";

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
                ChinaImportNavigation.Navigate("MoveToWarehouse", _loaded?.Container.Status);
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

    private static DataGrid BuildTypeLinesGrid(IReadOnlyList<ContainerFabricTypeLineDto> lines)
    {
        var g = ErpUiFactory.BuildGrid(lines.OrderBy(l => l.LineNumber).ToList(), false);
        g.AutoGenerateColumns = false;
        g.IsReadOnly = true;
        foreach (var (h, p, w, fmt) in new (string, string, object, string?)[]
        {
            ("النوع", nameof(ContainerFabricTypeLineDto.TypeDisplayName), 160, null),
            ("أمتار", nameof(ContainerFabricTypeLineDto.LengthMeters), 80, "N0"),
            ("وزن (كغ)", nameof(ContainerFabricTypeLineDto.NetWeightKg), 80, "N0"),
            ("سعر الصين/م", nameof(ContainerFabricTypeLineDto.ChinaUnitPriceUsd), 90, "N4"),
            ("حصة المصاريف", nameof(ContainerFabricTypeLineDto.ExpenseShareUsd), 90, "N2"),
            ("تكلفة/م", nameof(ContainerFabricTypeLineDto.LandedCostPerMeterUsd), 90, "N4"),
            ("سعر البيع/م", nameof(ContainerFabricTypeLineDto.SalePricePerMeterUsd), 90, "N4")
        })
        {
            ErpUiFactory.AddGridColumn(g, h, p, w, fmt);
        }
        return g;
    }
}
