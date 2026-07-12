using ERPSystem.Core;
using ERPSystem.Views.China;
using ERPSystem.Views.Inventory;

namespace ERPSystem.Core.Navigation
{
    public sealed record SubmoduleDef(string Key, string LabelAr, string IconGlyph = "\uE8A5");

    public static class SubmoduleRegistry
    {
        public static IReadOnlyList<SubmoduleDef> Get(AppModule module) => module switch
        {
            AppModule.ChinaImport => new[]
            {
                new SubmoduleDef("Containers", "قائمة الحاويات", "\uE7BF"),
                new SubmoduleDef("NewImport", "استيراد حاوية جديدة", "\uE8B7"),
                new SubmoduleDef("FileAnalysis", "تحليل الملف", "\uE8A5"),
                new SubmoduleDef("CostEntry", "إدخال التكلفة", "\uE8C1"),
                new SubmoduleDef("Distribution", "توزيع الكميات", "\uE8AB"),
                new SubmoduleDef("Stocktake", "جرد الحاوية", "\uE7B3"),
                new SubmoduleDef("LandingCost", "ملخص تكلفة الاستيراد", "\uE8C1"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.Inventory => new[]
            {
                new SubmoduleDef("Dashboard", "لوحة المخزون", "\uE9D9"),
                new SubmoduleDef("Warehouses", "المستودعات", "\uE8B7"),
                new SubmoduleDef("Categories", "التصنيفات", "\uECA5"),
                new SubmoduleDef("ImportExcel", "استيراد Excel", "\uE8B7"),
                new SubmoduleDef("OpeningStock", "مواد أول مدة", "\uE710"),
                new SubmoduleDef("Stocktake", "الجرد", "\uE7B3"),
                new SubmoduleDef("Transfers", "المناقلات", "\uE8AB"),
                new SubmoduleDef("Settings", "إعدادات المخزون", "\uE713"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.Sales => new[]
            {
                new SubmoduleDef("NewInvoice", "فاتورة بيع جديدة", "\uE710"),
                new SubmoduleDef("Invoices", "قائمة فواتير البيع", "\uE9F9"),
                new SubmoduleDef("InvoiceView", "عرض فاتورة بيع", "\uE7B3"),
                new SubmoduleDef("NewReturn", "مرتجع بيع جديد", "\uE7A6"),
                new SubmoduleDef("Returns", "قائمة مرتجعات البيع", "\uE8FD"),
                new SubmoduleDef("Detailing", "تفصيل الأطوال — المستودع", "\uE8CB"),
                new SubmoduleDef("Delivery", "التسليم", "\uE898"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.Customers => new[]
            {
                new SubmoduleDef("List", "سجل العملاء", "\uE716"),
                new SubmoduleDef("Form", "إضافة / تعديل عميل", "\uE70F"),
                new SubmoduleDef("Opening", "أرصدة افتتاحية", "\uE8C1"),
                new SubmoduleDef("Statement", "كشف حساب عميل", "\uE8A1"),
                new SubmoduleDef("Invoices", "كشف فواتير عميل", "\uE9F9"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.Suppliers => new[]
            {
            new SubmoduleDef("List", "سجل الموردين", "\uE779"),
            new SubmoduleDef("Form", "إضافة / تعديل مورد", "\uE70F"),
            new SubmoduleDef("Opening", "أرصدة افتتاحية", "\uE8C1"),
            new SubmoduleDef("Statement", "كشف حساب مورد", "\uE8A1"),
                new SubmoduleDef("Invoices", "كشف فواتير مورد", "\uE9F9"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.Accounting => new[]
            {
                new SubmoduleDef("Chart", "شجرة الحسابات", "\uE8C3"),
                new SubmoduleDef("Journal", "دفتر اليومية", "\uE8C1"),
                new SubmoduleDef("JournalBooks", "دفاتر اليومية", "\uE8A5"),
                new SubmoduleDef("AccountLedger", "كشف حساب", "\uE8A1"),
                new SubmoduleDef("Receipts", "سند قبض", "\uE7A6"),
                new SubmoduleDef("Payments", "سند صرف", "\uE719"),
                new SubmoduleDef("Cashboxes", "الصناديق", "\uE825"),
                new SubmoduleDef("Transfers", "تحويل بين الصناديق", "\uE8AB"),
                new SubmoduleDef("Receivables", "الذمم المدينة", "\uE8F1"),
                new SubmoduleDef("Payables", "الذمم الدائنة", "\uE7BF"),
                new SubmoduleDef("TrialBalance", "ميزان مراجعة", "\uE9D2"),
                new SubmoduleDef("OpeningBalances", "أرصدة افتتاحية", "\uE8F1"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.Expenses => new[]
            {
                new SubmoduleDef("List", "تعريفات المصاريف", "\uE9D9"),
                new SubmoduleDef("Entries", "سجل القيود", "\uE8A5"),
                new SubmoduleDef("Entry", "قيد مصروف جديد", "\uE70F"),
                new SubmoduleDef("Form", "تعريف مصروف جديد", "\uE710"),
                new SubmoduleDef("Dashboard", "لوحة المصاريف", "\uE9D2"),
                new SubmoduleDef("Reports", "تقارير المصاريف", "\uE8A1"),
                new SubmoduleDef("Workspace", "مركز عمل المصروف", "\uE8A7"),
                new SubmoduleDef("Categories", "فئات المصاريف", "\uECA5"),
            },
            AppModule.CapitalPartners => new[]
            {
                new SubmoduleDef("List", "سجل الشركاء", "\uE716"),
                new SubmoduleDef("Transactions", "حركات رأس المال", "\uE8A5"),
                new SubmoduleDef("Investment", "حركة رأس مال", "\uE710"),
                new SubmoduleDef("Form", "شريك جديد", "\uE70F"),
                new SubmoduleDef("Dashboard", "لوحة رأس المال", "\uE8C1"),
                new SubmoduleDef("Distributions", "توزيع الأرباح", "\uE9D2"),
                new SubmoduleDef("Reports", "تقارير رأس المال", "\uE8A1"),
                new SubmoduleDef("Workspace", "مركز عمل الشريك", "\uE8A7"),
            },
            AppModule.Reports => new[]
            {
                new SubmoduleDef("BI", "لوحة الإدارة", "\uE9D2"),
                new SubmoduleDef("Reports", "التقارير التنفيذية", "\uE8A1"),
            },
            AppModule.Purchases => new[]
            {
                new SubmoduleDef("Invoices", "فواتير الشراء", "\uE9F9"),
                new SubmoduleDef("Form", "فاتورة شراء جديدة", "\uE70F"),
                new SubmoduleDef("Orders", "أمر شراء", "\uE8A5"),
                new SubmoduleDef("Returns", "مرتجع شراء", "\uE7A6"),
                new SubmoduleDef("Reports", "التقارير", "\uE9D2"),
            },
            AppModule.HR => new[]
            {
                new SubmoduleDef("Employees", "الموظفون", "\uE716"),
                new SubmoduleDef("Departments", "الأقسام", "\uEE57"),
                new SubmoduleDef("Attendance", "الحضور والانصراف", "\uE823"),
                new SubmoduleDef("Leaves", "الإجازات", "\uE787"),
                new SubmoduleDef("Shifts", "الورديات", "\uE728"),
                new SubmoduleDef("Contracts", "العقود", "\uE8A5"),
                new SubmoduleDef("Payroll", "الرواتب", "\uE8C1"),
                new SubmoduleDef("Advances", "السلف والخصومات", "\uE719"),
                new SubmoduleDef("Reports", "تقارير HR", "\uE9D2"),
            },
            AppModule.Settings => new[]
            {
                new SubmoduleDef("Company", "هوية الشركة", "\uE8D7"),
                new SubmoduleDef("Branches", "الفروع والمستودعات", "\uE909"),
                new SubmoduleDef("Users", "المستخدمون والصلاحيات", "\uE716"),
                new SubmoduleDef("Locale", "اللغة والمنطقة", "\uE774"),
                new SubmoduleDef("Currencies", "العملات وأسعار الصرف", "\uE8C1"),
                new SubmoduleDef("Finance", "الإعدادات المالية", "\uE8C3"),
                new SubmoduleDef("Taxes", "الضرائب", "\uE8C3"),
                new SubmoduleDef("Numbering", "ترقيم المستندات", "\uE8A5"),
                new SubmoduleDef("Print", "قوالب الطباعة", "\uE749"),
                new SubmoduleDef("Inventory", "إعدادات المخزون", "\uE821"),
                new SubmoduleDef("Sales", "إعدادات المبيعات", "\uE8F1"),
                new SubmoduleDef("Backup", "النسخ الاحتياطي", "\uE8B7"),
                new SubmoduleDef("Audit", "سجل التدقيق", "\uE7C3"),
            },
            _ => Array.Empty<SubmoduleDef>()
        };

        public static string ResolveKey(AppModule module, string? subPage)
        {
            var subs = Get(module);
            if (subs.Count == 0) return "";
            if (string.IsNullOrWhiteSpace(subPage)) return subs[0].Key;

            if (module == AppModule.ChinaImport)
            {
                if (subPage.Equals("ExcelReview", StringComparison.OrdinalIgnoreCase))
                    subPage = "FileAnalysis";

                if (ChinaViews.IsKnownRoute(subPage))
                    return subPage;
            }

            if (module == AppModule.Inventory && InventoryViews.IsKnownRoute(subPage))
                return subPage;

            return subs.FirstOrDefault(s =>
                s.Key.Equals(subPage, StringComparison.OrdinalIgnoreCase) ||
                s.LabelAr.Contains(subPage, StringComparison.OrdinalIgnoreCase))?.Key ?? subs[0].Key;
        }
    }
}
