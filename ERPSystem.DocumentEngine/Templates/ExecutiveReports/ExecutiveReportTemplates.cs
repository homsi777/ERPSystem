using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.ExecutiveReports;

/// <summary>Executive Dashboard Report — KPI cards, charts and summary tables.</summary>
public sealed class ExecutiveDashboardReportTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.ExecutiveDashboardReport;
}

/// <summary>Trial Balance — accounts with debit / credit columns.</summary>
public sealed class TrialBalanceTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.TrialBalance;
}

/// <summary>Balance Sheet — assets, liabilities and equity.</summary>
public sealed class BalanceSheetTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.BalanceSheet;
}

/// <summary>Income Statement — revenue, expenses and net result.</summary>
public sealed class IncomeStatementTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.IncomeStatement;
}

/// <summary>Cash Flow — operating, investing and financing activities.</summary>
public sealed class CashFlowTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.CashFlow;
}

/// <summary>General Ledger — account movement detail over a period.</summary>
public sealed class GeneralLedgerTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.GeneralLedger;
}

/// <summary>Journal Voucher — a single journal entry with balanced lines.</summary>
public sealed class JournalVoucherTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.JournalVoucher;
}
