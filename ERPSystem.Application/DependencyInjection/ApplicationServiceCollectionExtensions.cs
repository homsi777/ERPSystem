using ERPSystem.Application.Abstractions;
using ERPSystem.Application.Commands.Customers;
using ERPSystem.Application.Commands.Sales;
using ERPSystem.Application.Queries.Customers;
using ERPSystem.Application.Queries.Dashboard;
using ERPSystem.Application.Results;
using ERPSystem.Application.UseCases.Customers;
using ERPSystem.Application.UseCases.Queries;
using ERPSystem.Application.UseCases.Sales;
using Microsoft.Extensions.DependencyInjection;

namespace ERPSystem.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        RegisterCustomerHandlers(services);
        RegisterSalesHandlers(services);
        RegisterQueryHandlers(services);
        return services;
    }

    private static void RegisterCustomerHandlers(IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<CreateCustomerCommand, ApplicationResult<Guid>>, CreateCustomerHandler>();
        services.AddScoped<ICommandHandler<UpdateCustomerCommand, ApplicationResult>, UpdateCustomerHandler>();
        services.AddScoped<ICommandHandler<DeactivateCustomerCommand, ApplicationResult>, DeactivateCustomerHandler>();
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

    private static void RegisterQueryHandlers(IServiceCollection services)
    {
        services.AddScoped<GetCustomerListHandler>();
        services.AddScoped<GetCustomerDetailsHandler>();
        services.AddScoped<GetCustomerOperationsCenterHandler>();
        services.AddScoped<GetCustomerStatementHandler>();
        services.AddScoped<GetDashboardSummaryHandler>();
        services.AddScoped<GetChinaContainerListHandler>();
        services.AddScoped<GetWarehouseListHandler>();
        services.AddScoped<GetSalesInvoiceListHandler>();
        services.AddScoped<GetSalesInvoiceOperationsCenterHandler>();
        services.AddScoped<GetWarehouseDetailingQueueHandler>();
    }
}
