namespace ERPSystem.Core.Actions
{
    public static class EntityActionRegistry
    {
        private static readonly Dictionary<EntityType, IReadOnlyList<EntityActionDefinition>> _actions = new()
        {
            [EntityType.Customer] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.CustomerStatement, "كشف حساب", "\uE8A1", "الحسابات"),
                new(EntityActionId.CustomerReceivables, "الذمم", "\uE8C1", "الحسابات"),
                new(EntityActionId.CustomerReceipt, "سند قبض", "\uE7BF", "السندات"),
                new(EntityActionId.CustomerPayment, "سند دفع", "\uE719", "السندات"),
                new(EntityActionId.CustomerInvoices, "فواتير العميل", "\uE9F9", "المستندات"),
                new(EntityActionId.CustomerDetails, "تفاصيل العميل", "\uE716", "العميل"),
                new(EntityActionId.CustomerEdit, "تعديل العميل", "\uE70F", "العميل"),
                new(EntityActionId.CustomerDeactivate, "تعطيل العميل", "\uE7E8", "العميل", destructive: true),
            ],
            [EntityType.SalesInvoice] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.InvoiceView, "عرض التفاصيل", "\uE7B3"),
                new(EntityActionId.InvoiceEdit, "تعديل الفاتورة", "\uE70F"),
                new(EntityActionId.InvoiceDetailLengths, "تفصيل الأطوال", "\uE8CB"),
                new(EntityActionId.InvoicePrint, "طباعة", "\uE749"),
                new(EntityActionId.InvoiceExportPdf, "تصدير PDF", "\uEDE1"),
                new(EntityActionId.InvoiceReturn, "إنشاء مرتجع", "\uE7A6"),
                new(EntityActionId.InvoiceCancel, "إلغاء الفاتورة", "\uE711", destructive: true),
            ],
            [EntityType.FabricItem] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.FabricCard, "بطاقة الصنف", "\uE8A5"),
                new(EntityActionId.FabricMovement, "حركة الصنف", "\uE8CB"),
                new(EntityActionId.FabricEdit, "تعديل الصنف", "\uE70F"),
                new(EntityActionId.FabricPriceEdit, "تعديل السعر", "\uE8C1"),
                new(EntityActionId.FabricTransfer, "مناقلة", "\uE8AB"),
                new(EntityActionId.FabricDeactivate, "تعطيل الصنف", "\uE7E8", destructive: true),
            ],
            [EntityType.Supplier] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.SupplierStatement, "كشف حساب المورد", "\uE8A1"),
                new(EntityActionId.SupplierPayment, "سند دفع", "\uE719"),
                new(EntityActionId.SupplierInvoices, "فواتير المورد", "\uE9F9"),
                new(EntityActionId.SupplierDetails, "تفاصيل المورد", "\uE779"),
                new(EntityActionId.SupplierEdit, "تعديل المورد", "\uE70F"),
                new(EntityActionId.SupplierDeactivate, "تعطيل المورد", "\uE7E8", destructive: true),
            ],
            [EntityType.PurchaseInvoice] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.PurchaseView, "عرض التفاصيل", "\uE7B3"),
                new(EntityActionId.PurchasePrint, "طباعة", "\uE749"),
                new(EntityActionId.PurchaseExportPdf, "تصدير PDF", "\uEDE1"),
                new(EntityActionId.PurchaseReturn, "إنشاء مرتجع", "\uE7A6"),
                new(EntityActionId.PurchaseCancel, "إلغاء", "\uE711", destructive: true),
            ],
            [EntityType.ImportContainer] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.ContainerDetails, "تفاصيل الحاوية", "\uE7B3"),
                new(EntityActionId.ContainerImportReview, "مراجعة الاستيراد", "\uE8B7"),
                new(EntityActionId.ContainerDistribution, "توزيع الكميات على العملاء", "\uE8AB"),
                new(EntityActionId.ContainerStocktake, "جرد الحاوية", "\uE7B3"),
                new(EntityActionId.ContainerCosts, "تكلفة الاستيراد", "\uE8C1"),
                new(EntityActionId.ContainerApprove, "اعتماد الحاوية", "\uE73E"),
                new(EntityActionId.ContainerArchive, "أرشفة الحاوية", "\uE7B8", destructive: true),
                new(EntityActionId.ContainerDelete, "حذف تجريبي", "\uE74D", destructive: true),
            ],
            [EntityType.Employee] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.EmployeeProfile, "ملف الموظف", "\uE716"),
                new(EntityActionId.EmployeeAttendance, "سجل الدوام", "\uE823"),
                new(EntityActionId.EmployeeLeaves, "الإجازات", "\uE787"),
                new(EntityActionId.EmployeeContracts, "العقود", "\uE8A5"),
                new(EntityActionId.EmployeeEdit, "تعديل الموظف", "\uE70F"),
                new(EntityActionId.EmployeeDeactivate, "تعطيل الموظف", "\uE7E8", destructive: true),
            ],
            [EntityType.Warehouse] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.FabricMovement, "حركات المستودع", "\uE8CB"),
                new(EntityActionId.FabricTransfer, "مناقلة", "\uE8AB"),
            ],
            [EntityType.Cashbox] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.VoucherPrint, "طباعة", "\uE749"),
                new(EntityActionId.VoucherExportPdf, "تصدير PDF", "\uEDE1"),
            ],
            [EntityType.JournalEntry] =
            [
                new(EntityActionId.OpenOperationsCenter, "فتح مركز العمليات", "\uE8A7"),
                new(EntityActionId.JournalView, "عرض القيد", "\uE7B3"),
                new(EntityActionId.VoucherPrint, "طباعة السند", "\uE749"),
                new(EntityActionId.VoucherExportPdf, "تصدير PDF", "\uEDE1"),
                new(EntityActionId.JournalCancel, "إلغاء العملية", "\uE711", destructive: true),
            ],
        };

        public static IReadOnlyList<EntityActionDefinition> GetActions(EntityType entityType) =>
            _actions.TryGetValue(entityType, out var list) ? list : Array.Empty<EntityActionDefinition>();

        public static string GetActionTitle(EntityActionId actionId, string entityName)
        {
            var baseTitle = actionId switch
            {
                EntityActionId.OpenOperationsCenter => "مركز العمليات",
                EntityActionId.CustomerStatement => "كشف حساب العميل",
                EntityActionId.CustomerReceivables => "ذمم العميل",
                EntityActionId.CustomerReceipt => "سند قبض",
                EntityActionId.CustomerPayment => "سند دفع",
                EntityActionId.CustomerInvoices => "فواتير العميل",
                EntityActionId.CustomerDetails => "مركز عمليات العميل",
                EntityActionId.CustomerEdit => "تعديل العميل",
                EntityActionId.CustomerDeactivate => "تعطيل العميل",
                EntityActionId.InvoiceView => "مركز عمليات فاتورة البيع",
                EntityActionId.InvoiceEdit => "تعديل فاتورة بيع",
                EntityActionId.InvoiceDetailLengths => "تفصيل الأطوال",
                EntityActionId.InvoicePrint => "طباعة فاتورة",
                EntityActionId.InvoiceExportPdf => "تصدير فاتورة PDF",
                EntityActionId.InvoiceReturn => "مرتجع بيع",
                EntityActionId.InvoiceCancel => "إلغاء فاتورة",
                EntityActionId.FabricCard => "مركز عمليات الصنف",
                EntityActionId.FabricMovement => "حركة صنف",
                EntityActionId.FabricEdit => "تعديل صنف",
                EntityActionId.FabricPriceEdit => "تعديل سعر الصنف",
                EntityActionId.FabricTransfer => "مناقلة صنف",
                EntityActionId.FabricDeactivate => "تعطيل صنف",
                EntityActionId.SupplierStatement => "كشف حساب المورد",
                EntityActionId.SupplierPayment => "سند دفع",
                EntityActionId.SupplierInvoices => "فواتير المورد",
                EntityActionId.SupplierDetails => "مركز عمليات المورد",
                EntityActionId.SupplierEdit => "تعديل المورد",
                EntityActionId.SupplierDeactivate => "تعطيل المورد",
                EntityActionId.PurchaseView => "مركز عمليات فاتورة الشراء",
                EntityActionId.PurchasePrint => "طباعة فاتورة شراء",
                EntityActionId.PurchaseExportPdf => "تصدير فاتورة شراء PDF",
                EntityActionId.PurchaseReturn => "مرتجع شراء",
                EntityActionId.PurchaseCancel => "إلغاء فاتورة شراء",
                EntityActionId.ContainerDetails => "مركز عمليات الحاوية",
                EntityActionId.ContainerImportReview => "مراجعة الاستيراد",
                EntityActionId.ContainerDistribution => "توزيع الكميات",
                EntityActionId.ContainerStocktake => "جرد الحاوية",
                EntityActionId.ContainerApprove => "اعتماد الحاوية",
                EntityActionId.ContainerArchive => "أرشفة الحاوية",
                EntityActionId.ContainerDelete => "حذف الحاوية",
                EntityActionId.ContainerItems => "أصناف الحاوية",
                EntityActionId.ContainerCosts => "تكاليف الاستيراد",
                EntityActionId.ContainerExcelImport => "استيراد Excel",
                EntityActionId.EmployeeProfile => "مركز عمليات الموظف",
                EntityActionId.EmployeeAttendance => "سجل الدوام",
                EntityActionId.EmployeeLeaves => "إجازات الموظف",
                EntityActionId.EmployeeContracts => "عقود الموظف",
                EntityActionId.EmployeeEdit => "تعديل الموظف",
                EntityActionId.EmployeeDeactivate => "تعطيل الموظف",
                EntityActionId.JournalView => "مركز عمليات القيد",
                EntityActionId.VoucherPrint => "طباعة السند",
                EntityActionId.VoucherExportPdf => "تصدير السند PDF",
                EntityActionId.JournalCancel => "إلغاء القيد",
                _ => "مساحة عمل"
            };
            return string.IsNullOrWhiteSpace(entityName) ? baseTitle : $"{baseTitle} — {entityName}";
        }
    }
}
