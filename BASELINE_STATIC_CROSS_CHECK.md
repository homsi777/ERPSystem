# Baseline Static Cross-Check

## Scope

This investigation read existing JSON, text, Markdown, and log-like files only. No executable, application, SSH client, PostgreSQL utility, or database service was run; no database connection was attempted.

## 1. Complete baseline artifact content

### `wpf-performance-prechange-baseline-accounting.json`

The top level contains exactly two objects: `baseline` and `health`.

#### `baseline`

- `generatedAtUtc`: `2026-07-11T12:39:26.4567995Z`
- `companyId`: `e2e00001-0001-0001-0001-000000000001`
- `companyName`: `شركة اختبار ضريبة E2E`

All fields in `summary`:

| Field | Value |
|---|---:|
| `totalInvoices` | 83 |
| `approvedInvoicesGrandTotal` | 72,670.00 |
| `approvedInvoiceCount` | 68 |
| `postedReceiptsTotal` | 0 |
| `postedReceiptCount` | 0 |
| `totalAllocationsAmount` | 0 |
| `allocationCount` | 0 |
| `postedAllocationsAmount` | 0 |
| `storedCustomerBalancesTotal` | 65,425.00 |
| `customerCount` | 1 |
| `accountsReceivableGlBalance` | 0 |
| `operationalCashboxBalancesTotal` | 0 |
| `cashUsdGlBalance` | 0 |
| `linkedCashboxGlBalancesTotal` | 0 |
| `inventoryOperationalValue` | 12,000.00 |
| `inventoryAssetGlBalance` | 0 |
| `costOfGoodsSoldGlTotal` | 0.0 |
| `postedSalesReturnsTotal` | 6,300.00 |
| `issueCount` | 1 |

All five `invoiceCountsByStatus` rows:

| `statusName` | `statusValue` | `count` | `grandTotalSum` |
|---|---:|---:|---:|
| Draft | 0 | 1 | 0.00 |
| Detailed | 2 | 14 | 16,100.00 |
| Approved | 4 | 54 | 60,595.00 |
| PartiallyReturned | 8 | 7 | 8,050.00 |
| Returned | 9 | 7 | 4,025.00 |

All remaining collection fields and their contents:

- `invoicesWithNegativeOpenAmount`: empty (`0` rows).
- `invoicesOverAllocated`: empty (`0` rows).
- `receiptsOverAllocated`: empty (`0` rows).
- `duplicateJournalEntries`: empty (`0` rows).
- `unbalancedJournalEntries`: empty (`0` rows).
- `journalEntriesWithoutSource`: empty (`0` rows).
- `orphanAllocations`: empty (`0` rows).
- `returnsWithoutCostTrace`: empty (`0` rows).
- `customerBalanceDifferences`: one row — `customerId=e2e00004-0004-0004-0004-000000000004`, `customerCode=E2E-CUST`, `customerName=عميل اختبار ضريبة`, `storedBalance=65,425.00`, `subledgerBalance=0`, `difference=65,425.00`.
- `cashboxBalanceDifferences`: two rows:
  - `cashboxId=5fad64db-7a08-4d9c-a16d-3588c9c45787`, `cashboxCode=CASH-MAIN-000001`, `cashboxName=صندوق مبيعات`, `operationalBalance=0.00`, no `glBalance` or `difference` field in the serialized row, `notes=No GL AccountId linked — compare manually with CashUsd aggregate.`
  - `cashboxId=66666666-6666-6666-6666-666666666666`, `cashboxCode=CASH-MAIN`, `cashboxName=Main Cashbox`, `operationalBalance=0.00`, no `glBalance` or `difference` field in the serialized row, with the same note.

The embedded `health` object is identical to the standalone health artifact described below.

### `wpf-performance-prechange-baseline-accounting-health.json`

- `generatedAtUtc`: `2026-07-11T12:39:34.1694577Z`
- `companyId`: `e2e00001-0001-0001-0001-000000000001`
- `companyName`: `شركة اختبار ضريبة E2E`
- `passCount`: 14
- `failCount`: 3
- `criticalFailCount`: 1
- `checks`: 17 rows. Every row contains `checkId`, `title`, numeric `severity`, numeric `status`, `issueCount`, `message`, and `sampleDetails`.

All check rows:

