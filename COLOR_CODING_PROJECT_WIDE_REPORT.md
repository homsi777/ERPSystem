# Color Coding Report — Debit/Credit + Sales Invoice Payment Type

**Date:** 2026-07-11  
**Scope:** UI styling only — zero query, calculation, or binding logic changes (except exposing `PaymentType` on list row VM for existing DTO field).

---

## Part A — Accounting Debit/Credit (مدين / دائن)

### A1 — Discovery list

| Screen | File | Fields | Scope |
|--------|------|--------|-------|
| دفتر اليومية | `Controls/Accounting/JournalEntryListPageControl.cs` | `DebitTotal`, `CreditTotal` columns | **IN SCOPE — modified** |
| تفاصيل القيد | `Controls/Accounting/Popups/JournalEntryDetailsPopupControl.cs` | KPI + line `Debit`/`Credit` | **IN SCOPE — modified** |
| بطاقة القيد | `Controls/Accounting/JournalEntryCardControl.cs` | Debit/Credit totals | **IN SCOPE — modified** |
| ميزان المراجعة | `Controls/Accounting/TrialBalanceReportControl.cs` | Debit/Credit/Balance | **IN SCOPE — modified** |
| كشف حساب GL | `Controls/Accounting/AccountLedgerReportControl.cs` | Debit/Credit/Running balance | **IN SCOPE — modified** |
| كشف حساب مورد | `Controls/Suppliers/SupplierAccountStatementControl.cs` | Debit/Credit/Balance | **IN SCOPE — modified** |
| أرصدة افتتاحية (عملاء) | `Controls/Customers/CustomerOpeningBalanceControl.cs` | TotalDebit/TotalCredit/NetBalance | **IN SCOPE — modified** |
| استيراد OB عملاء | `Controls/Customers/CustomerOpeningBalanceImportControl.cs` | Preview مدين/دائن | **IN SCOPE — modified** |
| نموذج OB | `Controls/Finance/OpeningBalanceFormControl.cs` | Lines grid مدين/دائن | **IN SCOPE — modified** |
| مركز عمليات OB | `Controls/Finance/OpeningBalanceOperationsCenterControl.cs` | Accounting + Journal tabs | **IN SCOPE — modified** |
| قيود GL (فاتورة بيع) | `Controls/Sales/SalesInvoiceOperationsCenterControl.cs` | Journal line Debit/Credit | **IN SCOPE — modified** |
| قيود (فاتورة شراء) | `Controls/Purchases/PurchaseInvoiceOperationsCenterControl.cs` | Journal tab | **IN SCOPE — modified** |
| قيد يومية يدوي | `Controls/Accounting/JournalEntryFormControl.cs` | TextBox inputs per line | **NEEDS CONFIRMATION** — custom line editors, not DataGrid cells |
| كشف حساب عميل | `Controls/Customers/CustomerAccountStatementControl.xaml` | Running balance only (no Dr/Cr columns) | **NEEDS CONFIRMATION** |
| حركات الصندوق | `Controls/Finance/CashboxOperationsCenterControl.cs` | In/Out direction, not GL Dr/Cr | **NEEDS CONFIRMATION** |
| أعمار الديون AR/AP | `Controls/Accounting/AgingListControls.cs` | Outstanding amounts only | **OUT OF SCOPE** |
| تقارير عامة (API columns) | `Controls/Reports/ModuleReportViewControl.cs` | Dynamic columns | **NEEDS CONFIRMATION** |
| لوحة التحكم AR/AP | `Modules/DashboardModule.xaml` | Metric cards | **OUT OF SCOPE** |

### A2 — Color tokens (reused existing theme)

Defined in `Resources/Themes/EnterpriseTheme.xaml`:

| Token | Source color | Use |
|-------|--------------|-----|
| `AccountingDebitTintBrush` | `SuccessBgColor` `#ECFDF5` (light green) | مدين / Cash row |
| `AccountingCreditTintBrush` | `DangerBgColor` `#FEF2F2` (light red/pink) | دائن / Credit row |

Helper: `Helpers/ErpAccountingColorHelper.cs` — `DataTrigger`-based cell/row styles (virtualization-safe).

**Rule:** Debit (مدين) → green tint. Credit (دائن) → red tint (per task spec, not prior Danger/Success text semantics).

### A3 — Screens modified (13)

1. Journal Book list  
2. Journal Entry details popup  
3. Journal Entry card  
4. Trial Balance report  
5. Account Ledger report  
6. Supplier Account Statement  
7. Customer Opening Balance list  
8. Customer OB import preview  
9. Opening Balance form lines grid  
10. Opening Balance operations center (Accounting + Journal tabs)  
11. Sales Invoice operations center (GL journal lines)  
12. Purchase Invoice operations center (Journal tab)  

**Cell vs row:** Journal/ledger/statement screens tint **individual مدين/دائن cells** when amount &gt; 0. Balance columns use **signed tint** (positive → green, negative → red).

---

## Part B — Sales Invoice List payment type

### B1 — Field identified

| Item | Value |
|------|-------|
| DTO | `SalesInvoiceDto.PaymentType` |
| Enum | `ERPSystem.Domain.Enums.PaymentType` |
| `Cash = 0` | **نقدي** → green row tint |
| `Credit = 1` | **آجل** → red row tint |

Exposed on list row: `Core/Sales/SalesInvoiceListRow.PaymentType` (mapped from existing DTO — **no API/query change**).

### B2 — Screen modified

| Screen | File | Styling |
|--------|------|---------|
| قائمة فواتير البيع | `Controls/Sales/SalesInvoiceListPageControl.cs` | `RowStyle = CreatePaymentTypeRowStyle()` |

### Conflict check — existing status colors

The invoice list **did not** have per-status row background colors before this change (only default hover/selection via `EnterpriseDataGridRowStyle`). **No conflict.** Hover and selection still override payment tint via later triggers (blue `PrimaryVeryLightBrush`).

---

## Verification

| Check | Result |
|-------|--------|
| Build | `dotnet build ERPSystem.csproj` ✅ |
| Logic/queries unchanged | ✅ UI-only styles + row `PaymentType` display field |
| DataGrid virtualization | Styles use `ElementStyle` / `RowStyle` `DataTrigger` — compatible with virtualization |
| Accounting baseline | Not required (no financial logic change) |

### Screenshots

Capture manually after `dotnet run` on production data:

1. **Accounting:** دفتر اليومية — row with both مدين and دائن columns tinted in different cells  
2. **Accounting:** تفاصيل قيد — green KPI/card for مدين, red for دائن  
3. **Reports:** ميزان المراجعة or كشف حساب  
4. **Suppliers:** كشف حساب مورد  
5. **Sales:** قائمة فواتير — one نقدي (green) + one آجل (red) row  

Suggested save path: `examples/color-coding/` (create when capturing).

---

## Files changed

- `Resources/Themes/EnterpriseTheme.xaml` — alias brushes  
- `Helpers/ErpAccountingColorHelper.cs` — **new** shared styling  
- `Core/Sales/SalesInvoiceListRow.cs` — `PaymentType` property  
- 12 control files listed above  

---

## For Nabil — screens needing confirmation

1. **كشف حساب عميل** — shows fabric ledger / running balance only; add signed balance tint if desired  
2. **قيد يومية يدوي** — tint debit/credit TextBoxes on manual entry form?  
3. **Cashbox movements** — not true GL Dr/Cr  
4. **Generic module reports** — only if API returns مدين/دائن column headers  
