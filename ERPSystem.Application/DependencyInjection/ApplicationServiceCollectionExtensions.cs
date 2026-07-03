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
using ERPSystem.Application.Queries.Accounting;
using ERPSystem.Application.Queries.Capital;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.Queries.Expenses;
using ERPSystem.Application.Queries.Purchases;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Accounting;
using ERPSystem.Application.UseCases.Capital;
using ERPSystem.Application.UseCases.Containers;
using ERPSystem.Application.UseCases.Customers;
using ERPSystem.Application.UseCases.Expenses;
using ERPSystem.Application.UseCases.Finance;
using ERPSystem.Application.Abstractions.Repositories;
using ERPSystem.Application.DTOs.Reports;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Application.UseCases.Reports;
using ERPSystem.Application.UseCases.Sales;
using ERPSystem.Application.UseCases.Purchases;
using ERPSystem.Application.UseCases.Suppliers;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEvents.DomainEventDispatcher>();
        RegisterCustomerHandlers(services);
        RegisterSupplierHandlers(services);
        RegisterPurchaseHandlers(services);
        RegisterExpenseHandlers(services);
        RegisterCapitalHandlers(services);
        RegisterAccountingHandlers(services);
        RegisterFinanceHandlers(services);
        RegisterSalesHandlers(services);
        RegisterContainerHandlers(services);
        RegisterQueryHandlers(services);
        return services;
    }

    private static void RegisterCustomerHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateCustomerCommand, ApplicationResult<Guid>>, CreateCustomerHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomerCommand, ApplicationResult>, UpdateCustomerHandler>();
        services.AddScoped<ICommandHandler<DeactivateCustomerCommand, ApplicationResult>, DeactivateCustomerHandler>();
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
    }

    private static void RegisterFinanceHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateReceiptVoucherCommand, ApplicationResult<Guid>>, CreateReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<PostReceiptVoucherCommand, ApplicationResult>, PostReceiptVoucherHandler>();
        services.AddScoped<ICommandHandler<CreatePaymentVoucherCommand, ApplicationResult<Guid>>, CreatePaymentVoucherHandler>();
        services.AddScoped<ICommandHandler<PostPaymentVoucherCommand, ApplicationResult>, PostPaymentVoucherHandler>();
    }

    private static void RegisterSalesHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateSalesInvoiceDraftCommand, ApplicationResult<Guid>>, CreateSalesInvoiceDraftHandler>();
        services.AddScoped<ICommandHandler<UpdateSalesInvoiceDraftCommand, ApplicationResult>, UpdateSalesInvoiceDraftHandler>();
        services.AddScoped<ICommandHandler<SendSalesInvoiceToWarehouseCommand, ApplicationResult>, SendSalesInvoiceToWarehouseHandler>();
        services.AddScoped<ICommandHandler<CompleteWarehouseDetailingCommand, ApplicationResult>, CompleteWarehouseDetailingHandler>();
        services.AddScoped<ICommandHandler<ApproveSalesInvoiceCommand, ApplicationResult>, ApproveSalesInvoiceHandler>();
        services.AddScoped<ICommandHandler<CancelSalesInvoiceCommand, ApplicationResult>, CancelSalesInvoiceHandler>();
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

    private static void RegisterQueryHandlers(IServiceCollection services)
    {
        services.AddScoped<GetCustomerListHandler>();
        services.AddScoped<GetCustomerDetailsHandler>();
        services.AddScoped<GetCustomerOperationsCenterHandler>();
        services.AddScoped<GetCustomerStatementHandler>();
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
        services.AddScoped<GetReportPreviewHandler>();
        services.AddScoped<GetModuleReportHandler>();
    }
}
