using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.Common;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Domain.Enums;
using ERPSystem.Infrastructure.Persistence;
using ERPSystem.Infrastructure.Persistence.Models.Accounting;
using ERPSystem.Infrastructure.Persistence.Models.ChinaImport;
using ERPSystem.Infrastructure.Persistence.Models.Inventory;
using ERPSystem.Infrastructure.Persistence.Models.Parties;
using ERPSystem.Infrastructure.Persistence.Models.Purchasing;
using ERPSystem.Infrastructure.Persistence.Models.Sales;
using Microsoft.EntityFrameworkCore;

namespace ERPSystem.Infrastructure.Repositories;

internal sealed class ModuleReportRepository(ErpDbContext context) : IModuleReportRepository
{
    private const decimal LowStockThreshold = 50m;

    public async Task<ModuleReportResultDto> RunAsync(
        GetModuleReportQuery query,
        CancellationToken cancellationToken = default)
    {
        var key = query.ReportKey.Trim().ToLowerInvariant();
        return key switch
        {
            "inv.warehouses" => await BuildWarehouseSummaryAsync(query, cancellationToken),
            "inv.stock_balance" => await BuildStockBalanceAsync(query, cancellationToken),
            "inv.warehouse_move" => await BuildWarehouseMovementsAsync(query, cancellationToken),
            "inv.item_move" => await BuildItemMovementsAsync(query, cancellationToken),
            "inv.item_analysis" => await BuildItemAnalysisAsync(query, cancellationToken),
            "inv.low_stock" => await BuildLowStockAsync(query, cancellationToken),
            "inv.valuation" => await BuildValuationAsync(query, cancellationToken),
            "sal.invoices" or "sal.returns" or "sal.delivery" => await BuildSalesInvoicesAsync(query, key, cancellationToken),
            "sal.by_customer" => await BuildSalesByCustomerAsync(query, cancellationToken),
            "sal.detailing" => await BuildDetailingQueueAsync(query, cancellationToken),
            "cus.balances" or "cus.statements" => await BuildCustomerBalancesAsync(query, cancellationToken),
            "cus.invoices" => await BuildCustomerInvoicesAsync(query, cancellationToken),
            "sup.balances" or "sup.statements" => await BuildSupplierBalancesAsync(query, cancellationToken),
            "sup.invoices" => await BuildPurchaseInvoicesAsync(query, cancellationToken),
            "pur.invoices" or "pur.by_supplier" => await BuildPurchaseInvoicesAsync(query, cancellationToken),
            "cn.containers" => await BuildContainerReportAsync(query, cancellationToken),
            "cn.sale_ready" => await BuildSaleReadyContainersAsync(query, cancellationToken),
            "cn.landing_cost" => await BuildLandingCostReportAsync(query, cancellationToken),
            "cn.inventory" => await BuildContainerInventoryReportAsync(query, cancellationToken),
            "acc.journal" => await BuildJournalReportAsync(query, cancellationToken),
            "acc.receipts" => await BuildReceiptsReportAsync(query, cancellationToken),
            "acc.payments" => await BuildPaymentsReportAsync(query, cancellationToken),
            "acc.receivables" => await BuildCustomerBalancesAsync(query, cancellationToken),
            "acc.payables" => await BuildSupplierBalancesAsync(query, cancellationToken),
            "exp.outstanding" or "exp.upcoming" or "exp.recurring" => await BuildExpenseSliceAsync(query, key, cancellationToken),
            "hr.employees" => await BuildEmployeesAsync(query, cancellationToken),
            _ => Empty(query, "التقرير قيد التطوير")
        };
    }

    private async Task<ModuleReportResultDto> BuildWarehouseSummaryAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var warehouses = await WarehouseEntities(ct)
            .Where(w => w.BranchId == query.BranchId && w.IsActive && !w.IsArchived)
            .OrderBy(w => w.Code)
            .ToListAsync(ct);

        var stockByWh = await context.WarehouseStocks.AsNoTracking()
            .Where(s => warehouses.Select(w => w.Id).Contains(s.WarehouseId))
            .GroupBy(s => s.WarehouseId)
            .Select(g => new
            {
                WarehouseId = g.Key,
                Rolls = g.Sum(x => x.RollCount),
                Meters = g.Sum(x => x.TotalMeters),
                Available = g.Sum(x => x.AvailableMeters)
            })
            .ToListAsync(ct);

