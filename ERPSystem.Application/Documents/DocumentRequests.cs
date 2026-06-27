namespace ERPSystem.Application.Documents;

public sealed class PrintSalesInvoiceRequest
{
    public Guid InvoiceId { get; init; }
    public string TemplateCode { get; init; } = "SALES_INVOICE_DEFAULT";
    public bool IncludeRollDetails { get; init; } = true;
}

public sealed class PrintCustomerStatementRequest
{
    public Guid CustomerId { get; init; }
    public DateTime FromDate { get; init; }
    public DateTime ToDate { get; init; }
    public string TemplateCode { get; init; } = "CUSTOMER_STATEMENT_DEFAULT";
}

public sealed class ExportReportRequest
{
    public string ReportCode { get; init; } = "";
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public DateTime? FromDate { get; init; }
    public DateTime? ToDate { get; init; }
    public string Format { get; init; } = "PDF";
    public Dictionary<string, string> Parameters { get; init; } = new();
}

public sealed class PrintContainerLandingCostRequest
{
    public Guid ContainerId { get; init; }
    public string TemplateCode { get; init; } = "LANDING_COST_DEFAULT";
}

public sealed class PrintReceiptVoucherRequest
{
    public Guid VoucherId { get; init; }
    public string TemplateCode { get; init; } = "RECEIPT_VOUCHER_DEFAULT";
}
