using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Sales;
using ERPSystem.Application.Results;
using ERPSystem.Controls.OperationsCenter;
using ERPSystem.Controls.Workspace;
using ERPSystem.Core;
using ERPSystem.Core.Actions;
using ERPSystem.Core.Sales;
using ERPSystem.Diagnostics.Performance;
using ERPSystem.Domain.Enums;
using ERPSystem.Helpers;
using ERPSystem.Services;
using ERPSystem.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace ERPSystem.Controls.Sales;

public sealed class SalesInvoiceOperationsCenterControl : UserControl
{
    private readonly TextBlock _loading = new()
    {
        Text = "جاري تحميل مركز عمليات الفاتورة...",
        Margin = new Thickness(24),
        FontSize = 14,
        Foreground = Brushes.Gray
    };

    private Guid _invoiceId;
    private string _initialTab = "Overview";

    public SalesInvoiceOperationsCenterControl()
    {
        Content = _loading;
    }

    public void Initialize(Guid invoiceId, string? initialTab = null)
    {
        _invoiceId = invoiceId;
        _initialTab = initialTab ?? "Overview";
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnLoaded;
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        if (!AppServices.IsInitialized) return;

        Content = _loading;

        using var perfScope = AppServices.GetRequiredService<IWpfPerformanceProfiler>()
            .BeginScreenLoad("Sales.OperationsCenter");

        SalesInvoiceOperationsCenterDto? value;
        ApplicationResult<SalesInvoiceOperationsCenterDto> result;
        using (perfScope.MeasureDataLoad())
        {
            result = await SalesUiService.Instance.GetOperationsCenterAsync(_invoiceId);
        }
        perfScope.IncrementServiceCalls();
        value = result.Value;

        if (!ApplicationResultPresenter.Present(result) || value is null)
        {
            Content = new TextBlock
            {
                Text = "تعذّر تحميل بيانات الفاتورة.",
                Margin = new Thickness(24),
                TextWrapping = TextWrapping.Wrap
            };
            return;
        }

        using (perfScope.MeasureRendering())
        {
            Content = BuildShell(value);
        }
        perfScope.SetRowsReturned(1);
    }

