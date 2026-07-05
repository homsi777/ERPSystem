using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.ExecutiveReports;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Services;

/// <summary>
/// Resolves the single template responsible for each <see cref="DocumentType"/>.
/// Registering a new document type is a one-line addition here — templates are
/// never duplicated and there is exactly one per type.
/// </summary>
public sealed class TemplateRegistry
{
    private readonly Dictionary<DocumentType, IDocumentTemplate> _templates = new();

    public TemplateRegistry()
    {
        RegisterDefaults();
    }

    public void Register(IDocumentTemplate template) => _templates[template.Type] = template;

    public IDocumentTemplate Resolve(DocumentType type)
    {
        if (_templates.TryGetValue(type, out var template))
        {
            return template;
        }

        // Fall back to a generic body so new types never crash the pipeline.
        return _fallback;
    }

    public bool Contains(DocumentType type) => _templates.ContainsKey(type);

    public IReadOnlyCollection<DocumentType> RegisteredTypes => _templates.Keys;

    private readonly IDocumentTemplate _fallback = new GenericTemplate();

    private void RegisterDefaults()
    {
        Register(new Templates.SalesInvoice.SalesInvoiceTemplate());
        Register(new Templates.PurchaseInvoice.PurchaseInvoiceTemplate());
        Register(new Templates.PurchaseInvoice.PurchaseOrderTemplate());
        Register(new Templates.Quotation.QuotationTemplate());
        Register(new Templates.CustomerStatement.CustomerStatementTemplate());
        Register(new Templates.SupplierStatement.SupplierStatementTemplate());
        Register(new Templates.ReceiptVoucher.ReceiptVoucherTemplate());
        Register(new Templates.PaymentVoucher.PaymentVoucherTemplate());
        Register(new Templates.ExpenseVoucher.ExpenseVoucherTemplate());
        Register(new Templates.InventoryTransfer.InventoryTransferTemplate());
        Register(new Templates.Stocktake.StocktakeTemplate());
        Register(new Templates.Stocktake.OpeningStockTemplate());
        Register(new Templates.InventoryReport.InventoryReportTemplate());
        Register(new Templates.ContainerReport.ContainerReportTemplate());
        Register(new Templates.PartnerStatement.PartnerStatementTemplate());
        Register(new ExecutiveDashboardReportTemplate());
        Register(new TrialBalanceTemplate());
        Register(new BalanceSheetTemplate());
        Register(new IncomeStatementTemplate());
        Register(new CashFlowTemplate());
        Register(new GeneralLedgerTemplate());
        Register(new JournalVoucherTemplate());
    }

    /// <summary>Generic body used when a type has no dedicated template yet.</summary>
    private sealed class GenericTemplate : BaseDocumentTemplate
    {
        public override DocumentType Type => DocumentType.SalesInvoice;
    }
}
