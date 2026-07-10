using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Domain.Enums;
using ERPSystem.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace ERPSystem.Services.Sales;

/// <summary>
/// قائمة مهام فاتورة المبيعات — تظهر بالنقر بالزر الأيمن أو زر «⋮» عند صف الفاتورة.
/// يتم فلترة الإجراءات حسب حالة الفاتورة (Draft, AwaitingDetailing, Approved, ...).
/// </summary>
public static class SalesContextMenuService
{
    public static void Show(SalesInvoiceListRow row, UIElement placementTarget)
    {
        var items = BuildActionsForStatus(row.Status);
        if (items.Count == 0) return;

        var menu = new ContextMenu
        {
            FlowDirection = FlowDirection.RightToLeft,
            MinWidth = 240,
            Padding = new Thickness(4),
            Background = Br("SurfaceBrush"),
            BorderBrush = Br("BorderBrush"),
            BorderThickness = new Thickness(1)
        };

        menu.Items.Add(BuildInvoiceHeader(row));
        menu.Items.Add(new Separator());

        string? currentGroup = null;
        foreach (var it in items)
        {
            if (it.Group != currentGroup && it.Group != null)
            {
                if (menu.Items.Count > 2)
                    menu.Items.Add(new Separator());
                menu.Items.Add(BuildGroupHeader(it.Group));
                currentGroup = it.Group;
            }

            var captured = it;
            var mi = new MenuItem
            {
                Header = BuildMenuHeader(captured.IconGlyph, captured.LabelAr, captured.IsDestructive),
                FontFamily = new FontFamily("Segoe UI, Tahoma, Arial"),
                FontSize = 13,
                Padding = new Thickness(12, 8, 12, 8)
            };
            if (captured.IsDestructive)
                mi.Foreground = Br("DangerBrush");

            mi.Click += (_, _) => SalesActionRouter.Handle(captured.ActionId, row);
            menu.Items.Add(mi);
        }

        menu.PlacementTarget = placementTarget;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static IReadOnlyList<SalesMenuItem> BuildActionsForStatus(SalesInvoiceStatus status) => status switch
    {
        SalesInvoiceStatus.Draft =>
        [
            new("العرض والتحرير", "\uE70F", "تعديل", EntityActionId.InvoiceEdit, false),
            new("العرض والتحرير", "\uE8A7", "مركز العمليات", EntityActionId.OpenOperationsCenter, false),
            new("سير العمل", "\uE72A", "إرسال للمستودع", EntityActionId.InvoiceSendToWarehouse, false),
            new("خطر", "\uE711", "إلغاء الفاتورة", EntityActionId.InvoiceCancel, true)
        ],
        SalesInvoiceStatus.AwaitingDetailing =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات", EntityActionId.OpenOperationsCenter, false),
            new("سير العمل", "\uE9F5", "تفصيل الأطوال", EntityActionId.InvoiceDetailLengths, false),
            new("خطر", "\uE711", "إلغاء الفاتورة", EntityActionId.InvoiceCancel, true)
        ],
        SalesInvoiceStatus.Detailed or SalesInvoiceStatus.ReadyForApproval =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات", EntityActionId.OpenOperationsCenter, false),
            new("سير العمل", "\uE73E", "اعتماد", EntityActionId.InvoiceApprove, false),
            new("سير العمل", "\uE7C1", "اعتماد وتسليم", EntityActionId.InvoiceApproveAndDeliver, false),
            new("طباعة", "\uE749", "طباعة", EntityActionId.InvoicePrint, false),
            new("طباعة", "\uE896", "تصدير PDF", EntityActionId.InvoiceExportPdf, false),
            new("خطر", "\uE711", "إلغاء الفاتورة", EntityActionId.InvoiceCancel, true)
        ],
        SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات", EntityActionId.OpenOperationsCenter, false),
            new("سير العمل", "\uE7C1", "تسليم", EntityActionId.InvoiceDeliver, false),
            new("سير العمل", "\uE72C", "مرتجع", EntityActionId.InvoiceReturn, false),
            new("طباعة", "\uE749", "طباعة", EntityActionId.InvoicePrint, false),
            new("طباعة", "\uE896", "تصدير PDF", EntityActionId.InvoiceExportPdf, false),
            new("العميل", "\uE717", "اتصل بالعميل", EntityActionId.InvoiceCallCustomer, false)
        ],
        SalesInvoiceStatus.Delivered =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات", EntityActionId.OpenOperationsCenter, false),
            new("سير العمل", "\uE72C", "مرتجع", EntityActionId.InvoiceReturn, false),
            new("طباعة", "\uE749", "طباعة", EntityActionId.InvoicePrint, false),
            new("طباعة", "\uE896", "تصدير PDF", EntityActionId.InvoiceExportPdf, false),
            new("العميل", "\uE717", "اتصل بالعميل", EntityActionId.InvoiceCallCustomer, false)
        ],
        SalesInvoiceStatus.PartiallyReturned =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات", EntityActionId.OpenOperationsCenter, false),
            new("سير العمل", "\uE72C", "مرتجع", EntityActionId.InvoiceReturn, false),
            new("العرض والتحرير", "\uE8F1", "عرض المرتجعات", EntityActionId.InvoiceViewReturns, false),
            new("طباعة", "\uE749", "طباعة", EntityActionId.InvoicePrint, false)
        ],
        SalesInvoiceStatus.Returned =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات (قراءة فقط)", EntityActionId.OpenOperationsCenter, false),
            new("العرض والتحرير", "\uE8F1", "عرض المرتجعات", EntityActionId.InvoiceViewReturns, false),
            new("طباعة", "\uE749", "طباعة", EntityActionId.InvoicePrint, false)
        ],
        SalesInvoiceStatus.Cancelled =>
        [
            new("العرض والتحرير", "\uE8A7", "مركز العمليات (قراءة فقط)", EntityActionId.OpenOperationsCenter, false)
        ],
        _ => Array.Empty<SalesMenuItem>()
    };

    private sealed record SalesMenuItem(string? Group, string IconGlyph, string LabelAr, EntityActionId ActionId, bool IsDestructive);

    private static MenuItem BuildInvoiceHeader(SalesInvoiceListRow row)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = $"{row.InvoiceNumber} • {row.CustomerName}",
            FontWeight = FontWeights.SemiBold,
            FontSize = 13,
            Foreground = Br("TextPrimaryBrush")
        });
        sp.Children.Add(new TextBlock
        {
            Text = $"{row.StatusDisplay} • ${row.Amount:N2} • {row.Date:yyyy/MM/dd}",
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 0),
            Foreground = Br("TextMutedBrush")
        });
        return new MenuItem
        {
            Header = sp,
            IsEnabled = false,
            Padding = new Thickness(12, 10, 12, 10),
            FontFamily = new FontFamily("Segoe UI, Tahoma, Arial")
        };
    }

    private static MenuItem BuildGroupHeader(string group) => new()
    {
        Header = group,
        IsEnabled = false,
        FontWeight = FontWeights.SemiBold,
        FontSize = 11,
        Padding = new Thickness(12, 6, 12, 4),
        Foreground = Br("TextMutedBrush")
    };

    private static object BuildMenuHeader(string glyph, string label, bool danger)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal };
        sp.Children.Add(new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily("Segoe MDL2 Assets"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 10, 0),
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = danger ? Br("DangerBrush") : Br("TextSecondaryBrush")
        });
        sp.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
        return sp;
    }

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
}