    private UserControl BuildShell(SalesInvoiceOperationsCenterDto data)
    {
        var invoice = data.Invoice;
        var row = SalesInvoiceListRow.FromDto(invoice, data.WarehouseName ?? "—", "—");

        var status = StatusDisplay(invoice.Status);
        var (statusBg, statusFg) = StatusColors(invoice.Status);
        var rollCount = invoice.Lines.Sum(l => l.RollCount);
        var daysSince = (int)(DateTime.UtcNow - invoice.InvoiceDate).TotalDays;
        var collectionState = data.RemainingBalance == 0 && data.CollectedAmount > 0
            ? "مُحصّلة كاملاً"
            : data.CollectedAmount > 0 ? "محصّلة جزئياً" : "غير مُحصّلة";

        UIElement detailingContent;
        if (data.Detailing is not null && invoice.Status == SalesInvoiceStatus.AwaitingDetailing)
        {
            var detailing = new WarehouseDetailingWorkspaceControl();
            detailing.LoadFromDatabase(
                invoice.Id,
                invoice.InvoiceNumber,
                invoice.CustomerName,
                data.WarehouseName ?? "—",
                data.Detailing.Rolls,
                invoice.Lines.FirstOrDefault()?.UnitPrice ?? 0m);
            detailingContent = WrapDetailing(detailing);
        }
        else if (data.Detailing is not null)
        {
            detailingContent = BuildDetailingReadOnlySummary(data.Detailing, invoice);
        }
        else
        {
            detailingContent = BuildDetailingSummary(invoice, data);
        }

        return OperationsCenterShell.Build(new OperationsCenterSpec
        {
            Title = invoice.InvoiceNumber,
            Subtitle = $"مركز عمليات فاتورة البيع — {invoice.CustomerName}",
            Breadcrumb = "الأمل.AB › المبيعات › فاتورة",
            IconGlyph = "\uE9F9",
            Accent = Br("AccentSalesBrush"),
            AccentLight = Br("PrimaryVeryLightBrush"),
            StatusBadge = status,
            StatusBadgeBackground = statusBg,
            StatusBadgeForeground = statusFg,
            HeaderFields =
            [
                ("العميل", invoice.CustomerName),
                ("المستودع", data.WarehouseName ?? "—"),
                ("تاريخ الفاتورة", invoice.InvoiceDate.ToString("yyyy/MM/dd")),
                ("طريقة الدفع", invoice.PaymentType == PaymentType.Cash ? "نقدي" : "آجل"),
            ],
            Kpis =
            [
                ("الإجمالي", $"{invoice.GrandTotal:N2}", "\uE8C1"),
                ("المُحصّل", $"{data.CollectedAmount:N2}", "\uE8FD"),
                ("المتبقي", $"{data.RemainingBalance:N2}", "\uE7BF"),
                ("الأثواب", rollCount.ToString(), "\uE7C3"),
                ("منذ الإصدار", $"{daysSince} يوم", "\uE787"),
                ("الحالة", collectionState, "\uE9F9"),
            ],
            Workflow =
            [
                ("مسودة", invoice.Status >= SalesInvoiceStatus.Draft, invoice.Status == SalesInvoiceStatus.Draft),
                ("مستودع", invoice.Status >= SalesInvoiceStatus.AwaitingDetailing, invoice.Status == SalesInvoiceStatus.AwaitingDetailing),
                ("تفصيل", invoice.Status >= SalesInvoiceStatus.Detailed, invoice.Status == SalesInvoiceStatus.Detailed),
                ("معتمدة", invoice.Status >= SalesInvoiceStatus.Approved, invoice.Status == SalesInvoiceStatus.Approved),
                ("مسلمة", invoice.Status >= SalesInvoiceStatus.Delivered, invoice.Status == SalesInvoiceStatus.Delivered),
            ],
            Tabs =
            [
                Tab("Overview", "الملخص", BuildOverview(data)),
                Tab("Lines", "السطور", BuildLinesTab(invoice.Lines)),
                Tab("Detailing", "التفصيل", detailingContent),
                Tab("Journal", "قيود GL", BuildJournalTab(data.JournalEntries)),
                Tab("Receipts", "التحصيلات", BuildPaymentsTab(data)),
                Tab("Returns", "المرتجعات", BuildReturnsTab(data.Returns)),
                Tab("Timeline", "الخط الزمني", BuildTimelineTab(invoice)),
            ],
            QuickActions = BuildQuickActions(data),
            InitialTabIndex = ResolveTabIndex(_initialTab, "Overview", "Lines", "Detailing", "Journal", "Receipts", "Returns", "Timeline"),
            Context = new OperationsCenterContext
            {
                EntityType = EntityType.SalesInvoice,
                EntityRow = row,
                SourceModule = AppModule.Sales,
                Title = invoice.InvoiceNumber
            }
        });
    }

    private static IReadOnlyList<OperationsCenterQuickAction> BuildQuickActions(SalesInvoiceOperationsCenterDto data)
    {
        var actions = new List<OperationsCenterQuickAction>();
        var s = data.Invoice.Status;

        if (s == SalesInvoiceStatus.Draft)
        {
            actions.Add(Q("تعديل الفاتورة", false, null, actionKey: "sales:edit"));
            actions.Add(Q("إرسال للمستودع", data.CanSendToWarehouse, null, actionKey: "sales:send-to-warehouse"));
        }

        if (data.CanApprove)
            actions.Add(Q("اعتماد وتسليم", true, null, actionKey: "sales:approve-deliver", confirm: true));

        if (s is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed)
            actions.Add(Q("تأكيد التسليم", false, null, actionKey: "sales:deliver"));

        if (s is SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed
            or SalesInvoiceStatus.Delivered or SalesInvoiceStatus.PartiallyReturned)
            actions.Add(Q("مرتجع بيع", false, null, actionKey: "sales:return"));

        actions.Add(Q("طباعة الفاتورة", false, null, actionKey: "sales:print"));
        actions.Add(Q("تصدير PDF", false, null, actionKey: "sales:pdf"));

        if (!string.IsNullOrWhiteSpace(data.CustomerPhone))
            actions.Add(Q("اتصل بالعميل", false, null, actionKey: "sales:call-customer"));

        if (data.CanCancel)
            actions.Add(Q("إلغاء الفاتورة", false, null, actionKey: "sales:cancel", destructive: true, confirm: true));

        return actions;
    }

