using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Application.DTOs.Capital;
using ERPSystem.Application.DTOs.Expenses;
using ERPSystem.Controls.China;
using ERPSystem.Core.Accounting;
using ERPSystem.Core.Customers;
using ERPSystem.Core.Domain;
using ERPSystem.Core.HR;
using ERPSystem.Core.Inventory;
using ERPSystem.Core.Purchases;
using ERPSystem.Core.Sales;
using ERPSystem.Core.Suppliers;

namespace ERPSystem.Core.Actions
{
    public static class EntityDisplayNameResolver
    {
        public static string Resolve(object? row, EntityType entityType)
        {
            if (row == null) return "";

            return entityType switch
            {
                EntityType.Customer when row is CustomerListRow c =>
                    string.IsNullOrWhiteSpace(c.NameAr) ? c.Code : c.NameAr,

                EntityType.Customer when row is CustomerModel c =>
                    string.IsNullOrWhiteSpace(c.NameAr) ? c.Code : c.NameAr,

                EntityType.SalesInvoice when row is SalesInvoice inv =>
                    inv.CustomerNameAr,

                EntityType.SalesInvoice when row is Views.Sales.FabricSalesInvoiceRow fr =>
                    fr.CustomerName,

                EntityType.FabricItem when row is FabricItemModel f =>
                    f.FabricName,

                EntityType.Supplier when row is SupplierListRow s =>
                    string.IsNullOrWhiteSpace(s.NameAr) ? s.Code : s.NameAr,

                EntityType.Supplier when row is SupplierModel s =>
                    s.Name,

                EntityType.PurchaseInvoice when row is PurchaseListRow p =>
                    p.InvoiceNumber,

                EntityType.PurchaseInvoice when row is PurchaseInvoiceModel p =>
                    p.InvoiceNumber,

                EntityType.ImportContainer when row is ContainerListRow c =>
                    c.ContainerNumber,

                EntityType.Employee when row is EmployeeModel e =>
                    e.FullName,

                EntityType.JournalEntry when row is JournalEntryModel j =>
                    j.EntryNumber,

                EntityType.Warehouse when row is WarehouseEntity w =>
                    w.Name,

                EntityType.Cashbox when row is Cashbox cb =>
                    cb.Name,

                EntityType.Expense when row is ExpenseListDto ex =>
                    ex.Name,

                EntityType.CapitalPartner when row is CapitalPartnerListDto p =>
                    p.FullName,

                EntityType.Account when row is AccountListDto a =>
                    string.IsNullOrWhiteSpace(a.NameAr) ? a.Code : a.NameAr,

                EntityType.JournalEntry when row is JournalEntryListDto j =>
                    j.EntryNumber,

                _ => row.ToString() ?? ""
            };
        }

        public static string ResolveKey(object? row, EntityType entityType)
        {
            if (row == null) return Guid.NewGuid().ToString("N");

            return entityType switch
            {
                EntityType.Customer when row is CustomerListRow c => c.Code,
                EntityType.Customer when row is CustomerModel c => c.Code,
                EntityType.SalesInvoice when row is SalesInvoice inv => inv.InvoiceNumber,
                EntityType.SalesInvoice when row is Views.Sales.FabricSalesInvoiceRow fr => fr.InvoiceNumber,
                EntityType.FabricItem when row is FabricItemModel f => f.Code,
                EntityType.Supplier when row is SupplierListRow s => s.Code,
                EntityType.Supplier when row is SupplierModel s => s.Code,
                EntityType.PurchaseInvoice when row is PurchaseListRow p => p.InvoiceNumber,
                EntityType.PurchaseInvoice when row is PurchaseInvoiceModel p => p.InvoiceNumber,
                EntityType.ImportContainer when row is ContainerListRow c => c.ContainerNumber,
                EntityType.Employee when row is EmployeeModel e => e.EmployeeCode,
                EntityType.JournalEntry when row is JournalEntryModel j => j.EntryNumber,
                EntityType.Warehouse when row is WarehouseEntity w => w.Code,
                EntityType.Cashbox when row is Cashbox cb => cb.Code,
                EntityType.Expense when row is ExpenseListDto ex => ex.Code,
                EntityType.CapitalPartner when row is CapitalPartnerListDto p => p.Code,
                EntityType.Account when row is AccountListDto a => a.Code,
                EntityType.JournalEntry when row is JournalEntryListDto j => j.EntryNumber,
                _ => Guid.NewGuid().ToString("N")
            };
        }
    }
}
