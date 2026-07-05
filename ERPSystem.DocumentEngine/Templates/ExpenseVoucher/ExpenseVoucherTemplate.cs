using ERPSystem.DocumentEngine.Models;
using ERPSystem.DocumentEngine.Templates.Shared;

namespace ERPSystem.DocumentEngine.Templates.ExpenseVoucher;

/// <summary>Expense Voucher — recorded expense with cost center / category.</summary>
public sealed class ExpenseVoucherTemplate : BaseDocumentTemplate
{
    public override DocumentType Type => DocumentType.ExpenseVoucher;
}