    private static UIElement BuildOverview(SalesInvoiceOperationsCenterDto data)
    {
        var invoice = data.Invoice;
        var stack = new StackPanel { Margin = new Thickness(12) };

        stack.Children.Add(ErpUiFactory.SectionTitle("الملخص المالي"));
        stack.Children.Add(ErpUiFactory.Card(ErpUiFactory.BuildFormGrid(
            ("الإجمالي الفرعي", ReadOnly($"{invoice.SubTotal:N2}")),
            ("الخصم", ReadOnly($"{invoice.DiscountTotal:N2}")),
            ("الضريبة", ReadOnly($"{invoice.TaxTotal:N2}")),
            ("الإجمالي المستحق", ReadOnly($"{invoice.GrandTotal:N2}")),
            ("المُحصّل", ReadOnly($"{data.CollectedAmount:N2}")),
            ("المتبقي", ReadOnly($"{data.RemainingBalance:N2}"))
        )));

        stack.Children.Add(ErpUiFactory.SectionTitle("بيانات العميل"));
        var customerCard = new StackPanel();
        customerCard.Children.Add(BuildInfo("الاسم", invoice.CustomerName));
        if (!string.IsNullOrWhiteSpace(data.CustomerPhone))
            customerCard.Children.Add(BuildInfo("الهاتف", data.CustomerPhone!));
        customerCard.Children.Add(BuildInfo("طريقة الدفع", invoice.PaymentType == PaymentType.Cash ? "نقدي" : "آجل"));
        stack.Children.Add(ErpUiFactory.Card(customerCard));

        if (invoice.Status == SalesInvoiceStatus.Delivered && !string.IsNullOrWhiteSpace(invoice.DeliveredToName))
        {
            stack.Children.Add(ErpUiFactory.SectionTitle("معلومات التسليم"));
            var d = new StackPanel();
            d.Children.Add(BuildInfo("المستلم", invoice.DeliveredToName!));
            if (!string.IsNullOrWhiteSpace(invoice.DeliveryDriverName))
                d.Children.Add(BuildInfo("السائق", invoice.DeliveryDriverName!));
            d.Children.Add(BuildInfo("تاريخ التسليم", invoice.DeliveredAt?.ToString("yyyy/MM/dd HH:mm") ?? "—"));
            if (!string.IsNullOrWhiteSpace(invoice.DeliveryNotes))
                d.Children.Add(BuildInfo("ملاحظات", invoice.DeliveryNotes!));
            stack.Children.Add(ErpUiFactory.Card(d));
        }

        return stack;
    }

