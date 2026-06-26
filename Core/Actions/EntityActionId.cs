namespace ERPSystem.Core.Actions
{
    public enum EntityActionId
    {
        OpenOperationsCenter,

        CustomerStatement,
        CustomerReceivables,
        CustomerReceipt,
        CustomerPayment,
        CustomerInvoices,
        CustomerDetails,
        CustomerEdit,
        CustomerDeactivate,

        InvoiceView,
        InvoiceEdit,
        InvoiceDetailLengths,
        InvoicePrint,
        InvoiceExportPdf,
        InvoiceReturn,
        InvoiceCancel,

        FabricCard,
        FabricMovement,
        FabricEdit,
        FabricPriceEdit,
        FabricTransfer,
        FabricDeactivate,

        SupplierStatement,
        SupplierPayment,
        SupplierInvoices,
        SupplierDetails,
        SupplierEdit,
        SupplierDeactivate,

        PurchaseView,
        PurchasePrint,
        PurchaseExportPdf,
        PurchaseReturn,
        PurchaseCancel,

        ContainerDetails,
        ContainerImportReview,
        ContainerDistribution,
        ContainerStocktake,
        ContainerApprove,
        ContainerArchive,
        ContainerDelete,
        ContainerItems,
        ContainerCosts,
        ContainerExcelImport,

        EmployeeProfile,
        EmployeeAttendance,
        EmployeeLeaves,
        EmployeeContracts,
        EmployeeEdit,
        EmployeeDeactivate,

        JournalView,
        VoucherPrint,
        VoucherExportPdf,
        JournalCancel
    }
}
