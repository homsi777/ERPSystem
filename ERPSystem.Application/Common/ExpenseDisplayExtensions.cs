using ERPSystem.Domain.Enums;

namespace ERPSystem.Application.Common;

public static class ExpenseDisplayExtensions
{
    public static string ToArabic(this ExpenseCategoryKind kind) => kind switch
    {
        ExpenseCategoryKind.Capital => "مصاريف رأسمالية",
        ExpenseCategoryKind.Personal => "مصاريف شخصية مستمرة",
        ExpenseCategoryKind.Operating => "مصاريف تشغيلية",
        _ => kind.ToString()
    };

    public static string ToArabic(this ExpenseStatus status) => status switch
    {
        ExpenseStatus.Draft => "مسودة",
        ExpenseStatus.PendingApproval => "بانتظار الاعتماد",
        ExpenseStatus.Approved => "معتمد",
        ExpenseStatus.Scheduled => "مجدول",
        ExpenseStatus.PartiallyPaid => "مدفوع جزئياً",
        ExpenseStatus.Paid => "مدفوع",
        ExpenseStatus.Closed => "مغلق",
        ExpenseStatus.Cancelled => "ملغى",
        ExpenseStatus.Archived => "مؤرشف",
        _ => status.ToString()
    };

    public static string ToArabic(this ExpensePaymentMethod method) => method switch
    {
        ExpensePaymentMethod.Cash => "نقدي",
        ExpensePaymentMethod.BankTransfer => "تحويل بنكي",
        ExpensePaymentMethod.Card => "بطاقة",
        ExpensePaymentMethod.Cheque => "شيك",
        ExpensePaymentMethod.Other => "أخرى",
        _ => method.ToString()
    };

    public static string ToArabic(this ExpenseFundingSource source) => source switch
    {
        ExpenseFundingSource.Cash => "نقد",
        ExpenseFundingSource.Bank => "بنك",
        ExpenseFundingSource.Treasury => "خزينة",
        ExpenseFundingSource.Partner => "شريك",
        ExpenseFundingSource.Loan => "قرض",
        ExpenseFundingSource.CreditFacility => "تسهيل ائتماني",
        ExpenseFundingSource.InternalTransfer => "تحويل داخلي",
        _ => source.ToString()
    };

    public static string ToArabic(this ExpensePaymentStatus status) => status switch
    {
        ExpensePaymentStatus.Pending => "معلق",
        ExpensePaymentStatus.Scheduled => "مجدول",
        ExpensePaymentStatus.Completed => "مكتمل",
        ExpensePaymentStatus.Cancelled => "ملغى",
        ExpensePaymentStatus.Adjusted => "معدّل",
        _ => status.ToString()
    };

    public static string ToArabic(this ExpensePaymentApprovalStatus status) => status switch
    {
        ExpensePaymentApprovalStatus.Pending => "بانتظار الاعتماد",
        ExpensePaymentApprovalStatus.Approved => "معتمد",
        ExpensePaymentApprovalStatus.Rejected => "مرفوض",
        _ => status.ToString()
    };

    public static string ToArabic(this ExpenseInstallmentStatus status) => status switch
    {
        ExpenseInstallmentStatus.Pending => "معلق",
        ExpenseInstallmentStatus.Scheduled => "مجدول",
        ExpenseInstallmentStatus.Paid => "مدفوع",
        ExpenseInstallmentStatus.Overdue => "متأخر",
        ExpenseInstallmentStatus.Cancelled => "ملغى",
        _ => status.ToString()
    };

    public static string ToArabic(this ExpenseRecurrenceFrequency frequency) => frequency switch
    {
        ExpenseRecurrenceFrequency.None => "غير متكرر",
        ExpenseRecurrenceFrequency.Daily => "يومي",
        ExpenseRecurrenceFrequency.Weekly => "أسبوعي",
        ExpenseRecurrenceFrequency.Monthly => "شهري",
        ExpenseRecurrenceFrequency.Quarterly => "ربع سنوي",
        ExpenseRecurrenceFrequency.Yearly => "سنوي",
        ExpenseRecurrenceFrequency.Custom => "فترة مخصصة",
        _ => frequency.ToString()
    };

    public static string ToArabic(this CostCenterStatus status) => status switch
    {
        CostCenterStatus.Active => "نشط",
        CostCenterStatus.Inactive => "غير نشط",
        _ => status.ToString()
    };
}
