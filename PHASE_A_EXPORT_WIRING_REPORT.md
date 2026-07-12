# Phase A — WPF Export/Print Wiring Report

**Date (UTC):** 2026-07-12  
**Company baseline:** `11111111-1111-1111-1111-111111111111` (الأمل.AB — تجارة أقمشة الجinz)  
**Scope:** WPF wiring only — no API/Web, no generator layout/calculation changes (except Delivery Note extraction preserving identical fields).

---

## Mandatory safety steps

| Step | Result |
|------|--------|
| Production DB backup + `pg_restore --list` | **BLOCKED** — SSH `sudo` requires interactive password on VPS (`ubuntu@65.21.136.217:2727`). Nabil must run backup manually (see deploy note below). |
| Baseline **before** changes | `artifacts/phase-a-baseline-before.json` |
| Baseline **after** all items | `artifacts/phase-a-baseline-after.json` |

### Accounting baseline diff (required trio)

| Metric | Before | After | Match |
|--------|-------:|------:|:-----:|
| AR GL (`accountsReceivableGlBalance`) | 320.00 | 320.00 | **PASS** |
| Operational Inventory (`inventoryOperationalValue`) | 104,968.412982 | 104,968.412982 | **PASS** |
| Inventory GL (`inventoryAssetGlBalance`) | 15,598.92 | 15,598.92 | **PASS** |

All items: **baseline diff PASS** (UI-only changes; no posting/calculation touched).

---

## Item 1 — PurchasePrint / PurchaseExportPdf

### What was broken
- `EntityActionRegistry` registered `PurchasePrint` / `PurchaseExportPdf` for `EntityType.PurchaseInvoice`.
- `RowContextMenuService` routed unmatched actions to `WorkspaceWindowManager` → `ActionWorkspaceView` placeholder (no PDF).
- `PurchaseDocumentService` + `PurchaseInvoicePdfGenerator` worked only via OC quick action `preview:PurchaseInvoice`.

### What changed
| File | Change |
|------|--------|
| `Services/Purchases/PurchaseUiService.cs` | `PurchaseActionRouter.TryHandle`, `PrintAsync`, `TryHandleQuickAction` → `PurchaseDocumentService.ShowInvoicePreview(..., exportPdf)` |
| `Services/RowContextMenuService.cs` | Route purchase list context menu through `PurchaseActionRouter` before workspace fallback |
| `Services/MockQuickActionRouter.cs` | Use `PurchaseActionRouter.PrintAsync` for `preview:PurchaseInvoice` |
| `Controls/OperationsCenter/OperationsCenterShell.cs` | `purchase:pdf` quick action via `PurchaseActionRouter` |
| `Controls/Purchases/PurchaseInvoiceOperationsCenterControl.cs` | Added «تصدير PDF» quick action + Print tab |

### Pattern
Mirrors `SalesActionRouter` / `SalesPopupService.PrintAsync`: `PurchasePrint` → preview (`exportPdf: false`), `PurchaseExportPdf` → save PDF (`exportPdf: true`).

### Build / baseline
- Build: **0 errors, 0 warnings**
- Baseline diff: **PASS**

---

## Item 2 — Operations Center Print Preview (mock → real)

### What was broken
- `OperationsCenterFactory.PrintPreview()` called `ErpUxFactory.ExportBar()` with no callback → `MockInteractionService.ShowDocumentPreview`.

### What changed
| File | Change |
|------|--------|
| `Views/OperationsCenters/OperationsCenterPrintPreviewFactory.cs` | **New** — entity-aware export bar wired to existing document services |
| `Views/OperationsCenters/OperationsCenterFactory.cs` | `PrintPreview(context)` delegates to factory; Print tab on Journal/Fabric/Warehouse/Employee shells |
| `Controls/Sales/SalesInvoiceOperationsCenterControl.cs` | Print tab |
| `Controls/Purchases/PurchaseInvoiceOperationsCenterControl.cs` | Print tab |
| `Controls/Expenses/ExpenseOperationsCenterControl.cs` | Print tab |

### Entity types — Print Preview status

