# Sales Tax and Discount Posting Policy

**Effective:** Phase 2 Final Acceptance  
**Scope:** Sales invoice approval (`PostingKind.SalesInvoicePosting`)

## Policy name

**Hybrid contra-revenue line discount + net invoice discount**

This is a single documented policy with two discount *sources*, each with one treatment — not two interchangeable policies on the same amount.

| Discount source | GL treatment | Revenue impact |
|-----------------|--------------|----------------|
| **Line discount** (catalog price − applied price × meters) | `Sales Discounts` **Dr** (contra-revenue) | Revenue credit includes line discount (gross catalog presentation) |
| **Invoice discount** (`DiscountTotal` on header) | **No separate GL line** | Reduces taxable base and net revenue directly |

## Posting formulas

```
AR Dr                    = GrandTotal
Sales Discounts Dr       = TotalLineDiscount          (only if > 0)
    Sales Revenue Cr     = GrandTotal − TaxTotal + TotalLineDiscount
    VAT Payable Cr       = TaxTotal (grouped by snapshot SalesTaxAccountId)
```

Legacy untaxed invoices (`IsLegacyUntaxed = true`): posting unchanged from Phase 1 — no VAT line, `TaxTotal = 0`.

## Worked examples

### Example A — no discount, 15% exclusive

| Field | Amount |
|-------|-------:|
| Net merchandise | 1,000 |
| VAT | 150 |
| Grand total | 1,150 |

```
Dr  Accounts Receivable     1,150
    Cr  Sales Revenue       1,000
    Cr  VAT Payable           150
```

### Example B — invoice discount 100, 15% on net 900

| Field | Amount |
|-------|-------:|
| Gross lines | 1,000 |
| Invoice discount | 100 |
| Taxable | 900 |
| VAT | 135 |
| Grand total | 1,035 |

```
Dr  Accounts Receivable     1,035
    Cr  Sales Revenue         900
    Cr  VAT Payable           135
```

(No Sales Discounts line — invoice discount is net-revenue embedded.)

### Example C — line discount 100 (catalog 1,000, applied 900), tax on 900

| Field | Amount |
|-------|-------:|
| Line total (net) | 900 |
| Line discount (contra) | 100 |
| VAT 15% | 135 |
| Grand total | 1,035 |

```
Dr  Accounts Receivable     1,035
Dr  Sales Discounts           100
    Cr  Sales Revenue       1,000
    Cr  VAT Payable           135
```

Revenue credit = 1,035 − 135 + 100 = **1,000** (catalog gross).

## Balance proof

Debits = GrandTotal + LineDiscount  
Credits = (GrandTotal − TaxTotal + LineDiscount) + TaxTotal = GrandTotal + LineDiscount ✓

## Implementation

- Builder: `SalesInvoiceApprovalPostingBuilder` (Application layer)
- Consumer: `IntegratedAccountingService.PostSalesInvoiceApprovalAsync`
- Tax amounts: frozen invoice snapshots only — never recalculated at posting time