        var map = stockByWh.ToDictionary(x => x.WarehouseId);
        var rows = warehouses.Select(w =>
        {
            var s = map.GetValueOrDefault(w.Id);
            var rolls = s?.Rolls ?? 0;
            var cap = w.CapacityRolls ?? 0;
            var pct = cap > 0 ? (int)Math.Round(rolls * 100m / cap) : 0;
            return Row(
                ("Code", w.Code),
                ("Name", w.NameAr),
                ("City", w.City),
                ("Rolls", rolls),
                ("Meters", s?.Meters ?? 0m),
                ("Available", s?.Available ?? 0m),
                ("CapacityPct", pct),
                ("Status", w.IsActive ? "نشط" : "معطّل"));
        }).ToList();

        return Result(query, "تقرير المستودعات", "ملخص المستودعات وأرصدة الأثواب",
            Cols(
                ("Code", "الكود", 80, null),
                ("Name", "المستودع", "*", null),
                ("City", "المدينة", 90, null),
                ("Rolls", "الأثواب", 80, null),
                ("Meters", "الأمتار", 100, "N0"),
                ("Available", "متاح", 100, "N0"),
                ("CapacityPct", "السعة %", 80, null),
                ("Status", "الحالة", 80, null)),
            rows,
            Kpi("المستودعات", warehouses.Count.ToString()),
            Kpi("إجمالي الأثواب", stockByWh.Sum(x => x.Rolls).ToString("N0"), "\uE821"),
            Kpi("إجمالي الأمتار", stockByWh.Sum(x => x.Meters).ToString("N0"), "\uE8C1"));
    }

    private async Task<ModuleReportResultDto> BuildStockBalanceAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Join(WarehouseEntities(ct).Where(w => w.BranchId == query.BranchId),
                s => s.WarehouseId, w => w.Id, (s, w) => new { s, w })
            .ToListAsync(ct);

        if (stocks.Count == 0)
            return Empty(query, "لا توجد أرصدة مخزون");

        var fabricIds = stocks.Select(x => x.s.FabricItemId).Distinct().ToList();
        var colorIds = stocks.Select(x => x.s.FabricColorId).Distinct().ToList();
        var containerIds = stocks.Select(x => x.s.ContainerId).Distinct().ToList();

        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id)).ToDictionaryAsync(f => f.Id, f => f.NameAr, ct);
        var colors = await context.FabricColors.AsNoTracking()
            .Where(c => colorIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);
        var containers = await context.Containers.AsNoTracking()
            .Where(c => containerIds.Contains(c.Id)).ToDictionaryAsync(c => c.Id, c => c.ContainerNumber, ct);

        var rows = stocks.OrderBy(x => x.w.NameAr).Select(x => Row(
            ("Warehouse", x.w.NameAr),
            ("Fabric", fabrics.GetValueOrDefault(x.s.FabricItemId, "—")),
            ("Color", colors.GetValueOrDefault(x.s.FabricColorId, "—")),
            ("Container", containers.GetValueOrDefault(x.s.ContainerId, "—")),
            ("Rolls", x.s.RollCount),
            ("TotalMeters", x.s.TotalMeters),
            ("Available", x.s.AvailableMeters),
            ("Reserved", x.s.ReservedMeters))).ToList();

        return Result(query, "أرصدة المخزون", "تفصيل الأرصدة حسب المستودع والصنف",
            Cols(
                ("Warehouse", "المستودع", 120, null),
                ("Fabric", "نوع القماش", 130, null),
                ("Color", "اللون", 90, null),
                ("Container", "الحاوية", 100, null),
                ("Rolls", "أثواب", 70, null),
                ("TotalMeters", "إجمالي م", 100, "N0"),
                ("Available", "متاح", 90, "N0"),
                ("Reserved", "محجوز", 90, "N0")),
            rows,
            Kpi("سجلات", rows.Count.ToString()),
            Kpi("أثواب", rows.Sum(r => (int)(r["Rolls"] ?? 0)).ToString("N0")),
            Kpi("أمتار متاحة", rows.Sum(r => Convert.ToDecimal(r["Available"] ?? 0m)).ToString("N0")));
    }

    private async Task<ModuleReportResultDto> BuildWarehouseMovementsAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var from = query.FromDate;
        var to = query.ToDate;
        var q = context.StockMovements.AsNoTracking()
            .Where(m => !m.IsArchived);

        if (from.HasValue)
            q = q.Where(m => m.MovementDate >= UtcDateTimeNormalizer.ToUtc(from.Value));
        if (to.HasValue)
            q = q.Where(m => m.MovementDate <= UtcDateTimeNormalizer.ToUtc(to.Value));

        var movements = await q.OrderByDescending(m => m.MovementDate).Take(5000).ToListAsync(ct);
        var whIds = movements.Select(m => m.WarehouseId).Distinct().ToList();
        var whMap = await WarehouseEntities(ct)
            .Where(w => whIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.NameAr, ct);

        var rows = movements.Select(m => Row(
            ("Number", m.MovementNumber),
            ("Date", m.MovementDate),
            ("Type", ((MovementType)m.Type).ToString()),
            ("Warehouse", whMap.GetValueOrDefault(m.WarehouseId, "—")),
            ("Status", ((StockMovementStatus)m.Status).ToString()))).ToList();

        return Result(query, "حركة المستودع", "حركات المخزون المسجلة",
            Cols(
                ("Number", "رقم الحركة", 110, null),
                ("Date", "التاريخ", 100, "yyyy/MM/dd"),
                ("Type", "النوع", 100, null),
                ("Warehouse", "المستودع", "*", null),
                ("Status", "الحالة", 90, null)),
            rows,
            Kpi("حركات", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildItemMovementsAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var rolls = await context.FabricRolls.AsNoTracking()
            .OrderByDescending(r => r.UpdatedAt ?? r.CreatedAt)
            .Take(3000)
            .ToListAsync(ct);

        var fabricIds = rolls.Select(r => r.FabricItemId).Distinct().ToList();
        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.NameAr, ct);

        var rows = rolls.Select(r => Row(
            ("Roll", r.RollNumber),
            ("Fabric", fabrics.GetValueOrDefault(r.FabricItemId, "—")),
            ("Status", ((FabricRollStatus)r.Status).ToString()),
            ("Length", r.LengthMeters),
            ("Remaining", r.RemainingLengthMeters),
            ("CostPerMeter", r.CostPerMeter))).ToList();

        return Result(query, "حركة المادة", "حركة الأثواب (rolls) في المخزون",
            Cols(
                ("Roll", "رقم التوب", 90, null),
                ("Fabric", "الصنف", "*", null),
                ("Status", "الحالة", 90, null),
                ("Length", "الطول", 90, "N2"),
                ("Remaining", "متبقي", 90, "N2"),
                ("CostPerMeter", "تكلفة/م", 90, "N4")),
            rows,
            Kpi("أثواب", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildItemAnalysisAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var grouped = await context.WarehouseStocks.AsNoTracking()
            .Join(WarehouseEntities(ct).Where(w => w.BranchId == query.BranchId),
                s => s.WarehouseId, w => w.Id, (s, w) => s)
            .GroupBy(s => s.FabricItemId)
            .Select(g => new
            {
                FabricItemId = g.Key,
                Rolls = g.Sum(x => x.RollCount),
                Meters = g.Sum(x => x.TotalMeters),
                Available = g.Sum(x => x.AvailableMeters)
            })
            .OrderByDescending(x => x.Meters)
            .ToListAsync(ct);

        var fabricIds = grouped.Select(g => g.FabricItemId).ToList();
        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => (f.Code, f.NameAr), ct);

        var rows = grouped.Select(g =>
        {
            var f = fabrics.GetValueOrDefault(g.FabricItemId);
            return Row(
                ("Code", f.Code ?? "—"),
                ("Name", f.NameAr ?? "—"),
                ("Rolls", g.Rolls),
                ("Meters", g.Meters),
                ("Available", g.Available));
        }).ToList();

        return Result(query, "تحليل مادة", "تحليل تجميعي لكل نوع قماش",
            Cols(
                ("Code", "الكود", 90, null),
                ("Name", "نوع القماش", "*", null),
                ("Rolls", "أثواب", 80, null),
                ("Meters", "أمتار", 100, "N0"),
                ("Available", "متاح", 100, "N0")),
            rows,
            Kpi("أصناف", rows.Count.ToString()),
            Kpi("إجمالي م", grouped.Sum(g => g.Meters).ToString("N0")));
    }

    private async Task<ModuleReportResultDto> BuildLowStockAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var stocks = await context.WarehouseStocks.AsNoTracking()
            .Where(s => s.AvailableMeters > 0 && s.AvailableMeters <= LowStockThreshold)
            .Join(WarehouseEntities(ct).Where(w => w.BranchId == query.BranchId),
                s => s.WarehouseId, w => w.Id, (s, w) => new { s, w })
            .ToListAsync(ct);

        var fabricIds = stocks.Select(x => x.s.FabricItemId).Distinct().ToList();
        var fabrics = await context.FabricItems.AsNoTracking()
            .Where(f => fabricIds.Contains(f.Id))
            .ToDictionaryAsync(f => f.Id, f => f.NameAr, ct);

        var rows = stocks.Select(x => Row(
            ("Warehouse", x.w.NameAr),
            ("Fabric", fabrics.GetValueOrDefault(x.s.FabricItemId, "—")),
            ("Available", x.s.AvailableMeters),
            ("Rolls", x.s.RollCount))).ToList();

        return Result(query, "تنبيه نقص المخزون", $"أصناف بمتاح ≤ {LowStockThreshold:N0} م",
            Cols(
                ("Warehouse", "المستودع", 120, null),
                ("Fabric", "الصنف", "*", null),
                ("Available", "متاح (م)", 100, "N0"),
                ("Rolls", "أثواب", 70, null)),
            rows,
            Kpi("تنبيهات", rows.Count.ToString(), "\uE783"));
    }

    private async Task<ModuleReportResultDto> BuildValuationAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var rolls = await context.FabricRolls.AsNoTracking()
            .Where(r => r.Status == (int)FabricRollStatus.Available && r.RemainingLengthMeters > 0)
            .ToListAsync(ct);

        var whIds = rolls.Select(r => r.WarehouseId).Distinct().ToList();
        var whMap = await WarehouseEntities(ct)
            .Where(w => whIds.Contains(w.Id))
            .ToDictionaryAsync(w => w.Id, w => w.NameAr, ct);

        var byWh = rolls
            .GroupBy(r => r.WarehouseId)
            .Select(g => new
            {
                Warehouse = whMap.GetValueOrDefault(g.Key, "—"),
                Meters = g.Sum(r => r.RemainingLengthMeters),
                Value = g.Sum(r => r.RemainingLengthMeters * r.CostPerMeter)
            })
            .OrderByDescending(x => x.Value)
            .ToList();

        var rows = byWh.Select(x => Row(
            ("Warehouse", x.Warehouse),
            ("Meters", x.Meters),
            ("Value", x.Value))).ToList();

        var total = byWh.Sum(x => x.Value);
        return Result(query, "تقييم المخزون", "قيمة المخزون المتاح (USD)",
            Cols(
                ("Warehouse", "المستودع", "*", null),
                ("Meters", "أمتار", 110, "N0"),
                ("Value", "القيمة USD", 120, "N2")),
            rows,
            Kpi("إجمالي USD", total.ToString("N2"), "\uE8C1"));
    }

    private async Task<ModuleReportResultDto> BuildSalesInvoicesAsync(
        GetModuleReportQuery query, string key, CancellationToken ct)
    {
        var q = context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == query.CompanyId && i.BranchId == query.BranchId && !i.IsArchived);

        if (query.FromDate.HasValue)
            q = q.Where(i => i.InvoiceDate >= UtcDateTimeNormalizer.ToUtc(query.FromDate.Value));
        if (query.ToDate.HasValue)
            q = q.Where(i => i.InvoiceDate <= UtcDateTimeNormalizer.ToUtc(query.ToDate.Value));

        if (key == "sal.detailing")
            q = q.Where(i => i.SentToWarehouseAt != null && i.DetailedAt == null);
        else if (key == "sal.delivery")
            q = q.Where(i => i.DetailedAt != null && i.DeliveredAt == null);

        var invoices = await q.OrderByDescending(i => i.InvoiceDate).Take(5000).ToListAsync(ct);
        var custIds = invoices.Select(i => i.CustomerId).Distinct().ToList();
        var custMap = await context.Customers.AsNoTracking()
            .Where(c => custIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);

        var rows = invoices.Select(i => Row(
            ("Number", i.InvoiceNumber),
            ("Date", i.InvoiceDate),
            ("Customer", custMap.GetValueOrDefault(i.CustomerId, "—")),
            ("Total", i.GrandTotal),
            ("Status", ((SalesInvoiceStatus)i.Status).ToString()))).ToList();

        var title = key switch
        {
            "sal.detailing" => "طابور التفصيل",
            "sal.delivery" => "جاهزة للتسليم",
            _ => "فواتير البيع"
        };

        return Result(query, title, "فواتير البيع ضمن الفترة",
            Cols(
                ("Number", "الفاتورة", 110, null),
                ("Date", "التاريخ", 100, "yyyy/MM/dd"),
                ("Customer", "العميل", "*", null),
                ("Total", "الإجمالي", 100, "N2"),
                ("Status", "الحالة", 100, null)),
            rows,
            Kpi("فواتير", rows.Count.ToString()),
            Kpi("إجمالي", invoices.Sum(i => i.GrandTotal).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildSalesByCustomerAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var q = context.SalesInvoices.AsNoTracking()
            .Where(i => i.CompanyId == query.CompanyId && i.BranchId == query.BranchId && !i.IsArchived);
        if (query.FromDate.HasValue)
            q = q.Where(i => i.InvoiceDate >= UtcDateTimeNormalizer.ToUtc(query.FromDate.Value));
        if (query.ToDate.HasValue)
            q = q.Where(i => i.InvoiceDate <= UtcDateTimeNormalizer.ToUtc(query.ToDate.Value));

        var grouped = await q.GroupBy(i => i.CustomerId)
            .Select(g => new { CustomerId = g.Key, Count = g.Count(), Total = g.Sum(x => x.GrandTotal) })
            .OrderByDescending(x => x.Total)
            .ToListAsync(ct);

        var custMap = await context.Customers.AsNoTracking()
            .Where(c => grouped.Select(g => g.CustomerId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.NameAr, ct);

        var rows = grouped.Select(g => Row(
            ("Customer", custMap.GetValueOrDefault(g.CustomerId, "—")),
            ("Count", g.Count),
            ("Total", g.Total))).ToList();

        return Result(query, "مبيعات حسب العميل", "تحليل تجميعي للمبيعات",
            Cols(
                ("Customer", "العميل", "*", null),
                ("Count", "فواتير", 80, null),
                ("Total", "الإجمالي", 120, "N2")),
            rows,
            Kpi("عملاء", rows.Count.ToString()),
            Kpi("مبيعات", grouped.Sum(g => g.Total).ToString("N2")));
    }

    private Task<ModuleReportResultDto> BuildDetailingQueueAsync(
        GetModuleReportQuery query, CancellationToken ct) =>
        BuildSalesInvoicesAsync(query, "sal.detailing", ct);

    private async Task<ModuleReportResultDto> BuildCustomerBalancesAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var customers = await context.Customers.AsNoTracking()
            .Where(c => c.CompanyId == query.CompanyId && c.IsActive && !c.IsArchived)
            .OrderBy(c => c.Code)
            .ToListAsync(ct);

        var rows = customers.Select(c => Row(
            ("Code", c.Code),
            ("Name", c.NameAr),
            ("Balance", c.Balance),
            ("Currency", c.BalanceCurrency),
            ("CreditLimit", c.CreditLimit),
            ("Phone", c.Phone ?? "—"))).ToList();

        return Result(query, "أرصدة العملاء", "ذمم مدينة وحدود ائتمان",
            Cols(
                ("Code", "الكود", 80, null),
                ("Name", "العميل", "*", null),
                ("Balance", "الرصيد", 100, "N2"),
                ("Currency", "عملة", 60, null),
                ("CreditLimit", "حد ائتمان", 100, "N2"),
                ("Phone", "هاتف", 100, null)),
            rows,
            Kpi("عملاء", rows.Count.ToString()),
            Kpi("إجمالي ذمم", customers.Sum(c => c.Balance).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildCustomerInvoicesAsync(
        GetModuleReportQuery query, CancellationToken ct) =>
        await BuildSalesInvoicesAsync(query, "sal.invoices", ct);

    private async Task<ModuleReportResultDto> BuildSupplierBalancesAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var suppliers = await context.Suppliers.AsNoTracking()
            .Where(s => s.CompanyId == query.CompanyId && s.IsActive && !s.IsArchived)
            .OrderBy(s => s.Code)
            .ToListAsync(ct);

        var rows = suppliers.Select(s => Row(
            ("Code", s.Code),
            ("Name", s.Name),
            ("Balance", s.Balance),
            ("Currency", s.BalanceCurrency))).ToList();

        return Result(query, "أرصدة الموردين", "ذمم دائنة",
            Cols(
                ("Code", "الكود", 80, null),
                ("Name", "المورد", "*", null),
                ("Balance", "الرصيد", 100, "N2"),
                ("Currency", "عملة", 60, null)),
            rows,
            Kpi("موردون", rows.Count.ToString()),
            Kpi("إجمالي", suppliers.Sum(s => s.Balance).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildPurchaseInvoicesAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var invoices = await context.PurchaseInvoices.AsNoTracking()
            .Where(p => !p.IsArchived)
            .OrderByDescending(p => p.InvoiceDate)
            .Take(5000)
            .ToListAsync(ct);

        var supIds = invoices.Select(p => p.SupplierId).Distinct().ToList();
        var supMap = await context.Suppliers.AsNoTracking()
            .Where(s => supIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name, ct);

        var rows = invoices.Select(p => Row(
            ("Number", p.InvoiceNumber),
            ("Date", p.InvoiceDate),
            ("Supplier", supMap.GetValueOrDefault(p.SupplierId, "—")),
            ("Total", p.TotalAmount),
            ("Status", p.Status.ToString()))).ToList();

        return Result(query, "فواتير الشراء", "فواتير الموردين",
            Cols(
                ("Number", "الفاتورة", 110, null),
                ("Date", "التاريخ", 100, "yyyy/MM/dd"),
                ("Supplier", "المورد", "*", null),
                ("Total", "الإجمالي", 100, "N2"),
                ("Status", "الحالة", 90, null)),
            rows,
            Kpi("فواتير", rows.Count.ToString()),
            Kpi("إجمالي", invoices.Sum(p => p.TotalAmount).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildContainerReportAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var containers = await context.Containers.AsNoTracking()
            .Where(c => c.CompanyId == query.CompanyId && c.BranchId == query.BranchId && !c.IsArchived)
            .OrderByDescending(c => c.ShipmentDate)
            .ToListAsync(ct);

        var rows = containers.Select(c => Row(
            ("Number", c.ContainerNumber),
            ("Status", ((ChinaContainerStatus)c.Status).ToString()),
            ("Rolls", c.TotalRolls),
            ("Meters", c.TotalMeters),
            ("Shipment", c.ShipmentDate),
            ("Arrival", c.ArrivalDate))).ToList();

        return Result(query, "تقرير الحاويات", "حالة حاويات الاستيراد",
            Cols(
                ("Number", "الحاوية", 110, null),
                ("Status", "الحالة", 100, null),
                ("Rolls", "أثواب", 70, null),
                ("Meters", "أمتار", 90, "N0"),
                ("Shipment", "شحن", 100, "yyyy/MM/dd"),
                ("Arrival", "وصول", 100, "yyyy/MM/dd")),
            rows,
            Kpi("حاويات", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildLandingCostReportAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var costs = await context.LandingCosts.AsNoTracking()
            .Join(context.Containers.AsNoTracking(),
                l => l.ContainerId, c => c.Id, (l, c) => new { l, c })
            .Where(x => x.c.CompanyId == query.CompanyId)
            .ToListAsync(ct);

        var rows = costs.Select(x => Row(
            ("Container", x.c.ContainerNumber),
            ("Customs", x.l.CustomsAmount),
            ("Shipping", x.l.Shipping),
            ("Insurance", x.l.Insurance),
            ("TotalMeters", x.l.TotalLengthMeters))).ToList();

        return Result(query, "تكلفة الاستيراد", "Landing Cost للحاويات",
            Cols(
                ("Container", "الحاوية", 110, null),
                ("Customs", "جمارك", 90, "N2"),
                ("Shipping", "شحن", 90, "N2"),
                ("Insurance", "تأمين", 90, "N2"),
                ("TotalMeters", "أمتار", 90, "N0")),
            rows,
            Kpi("حاويات", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildContainerInventoryReportAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var containers = await context.Containers.AsNoTracking()
            .Where(c => c.CompanyId == query.CompanyId && c.BranchId == query.BranchId && !c.IsArchived)
            .ToListAsync(ct);

        var rows = new List<Dictionary<string, object?>>();
        foreach (var c in containers)
        {
            var stocks = await context.WarehouseStocks.AsNoTracking()
                .Where(s => s.ContainerId == c.Id)
                .ToListAsync(ct);
            if (stocks.Count == 0) continue;
            rows.Add(Row(
                ("Container", c.ContainerNumber),
                ("Rolls", stocks.Sum(s => s.RollCount)),
                ("Meters", stocks.Sum(s => s.TotalMeters)),
                ("Available", stocks.Sum(s => s.AvailableMeters)),
                ("Reserved", stocks.Sum(s => s.ReservedMeters))));
        }

        return Result(query, "مخزون الحاوية", "أمتار متاحة ومحجوزة لكل حاوية",
            Cols(
                ("Container", "الحاوية", 110, null),
                ("Rolls", "أثواب", 70, null),
                ("Meters", "إجمالي", 100, "N0"),
                ("Available", "متاح", 100, "N0"),
                ("Reserved", "محجوز", 100, "N0")),
            rows,
            Kpi("حاويات", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildJournalReportAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var q = context.JournalEntries.AsNoTracking()
            .Where(j => j.CompanyId == query.CompanyId);
        if (query.FromDate.HasValue)
            q = q.Where(j => j.EntryDate >= UtcDateTimeNormalizer.ToUtc(query.FromDate.Value));
        if (query.ToDate.HasValue)
            q = q.Where(j => j.EntryDate <= UtcDateTimeNormalizer.ToUtc(query.ToDate.Value));

        var entries = await q.OrderByDescending(j => j.EntryDate).Take(5000).ToListAsync(ct);
        var rows = entries.Select(j => Row(
            ("Number", j.EntryNumber),
            ("Date", j.EntryDate),
            ("Description", j.Description),
            ("Status", ((JournalEntryStatus)j.Status).ToString()))).ToList();

        return Result(query, "دفتر اليومية", "قيود اليومية",
            Cols(
                ("Number", "رقم القيد", 100, null),
                ("Date", "التاريخ", 100, "yyyy/MM/dd"),
                ("Description", "البيان", "*", null),
                ("Status", "الحالة", 90, null)),
            rows,
            Kpi("قيود", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildReceiptsReportAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var q = context.ReceiptVouchers.AsNoTracking().Where(r => !r.IsArchived);
        if (query.FromDate.HasValue)
            q = q.Where(r => r.VoucherDate >= UtcDateTimeNormalizer.ToUtc(query.FromDate.Value));
        if (query.ToDate.HasValue)
            q = q.Where(r => r.VoucherDate <= UtcDateTimeNormalizer.ToUtc(query.ToDate.Value));

        var items = await q.OrderByDescending(r => r.VoucherDate).Take(5000).ToListAsync(ct);
        var rows = items.Select(r => Row(
            ("Number", r.VoucherNumber),
            ("Date", r.VoucherDate),
            ("Amount", r.Amount),
            ("Status", r.Status.ToString()))).ToList();

        return Result(query, "سندات القبض", "تحصيلات نقدية",
            Cols(
                ("Number", "السند", 100, null),
                ("Date", "التاريخ", 100, "yyyy/MM/dd"),
                ("Amount", "المبلغ", 100, "N2"),
                ("Status", "الحالة", 90, null)),
            rows,
            Kpi("سندات", rows.Count.ToString()),
            Kpi("إجمالي", items.Sum(r => r.Amount).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildPaymentsReportAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var q = context.PaymentVouchers.AsNoTracking().Where(p => !p.IsArchived);
        if (query.FromDate.HasValue)
            q = q.Where(p => p.VoucherDate >= UtcDateTimeNormalizer.ToUtc(query.FromDate.Value));
        if (query.ToDate.HasValue)
            q = q.Where(p => p.VoucherDate <= UtcDateTimeNormalizer.ToUtc(query.ToDate.Value));

        var items = await q.OrderByDescending(p => p.VoucherDate).Take(5000).ToListAsync(ct);
        var rows = items.Select(p => Row(
            ("Number", p.VoucherNumber),
            ("Date", p.VoucherDate),
            ("Amount", p.Amount),
            ("Status", p.Status.ToString()))).ToList();

        return Result(query, "سندات الصرف", "مدفوعات نقدية",
            Cols(
                ("Number", "السند", 100, null),
                ("Date", "التاريخ", 100, "yyyy/MM/dd"),
                ("Amount", "المبلغ", 100, "N2"),
                ("Status", "الحالة", 90, null)),
            rows,
            Kpi("سندات", rows.Count.ToString()),
            Kpi("إجمالي", items.Sum(p => p.Amount).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildExpenseSliceAsync(
        GetModuleReportQuery query, string key, CancellationToken ct)
    {
        var q = context.Expenses.AsNoTracking()
            .Where(e => e.CompanyId == query.CompanyId && !e.IsArchived);
        if (query.FromDate.HasValue)
            q = q.Where(e => e.StartDate >= UtcDateTimeNormalizer.ToUtc(query.FromDate.Value));
        if (query.ToDate.HasValue)
            q = q.Where(e => e.StartDate <= UtcDateTimeNormalizer.ToUtc(query.ToDate.Value));

        if (key == "exp.recurring")
            q = q.Where(e => e.IsRecurring);

        var expenses = await q.Include(e => e.Payments).OrderByDescending(e => e.StartDate).Take(5000).ToListAsync(ct);

        if (key == "exp.outstanding")
            expenses = expenses.Where(e => e.BaseAmount > e.Payments.Where(p => p.Status == (int)ExpensePaymentStatus.Completed).Sum(p => p.AmountBase)).ToList();

        var rows = expenses.Select(e =>
        {
            var paid = e.Payments.Where(p => p.Status == (int)ExpensePaymentStatus.Completed).Sum(p => p.AmountBase);
            return Row(
                ("Code", e.Code),
                ("Name", e.Name),
                ("Start", e.StartDate),
                ("Base", e.BaseAmount),
                ("Paid", paid),
                ("Remaining", Math.Max(0, e.BaseAmount - paid)),
                ("Status", ((ExpenseStatus)e.Status).ToString()));
        }).ToList();

        var title = key switch
        {
            "exp.upcoming" => "دفعات قادمة",
            "exp.recurring" => "مصاريف متكررة",
            _ => "مصاريف مستحقة"
        };

        return Result(query, title, "تقرير مصاريف",
            Cols(
                ("Code", "الكود", 80, null),
                ("Name", "المصروف", "*", null),
                ("Start", "التاريخ", 100, "yyyy/MM/dd"),
                ("Base", "المبلغ", 90, "N2"),
                ("Paid", "مدفوع", 90, "N2"),
                ("Remaining", "متبقي", 90, "N2"),
                ("Status", "الحالة", 90, null)),
            rows,
            Kpi("سجلات", rows.Count.ToString()),
            Kpi("إجمالي USD", expenses.Sum(e => e.BaseAmount).ToString("N2")));
    }

    private async Task<ModuleReportResultDto> BuildEmployeesAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var employees = await context.Employees.AsNoTracking()
            .Where(e => e.CompanyId == query.CompanyId && e.IsActive && !e.IsArchived)
            .OrderBy(e => e.FullName)
            .ToListAsync(ct);

        var deptIds = employees.Select(e => e.DepartmentId).Distinct().ToList();
        var depts = await context.Departments.AsNoTracking()
            .Where(d => deptIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.Name, ct);

        var rows = employees.Select(e => Row(
            ("Code", e.EmployeeCode),
            ("Name", e.FullName),
            ("Department", depts.GetValueOrDefault(e.DepartmentId, "—")),
            ("Job", e.JobTitle),
            ("Salary", e.BasicSalary),
            ("HireDate", e.HireDate))).ToList();

        return Result(query, "الموظفون", "قائمة الموظفين",
            Cols(
                ("Code", "الكود", 80, null),
                ("Name", "الاسم", "*", null),
                ("Department", "القسم", 120, null),
                ("Job", "المسمى", 120, null),
                ("Salary", "الراتب", 100, "N2"),
                ("HireDate", "التعيين", 100, "yyyy/MM/dd")),
            rows,
            Kpi("موظفون", rows.Count.ToString()));
    }

    private async Task<ModuleReportResultDto> BuildSaleReadyContainersAsync(
        GetModuleReportQuery query, CancellationToken ct)
    {
        var readyStatuses = new[]
        {
            (int)ChinaContainerStatus.Approved,
            (int)ChinaContainerStatus.InWarehouse,
            (int)ChinaContainerStatus.LandingCostReviewed
        };

        var containers = await context.Containers.AsNoTracking()
            .Where(c => c.CompanyId == query.CompanyId && c.BranchId == query.BranchId
                        && !c.IsArchived && readyStatuses.Contains(c.Status))
            .OrderByDescending(c => c.ArrivalDate)
            .ToListAsync(ct);

        var rows = containers.Select(c => Row(
            ("Number", c.ContainerNumber),
            ("Status", ((ChinaContainerStatus)c.Status).ToString()),
            ("Rolls", c.TotalRolls),
            ("Meters", c.TotalMeters),
            ("Arrival", c.ArrivalDate))).ToList();

        return Result(query, "حاويات جاهزة للبيع", "حاويات معتمدة أو في المخزن",
            Cols(
                ("Number", "الحاوية", 110, null),
                ("Status", "الحالة", 100, null),
                ("Rolls", "أثواب", 70, null),
                ("Meters", "أمتار", 90, "N0"),
                ("Arrival", "الوصول", 100, "yyyy/MM/dd")),
            rows,
            Kpi("حاويات", rows.Count.ToString()));
    }

    private IQueryable<WarehouseEntity> WarehouseEntities(CancellationToken ct) =>
        context.Warehouses.AsNoTracking();

    private static ModuleReportResultDto Empty(GetModuleReportQuery query, string message) =>
        Result(query, message, "", [], [], Kpi("—", "0"));

    private static ModuleReportResultDto Result(
        GetModuleReportQuery query,
        string title,
        string description,
        IReadOnlyList<ModuleReportColumnDto> columns,
        IReadOnlyList<Dictionary<string, object?>> rows,
        params ModuleReportKpiDto[] kpis) => new()
    {
        ReportKey = query.ReportKey,
        Title = title,
        Description = description,
        GeneratedAt = DateTime.UtcNow,
        FromDate = query.FromDate,
        ToDate = query.ToDate,
        Columns = columns,
        Rows = rows,
        Kpis = kpis
    };

    private static ModuleReportKpiDto Kpi(string label, string value, string icon = "\uE9D2") =>
        new() { Label = label, Value = value, IconGlyph = icon };

    private static Dictionary<string, object?> Row(params (string Key, object? Value)[] cells) =>
        cells.ToDictionary(c => c.Key, c => c.Value);

    private static List<ModuleReportColumnDto> Cols(params (string Key, string Header, object Width, string? Format)[] cols) =>
        cols.Select(c => new ModuleReportColumnDto
        {
            Key = c.Key,
            HeaderAr = c.Header,
            Width = c.Width is string ? 0 : Convert.ToDouble(c.Width),
            IsStar = c.Width is string,
            Format = c.Format
        }).ToList();
}
