using System.ComponentModel;
using System.Windows;

namespace ERPSystem.Core
{
    public enum AppLanguage { Arabic, English }

    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private AppLanguage _language = AppLanguage.Arabic;

        public AppLanguage CurrentLanguage
        {
            get => _language;
            set
            {
                if (_language != value)
                {
                    _language = value;
                    OnPropertyChanged(nameof(CurrentLanguage));
                    OnPropertyChanged(nameof(IsArabic));
                    OnPropertyChanged(nameof(FlowDir));
                    LanguageChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public bool IsArabic => _language == AppLanguage.Arabic;
        public FlowDirection FlowDir => IsArabic ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

        public event EventHandler? LanguageChanged;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly Dictionary<string, Dictionary<string, string>> _strings = new()
        {
            ["AppName"] = new() { ["ar"] = "نظام إدارة الأعمال", ["en"] = "Business ERP" },
            ["CompanyName"] = new() { ["ar"] = "شركة الحمصي لاستيراد الأقمشة", ["en"] = "Al-Homsi Fabrics Import" },
            ["Branch"] = new() { ["ar"] = "الفرع", ["en"] = "Branch" },
            ["MainBranch"] = new() { ["ar"] = "الفرع الرئيسي", ["en"] = "Main Branch" },
            ["Search"] = new() { ["ar"] = "بحث...", ["en"] = "Search..." },
            ["Language"] = new() { ["ar"] = "English", ["en"] = "العربية" },
            ["Notifications"] = new() { ["ar"] = "الإشعارات", ["en"] = "Notifications" },
            ["Settings"] = new() { ["ar"] = "الإعدادات", ["en"] = "Settings" },
            ["User"] = new() { ["ar"] = "المستخدم", ["en"] = "User" },
            ["AdminUser"] = new() { ["ar"] = "مدير النظام", ["en"] = "System Admin" },

            // Navigation
            ["Nav_Dashboard"] = new() { ["ar"] = "الرئيسية", ["en"] = "Dashboard" },
            ["Nav_ChinaImport"] = new() { ["ar"] = "طلبات الصين", ["en"] = "China Import" },
            ["Nav_Sales"] = new() { ["ar"] = "المبيعات", ["en"] = "Sales" },
            ["Nav_Purchases"] = new() { ["ar"] = "المشتريات", ["en"] = "Purchases" },
            ["Nav_Inventory"] = new() { ["ar"] = "المخزون", ["en"] = "Inventory" },
            ["Nav_Customers"] = new() { ["ar"] = "العملاء", ["en"] = "Customers" },
            ["Nav_Suppliers"] = new() { ["ar"] = "الموردون", ["en"] = "Suppliers" },
            ["Nav_Accounting"] = new() { ["ar"] = "المالية", ["en"] = "Finance" },
            ["Nav_Reports"] = new() { ["ar"] = "التقارير", ["en"] = "Reports" },
            ["Nav_HR"] = new() { ["ar"] = "الموارد البشرية", ["en"] = "HR" },
            ["Nav_Settings"] = new() { ["ar"] = "الإعدادات", ["en"] = "Settings" },

            ["Sub_China_Containers"] = new() { ["ar"] = "قائمة الحاويات", ["en"] = "Containers" },
            ["Sub_China_Excel"] = new() { ["ar"] = "استيراد Excel", ["en"] = "Excel Import" },
            ["Sub_China_Summary"] = new() { ["ar"] = "ملخص الأمتار والأتواب", ["en"] = "Meters Summary" },
            ["Sub_Inv_Balances"] = new() { ["ar"] = "أرصدة الأقمشة", ["en"] = "Fabric Balances" },
            ["Sub_Rep_Containers"] = new() { ["ar"] = "تقارير الحاويات", ["en"] = "Container Reports" },
            ["Sub_Rep_Receivables"] = new() { ["ar"] = "تقارير الذمم", ["en"] = "Receivables Reports" },
            ["Sub_HR_Employees"] = new() { ["ar"] = "الموظفون", ["en"] = "Employees" },
            ["Sub_HR_Attendance"] = new() { ["ar"] = "الحضور والانصراف", ["en"] = "Attendance" },
            ["Sub_HR_Leaves"] = new() { ["ar"] = "الإجازات", ["en"] = "Leaves" },
            ["Sub_HR_Shifts"] = new() { ["ar"] = "الورديات", ["en"] = "Shifts" },
            ["Sub_HR_Contracts"] = new() { ["ar"] = "العقود", ["en"] = "Contracts" },
            ["Sub_Set_Invoices"] = new() { ["ar"] = "إعدادات الفواتير", ["en"] = "Invoice Settings" },
            ["Sub_Set_Backup"] = new() { ["ar"] = "النسخ الاحتياطي", ["en"] = "Backup" },
            ["Sub_Set_Database"] = new() { ["ar"] = "قاعدة البيانات", ["en"] = "Database" },

            // Dashboard
            ["Dashboard_Title"] = new() { ["ar"] = "لوحة التحكم", ["en"] = "Dashboard" },
            ["Dashboard_Subtitle"] = new() { ["ar"] = "ملخص استيراد وبيع الأقمشة بالجملة", ["en"] = "Fabric import & wholesale overview" },
            ["Dashboard_QuickActions"] = new() { ["ar"] = "إجراءات سريعة", ["en"] = "Quick Actions" },
            ["Dashboard_RecentActivity"] = new() { ["ar"] = "النشاط الأخير", ["en"] = "Recent Activity" },
            ["Dashboard_SalesOverview"] = new() { ["ar"] = "نظرة عامة على المبيعات", ["en"] = "Sales Overview" },

            // Metric Cards
            ["Metric_SalesToday"] = new() { ["ar"] = "مبيعات اليوم", ["en"] = "Sales Today" },
            ["Metric_InvoicesCount"] = new() { ["ar"] = "عدد الفواتير", ["en"] = "Invoices Count" },
            ["Metric_StockValue"] = new() { ["ar"] = "قيمة المخزون", ["en"] = "Stock Value" },
            ["Metric_Receivables"] = new() { ["ar"] = "الذمم المدينة", ["en"] = "Receivables" },
            ["Metric_Payables"] = new() { ["ar"] = "الذمم الدائنة", ["en"] = "Payables" },
            ["Metric_ActiveCustomers"] = new() { ["ar"] = "العملاء النشطون", ["en"] = "Active Customers" },
            ["Metric_TrendUp"] = new() { ["ar"] = "↑ ارتفاع", ["en"] = "↑ Increase" },
            ["Metric_TrendDown"] = new() { ["ar"] = "↓ انخفاض", ["en"] = "↓ Decrease" },
            ["Metric_VsYesterday"] = new() { ["ar"] = "مقارنة بالأمس", ["en"] = "vs yesterday" },

            // Quick Actions
            ["Action_NewInvoice"] = new() { ["ar"] = "فاتورة جديدة", ["en"] = "New Invoice" },
            ["Action_NewCustomer"] = new() { ["ar"] = "عميل جديد", ["en"] = "New Customer" },
            ["Action_NewProduct"] = new() { ["ar"] = "صنف قماش جديد", ["en"] = "New Fabric" },
            ["Action_NewContainer"] = new() { ["ar"] = "حاوية جديدة", ["en"] = "New Container" },
            ["Action_NewPurchase"] = new() { ["ar"] = "أمر شراء جديد", ["en"] = "New Purchase Order" },
            ["Action_NewReport"] = new() { ["ar"] = "تقرير جديد", ["en"] = "New Report" },

            // Modules
            ["Module_Sales"] = new() { ["ar"] = "إدارة المبيعات", ["en"] = "Sales Management" },
            ["Module_Sales_Sub"] = new() { ["ar"] = "إدارة فواتير البيع وعروض الأسعار وأوامر البيع", ["en"] = "Manage sales invoices, quotes and sales orders" },
            ["Module_Purchases"] = new() { ["ar"] = "إدارة المشتريات", ["en"] = "Purchases Management" },
            ["Module_Purchases_Sub"] = new() { ["ar"] = "إدارة أوامر الشراء والموردين وفواتير المشتريات", ["en"] = "Manage purchase orders, suppliers and purchase invoices" },
            ["Module_Inventory"] = new() { ["ar"] = "إدارة المخزون", ["en"] = "Inventory Management" },
            ["Module_Inventory_Sub"] = new() { ["ar"] = "تتبع المنتجات والمواد والمستودعات", ["en"] = "Track products, materials and warehouses" },
            ["Module_Customers"] = new() { ["ar"] = "إدارة العملاء", ["en"] = "Customer Management" },
            ["Module_Customers_Sub"] = new() { ["ar"] = "قاعدة بيانات العملاء والمتابعة والذمم", ["en"] = "Customer database, follow-up and receivables" },
            ["Module_Suppliers"] = new() { ["ar"] = "إدارة الموردين", ["en"] = "Supplier Management" },
            ["Module_Suppliers_Sub"] = new() { ["ar"] = "قاعدة بيانات الموردين والذمم الدائنة", ["en"] = "Supplier database and payables" },
            ["Module_Accounting"] = new() { ["ar"] = "الحسابات والمالية", ["en"] = "Accounting & Finance" },
            ["Module_Accounting_Sub"] = new() { ["ar"] = "القيود المحاسبية وميزان المراجعة والتقارير المالية", ["en"] = "Journal entries, trial balance and financial reports" },
            ["Module_Reports"] = new() { ["ar"] = "التقارير والتحليلات", ["en"] = "Reports & Analytics" },
            ["Module_Reports_Sub"] = new() { ["ar"] = "تقارير المبيعات والمخزون والمالية", ["en"] = "Sales, inventory and financial reports" },
            ["Module_POS"] = new() { ["ar"] = "نقطة البيع", ["en"] = "Point of Sale" },
            ["Module_POS_Sub"] = new() { ["ar"] = "نظام البيع السريع للمحلات والمطاعم", ["en"] = "Fast sales system for retail and restaurants" },
            ["Module_Settings"] = new() { ["ar"] = "إعدادات النظام", ["en"] = "System Settings" },
            ["Module_Settings_Sub"] = new() { ["ar"] = "تكوين النظام والمستخدمين والصلاحيات", ["en"] = "System configuration, users and permissions" },

            // Common
            ["New"] = new() { ["ar"] = "جديد", ["en"] = "New" },
            ["Edit"] = new() { ["ar"] = "تعديل", ["en"] = "Edit" },
            ["Delete"] = new() { ["ar"] = "حذف", ["en"] = "Delete" },
            ["Save"] = new() { ["ar"] = "حفظ", ["en"] = "Save" },
            ["Cancel"] = new() { ["ar"] = "إلغاء", ["en"] = "Cancel" },
            ["Search2"] = new() { ["ar"] = "بحث", ["en"] = "Search" },
            ["Filter"] = new() { ["ar"] = "تصفية", ["en"] = "Filter" },
            ["Export"] = new() { ["ar"] = "تصدير", ["en"] = "Export" },
            ["Import"] = new() { ["ar"] = "استيراد", ["en"] = "Import" },
            ["Print"] = new() { ["ar"] = "طباعة", ["en"] = "Print" },
            ["ViewAll"] = new() { ["ar"] = "عرض الكل", ["en"] = "View All" },
            ["ComingSoon"] = new() { ["ar"] = "قريباً", ["en"] = "Coming Soon" },
            ["NoData"] = new() { ["ar"] = "لا توجد بيانات", ["en"] = "No data available" },
            ["Loading"] = new() { ["ar"] = "جاري التحميل...", ["en"] = "Loading..." },

            // Currency
            ["Currency"] = new() { ["ar"] = "ر.س", ["en"] = "SAR" },

            // POS
            ["POS_Categories"] = new() { ["ar"] = "الفئات", ["en"] = "Categories" },
            ["POS_Products"] = new() { ["ar"] = "المنتجات", ["en"] = "Products" },
            ["POS_Cart"] = new() { ["ar"] = "عربة الشراء", ["en"] = "Cart" },
            ["POS_Customer"] = new() { ["ar"] = "العميل", ["en"] = "Customer" },
            ["POS_Total"] = new() { ["ar"] = "الإجمالي", ["en"] = "Total" },
            ["POS_Discount"] = new() { ["ar"] = "الخصم", ["en"] = "Discount" },
            ["POS_Tax"] = new() { ["ar"] = "الضريبة", ["en"] = "Tax" },
            ["POS_NetTotal"] = new() { ["ar"] = "الصافي", ["en"] = "Net Total" },
            ["POS_Payment"] = new() { ["ar"] = "الدفع", ["en"] = "Payment" },
            ["POS_Cash"] = new() { ["ar"] = "نقداً", ["en"] = "Cash" },
            ["POS_Card"] = new() { ["ar"] = "بطاقة", ["en"] = "Card" },
            ["POS_Transfer"] = new() { ["ar"] = "تحويل", ["en"] = "Transfer" },
            ["POS_Checkout"] = new() { ["ar"] = "إتمام البيع", ["en"] = "Checkout" },

            // ─── Submenu: Sales ───
            ["Sub_Sales_Invoices"] = new() { ["ar"] = "فواتير البيع", ["en"] = "Sales Invoices" },
            ["Sub_Sales_Returns"] = new() { ["ar"] = "مرتجعات البيع", ["en"] = "Sales Returns" },
            ["Sub_Sales_Quotes"] = new() { ["ar"] = "عروض الأسعار", ["en"] = "Price Quotes" },
            ["Sub_Sales_Orders"] = new() { ["ar"] = "طلبات البيع", ["en"] = "Sales Orders" },
            ["Sub_Sales_Sessions"] = new() { ["ar"] = "جلسات البيع", ["en"] = "Sales Sessions" },
            ["Sub_Sales_Reports"] = new() { ["ar"] = "تقارير المبيعات", ["en"] = "Sales Reports" },
            ["Sub_Sales_Section1"] = new() { ["ar"] = "المستندات", ["en"] = "Documents" },
            ["Sub_Sales_Section2"] = new() { ["ar"] = "إدارة", ["en"] = "Management" },

            // ─── Submenu: Purchases ───
            ["Sub_Purch_Orders"] = new() { ["ar"] = "أوامر الشراء", ["en"] = "Purchase Orders" },
            ["Sub_Purch_Returns"] = new() { ["ar"] = "مرتجعات الشراء", ["en"] = "Purchase Returns" },
            ["Sub_Purch_Requests"] = new() { ["ar"] = "طلبات الشراء", ["en"] = "Purchase Requests" },
            ["Sub_Purch_Invoices"] = new() { ["ar"] = "فواتير الموردين", ["en"] = "Supplier Invoices" },
            ["Sub_Purch_Reports"] = new() { ["ar"] = "تقارير المشتريات", ["en"] = "Purchase Reports" },
            ["Sub_Purch_Section1"] = new() { ["ar"] = "المستندات", ["en"] = "Documents" },
            ["Sub_Purch_Section2"] = new() { ["ar"] = "تقارير", ["en"] = "Reports" },

            // ─── Submenu: Inventory ───
            ["Sub_Inv_Products"] = new() { ["ar"] = "المنتجات", ["en"] = "Products" },
            ["Sub_Inv_Categories"] = new() { ["ar"] = "التصنيفات", ["en"] = "Categories" },
            ["Sub_Inv_Units"] = new() { ["ar"] = "وحدات القياس", ["en"] = "Units of Measure" },
            ["Sub_Inv_Warehouses"] = new() { ["ar"] = "المخازن", ["en"] = "Warehouses" },
            ["Sub_Inv_Count"] = new() { ["ar"] = "الجرد", ["en"] = "Stock Count" },
            ["Sub_Inv_Transfers"] = new() { ["ar"] = "تحويلات المخزون", ["en"] = "Stock Transfers" },
            ["Sub_Inv_Movements"] = new() { ["ar"] = "حركات المخزون", ["en"] = "Stock Movements" },
            ["Sub_Inv_Alerts"] = new() { ["ar"] = "تنبيهات النقص", ["en"] = "Low Stock Alerts" },
            ["Sub_Inv_Section1"] = new() { ["ar"] = "المنتجات والفئات", ["en"] = "Products & Categories" },
            ["Sub_Inv_Section2"] = new() { ["ar"] = "العمليات", ["en"] = "Operations" },

            // ─── Submenu: Customers ───
            ["Sub_Cust_List"] = new() { ["ar"] = "قائمة العملاء", ["en"] = "Customer List" },
            ["Sub_Cust_Groups"] = new() { ["ar"] = "مجموعات العملاء", ["en"] = "Customer Groups" },
            ["Sub_Cust_Receivables"] = new() { ["ar"] = "الذمم المدينة", ["en"] = "Receivables" },
            ["Sub_Cust_Statements"] = new() { ["ar"] = "كشوف الحسابات", ["en"] = "Account Statements" },

            // ─── Submenu: Suppliers ───
            ["Sub_Supp_List"] = new() { ["ar"] = "قائمة الموردين", ["en"] = "Supplier List" },
            ["Sub_Supp_Groups"] = new() { ["ar"] = "مجموعات الموردين", ["en"] = "Supplier Groups" },
            ["Sub_Supp_Payables"] = new() { ["ar"] = "الذمم الدائنة", ["en"] = "Payables" },
            ["Sub_Supp_Statements"] = new() { ["ar"] = "كشوف الحسابات", ["en"] = "Account Statements" },

            // ─── Submenu: Accounting ───
            ["Sub_Acc_Chart"] = new() { ["ar"] = "دليل الحسابات", ["en"] = "Chart of Accounts" },
            ["Sub_Acc_Journal"] = new() { ["ar"] = "القيود اليومية", ["en"] = "Journal Entries" },
            ["Sub_Acc_Receipts"] = new() { ["ar"] = "سندات القبض", ["en"] = "Receipt Vouchers" },
            ["Sub_Acc_Payments"] = new() { ["ar"] = "سندات الدفع", ["en"] = "Payment Vouchers" },
            ["Sub_Acc_Receivables"] = new() { ["ar"] = "الذمم المدينة", ["en"] = "Receivables" },
            ["Sub_Acc_Payables"] = new() { ["ar"] = "الذمم الدائنة", ["en"] = "Payables" },
            ["Sub_Acc_TrialBalance"] = new() { ["ar"] = "ميزان المراجعة", ["en"] = "Trial Balance" },
            ["Sub_Acc_FinReports"] = new() { ["ar"] = "التقارير المالية", ["en"] = "Financial Reports" },
            ["Sub_Acc_Section1"] = new() { ["ar"] = "الدفاتر", ["en"] = "Books" },
            ["Sub_Acc_Section2"] = new() { ["ar"] = "الذمم والتقارير", ["en"] = "Balances & Reports" },

            // ─── Submenu: Reports ───
            ["Sub_Rep_Sales"] = new() { ["ar"] = "تقارير المبيعات", ["en"] = "Sales Reports" },
            ["Sub_Rep_Purchases"] = new() { ["ar"] = "تقارير المشتريات", ["en"] = "Purchase Reports" },
            ["Sub_Rep_Inventory"] = new() { ["ar"] = "تقارير المخزون", ["en"] = "Inventory Reports" },
            ["Sub_Rep_Financial"] = new() { ["ar"] = "التقارير المالية", ["en"] = "Financial Reports" },
            ["Sub_Rep_Customers"] = new() { ["ar"] = "تقارير العملاء", ["en"] = "Customer Reports" },
            ["Sub_Rep_Suppliers"] = new() { ["ar"] = "تقارير الموردين", ["en"] = "Supplier Reports" },
            ["Sub_Rep_POS"] = new() { ["ar"] = "تقارير نقطة البيع", ["en"] = "POS Reports" },
            ["Sub_Rep_Section1"] = new() { ["ar"] = "التشغيل", ["en"] = "Operations" },
            ["Sub_Rep_Section2"] = new() { ["ar"] = "المالية", ["en"] = "Finance" },

            // ─── Submenu: POS ───
            ["Sub_POS_Open"] = new() { ["ar"] = "فتح نقطة البيع", ["en"] = "Open POS" },
            ["Sub_POS_Sessions"] = new() { ["ar"] = "جلسات نقاط البيع", ["en"] = "POS Sessions" },
            ["Sub_POS_Settings"] = new() { ["ar"] = "إعدادات نقطة البيع", ["en"] = "POS Settings" },
            ["Sub_POS_Reports"] = new() { ["ar"] = "تقارير نقطة البيع", ["en"] = "POS Reports" },

            // ─── Submenu: Settings ───
            ["Sub_Set_System"] = new() { ["ar"] = "إعدادات النظام", ["en"] = "System Settings" },
            ["Sub_Set_Company"] = new() { ["ar"] = "إعدادات الشركة", ["en"] = "Company Settings" },
            ["Sub_Set_Branches"] = new() { ["ar"] = "إعدادات الفروع", ["en"] = "Branch Settings" },
            ["Sub_Set_Users"] = new() { ["ar"] = "المستخدمون والصلاحيات", ["en"] = "Users & Permissions" },
            ["Sub_Set_Print"] = new() { ["ar"] = "الطباعة", ["en"] = "Printing" },
            ["Sub_Set_Currencies"] = new() { ["ar"] = "العملات", ["en"] = "Currencies" },
            ["Sub_Set_Taxes"] = new() { ["ar"] = "الضرائب", ["en"] = "Taxes" },
            ["Sub_Set_Section1"] = new() { ["ar"] = "النظام", ["en"] = "System" },
            ["Sub_Set_Section2"] = new() { ["ar"] = "الإعدادات المالية", ["en"] = "Financial Config" },

            // Status bar
            ["Status_Ready"] = new() { ["ar"] = "جاهز", ["en"] = "Ready" },
            ["Status_Connected"] = new() { ["ar"] = "متصل بقاعدة البيانات", ["en"] = "Database Connected" },

            // Table columns
            ["Col_Number"] = new() { ["ar"] = "#", ["en"] = "#" },
            ["Col_Name"] = new() { ["ar"] = "الاسم", ["en"] = "Name" },
            ["Col_Date"] = new() { ["ar"] = "التاريخ", ["en"] = "Date" },
            ["Col_Amount"] = new() { ["ar"] = "المبلغ", ["en"] = "Amount" },
            ["Col_Status"] = new() { ["ar"] = "الحالة", ["en"] = "Status" },
            ["Col_Actions"] = new() { ["ar"] = "الإجراءات", ["en"] = "Actions" },
        };

        public string Get(string key)
        {
            var langKey = IsArabic ? "ar" : "en";
            if (_strings.TryGetValue(key, out var translations))
                if (translations.TryGetValue(langKey, out var value))
                    return value;
            return key;
        }

        public string this[string key] => Get(key);

        public void ToggleLanguage() =>
            CurrentLanguage = IsArabic ? AppLanguage.English : AppLanguage.Arabic;
    }
}
