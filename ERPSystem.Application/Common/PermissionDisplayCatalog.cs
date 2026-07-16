namespace ERPSystem.Application.Common;

public static class PermissionDisplayCatalog
{
    private static readonly Dictionary<string, string> ModuleLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accounting"] = "المحاسبة",
        ["suppliers"] = "الموردون",
        ["customers"] = "العملاء",
        ["sales"] = "المبيعات",
        ["purchases"] = "المشتريات",
        ["finance"] = "المالية",
        ["openingbalances"] = "الأرصدة الافتتاحية",
        ["containers"] = "استيراد الصين",
        ["warehouse"] = "المستودع",
        ["hr"] = "الموارد البشرية",
        ["expenses"] = "المصاريف",
        ["capital"] = "رأس المال والشركاء",
        ["settings"] = "الإعدادات",
        ["inventory"] = "المخزون",
        ["security"] = "الأمان والحساسية"
    };

    private static readonly Dictionary<string, string> PermissionLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accounting.account.create"] = "إنشاء حساب",
        ["accounting.account.edit"] = "تعديل حساب",
        ["accounting.account.deactivate"] = "إيقاف حساب",
        ["accounting.account.view"] = "عرض دليل الحسابات",
        ["accounting.journal.create"] = "إنشاء قيد يومية",
        ["accounting.journal.post"] = "ترحيل قيد",
        ["accounting.journal.reverse"] = "عكس قيد",
        ["suppliers.create"] = "إضافة مورد",
        ["suppliers.deactivate"] = "إيقاف مورد",
        ["suppliers.opening-balance"] = "رصيد افتتاحي للمورد",
        ["customers.create"] = "إضافة عميل",
        ["customers.deactivate"] = "إيقاف عميل",
        ["customers.opening-balance"] = "رصيد افتتاحي للعميل",
        ["sales.create"] = "إنشاء فاتورة بيع",
        ["sales.approve"] = "اعتماد فاتورة بيع",
        ["sales.send-to-warehouse"] = "إرسال للمستودع",
        ["sales.cancel"] = "إلغاء فاتورة",
        ["sales.deliver"] = "تأكيد التسليم اللوجستي (سطح المكتب)",
        ["sales.return"] = "مرتجع بيع",
        ["purchases.create"] = "إنشاء فاتورة شراء",
        ["purchases.post"] = "ترحيل فاتورة شراء",
        ["containers.create"] = "استيراد حاوية",
        ["containers.approve"] = "اعتماد حاوية",
        ["containers.landing-cost"] = "تكلفة وصول",
        ["containers.move-to-warehouse"] = "تحويل للمخزن",
        ["warehouse.detailing"] = "تفصيل المستودع — شاشة التسليم (ويب)",
        [OpeningBalanceAuthorization.InventoryOpeningStockPermission] = "مواد أول المدة — المخزون",
        ["finance.receipt.create"] = "إنشاء سند قبض",
        ["finance.receipt.post"] = "ترحيل سند قبض",
        ["finance.payment.create"] = "إنشاء سند صرف",
        ["finance.payment.post"] = "ترحيل سند صرف",
        ["finance.cashbox.create"] = "إنشاء صندوق",
        ["finance.cashbox.edit"] = "تعديل صندوق",
        ["finance.cashbox.transfer"] = "تحويل بين صناديق",
        ["openingbalances.view"] = "عرض الأرصدة الافتتاحية",
        ["openingbalances.create"] = "إنشاء رصيد افتتاحي",
        ["openingbalances.edit"] = "تعديل رصيد افتتاحي",
        ["openingbalances.import"] = "استيراد أرصدة",
        ["openingbalances.approve"] = "اعتماد رصيد",
        ["openingbalances.post"] = "ترحيل رصيد",
        ["openingbalances.archive"] = "أرشفة رصيد",
        ["openingbalances.export"] = "تصدير أرصدة",
        ["openingbalances.print"] = "طباعة أرصدة",
        ["hr.employee.manage"] = "إدارة الموظفين",
        ["hr.department.manage"] = "إدارة الأقسام",
        ["expenses.view"] = "عرض المصاريف",
        ["expenses.create"] = "إنشاء مصروف",
        ["expenses.edit"] = "تعديل مصروف",
        ["expenses.delete"] = "حذف مصروف",
        ["expenses.approve"] = "اعتماد مصروف",
        ["expenses.export"] = "تصدير مصاريف",
        ["expenses.print"] = "طباعة مصاريف",
        ["expenses.archive"] = "أرشفة مصروف",
        ["capital.view"] = "عرض رأس المال",
        ["capital.create"] = "إنشاء حركة رأس مال",
        ["capital.edit"] = "تعديل حركة",
        ["capital.delete"] = "حذف حركة",
        ["capital.approve"] = "اعتماد حركة",
        ["capital.export"] = "تصدير تقارير",
        ["capital.print"] = "طباعة تقارير",
        ["capital.archive"] = "أرشفة حركة",
        ["settings.users.view"] = "عرض المستخدمين",
        ["settings.users.manage"] = "إدارة المستخدمين",
        ["settings.roles.manage"] = "إدارة الأدوار والصلاحيات",
        ["security.general-manager"] = "مدير عام — أسعار التكلفة/الاستيراد وقسم الصين"
    };

    public static string GetModuleLabel(string module) =>
        ModuleLabels.GetValueOrDefault(module, module);

    public static string GetPermissionLabel(string code, string module, string action) =>
        PermissionLabels.GetValueOrDefault(code, $"{GetModuleLabel(module)} — {action.Replace('-', ' ')}");
}