| `checkId` | `title` | Sev. | Status | Issues | Message / samples |
|---|---|---:|---:|---:|---|
| `duplicate_journal_entries` | قيود محاسبية مكررة Legacy (SourceType + SourceId — ما قبل PostingKind) | 2 | 0 | 0 | No issues detected; samples empty |
| `unbalanced_journal_entries` | قيود غير متوازنة | 2 | 0 | 0 | No issues detected; samples empty |
| `journal_entries_without_source` | قيود آلية بلا مصدر (SourceType/SourceId) | 1 | 0 | 0 | No issues detected; samples empty |
| `invoices_negative_open_amount` | فواتير ذات متبقي سالب | 2 | 0 | 0 | No issues detected; samples empty |
| `invoices_over_allocated` | فواتير تخصيصاتها أكبر من إجماليها | 2 | 0 | 0 | No issues detected; samples empty |
| `receipts_over_allocated` | سندات قبض تخصيصاتها أكبر من قيمتها | 2 | 0 | 0 | No issues detected; samples empty |
| `orphan_allocations` | تخصيصات دون فاتورة أو سند صالح | 2 | 0 | 0 | No issues detected; samples empty |
| `customer_balance_mismatch` | عملاء رصيدهم المخزّن ≠ أستاذ AR | 2 | 1 | 1 | Detected 1 issue; `E2E-CUST ... stored 65,425.00 vs GL 0.00 (Δ 65,425.00)` |
| `cashbox_gl_mismatch` | صناديق مختلفة عن حسابات GL المرتبطة | 2 | 0 | 0 | No issues detected; samples empty |
| `inventory_gl_mismatch` | قيمة المخزون التشغيلية ≠ حساب المخزون في GL | 1 | 1 | 1 | Detected 1 issue; `Operational 12,000.00 USD vs GL 0.00 USD` |
| `ar_control_vs_stored_customers` | مجموع أرصدة العملاء المخزنة ≠ حساب AR في GL | 1 | 1 | 1 | Detected 1 issue; `Stored customers 65,425.00 vs AR GL 0.00` |
| `returns_without_cost_trace` | مرتجعات دون أثر تكلفة/حركة مخزون واضح | 1 | 0 | 0 | No issues detected; samples empty |
| `duplicate_protected_posting_identities` | تكرار هوية ترحيل محمية (v2: Company+Source+PostingKind) | 2 | 0 | 0 | No issues detected; samples empty |
| `legacy_critical_duplicate_evidence` | دليل Legacy Critical — تكرار تاريخي محفوظ (Phase 2) | 2 | 0 | 0 | No issues detected; samples empty |
| `stuck_posting_attempts` | محاولات ترحيل عالقة في حالة Posting | 2 | 0 | 0 | No issues detected; samples empty |
| `failed_posting_attempts` | محاولات ترحيل فاشلة | 1 | 0 | 0 | No issues detected; samples empty |
| `journal_entries_v2_without_posting_kind` | قيود v2 بدون PostingKind | 2 | 0 | 0 | No issues detected; samples empty |

The JSON files contain no host, port, database name, physical table names, SQL text, global table row counts, or general transaction-row counts beyond the company-scoped metrics listed above.

## 2. Comparison with documented production scale

`WPF_PERFORMANCE_DIAGNOSTIC_REPORT.md` states that the measured production `erp_pro` had **4 sales invoices** and that most tables were near-empty. The baseline instead reports **83 invoices for an explicitly named E2E tax-test company**, including 68 approved, 14 detailed, 7 partially returned, and 7 returned invoices. The status counts sum exactly to 83.

This is not compatible with the documented four-invoice production slice. The deterministic E2E identifiers (`e2e00001...`, `e2e00004...`), test names, single customer, round operational inventory value `12,000.00`, zero GL balances, zero receipts/allocations, and 83 highly structured invoice records are collectively characteristic of a seeded/test dataset or test-company slice.

Static evidence does **not** distinguish between (a) a different local/seeded database and (b) an E2E company accidentally selected inside the production database. It does show that the captured company dataset is different from the live production company/scale used for the WPF measurements.

## 3. `_perf_audit.txt` side-by-side evidence

The file is present. Its header says `database 'erp_pro' — 2026-07-07 23:06`. Its table numbers are labeled `est_rows`, so they are PostgreSQL statistics estimates rather than guaranteed exact `COUNT(*)` values.

Most relevant scale entries are:

| `_perf_audit.txt` table | Estimated rows | Baseline comparison |
|---|---:|---|
| `company.companies` | 1 | Baseline reports one selected E2E company, but does not expose global company count. |
| `sales.sales_invoices` | 0 | Diagnostic report separately records 4 actual sales invoices; baseline reports 83. |
| `sales.sales_invoice_items` | 0 | Baseline does not expose item-row count. |
| `sales.sales_returns` | 0 | Baseline exposes return amount/status counts, not physical return-row count. |
| `sales.sales_return_lines` | 0 | No directly comparable baseline count. |
| `parties.customers` | 0 | Baseline reports `customerCount=1`. |
| `finance.receipt_vouchers` | 0 | Baseline reports `postedReceiptCount=0`. |
| `sales.receipt_invoice_payments` | 0 | Baseline reports `allocationCount=0`. |
| `accounting.journal_entries` | 2 | Baseline gives GL totals and issue arrays, not total journal-entry count. |
| `accounting.journal_entry_lines` | 4 | No directly comparable baseline count. |
| `finance.cashboxes` | 1 | Baseline has two cashbox-difference detail rows; that list is not declared to be a total cashbox count. |
| `inventory.warehouses` | 1 | Baseline does not expose warehouse count. |
| `inventory.warehouse_stocks` | 8 | Baseline exposes value only (`12,000.00`), not stock-row count. |
| `inventory.inventory_valuation_snapshots` | 8 | No directly comparable baseline count. |
| `inventory.stock_movement_lines` | 8 | No directly comparable baseline count. |
| `catalog.fabric_items` | 5 | No directly comparable baseline count. |
| `public.FabricRolls` | 509 | No directly comparable baseline count. |
| `china_import.container_items` | 509 | No directly comparable baseline count. |
| `audit.audit_logs` | 5,391 | No directly comparable baseline count. |

Thus the strongest direct comparison is 83 baseline invoices versus the report's 4 production invoices; the `_perf_audit.txt` estimates independently depict a very small operational/accounting dataset.

## 4. Baseline runtime logs

No `.log`, `.txt`, `.out`, or `.err` file with a last-write time in the 20-minute window around `2026-07-11T12:39:47Z` was found. A content search of existing log-like files found no redirected baseline console output (`Generating accounting baseline`, `Baseline JSON`, `Issues found`, `Health fails`, or the baseline artifact prefix) and no runtime line stating the baseline connection host/database. `_perf_audit.txt` identifies its own audited database but predates the baseline and is not the baseline tool's console log.

## Revised confidence statement

**Consistent with a different/seeded dataset.** Confidence is high that the baseline captured an E2E seeded/test-company dataset inconsistent with the four-invoice production company used for Phase A WPF measurements. Static evidence alone cannot determine whether that dataset lived in a separate local database or was an E2E company selected within `erp_pro`; no claim about the runtime host is made.
