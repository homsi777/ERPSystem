namespace ERPSystem.Core.Navigation;

using ERPSystem.Core;

/// <summary>Odoo-style per-module reporting catalog.</summary>
public sealed record ModuleReportDef(
    string Key,
    string TitleAr,
    string DescriptionAr,
    string IconGlyph,
    string GroupAr,
    bool UsesCustomView = false);

public static class ModuleReportRegistry
{
    public static IReadOnlyList<ModuleReportDef> Get(AppModule module) => module switch
    {
        AppModule.Inventory =>
        [
            new("inv.warehouses", "تقرير المستودعات", "ملخص كل مستودع: الأثواب والأمتار والسعة", "\uE8B7", "المستودعات"),
            new("inv.stock_balance", "أرصدة المخزون", "تفصيل أرصدة الأقمشة حسب المستودع والحاوية", "\uE821", "المستودعات"),
            new("inv.warehouse_move", "حركة المستودع", "حركات المناقلة والإدخال والإخراج", "\uE8AB", "الحركات"),
            new("inv.item_move", "حركة المادة", "حركة الأثواب والأمتار (بيع، حجز، متاح)", "\uE8CB", "الحركات"),
            new("inv.item_analysis", "تحليل مادة", "تحليل تجميعي لكل نوع قماش", "\uE9D2", "تحليلي"),
            new("inv.low_stock", "تنبيه نقص المخزون", "أصناف تحت حد التنبيه", "\uE783", "تحليلي"),
            new("inv.valuation", "تقييم المخزون", "قيمة المخزون المتاح بالتكلفة", "\uE8C1", "تحليلي"),
            new("inv.stocktake", "تقرير الجرد", "جلسات الجرد والفروقات", "\uE7B3", "العمليات"),
        ],
        AppModule.Sales =>
        [
            new("sal.invoices", "فواتير البيع", "كل فواتير البيع ضمن الفترة", "\uE9F9", "المبيعات"),
            new("sal.by_customer", "مبيعات حسب العميل", "إجمالي المبيعات لكل عميل", "\uE716", "تحليلي"),
            new("sal.detailing", "طابور التفصيل", "فواتير بانتظار التفصيل بالمستودع", "\uE8CB", "العمليات"),
            new("sal.returns", "مرتجعات البيع", "ملخص المرتجعات", "\uE7A6", "المبيعات"),
            new("sal.delivery", "التسليم", "فواتير جاهزة للتسليم", "\uE898", "العمليات"),
        ],
        AppModule.Customers =>
        [
            new("cus.balances", "أرصدة العملاء", "ذمم مدينة وحدود ائتمان", "\uE8F1", "الذمم"),
            new("cus.statements", "كشوف حساب", "ملخص حسابات العملاء", "\uE8A1", "الذمم"),
            new("cus.invoices", "فواتير العملاء", "فواتير البيع حسب العميل", "\uE9F9", "المبيعات"),
        ],
        AppModule.Suppliers =>
        [
            new("sup.balances", "أرصدة الموردين", "ذمم دائنة للموردين", "\uE7BF", "الذمم"),
            new("sup.statements", "كشوف حساب", "ملخص حسابات الموردين", "\uE8A1", "الذمم"),
            new("sup.invoices", "فواتير الموردين", "فواتير الشراء", "\uE9F9", "المشتريات"),
            new("sup.top_suppliers", "أكبر الموردين", "أعلى الموردين حسب حجم المشتريات", "\uE9D2", "تحليلي"),
            new("sup.overdue", "متأخرات الموردين", "ذمم متأخرة حسب شروط السداد", "\uE823", "الذمم"),
        ],
        AppModule.Accounting =>
        [
            new("acc.trial_balance", "ميزان المراجعة", "أرصدة الحسابات في تاريخ محدد", "\uE9D2", "مالية", true),
            new("acc.account_ledger", "كشف حساب", "حركة حساب محاسبي", "\uE8A1", "مالية", true),
            new("acc.journal", "دفتر اليومية", "قيود اليومية ضمن الفترة", "\uE8C1", "مالية"),
            new("acc.receipts", "سندات القبض", "تحصيلات نقدية", "\uE7A6", "نقدية"),
            new("acc.payments", "سندات الصرف", "مدفوعات نقدية", "\uE719", "نقدية"),
            new("acc.receivables", "الذمم المدينة", "أرصدة العملاء", "\uE8F1", "ذمم"),
            new("acc.payables", "الذمم الدائنة", "أرصدة الموردين", "\uE7BF", "ذمم"),
        ],
        AppModule.Expenses =>
        [
            new("exp.detailed", "تقرير مفصل", "تقرير شامل بالتاريخ والتصنيف", "\uE8A1", "مصاريف", true),
            new("exp.outstanding", "المستحقة", "مصاريف غير مسددة", "\uE823", "مصاريف"),
            new("exp.upcoming", "دفعات قادمة", "التزامات الدفع القريبة", "\uE787", "مصاريف"),
            new("exp.recurring", "المتكررة", "مصاريف دورية", "\uE8A5", "تحليلي"),
        ],
        AppModule.CapitalPartners =>
        [
            new("cap.summary", "ملخص رأس المال", "رأس المال والاستثمارات", "\uE8C1", "رأس المال", true),
            new("cap.statement", "كشف شريك", "حركة حساب شريك", "\uE8A1", "شركاء", true),
            new("cap.distributions", "توزيعات الأرباح", "سجل التوزيعات", "\uE9D2", "شركاء"),
        ],
        AppModule.ChinaImport =>
        [
            new("cn.containers", "تقرير الحاويات", "حالة الحاويات والأمتار", "\uE7BF", "استيراد"),
            new("cn.landing_cost", "تكلفة الاستيراد", "Landing Cost والجمارك", "\uE8C1", "مالية"),
            new("cn.inventory", "مخزون الحاوية", "أمتار متاحة ومحجوزة ومباعة", "\uE821", "مخزون"),
            new("cn.sale_ready", "جاهزة للبيع", "حاويات جاهزة للتسعير والبيع", "\uE73E", "عمليات"),
        ],
        AppModule.Purchases =>
        [
            new("pur.invoices", "فواتير الشراء", "فواتير الموردين", "\uE9F9", "مشتريات"),
            new("pur.by_supplier", "مشتريات حسب المورد", "تحليل تجميعي", "\uE779", "تحليلي"),
            new("pur.overdue", "فواتير متأخرة", "مستحقات تجاوزت تاريخ الاستحقاق", "\uE823", "مشتريات"),
            new("pur.returns", "مرتجعات الشراء", "إشعارات دائنة للموردين", "\uE7A6", "مشتريات"),
            new("pur.orders", "أوامر الشراء", "طلبات الشراء وحالتها", "\uE8A5", "مشتريات"),
        ],
        AppModule.HR =>
        [
            new("hr.employees", "الموظفون", "قائمة الموظفين والأقسام", "\uE716", "موارد بشرية"),
            new("hr.attendance", "الحضور", "ملخص الحضور والغياب", "\uE823", "موارد بشرية"),
            new("hr.payroll", "الرواتب", "ملخص الرواتب", "\uE8C1", "موارد بشرية"),
        ],
        AppModule.Reports =>
        [
            new("exec.dashboard", "لوحة الإدارة", "مؤشرات شاملة عبر الأقسام", "\uE9D2", "إدارة"),
            new("exec.sales_vs_purch", "مبيعات مقابل مشتريات", "مقارنة دورية", "\uE8F1", "إدارة"),
        ],
        _ => []
    };

    public static ModuleReportDef? Find(AppModule module, string key) =>
        Get(module).FirstOrDefault(r => r.Key.Equals(key, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> Groups(AppModule module) =>
        Get(module).Select(r => r.GroupAr).Distinct().ToList();
}