    private static UIElement BuildLinesTab(IReadOnlyList<SalesInvoiceLineDto> lines)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(ErpUiFactory.SectionTitle("سطور الفاتورة"));

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ItemsSource = lines
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "#", Binding = new Binding(nameof(SalesInvoiceLineDto.LineNumber)), Width = 50 });
        grid.Columns.Add(new DataGridTextColumn { Header = "الصنف", Binding = new Binding(nameof(SalesInvoiceLineDto.FabricDisplayName)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        grid.Columns.Add(new DataGridTextColumn { Header = "الكود", Binding = new Binding(nameof(SalesInvoiceLineDto.FabricCode)), Width = 90 });
        grid.Columns.Add(new DataGridTextColumn { Header = "اللون", Binding = new Binding(nameof(SalesInvoiceLineDto.ColorDisplayName)), Width = 110 });
        grid.Columns.Add(new DataGridTextColumn { Header = "الأطباق", Binding = new Binding(nameof(SalesInvoiceLineDto.RollCount)), Width = 80 });
        grid.Columns.Add(new DataGridTextColumn { Header = "سعر المتر", Binding = new Binding(nameof(SalesInvoiceLineDto.UnitPrice)) { StringFormat = "N2" }, Width = 110 });
        grid.Columns.Add(new DataGridTextColumn { Header = "الإجمالي", Binding = new Binding(nameof(SalesInvoiceLineDto.LineTotal)) { StringFormat = "N2" }, Width = 110 });
        stack.Children.Add(ErpUiFactory.Card(grid));
        return stack;
    }

    private static UIElement BuildJournalTab(IReadOnlyList<JournalEntryDto> entries)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(ErpUiFactory.SectionTitle("قيود دفتر اليومية المرتبطة بالفاتورة"));

        if (entries.Count == 0)
        {
            stack.Children.Add(EmptyMessage("لا توجد قيود محاسبية بعد — القيود تُنشأ عند الاعتماد والتسليم والمرتجع."));
            return stack;
        }

        foreach (var e in entries)
        {
            var card = new StackPanel();
            var header = new StackPanel { Orientation = Orientation.Horizontal };
            header.Children.Add(new TextBlock { Text = e.EntryNumber, FontWeight = FontWeights.SemiBold, FontSize = 13, Margin = new Thickness(0, 0, 12, 0) });
            header.Children.Add(new TextBlock { Text = e.EntryDate.ToString("yyyy/MM/dd HH:mm"), FontSize = 11, Foreground = Br("TextMutedBrush"), VerticalAlignment = VerticalAlignment.Center });
            header.Children.Add(new TextBlock { Text = $" • {e.Status}", FontSize = 11, Foreground = Br("TextMutedBrush"), VerticalAlignment = VerticalAlignment.Center });
            card.Children.Add(header);
            if (!string.IsNullOrWhiteSpace(e.Description))
                card.Children.Add(new TextBlock { Text = e.Description, FontSize = 11, Foreground = Br("TextSecondaryBrush"), Margin = new Thickness(0, 4, 0, 8), TextWrapping = TextWrapping.Wrap });

            var g = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                ItemsSource = e.Lines,
                MaxHeight = 200
            };
            g.Columns.Add(new DataGridTextColumn { Header = "الحساب", Binding = new Binding(nameof(JournalEntryLineDto.AccountCode)), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            g.Columns.Add(new DataGridTextColumn { Header = "بيان", Binding = new Binding(nameof(JournalEntryLineDto.Narrative)), Width = 240 });
            g.Columns.Add(new DataGridTextColumn { Header = "مدين", Binding = new Binding(nameof(JournalEntryLineDto.Debit)) { StringFormat = "N2" }, Width = 110 });
            g.Columns.Add(new DataGridTextColumn { Header = "دائن", Binding = new Binding(nameof(JournalEntryLineDto.Credit)) { StringFormat = "N2" }, Width = 110 });
            card.Children.Add(g);

            var totals = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            totals.Children.Add(new TextBlock { Text = $"إجمالي المدين: {e.DebitTotal:N2}", FontSize = 11, Margin = new Thickness(0, 0, 20, 0) });
            totals.Children.Add(new TextBlock { Text = $"إجمالي الدائن: {e.CreditTotal:N2}", FontSize = 11 });
            card.Children.Add(totals);

            stack.Children.Add(ErpUiFactory.Card(card));
        }

        return stack;
    }

    private static UIElement BuildPaymentsTab(SalesInvoiceOperationsCenterDto data)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };

        var summary = new Grid();
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        summary.Children.Add(WithColumn(BuildMetric("الإجمالي المستحق", $"{data.Invoice.GrandTotal:N2}"), 0));
        summary.Children.Add(WithColumn(BuildMetric("المُحصّل", $"{data.CollectedAmount:N2}", Br("SuccessBrush")), 1));
        summary.Children.Add(WithColumn(BuildMetric("المتبقي", $"{data.RemainingBalance:N2}", Br("WarningBrush")), 2));
        stack.Children.Add(ErpUiFactory.Card(summary));

        stack.Children.Add(ErpUiFactory.SectionTitle("سندات القبض المرتبطة"));

        if (data.Payments.Count == 0)
        {
            stack.Children.Add(EmptyMessage("لم يتم تطبيق أي سند قبض على هذه الفاتورة بعد."));
            return stack;
        }

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ItemsSource = data.Payments,
            MaxHeight = 320
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "رقم السند", Binding = new Binding(nameof(ReceiptInvoicePaymentDto.ReceiptNumber)), Width = 150 });
        grid.Columns.Add(new DataGridTextColumn { Header = "المبلغ", Binding = new Binding(nameof(ReceiptInvoicePaymentDto.Amount)) { StringFormat = "N2" }, Width = 140 });
        grid.Columns.Add(new DataGridTextColumn { Header = "التاريخ", Binding = new Binding(nameof(ReceiptInvoicePaymentDto.AppliedAt)) { StringFormat = "yyyy/MM/dd HH:mm" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        stack.Children.Add(ErpUiFactory.Card(grid));
        return stack;
    }

    private static UIElement BuildReturnsTab(IReadOnlyList<SalesReturnDto> returns)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(ErpUiFactory.SectionTitle("مرتجعات على هذه الفاتورة"));

        if (returns.Count == 0)
        {
            stack.Children.Add(EmptyMessage("لا توجد مرتجعات لهذه الفاتورة."));
            return stack;
        }

        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            HeadersVisibility = DataGridHeadersVisibility.Column,
            ItemsSource = returns,
            MaxHeight = 380
        };
        grid.Columns.Add(new DataGridTextColumn { Header = "رقم المرتجع", Binding = new Binding(nameof(SalesReturnDto.ReturnNumber)), Width = 140 });
        grid.Columns.Add(new DataGridTextColumn { Header = "التاريخ", Binding = new Binding(nameof(SalesReturnDto.ReturnDate)) { StringFormat = "yyyy/MM/dd" }, Width = 110 });
        grid.Columns.Add(new DataGridTextColumn { Header = "السبب", Binding = new Binding(nameof(SalesReturnDto.Reason)), Width = 140 });
        grid.Columns.Add(new DataGridTextColumn { Header = "الحالة", Binding = new Binding(nameof(SalesReturnDto.Status)), Width = 120 });
        grid.Columns.Add(new DataGridTextColumn { Header = "الإجمالي", Binding = new Binding(nameof(SalesReturnDto.TotalAmount)) { StringFormat = "N2" }, Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
        stack.Children.Add(ErpUiFactory.Card(grid));
        return stack;
    }

    private static UIElement BuildTimelineTab(SalesInvoiceDto invoice)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(ErpUiFactory.SectionTitle("سجل الأحداث (Audit Timeline)"));

        var events = new List<(DateTime? At, string Icon, string Text, Brush Color)>
        {
            (invoice.InvoiceDate, "\uE787", "إنشاء الفاتورة (مسودة)", Br("PrimaryBrush")),
            (invoice.SentToWarehouseAt, "\uE72A", "أُرسلت للمستودع", Br("PrimaryBrush")),
            (invoice.DetailedAt, "\uE9F5", "اكتمل التفصيل", Br("PrimaryBrush")),
            (invoice.ApprovedAt, "\uE73E", "تم الاعتماد وتسجيل قيود GL", Br("SuccessBrush")),
            (invoice.PrintedAt, "\uE749", "تمت الطباعة", Br("TextMutedBrush")),
            (invoice.DeliveredAt, "\uE7C1", "تم التسليم للعميل", Br("SuccessBrush")),
            (invoice.CancelledAt, "\uE711", $"أُلغيت الفاتورة{(string.IsNullOrWhiteSpace(invoice.CancelReason) ? "" : $" — {invoice.CancelReason}")}", Br("DangerBrush"))
        };

        var card = new StackPanel();
        var any = false;
        foreach (var ev in events.Where(x => x.At.HasValue).OrderBy(x => x.At))
        {
            any = true;
            var row = new Grid { Margin = new Thickness(0, 4, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });

            var icon = new TextBlock
            {
                Text = ev.Icon,
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 14,
                Foreground = ev.Color,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(icon, 0);
            var text = new TextBlock
            {
                Text = ev.Text,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetColumn(text, 1);
            var dt = new TextBlock
            {
                Text = ev.At!.Value.ToString("yyyy/MM/dd HH:mm"),
                FontSize = 11,
                Foreground = Br("TextMutedBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            Grid.SetColumn(dt, 2);
            row.Children.Add(icon);
            row.Children.Add(text);
            row.Children.Add(dt);
            card.Children.Add(row);
        }
        if (!any) card.Children.Add(EmptyMessage("لا توجد أحداث مسجّلة."));

        stack.Children.Add(ErpUiFactory.Card(card));
        return stack;
    }

    private static UIElement BuildDetailingReadOnlySummary(WarehouseDetailingDto detailing, SalesInvoiceDto invoice)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };
        stack.Children.Add(ErpUiFactory.SectionTitle("أطوال الأثواب — مُدخلة"));
        stack.Children.Add(ErpUxFactory.InfoBanner("تم إكمال التفصيل. الأطوال أدناه للعرض فقط.", "success"));

        var grid = ErpUiFactory.BuildGrid(
            detailing.Rolls.Select(r => new
            {
                r.RollSequence,
                Fabric = string.IsNullOrWhiteSpace(r.FabricDisplayName) ? r.FabricCode : r.FabricDisplayName,
                r.ColorDisplayName,
                Length = r.HasValidLength ? $"{r.LengthMeters:N2} م" : "—"
            }).ToList(),
            false);
        grid.IsReadOnly = true;
        grid.AutoGenerateColumns = false;
        foreach (var (h, p, w) in new (string, string, object)[]
        {
            ("#", "RollSequence", 50),
            ("التوب", "Fabric", 160),
            ("اللون", "ColorDisplayName", 100),
            ("الطول", "Length", 100)
        })
            ErpUiFactory.AddGridColumn(grid, h, p, w, null);

        stack.Children.Add(ErpUiFactory.Card(grid));
        stack.Children.Add(BuildInfo("إجمالي الأطوال",
            $"{detailing.Rolls.Where(r => r.HasValidLength).Sum(r => r.LengthMeters):N2} م"));
        stack.Children.Add(BuildInfo("إجمالي الفاتورة", $"{invoice.GrandTotal:N2}"));
        return stack;
    }

    private static UIElement BuildDetailingSummary(SalesInvoiceDto invoice, SalesInvoiceOperationsCenterDto data)
    {
        var stack = new StackPanel { Margin = new Thickness(12) };
        if (invoice.Status >= SalesInvoiceStatus.Detailed && invoice.DetailedAt.HasValue)
        {
            stack.Children.Add(ErpUiFactory.SectionTitle("ملخص التفصيل"));
            var card = new StackPanel();
            card.Children.Add(BuildInfo("تاريخ إكمال التفصيل", invoice.DetailedAt.Value.ToString("yyyy/MM/dd HH:mm")));
            card.Children.Add(BuildInfo("المستودع", data.WarehouseName ?? "—"));
            card.Children.Add(BuildInfo("إجمالي الأثواب", invoice.Lines.Sum(l => l.RollCount).ToString()));
            stack.Children.Add(ErpUiFactory.Card(card));
        }
        else
        {
            stack.Children.Add(EmptyMessage("لا توجد مهمة تفصيل مفتوحة لهذه الفاتورة."));
        }
        return stack;
    }

    private static UIElement WrapDetailing(WarehouseDetailingWorkspaceControl ctrl)
    {
        var stack = new StackPanel();
        stack.Children.Add(ErpUxFactory.InfoBanner(
            "أدخل الطول الفعلي لكل توب. لا يُعتمد إجمالي الفاتورة قبل إكمال التفصيل.",
            "warning"));
        stack.Children.Add(ctrl);
        return stack;
    }

    private static StackPanel BuildMetric(string label, string value, Brush? valueColor = null)
    {
        var sp = new StackPanel { Margin = new Thickness(8) };
        sp.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = Br("TextMutedBrush") });
        sp.Children.Add(new TextBlock
        {
            Text = value,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = valueColor ?? Br("TextPrimaryBrush"),
            Margin = new Thickness(0, 4, 0, 0)
        });
        return sp;
    }

    private static UIElement WithColumn(UIElement el, int c) { Grid.SetColumn(el, c); return el; }

    private static Grid BuildInfo(string label, string value)
    {
        var g = new Grid { Margin = new Thickness(0, 4, 0, 4) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var lbl = new TextBlock { Text = label, FontSize = 11, Foreground = Br("TextMutedBrush") };
        var val = new TextBlock { Text = value, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(lbl, 0); Grid.SetColumn(val, 1);
        g.Children.Add(lbl); g.Children.Add(val);
        return g;
    }

    private static TextBox ReadOnly(string text) => new()
    {
        Text = text,
        IsReadOnly = true,
        BorderThickness = new Thickness(0),
        Background = Brushes.Transparent,
        FontSize = 12
    };

    private static Border EmptyMessage(string text) => new()
    {
        Background = Br("SurfaceAltBrush"),
        BorderBrush = Br("BorderBrush"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(6),
        Padding = new Thickness(16),
        Margin = new Thickness(0, 4, 0, 4),
        Child = new TextBlock
        {
            Text = text,
            FontSize = 12,
            Foreground = Br("TextMutedBrush"),
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        }
    };

    private static (Brush Bg, Brush Fg) StatusColors(SalesInvoiceStatus s) => s switch
    {
        SalesInvoiceStatus.Draft => (Br("SurfaceAltBrush"), Br("TextSecondaryBrush")),
        SalesInvoiceStatus.AwaitingDetailing or SalesInvoiceStatus.Detailed
            or SalesInvoiceStatus.ReadyForApproval => (Br("WarningBgBrush"), Br("WarningBrush")),
        SalesInvoiceStatus.Approved or SalesInvoiceStatus.Printed
            or SalesInvoiceStatus.Delivered => (Br("SuccessBgBrush"), Br("SuccessBrush")),
        SalesInvoiceStatus.PartiallyReturned => (Br("WarningBgBrush"), Br("WarningBrush")),
        SalesInvoiceStatus.Returned or SalesInvoiceStatus.Cancelled => (Br("DangerBgBrush"), Br("DangerBrush")),
        _ => (Br("SurfaceAltBrush"), Br("TextSecondaryBrush"))
    };

    private static string StatusDisplay(SalesInvoiceStatus status) => status switch
    {
        SalesInvoiceStatus.Draft => "مسودة",
        SalesInvoiceStatus.AwaitingDetailing => "بانتظار التفصيل",
        SalesInvoiceStatus.Detailed => "تم التفصيل",
        SalesInvoiceStatus.ReadyForApproval => "جاهزة للاعتماد",
        SalesInvoiceStatus.Approved => "معتمدة",
        SalesInvoiceStatus.Printed => "مطبوعة",
        SalesInvoiceStatus.Delivered => "مسلمة",
        SalesInvoiceStatus.PartiallyReturned => "مرتجع جزئي",
        SalesInvoiceStatus.Returned => "مرتجعة",
        SalesInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static int ResolveTabIndex(string selected, params string[] keys)
    {
        for (var i = 0; i < keys.Length; i++)
            if (keys[i].Equals(selected, StringComparison.OrdinalIgnoreCase))
                return i;
        return 0;
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

    private static SolidColorBrush Br(string k) => (SolidColorBrush)System.Windows.Application.Current.Resources[k]!;
}
