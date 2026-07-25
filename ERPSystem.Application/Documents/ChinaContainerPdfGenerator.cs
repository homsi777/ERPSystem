using System.Globalization;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Containers;
using ERPSystem.Domain.Enums;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using static ERPSystem.Application.Documents.FinanceDocumentTheme;

namespace ERPSystem.Application.Documents;

/// <summary>
/// Branded China-container report. Uses the same Arabic font and Navy/Gold
/// visual identity as sales and purchase invoice PDFs.
/// </summary>
public sealed class ChinaContainerPdfGenerator
{
    private static readonly CultureInfo WesternNumbers = CultureInfo.InvariantCulture;
    private readonly string _logoPath;

    public ChinaContainerPdfGenerator(string fontPath, string logoPath)
    {
        ConfigureQuestPdf(fontPath);
        _logoPath = logoPath;
    }

    public static ChinaContainerPdfGenerator FromContentRoot(string contentRoot)
    {
        var (fontPath, logoPath) = ResolveAssets(contentRoot);
        return new ChinaContainerPdfGenerator(fontPath, logoPath);
    }

    public byte[] Generate(ContainerOperationsCenterDto operations)
    {
        ArgumentNullException.ThrowIfNull(operations);
        var container = operations.Container;

        return Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(28);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(style => style
                    .FontFamily(FontFamily)
                    .FontSize(9)
                    .FontColor(Navy));
                page.ContentFromRightToLeft();

                page.Header().Element(c => ComposeHeader(c, container));
                page.Content().PaddingTop(10).Column(column =>
                {
                    column.Spacing(10);
                    column.Item().Element(c => ComposeContainerDetails(c, operations));
                    column.Item().Element(c => ComposeSummary(c, operations));
                    column.Item().Element(c => ComposeLogistics(c, operations));

                    if (container.FabricTypeLines.Count > 0)
                        column.Item().Element(c => ComposeFabricTypes(
                            c,
                            container.FabricTypeLines,
                            container.DplQuantityUnit));
                    else
                        column.Item().Element(ComposeEmptyFabricTypes);

                    if (container.LandingCost is not null)
                        column.Item().Element(c => ComposeLandingCost(
                            c,
                            container.LandingCost,
                            container.ChinaInvoiceAmountUsd,
                            container.DplQuantityUnit));
                    else
                        column.Item().Element(ComposePendingLandingCost);

                    if (operations.Inventory is not null)
                        column.Item().Element(c => ComposeInventory(c, operations));
                });
                page.Footer().Element(c => ComposeFooter(c, container.ContainerNumber));
            });
        }).GeneratePdf();
    }

    private void ComposeHeader(IContainer target, ContainerDetailsDto container)
    {
        target.Column(column =>
        {
            column.Item().AlignCenter().Height(72).Width(88).Image(_logoPath).FitArea();
            column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Gold);
            column.Item().PaddingTop(8).Row(row =>
            {
                row.RelativeItem().Column(meta =>
                {
                    meta.Item().AlignRight().Text("بيان حاوية الصين").FontSize(18).Bold().FontColor(Navy);
                    meta.Item().PaddingTop(2).Row(line =>
                    {
                        line.AutoItem().Text("رقم الحاوية:").SemiBold();
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(container.ContainerNumber).FontColor(Gold).SemiBold();
                    });
                    meta.Item().PaddingTop(2).Row(line =>
                    {
                        line.AutoItem().Text("تاريخ الشحن:").SemiBold();
                        line.AutoItem().PaddingRight(5).ContentFromLeftToRight()
                            .Text(Date(container.ShipmentDate));
                    });
                });

                row.RelativeItem().AlignLeft().Column(company =>
                {
                    company.Item().Text("شركة الأمل").FontSize(12).Bold().FontColor(Gold);
                    company.Item().ContentFromLeftToRight().Text("ALAMAL.AB").FontSize(8).FontColor(Muted);
                    company.Item().Text($"الحالة: {StatusLabel(container.Status)}").FontSize(9);
                });
            });
        });
    }

    private static void ComposeContainerDetails(IContainer target, ContainerOperationsCenterDto operations)
    {
        var container = operations.Container;
        target.Background(Paper).Border(1).BorderColor(Border).Padding(10).Row(row =>
        {
            row.RelativeItem().Column(details =>
            {
                details.Item().Text("بيانات المورد والحاوية").FontSize(10).Bold().FontColor(Gold);
                details.Item().PaddingTop(3).Text(container.SupplierName).FontSize(12).SemiBold();
                details.Item().PaddingTop(2).Text($"تاريخ الوصول: {OptionalDate(container.ArrivalDate)}");
                details.Item().PaddingTop(2).Text($"وحدة الأطوال: {UnitLabel(container.DplQuantityUnit)}");
            });

            row.RelativeItem().AlignLeft().Column(status =>
            {
                status.Item().Text($"حالة الحاوية: {StatusLabel(container.Status)}").SemiBold();
                status.Item().PaddingTop(2).Text(
                    operations.IsReadyForSale ? "جاهزة للبيع" : "قيد التجهيز").FontColor(
                    operations.IsReadyForSale ? Green : Muted);
                if (!string.IsNullOrWhiteSpace(operations.LinkedPurchaseInvoiceNumber))
                    status.Item().PaddingTop(2).Text(
                        $"فاتورة المشتريات: {operations.LinkedPurchaseInvoiceNumber}");
            });
        });
    }

    private static void ComposeSummary(IContainer target, ContainerOperationsCenterDto operations)
    {
        var container = operations.Container;
        var (totalRolls, totalMeters) = ResolveTotals(operations);

        target.Row(row =>
        {
            SummaryCard(row, "الأثواب", Integer(totalRolls));
            SummaryCard(
                row,
                ChinaImportLengthDisplay.TotalLengthLabel(container.DplQuantityUnit),
                DisplayLength(totalMeters, container.DplQuantityUnit));
            SummaryCard(row, "فاتورة الصين", Usd(container.ChinaInvoiceAmountUsd));
            SummaryCard(row, "سعر الصرف", Number(container.ExchangeRateToLocalCurrency));
        });
    }

    private static void ComposeLogistics(IContainer target, ContainerOperationsCenterDto operations)
    {
        var container = operations.Container;
        target.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("البيانات المالية واللوجستية")
                .FontSize(11).Bold().FontColor(Gold);
            column.Item().Border(1).BorderColor(Border).Row(row =>
            {
                row.RelativeItem().Padding(8).Column(details =>
                {
                    details.Item().Text("احتياطي الضريبة المالية").FontSize(8).FontColor(Muted);
                    details.Item().PaddingTop(2).ContentFromLeftToRight()
                        .Text(Usd(container.FinancialTaxReserveUsd)).SemiBold();
                });
                row.RelativeItem().BorderRight(1).BorderColor(Border).Padding(8).Column(details =>
                {
                    details.Item().Text("الوزن").FontSize(8).FontColor(Muted);
                    details.Item().PaddingTop(2).ContentFromLeftToRight()
                        .Text(container.TotalWeightKg.HasValue
                            ? $"{Number(container.TotalWeightKg.Value)} كغ"
                            : "غير محدد").SemiBold();
                });
                row.RelativeItem().BorderRight(1).BorderColor(Border).Padding(8).Column(details =>
                {
                    details.Item().Text("تكلفة الوصول").FontSize(8).FontColor(Muted);
                    details.Item().PaddingTop(2)
                        .Text(container.LandingCost is null ? "لم تُحسب بعد" : "محسوبة").SemiBold();
                });
                row.RelativeItem().BorderRight(1).BorderColor(Border).Padding(8).Column(details =>
                {
                    details.Item().Text("ترحيل المخزون").FontSize(8).FontColor(Muted);
                    details.Item().PaddingTop(2)
                        .Text(operations.Inventory?.IsStockPosted == true ? "نعم" : "لا").SemiBold();
                });
            });
        });
    }

    private static void SummaryCard(RowDescriptor row, string label, string value)
    {
        row.RelativeItem().PaddingHorizontal(2).Background(Paper).Border(1).BorderColor(Border)
            .Padding(8).Column(column =>
            {
                column.Item().Text(label).FontSize(8).FontColor(Muted);
                column.Item().PaddingTop(3).ContentFromLeftToRight()
                    .Text(value).FontSize(10).SemiBold().FontColor(Navy);
            });
    }

    private static void ComposeFabricTypes(
        IContainer target,
        IReadOnlyList<ContainerFabricTypeLineDto> lines,
        DplQuantityUnit? unit)
    {
        target.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("تفاصيل أنواع الأقمشة").FontSize(11).Bold().FontColor(Gold);
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(25);
                    columns.RelativeColumn(2.5f);
                    columns.RelativeColumn(1.05f);
                    columns.RelativeColumn(1.25f);
                    columns.RelativeColumn(1.25f);
                    columns.RelativeColumn(1.35f);
                });

                table.Header(header =>
                {
                    HeaderCell(header, "#");
                    HeaderCell(header, "نوع القماش");
                    HeaderCell(header, "الأثواب");
                    HeaderCell(header, ChinaImportLengthDisplay.LengthColumnHeader(unit));
                    HeaderCell(header, "الوزن (كغ)");
                    HeaderCell(header, ChinaImportLengthDisplay.SalePricePerUnitLabel(unit));
                });

                foreach (var line in lines.OrderBy(item => item.LineNumber))
                {
                    BodyCell(table, Integer(line.LineNumber));
                    BodyCell(table, line.TypeDisplayName, alignRight: true);
                    BodyCell(table, Integer(line.RollCount));
                    BodyCell(table, DisplayNumber(line.LengthMeters, unit));
                    BodyCell(table, Number(line.NetWeightKg));
                    BodyCell(
                        table,
                        line.HasSalePrice
                            ? Usd(ChinaImportLengthDisplay.FromStoredRate(
                                line.SalePricePerMeterUsd,
                                unit))
                            : "—");
                }
            });
        });
    }

    private static void ComposeEmptyFabricTypes(IContainer target) =>
        target.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("تفاصيل أنواع الأقمشة")
                .FontSize(11).Bold().FontColor(Gold);
            column.Item().Background(Paper).Border(1).BorderColor(Border).Padding(9)
                .Text("لا توجد بنود أنواع أقمشة مسجلة لهذه الحاوية.").FontColor(Muted);
        });

    private static void ComposeLandingCost(
        IContainer target,
        LandingCostDto cost,
        decimal chinaInvoiceAmountUsd,
        DplQuantityUnit? unit)
    {
        target.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("تكلفة الوصول").FontSize(11).Bold().FontColor(Gold);
            column.Item().Border(1).BorderColor(Border).Column(rows =>
            {
                DetailRow(rows, "فاتورة الصين", Usd(chinaInvoiceAmountUsd));
                DetailRow(rows, "الشحن", Usd(cost.Shipping));
                DetailRow(rows, "التأمين", Usd(cost.Insurance));
                DetailRow(rows, "الجمارك", Usd(cost.CustomsAmount));
                DetailRow(rows, "التخليص", Usd(cost.Clearance));
                DetailRow(rows, "المصاريف الأخرى", Usd(cost.OtherExpenses));
                DetailRow(
                    rows,
                    ChinaImportLengthDisplay.TotalLengthLabel(unit),
                    DisplayLength(cost.TotalLengthMeters, unit));
                rows.Item().Background(Navy).PaddingVertical(6).PaddingHorizontal(10).Row(row =>
                {
                    row.RelativeItem().Text("إجمالي مصاريف الاستيراد").FontColor(Colors.White).Bold();
                    row.ConstantItem(110).AlignLeft().ContentFromLeftToRight()
                        .Text(Usd(cost.TotalImportExpenses)).FontColor(GoldSoft).Bold();
                });
            });
        });
    }

    private static void ComposePendingLandingCost(IContainer target) =>
        target.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("تكلفة الوصول")
                .FontSize(11).Bold().FontColor(Gold);
            column.Item().Background(Paper).Border(1).BorderColor(Border).Padding(9)
                .Text("لم تُحسب تكلفة الوصول لهذه الحاوية بعد.").FontColor(Muted);
        });

    private static void ComposeInventory(IContainer target, ContainerOperationsCenterDto operations)
    {
        var inventory = operations.Inventory!;
        var unit = operations.Container.DplQuantityUnit;
        target.Column(column =>
        {
            column.Item().PaddingBottom(5).Text("حالة المخزون").FontSize(11).Bold().FontColor(Gold);
            column.Item().Row(row =>
            {
                SummaryCard(row, $"المتاح ({ChinaImportLengthDisplay.LengthAbbrev(unit)})",
                    DisplayNumber(inventory.AvailableMeters, unit));
                SummaryCard(row, $"المحجوز ({ChinaImportLengthDisplay.LengthAbbrev(unit)})",
                    DisplayNumber(inventory.ReservedMeters, unit));
                SummaryCard(row, $"المباع ({ChinaImportLengthDisplay.LengthAbbrev(unit)})",
                    DisplayNumber(inventory.SoldMeters, unit));
                SummaryCard(row, "قيمة المخزون", Usd(inventory.InventoryValuation));
            });
            column.Item().PaddingTop(6).Row(row =>
            {
                InventoryFact(row, "إجمالي الأثواب", Integer(inventory.TotalRolls));
                InventoryFact(row, "الأثواب المتاحة", Integer(inventory.AvailableRolls));
                InventoryFact(row, "الأثواب المحجوزة", Integer(inventory.ReservedRolls));
                InventoryFact(row, "الأثواب المباعة", Integer(inventory.SoldRolls));
                InventoryFact(
                    row,
                    ChinaImportLengthDisplay.CostPerUnitLabel(unit),
                    Usd(ChinaImportLengthDisplay.FromStoredRate(inventory.CostPerMeter, unit)));
            });
        });
    }

    private static void InventoryFact(RowDescriptor row, string label, string value) =>
        row.RelativeItem().PaddingHorizontal(2).Background(Paper).Border(1).BorderColor(Border)
            .Padding(6).Column(column =>
            {
                column.Item().Text(label).FontSize(7.5f).FontColor(Muted);
                column.Item().PaddingTop(2).ContentFromLeftToRight()
                    .Text(value).FontSize(8.5f).SemiBold();
            });

    private static (int TotalRolls, decimal TotalMeters) ResolveTotals(
        ContainerOperationsCenterDto operations)
    {
        var container = operations.Container;
        var inventory = operations.Inventory;
        var inventoryIsAuthoritative = inventory is not null
            && (inventory.IsStockPosted
                || container.TotalRolls == 0 && inventory.TotalRolls > 0
                || container.TotalMeters == 0 && inventory.TotalMeters > 0);

        return inventoryIsAuthoritative
            ? (inventory!.TotalRolls, inventory.TotalMeters)
            : (container.TotalRolls, container.TotalMeters);
    }

    private static void DetailRow(ColumnDescriptor column, string label, string value) =>
        column.Item().PaddingVertical(4).PaddingHorizontal(10).Row(row =>
        {
            row.RelativeItem().Text(label);
            row.ConstantItem(110).AlignLeft().ContentFromLeftToRight().Text(value).SemiBold();
        });

    private static void HeaderCell(TableCellDescriptor table, string text) =>
        table.Cell().Background(NavySoft).Border(0.5f).BorderColor(Gold)
            .PaddingVertical(6).PaddingHorizontal(4).AlignCenter().AlignMiddle()
            .Text(text).FontColor(Colors.White).FontSize(8).SemiBold();

    private static void BodyCell(TableDescriptor table, string text, bool alignRight = false)
    {
        var cell = table.Cell().BorderBottom(0.7f).BorderColor(Border)
            .PaddingVertical(5).PaddingHorizontal(4).AlignMiddle();
        var aligned = alignRight ? cell.AlignRight() : cell.AlignCenter();
        aligned.Text(text).FontSize(8.5f);
    }

    private static void ComposeFooter(IContainer target, string containerNumber) =>
        target.BorderTop(1).BorderColor(Gold).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text("وثيقة صادرة عن شركة الأمل").FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignCenter().ContentFromLeftToRight()
                .Text(containerNumber).FontSize(8).FontColor(Muted);
            row.RelativeItem().AlignLeft().DefaultTextStyle(style => style.FontSize(8).FontColor(Muted))
                .Text(text =>
                {
                    text.Span("صفحة ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
        });

    private static string StatusLabel(ChinaContainerStatus status) => status switch
    {
        ChinaContainerStatus.Draft => "مسودة",
        ChinaContainerStatus.InTransit => "قيد الشحن",
        ChinaContainerStatus.Arrived => "وصلت",
        ChinaContainerStatus.UnderReview => "قيد المراجعة",
        ChinaContainerStatus.LandingCostReviewed => "تمت مراجعة تكلفة الوصول",
        ChinaContainerStatus.Approved => "معتمدة",
        ChinaContainerStatus.InWarehouse => "في المستودع",
        ChinaContainerStatus.Closed => "مغلقة",
        ChinaContainerStatus.Archived => "مؤرشفة",
        ChinaContainerStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };

    private static string UnitLabel(DplQuantityUnit? unit) =>
        unit == DplQuantityUnit.Yards ? "ياردة (YDS)" : "متر (M)";

    private static decimal DisplayValue(decimal meters, DplQuantityUnit? unit) =>
        ChinaImportLengthDisplay.FromStoredLength(meters, unit);

    private static string DisplayNumber(decimal meters, DplQuantityUnit? unit) =>
        Number(DisplayValue(meters, unit));

    private static string DisplayLength(decimal meters, DplQuantityUnit? unit) =>
        $"{DisplayNumber(meters, unit)} {ChinaImportLengthDisplay.LengthAbbrev(unit)}";

    private static string Date(DateTime value) => value.ToString("yyyy-MM-dd", WesternNumbers);
    private static string OptionalDate(DateTime? value) => value.HasValue ? Date(value.Value) : "غير محدد";
    private static string Integer(int value) => value.ToString("0", WesternNumbers);
    private static string Number(decimal value) => value.ToString("N2", WesternNumbers);
    private static string Usd(decimal value) => $"$US {Number(value)}";
}
