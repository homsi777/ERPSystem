using ERPSystem.Application.DTOs.Accounting;
using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class AccountingDisplayExtensions
{
    public static string ToDisplay(this GlAccountType type) => type switch
    {
        GlAccountType.Asset => "أصول",
        GlAccountType.Liability => "خصوم",
        GlAccountType.Equity => "حقوق ملكية",
        GlAccountType.Revenue => "إيرادات",
        GlAccountType.Expense => "مصروفات",
        _ => type.ToString()
    };

    public static string ToDisplay(this JournalEntryStatus status) => status switch
    {
        JournalEntryStatus.Draft => "مسودة",
        JournalEntryStatus.Approved => "معتمد",
        JournalEntryStatus.Posted => "مرحّل",
        JournalEntryStatus.Reversed => "معكوس",
        JournalEntryStatus.Cancelled => "ملغى",
        _ => status.ToString()
    };

    public static string ToDisplay(this DocumentType? sourceType) => sourceType switch
    {
        DocumentType.SalesInvoice => "فاتورة مبيعات",
        DocumentType.ChinaContainer => "حاوية استيراد",
        DocumentType.JournalEntry => "قيد يومية",
        DocumentType.ReceiptVoucher => "سند قبض",
        DocumentType.PaymentVoucher => "سند صرف",
        DocumentType.ExpensePayment => "صرف مصروف",
        null => "—",
        _ => sourceType.Value.ToString()
    };

    public static string ToDisplay(this JournalBookType bookType) => bookType switch
    {
        JournalBookType.General => "يومية عامة",
        JournalBookType.Bank => "يومية بنك",
        JournalBookType.Sales => "يومية مبيعات",
        JournalBookType.Purchase => "يومية مشتريات",
        JournalBookType.Cash => "يومية نقدية",
        _ => bookType.ToString()
    };

    public static GlAccountType ParseAccountType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return GlAccountType.Asset;

        return value.Trim().ToLowerInvariant() switch
        {
            "asset" or "assets" or "أصول" => GlAccountType.Asset,
            "liability" or "liabilities" or "خصوم" => GlAccountType.Liability,
            "equity" or "حقوق ملكية" => GlAccountType.Equity,
            "revenue" or "إيرادات" => GlAccountType.Revenue,
            "expense" or "expenses" or "مصروفات" => GlAccountType.Expense,
            _ => Enum.TryParse<GlAccountType>(value, true, out var parsed) ? parsed : GlAccountType.Asset
        };
    }

    /// <summary>Odoo-style account category for chart grouping and type column.</summary>
    public static string ToOdooCategoryDisplay(this AccountListDto account)
    {
        if (account.AccountType == GlAccountType.Asset)
        {
            if (account.Code.StartsWith("101", StringComparison.Ordinal)
                || account.NameAr.Contains("بنك", StringComparison.OrdinalIgnoreCase)
                || account.NameAr.Contains("صندوق", StringComparison.OrdinalIgnoreCase)
                || account.NameAr.Contains("نقد", StringComparison.OrdinalIgnoreCase))
                return "البنك والنقد";
            return "أصول متداولة";
        }

        return account.AccountType switch
        {
            GlAccountType.Liability => "خصوم متداولة",
            GlAccountType.Equity => "حقوق ملكية",
            GlAccountType.Revenue => "الإيرادات",
            GlAccountType.Expense => "المصروفات",
            _ => account.AccountType.ToDisplay()
        };
    }

    public static readonly string[] OdooCategoryOrder =
    [
        "البنك والنقد",
        "أصول متداولة",
        "خصوم متداولة",
        "حقوق ملكية",
        "الإيرادات",
        "المصروفات"
    ];
}
