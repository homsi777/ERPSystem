using ERPSystem.Application.Common;
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

public sealed class ChinaImportReadyForSaleControl : UserControl
{
    public ChinaImportReadyForSaleControl()
    {
        var root = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Padding = new Thickness(16) };
        var stack = new StackPanel();

        stack.Children.Add(ErpUiFactory.SectionTitle("الخطوة 7: جاهز للبيع"));
        stack.Children.Add(ErpUxFactory.WorkflowStepper(
            ("وصول الحاوية", true, true),
            ("تحليل الملف", true, true),
            ("إدخال التكلفة", true, true),
            ("Landing Cost", true, true),
            ("اعتماد", true, true),
            ("تحويل للمخزن", true, true),
            ("جاهز للبيع", true, true)));

        var contentHost = new StackPanel();
        stack.Children.Add(contentHost);

        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 16, 0, 0) };

        var listButton = new Button
        {
            Content = "قائمة الحاويات",
            Style = (Style)WpfApplication.Current.Resources["SecondaryButtonStyle"]!,
            Margin = new Thickness(0, 0, 8, 0)
        };
        listButton.Click += (_, _) => ChinaImportNavigation.Navigate("Containers");

        var newImportButton = new Button
        {
            Content = "استيراد حاوية جديدة",
            Style = (Style)WpfApplication.Current.Resources["PrimaryButtonStyle"]!
        };
        newImportButton.Click += (_, _) =>
        {
            ChinaImportNavigationContext.Clear();
            ChinaImportNavigation.Navigate("NewImport");
        };

        actions.Children.Add(listButton);
        actions.Children.Add(newImportButton);
        stack.Children.Add(actions);

        root.Content = stack;
        Content = root;
        Background = (SolidColorBrush)WpfApplication.Current.Resources["AppBgBrush"]!;

        Loaded += async (_, _) => await PopulateAsync(contentHost);
    }

    private static async Task PopulateAsync(StackPanel host)
    {
        host.Children.Clear();

        if (!AppServices.IsInitialized)
        {
            host.Children.Add(ErpUxFactory.InfoBanner("الخدمات غير مهيأة.", "warning"));
            return;
        }

        var containerId = ChinaImportNavigationContext.ResolveContainerId();
        if (containerId is null || containerId == Guid.Empty)
        {
            host.Children.Add(ErpUxFactory.InfoBanner("لا توجد حاوية مكتملة لعرضها.", "warning"));
            return;
        }

        var result = await ContainerUiService.Instance.GetOperationsCenterAsync(containerId.Value);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
            return;

        Render(host, result.Value);
    }

    private static void Render(StackPanel host, ContainerOperationsCenterDto ops)
    {
        var c = ops.Container;
        var lc = c.LandingCost;
        var unit = c.DplQuantityUnit;

        if (c.Status == ChinaContainerStatus.InWarehouse && ops.IsReadyForSale)
        {
            host.Children.Add(ErpUxFactory.InfoBanner(
                $"✅ الحاوية «{c.ContainerNumber}» في المخزن وجاهزة للبيع.",
                "success"));
        }
        else if (c.Status == ChinaContainerStatus.Approved)
        {
            host.Children.Add(ErpUxFactory.InfoBanner(
                "الحاوية معتمدة لكن لم تُرحَّل للمخزن بعد. أكمل خطوة «تحويل للمخزن».",
                "warning"));
        }
        else
        {
            host.Children.Add(ErpUxFactory.InfoBanner(
                $"الحاوية في حالة «{c.Status.ToArabic()}». أكمل خطوات الاستيراد.",
                "info"));
        }

        host.Children.Add(new TextBlock
        {
            Text = $"الحاوية: {c.ContainerNumber}",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 12, 0, 8)
        });

        if (lc is not null)
        {
            host.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
                (ChinaImportLengthDisplay.TotalLengthLabel(unit),
                    ErpUiFactory.FormField(ChinaImportLengthDisplay.FormatLength(c.TotalMeters, unit))),
                ("إجمالي الأثواب", ErpUiFactory.FormField($"{c.TotalRolls}")),
                ("فاتورة الصين ($)", ErpUiFactory.FormField($"{c.ChinaInvoiceAmountUsd:N2}")),
                ($"{ChinaImportLengthDisplay.ExpenseCostPerUnitLabel(unit)} ($)",
                    ErpUiFactory.FormField($"{ChinaImportLengthDisplay.FromStoredRate(lc.ExpenseCostPerMeter, unit):N4}")),
                ("احتياطي 2% ($)", ErpUiFactory.FormField($"{c.FinancialTaxReserveUsd:N2}")),
                ("الحالة", ErpUiFactory.FormField(c.Status.ToArabic())))));
        }

        host.Children.Add(ErpUxFactory.KpiStrip(
            (ChinaImportLengthDisplay.LengthColumnHeader(unit),
                ChinaImportLengthDisplay.FormatLength(c.TotalMeters, unit, "N0")),
            ("الأثواب", $"{c.TotalRolls}"),
            (ChinaImportLengthDisplay.CostPerUnitLabel(unit),
                lc is null ? "—" : $"{ChinaImportLengthDisplay.FromStoredRate(lc.ExpenseCostPerMeter, unit):N4} $"),
            ("الحالة", c.Status.ToArabic())));
    }
}