| Entity | Real Print/PDF | Notes |
|--------|:--------------:|-------|
| Sales Invoice | ✅ | `SalesPopupService` → `SalesDocumentService` |
| Purchase Invoice | ✅ | `PurchaseActionRouter` → `PurchaseDocumentService` |
| Journal Entry (`JournalEntryListDto`) | ✅ | `AccountingJournalDocumentService` |
| Warehouse (`WarehouseListExtendedDto`) | ✅ | `WarehouseDocumentService`; Excel via `InventoryExportService` |
| Expense | ✅ | `ExpenseDocumentService` |
| Capital Partner | ✅ | `CapitalPartnerDocumentService` |
| Customer | ⏳ | Banner: use Statement tab (Phase B/C) |
| Supplier | ⏳ | Banner: use Statement tab (Phase B/C) |
| Import Container | ⏳ | Quick action «طباعة» only; dedicated tab Phase B/C |
| Fabric Item | ⏳ | Not yet available |
| Employee | ⏳ | Not yet available |
| Cashbox | ⏳ | Not yet available |
| Journal (`JournalEntryModel` without Id) | ⏳ | Legacy OC shell — open from journal list for real PDF |

### Build / baseline
- Build: **0 errors, 0 warnings**
- Baseline diff: **PASS**

---

## Item 3 — Delivery Note generator

### What was broken
- Inline QuestPDF in `SalesDocumentService` (Arial, no `FinanceDocumentTheme`).
- Duplicated `ShowPreviewShell` instead of `PdfPreviewWindow`.

### What changed
| File | Change |
|------|--------|
| `ERPSystem.Application/Documents/DeliveryNotePdfGenerator.cs` | **New** — same columns/values as legacy inline renderer; Noto + Navy/Gold theme |
| `Services/Sales/SalesDocumentService.cs` | Uses generator + `PdfPreviewWindow`; removed ~200 lines legacy preview shell |

### Build / baseline
- Build: **0 errors, 0 warnings**
- Baseline diff: **PASS**

---

## Item 4 — Receipt / Payment voucher PDF button

### What was broken
- `ReceiptVoucherPageControl` / `PaymentVoucherPageControl` had «طباعة» only (`exportPdf: false`).

### What changed
| File | Change |
|------|--------|
| `Controls/Accounting/ReceiptVoucherPageControl.cs` | Added «PDF» button → `ReceiptVoucherDocumentService.ShowVoucherPreview(..., exportPdf: true)` |
| `Controls/Accounting/PaymentVoucherPageControl.cs` | Same for payment vouchers |

Reuses existing `ReceiptVoucherPdfGenerator` / `PaymentVoucherPdfGenerator` — no new generators.

### Build / baseline
- Build: **0 errors, 0 warnings**
- Baseline diff: **PASS**

---

## Item 5 — Legacy StatementDocumentService

### Finding
- **Zero live callers** in codebase (customers → `CustomerStatementDocumentService`; suppliers → `SupplierStatementDocumentService`).

### Action
- Marked `[Obsolete]` with deprecation header in `Services/Documents/StatementDocumentService.cs` — retained for reference; removal in Phase B after final audit.

### Build / baseline
- Build: **0 errors, 0 warnings**
- Baseline diff: **PASS**

---

## Build summary

```
dotnet build ERPSystem.csproj
Build succeeded — 0 Warning(s), 0 Error(s)
```

---

## Deployment status

| Step | Status |
|------|--------|
| Git commit + push | **Done** — `e031c1f` on `main` |
| VPS `deploy-app.sh` | **BLOCKED** (sudo password) — Nabil must run manually |

**Manual deploy (Nabil):**
```bash
ssh -i C:/Users/Homsi/.ssh/alamal_ab_tunnel -p 2727 ubuntu@65.21.136.217
sudo bash /opt/erpsystem/src/deploy/deploy-app.sh
```

**Manual backup (recommended before deploy):**
```bash
sudo bash -c 'BACKUP_DIR=/opt/erpsystem/backups/phase-a-export; mkdir -p $BACKUP_DIR; TS=$(date -u +%Y%m%dT%H%M%SZ); sudo -u postgres pg_dump -d erp_pro -F c -f ${BACKUP_DIR}/erp_pro_phase_a_${TS}.dump; pg_restore --list ${BACKUP_DIR}/erp_pro_phase_a_${TS}.dump | head -5'
```

Production: https://alamal-ab.org

---

## Manual testing handoff

**No app testing was performed — awaiting Nabil's manual test session.**

Suggested smoke tests:
1. Purchases list → right-click → طباعة / PDF on an approved invoice.
2. Sales/Purchase OC → tab «معاينة الطباعة» → طباعة / PDF.
3. Sales delivery popup → delivery note preview (new theme).
4. Accounting → Receipt/Payment voucher → save draft → طباعة + PDF buttons.

---

*Result-neutral: no financial, accounting, or inventory calculations were modified.*
