using ERPSystem.Domain.Common;

namespace ERPSystem.Domain.Events.Sales;

public sealed record SalesInvoiceCreated(Guid InvoiceId, string InvoiceNumber) : DomainEvent;

public sealed record SalesInvoiceSentToWarehouse(Guid InvoiceId, string InvoiceNumber) : DomainEvent;

public sealed record SalesInvoiceDetailed(Guid InvoiceId, string InvoiceNumber, decimal GrandTotal) : DomainEvent;

public sealed record SalesInvoiceApproved(
    Guid InvoiceId,
    string InvoiceNumber,
    Guid CustomerId,
    decimal GrandTotal) : DomainEvent;

public sealed record SalesInvoicePrinted(Guid InvoiceId, string InvoiceNumber) : DomainEvent;
