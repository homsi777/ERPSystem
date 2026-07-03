using ERPSystem.Core;
using ERPSystem.Views.Accounting;
using ERPSystem.Views.Capital;
using ERPSystem.Views.China;
using ERPSystem.Views.Expenses;
using ERPSystem.Views.Finance;
using ERPSystem.Views.Hr;
using ERPSystem.Views.Inventory;
using ERPSystem.Views.Parties;
using ERPSystem.Views.Purchases;
using ERPSystem.Views.Reports;
using ERPSystem.Views.Sales;
using ERPSystem.Views.Settings;
using System.Windows.Controls;

namespace ERPSystem.Views
{
    public static class SubmoduleViewFactory
    {
        public static UserControl Create(AppModule module, string key) => (module, key) switch
        {
            (AppModule.ChinaImport, _) => ChinaViews.Create(key),
            (AppModule.Inventory, _) => InventoryViews.Create(key),
            (AppModule.Sales, _) => SalesViews.Create(key),
            (AppModule.Customers, _) => PartyViews.CreateCustomer(key),
            (AppModule.Suppliers, _) => PartyViews.CreateSupplier(key),
            (AppModule.Accounting, "Chart") => AccountingViews.Create(key),
            (AppModule.Accounting, "Journal") => AccountingViews.Create(key),
            (AppModule.Accounting, "JournalBooks") => AccountingViews.Create(key),
            (AppModule.Accounting, "TrialBalance") => AccountingViews.Create(key),
            (AppModule.Accounting, "AccountLedger") => AccountingViews.Create(key),
            (AppModule.Accounting, "Reports") => AccountingViews.Create(key),
            (AppModule.Accounting, "Receipts") => AccountingViews.Create(key),
            (AppModule.Accounting, "Payments") => AccountingViews.Create(key),
            (AppModule.Accounting, "AccountForm") => AccountingViews.Create(key),
            (AppModule.Accounting, "JournalForm") => AccountingViews.Create(key),
            (AppModule.Accounting, _) => FinanceViews.Create(key),
            (AppModule.Expenses, _) => ExpenseViews.Create(key),
            (AppModule.CapitalPartners, _) => CapitalViews.Create(key),
            (AppModule.Purchases, _) => PurchasesViews.Create(key),
            (AppModule.Reports, _) => ReportViews.Create(key),
            (AppModule.HR, _) => HrViews.Create(key),
            (AppModule.Settings, _) => SettingsViews.Create(key),
            _ => new UserControl()
        };
    }
}
