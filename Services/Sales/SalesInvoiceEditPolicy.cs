using ERPSystem.Domain.Enums;

namespace ERPSystem.Services.Sales;

/// <summary>
/// Rules for opening the sales invoice editor from list/context-menu/operations center.
/// Content edits (header/lines) remain draft-only at the domain layer.
/// </summary>
public static class SalesInvoiceEditPolicy
{
    /// <summary>
    /// Invoice statuses where the editor may be opened before final delivery
    /// (draft through printed — not yet delivered/cancelled/returned).
    /// </summary>
    public static bool CanOpenEditor(SalesInvoiceStatus status) =>
        status is SalesInvoiceStatus.Draft
            or SalesInvoiceStatus.AwaitingDetailing
            or SalesInvoiceStatus.Detailed
            or SalesInvoiceStatus.ReadyForApproval
            or SalesInvoiceStatus.Approved
            or SalesInvoiceStatus.Printed;

    public static bool CanEditContent(SalesInvoiceStatus status) =>
        status == SalesInvoiceStatus.Draft;
}
