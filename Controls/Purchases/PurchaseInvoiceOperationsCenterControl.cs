using ERPSystem.Application.DTOs.Purchases;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Workspace;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Purchases;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ERPSystem.Controls.Purchases;

public sealed class PurchaseInvoiceOperationsCenterControl : UserControl
{
    private Guid _invoiceId;

    public PurchaseInvoiceOperationsCenterControl()
    {
        Content = new TextBlock { Text = "جاري تحميل فاتورة الشراء...", Margin = new Thickness(24) };
    }

    public void Initialize(Guid invoiceId) => _invoiceId = invoiceId;

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        var result = await PurchaseUiService.Instance.GetOperationsCenterAsync(_invoiceId);
        if (!ApplicationResultPresenter.Present(result) || result.Value is null)
        {
            Content = PlaceholderUi.EmptyMessage("تعذّر تحميل الفاتورة");
            return;
        }
        Content = BuildShell(result.Value);
    }

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        Loaded += OnLoaded;
    }

    private static UserControl BuildShell(PurchaseOperationsCenterDto data)
    {
        var inv = data.Invoice;
        var row = PurchaseListRow.FromDto(new PurchaseInvoiceListDto
        {
            Id = inv.Id,
            InvoiceNumber = inv.InvoiceNumber,
            InvoiceDate = inv.InvoiceDate,
            DueDate = inv.DueDate,
            SupplierName = inv.SupplierName,
            TotalAmount = inv.TotalAmount,
            PaidAmount = inv.PaidAmount,
            RemainingAmount = inv.RemainingAmount,
            Status = inv.Status,
            StatusDisplay = inv.StatusDisplay,
            IsOverdue = data.IsOverdue
        });

        var linesGrid = ErpUiFactory.Card(ErpUiFactory.BuildGrid(inv.Lines.Select(l => new
        {
            النوع = l.LineType.ToString(),
            الصنف = l.FabricItemName ?? l.Description,
            الكمية = l.QuantityMeters,
            السعر = l.UnitPrice,
            الإجمالي = l.LineTotal
        }).ToArray(), false));

        var journalGrid = ErpUiFactory.Card(ErpUiFactory.BuildGrid(data.JournalEntries.Select(j => new
        {
            رقم_القيد = j.EntryNumber,
            التاريخ = j.EntryDate.ToString("yyyy/MM/dd"),
            البيان = j.Description,
            مدين = j.Debit,
            دائن = j.Credit
        }).ToArray(), false));

        var paymentsGrid = ErpUiFactory.Card(ErpUiFactory.BuildGrid(data.Payments.Select(p => new
        {
            رقم_السند = p.VoucherNumber,
            التاريخ = p.VoucherDate.ToString("yyyy/MM/dd"),
            المبلغ = p.Amount,
            الحالة = p.StatusDisplay
        }).ToArray(), false));

        var notesBox = new TextBox { Text = inv.Notes ?? "", AcceptsReturn = true, Height = 100, IsReadOnly = inv.IsReadOnly };

        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = inv.InvoiceNumber,
            Subtitle = $"فاتورة شراء — {inv.SupplierName}",
            Breadcrumb = "ERP PRO › المشتريات › مركز العمليات",
            IconGlyph = "\uE9F9",
            Accent = Br("AccentOrdersBrush"),
            StatusBadge = inv.StatusDisplay,
            HeaderFields =
            [
                ("المورد", inv.SupplierName),
                ("الإجمالي", $"{inv.TotalAmount:N2} $"),
                ("المدفوع", $"{inv.PaidAmount:N2} $"),
                ("المتبقي", $"{inv.RemainingAmount:N2} $"),
                ("الاستحقاق", inv.DueDate.ToString("yyyy/MM/dd")),
                ("شروط السداد", $"{inv.SupplierPaymentTermsDays} يوم"),
            ],
            Kpis =
            [
                ("الإجمالي", $"{inv.TotalAmount:N0} $", "\uE9F9"),
                ("المتبقي", $"{inv.RemainingAmount:N2} $", "\uE8C1"),
                ("أيام للاستحقاق", data.IsOverdue ? $"متأخر {Math.Abs(data.DaysUntilDue)} يوم" : data.DaysUntilDue.ToString(), "\uE823"),
                ("المدفوعات", data.Payments.Count.ToString(), "\uE719"),
            ],
            Tabs =
            [
                Tab("Overview", "نظرة عامة", Overview(inv, data)),
                Tab("Lines", "البنود", linesGrid),
                Tab("Journal", "قيود اليومية", journalGrid),
                Tab("Payments", "المدفوعات", paymentsGrid),
                Tab("Notes", "ملاحظات", notesBox),
            ],
            QuickActions =
            [
                Q("تسجيل دفعة", inv.RemainingAmount > 0, null, actionKey: "ws:PurchasePayment"),
                Q("طباعة", false, null, actionKey: "preview:PurchaseInvoice"),
                Q("تعديل", !inv.IsReadOnly, null, actionKey: "form:EditPurchaseInvoice"),
            ],
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.PurchaseInvoice,
                EntityRow = row,
                SourceModule = AppModule.Purchases,
                Title = inv.InvoiceNumber
            }
        });
    }

    private static UIElement Overview(PurchaseInvoiceDetailsDto inv, PurchaseOperationsCenterDto data)
    {
        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = data.IsOverdue
                ? $"⚠ متأخرة عن الاستحقاق بـ {Math.Abs(data.DaysUntilDue)} يوم"
                : $"متبقي {data.DaysUntilDue} يوم على الاستحقاق",
            Margin = new Thickness(0, 0, 0, 8)
        });
        sp.Children.Add(new TextBlock { Text = $"المستودع: {inv.WarehouseName ?? "—"}" });
        sp.Children.Add(new TextBlock { Text = $"مرجع المورد: {inv.SupplierReference ?? "—"}" });
        return ErpUiFactory.Card(sp);
    }

    private static OperationsCenterTab Tab(string key, string label, UIElement content) =>
        new() { Key = key, Label = label, Content = content };

    private static OperationsCenterQuickAction Q(
        string label, bool primary, string? tab,
        bool destructive = false, bool confirm = false, string? actionKey = null) =>
        new()
        {
            Label = label,
            Primary = primary,
            TabKey = tab,
            Destructive = destructive,
            RequiresConfirmation = confirm,
            ActionKey = actionKey
        };

    private static Brush Br(string key) => (Brush)System.Windows.Application.Current.Resources[key]!;
}
