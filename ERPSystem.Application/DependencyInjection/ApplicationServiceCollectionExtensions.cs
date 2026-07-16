using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Abstractions.Services;
using ERPSystem.Application.Commands.Accounting;
using ERPSystem.Application.Commands.Capital;
using ERPSystem.Application.Commands.Containers;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Commands.Expenses;
using ERPSystem.Application.Commands.Finance;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Commands.Purchases;
using ERPSystem.Application.Commands.Suppliers;
using ERPSystem.Application.Commands.Inventory;
using ERPSystem.Application.Commands.HR;
using ERPSystem.Application.Queries.HR;
using ERPSystem.Application.Commands.Identity;
using ERPSystem.Application.Queries.Identity;
using ERPSystem.Application.UseCases.HR;
using ERPSystem.Application.UseCases.Identity;
using ERPSystem.Application.Services;
using ERPSystem.Application.Queries.Inventory;
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Queries.Capital;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.Queries.Finance;
using ERPSystem.Application.Queries.Purchases;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Accounting;
using ERPSystem.Application.UseCases.Capital;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Customers;
using ERPSystem.Application.UseCases.Expenses;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Finance;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Application.UseCases.Reports;
using ERPSystem.Application.UseCases.Sales;
using ERPSystem.Application.UseCases.Purchases;
using ERPSystem.Application.Commands.Catalog;
using ERPSystem.Application.Queries.Catalog;
using ERPSystem.Application.UseCases.Catalog;
using ERPSystem.Application.UseCases.Inventory;
using ERPSystem.Application.UseCases.Suppliers;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEvents.DomainEventDispatcher>();
        services.AddScoped<IChinaContainerPurchaseBridgeService, ChinaContainerPurchaseBridgeService>();
        services.AddScoped<PermissionService>();
        services.AddScoped<IPermissionService>(sp => sp.GetRequiredService<PermissionService>());
        services.AddSingleton<ISalesTaxEngine, Tax.SalesTaxEngine>();
        services.AddScoped<SalesInvoiceTaxService>();
        services.AddScoped<SalesInvoiceTaxPreviewService>();
        RegisterCustomerHandlers(services);
        RegisterSupplierHandlers(services);
        RegisterPurchaseHandlers(services);
        RegisterExpenseHandlers(services);
        RegisterCapitalHandlers(services);
        RegisterAccountingHandlers(services);
        RegisterFinanceHandlers(services);
        RegisterOpeningBalanceHandlers(services);
        RegisterSalesHandlers(services);
        RegisterContainerHandlers(services);
        RegisterInventoryHandlers(services);
        RegisterFabricCatalogHandlers(services);
        RegisterHrHandlers(services);
        RegisterIdentityAdminHandlers(services);
        RegisterQueryHandlers(services);
        return services;
    }

    private static void RegisterIdentityAdminHandlers(IServiceCollection services)
    {
        services.AddScoped<GetIdentityUsersHandler>();
        services.AddScoped<GetIdentityRolesHandler>();
        services.AddScoped<GetPermissionTreeHandler>();
        services.AddScoped<GetRolePermissionsHandler>();
        services.AddScoped<ICommandHandler<UpdateRolePermissionsCommand, ApplicationResult>, UpdateRolePermissionsHandler>();
        services.AddScoped<ICommandHandler<CreateIdentityRoleCommand, ApplicationResult<Guid>>, CreateIdentityRoleHandler>();
        services.AddScoped<ICommandHandler<CreateIdentityUserCommand, ApplicationResult<Guid>>, CreateIdentityUserHandler>();
    }

    private static void RegisterHrHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateEmployeeCommand, ApplicationResult<Guid>>, CreateEmployeeHandler>();
        services.AddScoped<ICommandHandler<UpdateEmployeeCommand, ApplicationResult>, UpdateEmployeeHandler>();
        services.AddScoped<ICommandHandler<CreateDepartmentCommand, ApplicationResult<Guid>>, CreateDepartmentHandler>();
        services.AddScoped<ICommandHandler<UpdateDepartmentCommand, ApplicationResult>, UpdateDepartmentHandler>();
        services.AddScoped<GetEmployeeListHandler>();
        services.AddScoped<GetEmployeeDetailsHandler>();
        services.AddScoped<GetDepartmentListHandler>();
    }

    private static void RegisterCustomerHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateCustomerCommand, ApplicationResult<Guid>>, CreateCustomerHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomerCommand, ApplicationResult>, UpdateCustomerHandler>();
        services.AddScoped<ICommandHandler<DeactivateCustomerCommand, ApplicationResult>, DeactivateCustomerHandler>();
        services.AddScoped<ICommandHandler<ReconcileCustomerAccountCommand, ApplicationResult>, ReconcileCustomerAccountHandler>();
        services.AddScoped<ICommandHandler<PostCustomerOpeningBalanceCommand, ApplicationResult<Application.DTOs.Customers.CustomerOpeningBalanceResultDto>>, PostCustomerOpeningBalanceHandler>();
    }

    private static void RegisterSupplierHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateSupplierCommand, ApplicationResult<Guid>>, CreateSupplierHandler>();
        services.AddScoped<ICommandHandler<UpdateSupplierCommand, ApplicationResult>, UpdateSupplierHandler>();
        services.AddScoped<ICommandHandler<DeactivateSupplierCommand, ApplicationResult>, DeactivateSupplierHandler>();
        services.AddScoped<ICommandHandler<PostSupplierOpeningBalanceCommand, ApplicationResult<Application.DTOs.Suppliers.SupplierOpeningBalanceResultDto>>, PostSupplierOpeningBalanceHandler>();
    }

    private static void RegisterPurchaseHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreatePurchaseInvoiceDraftCommand, ApplicationResult<Guid>>, CreatePurchaseInvoiceDraftHandler>();
        services.AddScoped<ICommandHandler<UpdatePurchaseInvoiceDraftCommand, ApplicationResult>, UpdatePurchaseInvoiceDraftHandler>();
        services.AddScoped<ICommandHandler<PostPurchaseInvoiceCommand, ApplicationResult<string>>, PostPurchaseInvoiceHandler>();
        services.AddScoped<ICommandHandler<CancelPurchaseInvoiceCommand, ApplicationResult>, CancelPurchaseInvoiceHandler>();
        services.AddScoped<ICommandHandler<CreatePurchaseOrderCommand, ApplicationResult<Guid>>, CreatePurchaseOrderHandler>();
        services.AddScoped<ICommandHandler<UpdatePurchaseOrderCommand, ApplicationResult>, UpdatePurchaseOrderHandler>();
        services.AddScoped<ICommandHandler<SendPurchaseOrderCommand, ApplicationResult>, SendPurchaseOrderHandler>();
        services.AddScoped<ICommandHandler<CancelPurchaseOrderCommand, ApplicationResult>, CancelPurchaseOrderHandler>();
        services.AddScoped<ICommandHandler<ConvertPurchaseOrderToInvoiceCommand, ApplicationResult<Guid>>, ConvertPurchaseOrderToInvoiceHandler>();
        services.AddScoped<ICommandHandler<CreatePurchaseReturnCommand, ApplicationResult<Guid>>, CreatePurchaseReturnHandler>();
        services.AddScoped<ICommandHandler<UpdatePurchaseReturnDraftCommand, ApplicationResult>, UpdatePurchaseReturnDraftHandler>();
        services.AddScoped<ICommandHandler<PostPurchaseReturnCommand, ApplicationResult<string>>, PostPurchaseReturnHandler>();
        services.AddScoped<ICommandHandler<BackfillChinaContainerPurchaseInvoicesCommand, ApplicationResult<ChinaContainerPurchaseBridgeBackfillResult>>, BackfillChinaContainerPurchaseInvoicesHandler>();
    }

    private static void RegisterExpenseHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateExpenseCommand, ApplicationResult<Guid>>, CreateExpenseHandler>();
        services.AddScoped<ICommandHandler<UpdateExpenseCommand, ApplicationResult>, UpdateExpenseHandler>();
        services.AddScoped<ICommandHandler<TransitionExpenseStatusCommand, ApplicationResult>, TransitionExpenseStatusHandler>();
        services.AddScoped<ICommandHandler<ApproveExpenseCommand, ApplicationResult>, ApproveExpenseHandler>();
        services.AddScoped<ICommandHandler<RejectExpenseCommand, ApplicationResult>, RejectExpenseHandler>();
        services.AddScoped<ICommandHandler<ScheduleExpenseCommand, ApplicationResult>, ScheduleExpenseHandler>();
        services.AddScoped<ICommandHandler<CloseExpenseCommand, ApplicationResult>, CloseExpenseHandler>();
        services.AddScoped<ICommandHandler<CancelExpenseCommand, ApplicationResult>, CancelExpenseHandler>();
        services.AddScoped<ICommandHandler<ArchiveExpenseCommand, ApplicationResult>, ArchiveExpenseHandler>();
        services.AddScoped<ICommandHandler<DeleteExpenseCommand, ApplicationResult>, DeleteExpenseHandler>();
        services.AddScoped<ICommandHandler<DuplicateExpenseCommand, ApplicationResult<Guid>>, DuplicateExpenseHandler>();
        services.AddScoped<ICommandHandler<RecordExpensePaymentCommand, ApplicationResult>, RecordExpensePaymentHandler>();
        services.AddScoped<ICommandHandler<ScheduleExpensePaymentCommand, ApplicationResult>, ScheduleExpensePaymentHandler>();
        services.AddScoped<ICommandHandler<CancelExpensePaymentCommand, ApplicationResult>, CancelExpensePaymentHandler>();
        services.AddScoped<ICommandHandler<AdjustExpensePaymentCommand, ApplicationResult>, AdjustExpensePaymentHandler>();
        services.AddScoped<ICommandHandler<CreateCostCenterCommand, ApplicationResult<Guid>>, CreateCostCenterHandler>();
        services.AddScoped<ICommandHandler<UpdateCostCenterCommand, ApplicationResult>, UpdateCostCenterHandler>();
        services.AddScoped<GetExpenseListHandler>();
        services.AddScoped<GetExpenseEntriesHandler>();
        services.AddScoped<GetExpenseDetailsHandler>();
        services.AddScoped<GetExpenseOperationsCenterHandler>();
        services.AddScoped<GetExpenseDashboardHandler>();
        services.AddScoped<GetExpenseCategoriesHandler>();
        services.AddScoped<GetCostCentersHandler>();
        services.AddScoped<GetExpenseAuditTrailHandler>();
        services.AddScoped<GetExpenseTimelineHandler>();
        services.AddScoped<GetExpenseReportHandler>();
        services.AddScoped<GetExpensePaymentForecastHandler>();
    }

    private static void RegisterCapitalHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateCapitalPartnerCommand, ApplicationResult<Guid>>, CreateCapitalPartnerHandler>();
        services.AddScoped<ICommandHandler<CreateCapitalPartnerWithSetupCommand, ApplicationResult<Guid>>, CreateCapitalPartnerWithSetupHandler>();
        services.AddScoped<ICommandHandler<UpdateCapitalPartnerCommand, ApplicationResult>, UpdateCapitalPartnerHandler>();
        services.AddScoped<ICommandHandler<AddPartnerParticipationCommand, ApplicationResult<Guid>>, AddPartnerParticipationHandler>();
        services.AddScoped<ICommandHandler<SetPartnerCompanyOwnershipCommand, ApplicationResult>, SetPartnerCompanyOwnershipHandler>();
        services.AddScoped<ICommandHandler<RecordCapitalTransactionCommand, ApplicationResult<Guid>>, RecordCapitalTransactionHandler>();
        services.AddScoped<ICommandHandler<ArchiveCapitalPartnerCommand, ApplicationResult>, ArchiveCapitalPartnerHandler>();
        services.AddScoped<ICommandHandler<CreateProfitDistributionCommand, ApplicationResult<Guid>>, CreateProfitDistributionHandler>();
        services.AddScoped<ICommandHandler<ApproveProfitDistributionCommand, ApplicationResult>, ApproveProfitDistributionHandler>();
        services.AddScoped<ICommandHandler<PostProfitDistributionCommand, ApplicationResult>, PostProfitDistributionHandler>();
        services.AddScoped<ICommandHandler<CloseProfitDistributionCommand, ApplicationResult>, CloseProfitDistributionHandler>();
        services.AddScoped<GetCapitalPartnerListHandler>();
        services.AddScoped<GetCapitalTransactionsHandler>();
        services.AddScoped<GetCapitalPartnerDetailsHandler>();
        services.AddScoped<GetCapitalPartnerOperationsCenterHandler>();
        services.AddScoped<GetCapitalDashboardHandler>();
        services.AddScoped<GetCapitalPartnerAuditTrailHandler>();
        services.AddScoped<GetCapitalPartnerTimelineHandler>();
        services.AddScoped<GetProfitDistributionListHandler>();
        services.AddScoped<GetCapitalReportHandler>();
    }

    private static void RegisterAccountingHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateAccountCommand, ApplicationResult<Guid>>, CreateAccountHandler>();
        services.AddScoped<ICommandHandler<UpdateAccountCommand, ApplicationResult>, UpdateAccountHandler>();
        services.AddScoped<ICommandHandler<DeactivateAccountCommand, ApplicationResult>, DeactivateAccountHandler>();
        services.AddScoped<ICommandHandler<CreateJournalEntryCommand, ApplicationResult<Guid>>, CreateJournalEntryHandler>();
        services.AddScoped<ICommandHandler<ApproveJournalEntryCommand, ApplicationResult>, ApproveJournalEntryHandler>();
        services.AddScoped<ICommandHandler<PostJournalEntryCommand, ApplicationResult>, PostJournalEntryHandler>();
        services.AddScoped<ICommandHandler<ReverseJournalEntryCommand, ApplicationResult<Guid>>, ReverseJournalEntryHandler>();
        services.AddScoped<ICommandHandler<CancelJournalEntryCommand, ApplicationResult>, CancelJournalEntryHandler>();
        services.AddScoped<GetAccountListHandler>();
        services.AddScoped<GetAccountDetailsHandler>();
        services.AddScoped<GetPostableAccountsHandler>();
        services.AddScoped<GetJournalEntryListHandler>();
        services.AddScoped<GetJournalEntryDetailsHandler>();
        services.AddScoped<GetJournalBooksHandler>();
        services.AddScoped<GetTrialBalanceHandler>();
        services.AddScoped<GetAccountLedgerHandler>();
        services.AddScoped<GetReceivablesAgingHandler>();
        services.AddScoped<GetPayablesAgingHandler>();
    }

    private static void RegisterFinanceHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateReceiptVoucherCommand, ApplicationResult<Guid>>, CreateReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<ApproveReceiptVoucherCommand, ApplicationResult>, ApproveReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<PostReceiptVoucherCommand, ApplicationResult>, PostReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<CancelReceiptVoucherCommand, ApplicationResult>, CancelReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<ReverseReceiptVoucherCommand, ApplicationResult>, ReverseReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<CreatePaymentVoucherCommand, ApplicationResult<Guid>>, CreatePaymentVoucherHandler>();
        services.AddScoped<ICommandHandler<ApprovePaymentVoucherCommand, ApplicationResult>, ApprovePaymentVoucherHandler>();
        services.AddScoped<ICommandHandler<PostPaymentVoucherCommand, ApplicationResult>, PostPaymentVoucherHandler>();
        services.AddScoped<ICommandHandler<CreateCashboxCommand, ApplicationResult<Guid>>, CreateCashboxHandler>();
        services.AddScoped<ICommandHandler<UpdateCashboxCommand, ApplicationResult>, UpdateCashboxHandler>();
        services.AddScoped<ICommandHandler<DeactivateCashboxCommand, ApplicationResult>, DeactivateCashboxHandler>();
        services.AddScoped<ICommandHandler<ActivateCashboxCommand, ApplicationResult>, ActivateCashboxHandler>();
        services.AddScoped<ICommandHandler<CreateCashboxTransferCommand, ApplicationResult<Guid>>, CreateCashboxTransferHandler>();
        services.AddScoped<ICommandHandler<PostCashboxTransferCommand, ApplicationResult>, PostCashboxTransferHandler>();
        services.AddScoped<GetCashboxListHandler>();
        services.AddScoped<GetCashboxDetailsHandler>();
        services.AddScoped<GetCashboxMovementsHandler>();
        services.AddScoped<GetCashboxTransferListHandler>();
        services.AddScoped<GetCashboxOperationsCenterHandler>();
        services.AddScoped<GetReceiptVoucherPrintHandler>();
        services.AddScoped<GetPaymentVoucherPrintHandler>();
        services.AddScoped<GetPaymentVoucherDetailsHandler>();
        services.AddScoped<GetPaymentVoucherListHandler>();
    }

    private static void RegisterOpeningBalanceHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>, CreateOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<UpdateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>, UpdateOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<SubmitOpeningBalanceCommand, ApplicationResult>, SubmitOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<ApproveOpeningBalanceCommand, ApplicationResult>, ApproveOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<RejectOpeningBalanceCommand, ApplicationResult>, RejectOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<PostOpeningBalanceCommand, ApplicationResult<OpeningBalancePostResultDto>>, PostOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<ArchiveOpeningBalanceCommand, ApplicationResult>, ArchiveOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<DuplicateOpeningBalanceCommand, ApplicationResult<OpeningBalanceListDto>>, DuplicateOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<ValidateOpeningBalanceCommand, ApplicationResult<OpeningBalanceValidationReportDto>>, ValidateOpeningBalanceHandler>();
        services.AddScoped<ICommandHandler<ImportOpeningBalanceExcelCommand, ApplicationResult<OpeningBalanceImportResultDto>>, ImportOpeningBalanceExcelHandler>();
        services.AddScoped<GetOpeningBalanceListHandler>();
        services.AddScoped<GetOpeningBalanceDetailsHandler>();
        services.AddScoped<GetOpeningBalanceDashboardHandler>();
        services.AddScoped<GetOpeningBalanceLookupsHandler>();
        services.AddScoped<GetCustomerOpeningBalanceSummaryHandler>();
    }

    private static void RegisterSalesHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>>, CreateSalesInvoiceDraftHandler>();
        services.AddScoped<ICommandHandler<UpdateSalesInvoiceDraftCommand, ApplicationResult>, UpdateSalesInvoiceDraftHandler>();
        services.AddScoped<ICommandHandler<UpdateSalesInvoiceDiscountCommand, ApplicationResult>, UpdateSalesInvoiceDiscountHandler>();
        services.AddScoped<ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult>, SendSalesInvoiceToWarehouseHandler>();
        services.AddScoped<ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult>, CompleteWarehouseDetailingHandler>();
        services.AddScoped<ICommandHandler<SaveWarehouseDetailingDraftCommand, ApplicationResult>, SaveWarehouseDetailingDraftHandler>();
        services.AddScoped<ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult>, ApproveSalesInvoiceHandler>();
        services.AddScoped<ICommandHandler<CancelSalesInvoiceCommand, ApplicationResult>, CancelSalesInvoiceHandler>();
        services.AddScoped<ICommandHandler<ReverseSalesInvoiceCommand, ApplicationResult>, ReverseSalesInvoiceHandler>();
        services.AddScoped<ICommandHandler<ConfirmSalesInvoiceDeliveryCommand, ApplicationResult>, ConfirmSalesInvoiceDeliveryHandler>();
        services.AddScoped<ICommandHandler<UpdateSalesInvoiceWarehouseCommand, ApplicationResult>, UpdateSalesInvoiceWarehouseHandler>();
        services.AddScoped<ICommandHandler<CreateSalesReturnCommand, ApplicationResult<Guid>>, CreateSalesReturnHandler>();
        services.AddScoped<ICommandHandler<UpdateSalesReturnCommand, ApplicationResult>, UpdateSalesReturnHandler>();
        services.AddScoped<ICommandHandler<PostSalesReturnCommand, ApplicationResult>, PostSalesReturnHandler>();
        services.AddScoped<ICommandHandler<CancelSalesReturnCommand, ApplicationResult>, CancelSalesReturnHandler>();
        services.AddScoped<GetSalesReturnListHandler>();
        services.AddScoped<GetSalesReturnDetailsHandler>();
        services.AddScoped<GetTaxCodesHandler>();
        services.AddScoped<CalculateSalesInvoiceTaxHandler>();
        services.AddScoped<GetSalesTaxReportHandler>();
        services.AddScoped<GetDeliveryQueueHandler>();
        services.AddScoped<GetInvoicePaymentHistoryHandler>();
    }

    private static void RegisterContainerHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateChinaContainerCommand, ApplicationResult<Guid>>, CreateChinaContainerHandler>();
        services.AddScoped<ICommandHandler<CalculateLandingCostCommand, ApplicationResult>, CalculateLandingCostHandler>();
        services.AddScoped<ICommandHandler<ApproveContainerCommand, ApplicationResult>, ApproveContainerHandler>();
        services.AddScoped<ICommandHandler<MoveContainerToWarehouseCommand, ApplicationResult>, MoveContainerToWarehouseHandler>();
        services.AddScoped<ICommandHandler<ArchiveContainerCommand, ApplicationResult>, ArchiveContainerHandler>();
        services.AddScoped<ICommandHandler<SetContainerTypeSalePricesCommand, ApplicationResult>, SetContainerTypeSalePricesHandler>();
        services.AddScoped<ICommandHandler<SaveFabricTypeAliasCommand, ApplicationResult>, SaveFabricTypeAliasHandler>();
    }

    private static void RegisterInventoryHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateWarehouseCommand, ApplicationResult<Guid>>, CreateWarehouseHandler>();
        services.AddScoped<ICommandHandler<UpdateWarehouseCommand, ApplicationResult>, UpdateWarehouseHandler>();
        services.AddScoped<ICommandHandler<DeactivateWarehouseCommand, ApplicationResult>, DeactivateWarehouseHandler>();
        services.AddScoped<ICommandHandler<ArchiveWarehouseCommand, ApplicationResult>, ArchiveWarehouseHandler>();
        services.AddScoped<ICommandHandler<ActivateWarehouseCommand, ApplicationResult>, ActivateWarehouseHandler>();
        services.AddScoped<ICommandHandler<DuplicateWarehouseCommand, ApplicationResult<Guid>>, DuplicateWarehouseHandler>();
        services.AddScoped<ICommandHandler<CreateStorageLocationCommand, ApplicationResult<Guid>>, CreateStorageLocationHandler>();
        services.AddScoped<ICommandHandler<CreateStockTransferCommand, ApplicationResult<Guid>>, CreateStockTransferHandler>();
        services.AddScoped<ICommandHandler<ApproveStockTransferCommand, ApplicationResult>, ApproveStockTransferHandler>();
        services.AddScoped<ICommandHandler<CompleteStockTransferCommand, ApplicationResult>, CompleteStockTransferHandler>();
        services.AddScoped<ICommandHandler<CreateStocktakeCommand, ApplicationResult<Guid>>, CreateStocktakeHandler>();
        services.AddScoped<ICommandHandler<UpdateStocktakeLinesCommand, ApplicationResult>, UpdateStocktakeLinesHandler>();
        services.AddScoped<ICommandHandler<PostStocktakeCommand, ApplicationResult>, PostStocktakeHandler>();
        services.AddScoped<ICommandHandler<CreateOpeningStockCommand, ApplicationResult<Guid>>, CreateOpeningStockHandler>();
        services.AddScoped<ICommandHandler<PostOpeningStockCommand, ApplicationResult>, PostOpeningStockHandler>();
        services.AddScoped<GetInventoryWarehouseListHandler>();
        services.AddScoped<GetInventoryWarehouseDetailHandler>();
        services.AddScoped<GetInventoryOperationsCenterHandler>();
        services.AddScoped<GetInventoryDashboardHandler>();
        services.AddScoped<GetFabricStockBalancesHandler>();
        services.AddScoped<GetFabricSearchProfilesHandler>();
        services.AddScoped<GetInventoryMovementsHandler>();
        services.AddScoped<GetInventoryAlertsHandler>();
        services.AddScoped<GetStockTransfersHandler>();
        services.AddScoped<GetStockTransferDetailHandler>();
        services.AddScoped<GetWarehouseTransferRollsHandler>();
        services.AddScoped<GetFabricRollsPageHandler>();
        services.AddScoped<GetFabricRollsByStockHandler>();
        services.AddScoped<GetStocktakeSessionsHandler>();
        services.AddScoped<GetStocktakeDetailHandler>();
        services.AddScoped<GetOpeningStockDocumentsHandler>();
        services.AddScoped<GetWarehouseStorageLocationsHandler>();
        services.AddScoped<GetInventoryAuditTrailHandler>();
        services.AddScoped<GetInventoryTimelineHandler>();
    }

    private static void RegisterFabricCatalogHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateFabricCategoryCommand, ApplicationResult<Guid>>, CreateFabricCategoryHandler>();
        services.AddScoped<ICommandHandler<UpdateFabricCategoryCommand, ApplicationResult>, UpdateFabricCategoryHandler>();
        services.AddScoped<ICommandHandler<DeactivateFabricCategoryCommand, ApplicationResult>, DeactivateFabricCategoryHandler>();
        services.AddScoped<ICommandHandler<CreateFabricItemCommand, ApplicationResult<Guid>>, CreateFabricItemHandler>();
        services.AddScoped<ICommandHandler<UpdateFabricItemCommand, ApplicationResult>, UpdateFabricItemHandler>();
        services.AddScoped<ICommandHandler<DeactivateFabricItemCommand, ApplicationResult>, DeactivateFabricItemHandler>();
        services.AddScoped<ICommandHandler<CreateFabricColorCommand, ApplicationResult<Guid>>, CreateFabricColorHandler>();
        services.AddScoped<ICommandHandler<UpdateFabricColorCommand, ApplicationResult>, UpdateFabricColorHandler>();
        services.AddScoped<ICommandHandler<DeactivateFabricColorCommand, ApplicationResult>, DeactivateFabricColorHandler>();
        services.AddScoped<GetFabricCategoryListHandler>();
        services.AddScoped<GetFabricItemListHandler>();
        services.AddScoped<GetFabricColorListHandler>();
        services.AddScoped<GetImportedFabricClassificationListHandler>();
        services.AddScoped<GetImportedFabricContainerFiltersHandler>();
    }

    private static void RegisterQueryHandlers(IServiceCollection services)
    {
        services.AddScoped<GetCustomerListHandler>();
        services.AddScoped<GetCustomerDetailsHandler>();
        services.AddScoped<GetCustomerOperationsCenterHandler>();
        services.AddScoped<GetCustomerStatementHandler>();
        services.AddScoped<GetCustomerSalesDetailsHandler>();
        services.AddScoped<GetCustomerAccountLedgerHandler>();
        services.AddScoped<GetSupplierListHandler>();
        services.AddScoped<GetSupplierDetailsHandler>();
        services.AddScoped<GetSupplierOperationsCenterHandler>();
        services.AddScoped<GetSupplierStatementHandler>();
        services.AddScoped<GetSupplierInvoiceListHandler>();
        services.AddScoped<GetPurchaseInvoiceListHandler>();
        services.AddScoped<GetPurchaseInvoiceDetailsHandler>();
        services.AddScoped<GetPurchaseInvoiceOperationsCenterHandler>();
        services.AddScoped<GetPurchaseOrderListHandler>();
        services.AddScoped<GetPurchaseOrderDetailsHandler>();
        services.AddScoped<GetPurchaseReturnListHandler>();
        services.AddScoped<GetPurchaseReturnDetailsHandler>();
        services.AddScoped<GetPostedPurchaseInvoicesForSupplierHandler>();
        services.AddScoped<GetDashboardSummaryHandler>();
        services.AddScoped<GetChinaContainerListHandler>();
        services.AddScoped<GetContainerOperationsCenterHandler>();
        services.AddScoped<ImportContainerExcelHandler>();
        services.AddScoped<ParseChinaInvoiceExcelHandler>();
        services.AddScoped<ParseChinaPackingSummaryExcelHandler>();
        services.AddScoped<GetWarehouseListHandler>();
        services.AddScoped<GetSalesInvoiceListHandler>();
        services.AddScoped<GetSalesInvoiceOperationsCenterHandler>();
        services.AddScoped<GetWarehouseDetailingQueueHandler>();
        services.AddScoped<GetSalesWarehouseStockHandler>();
        services.AddScoped<GetFabricRollSalesReservationsHandler>();
        services.AddScoped<GetDetailingCandidateRollsHandler>();
        services.AddScoped<CheckSalesInvoiceBelowCostHandler>();
        services.AddScoped<GetReportPreviewHandler>();
        services.AddScoped<GetModuleReportHandler>();
        services.AddScoped<AuthenticateUserHandler>();
    }
}
