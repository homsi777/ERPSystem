# PHASE 3 COMPLETION REPORT — Cashboxes, Banks, Receipt Vouchers

**Date (UTC):** 2026-07-10  
**Production commit at start:** `caa3b87`  
**Phase 4 started:** **NO**

---

## Executive decision

```text
PHASE 3 CORE BACKEND: IMPLEMENTED (first increment)
PHASE 3 FINAL ACCEPTANCE: NOT YET PASSED
PHASE 4: NO-GO
```

**Reason:** Core posting stabilization, payment-method/tender model, cashbox GL resolution, reversal workflow, reconciliation services, and API extensions are implemented and build-clean. Full 28-scenario live matrix, isolated E2E company, WPF/React/PDF parity, VPS backup confirmation, and post-migration production baseline remain pending before `PHASE 3: PASSED`.

---

## 1. Backup verification

See `artifacts/phase3-prechange-backup-verification.md`.

| Item | Status |
|------|--------|
| Script | ✅ `tools/phase3-verification/backup-pre-phase3.sh` |
| VPS dump | ⏳ Pending — run with sudo on VPS |
| `pg_restore --list` | ⏳ Pending |

---

## 2. Pre baseline (production `11111111-…`)

| Metric | Value |
|--------|------:|
| AR GL | 320.00 |
| Stored customer balances | 320.00 |
| Operational inventory | 105,636.71 |
| Inventory GL | 15,622.43 |
| Posted receipts | 0 |
| Legacy duplicate journals | 1 / 2 |

Artifacts: `artifacts/phase3-prechange.json`, `.md`  
**No unexplained drift** — gate cleared for development.

---

## 3. Current-state analysis (before Phase 3)

| Operation | Entry point | Account used (before) | Problem |
|-----------|-------------|----------------------|---------|
| Post receipt voucher | `PostReceiptVoucherHandler` | `AccountingAccountIds.CashUsd` hardcoded | Ignored `Cashbox.AccountId` |
| Auto cash on invoice approve | `PostCashCollectionAsync` | Same `CashUsd` | Same |
| Payment voucher | `PostPaymentVoucherHandler` | `CashUsd` param | Payment path still uses static cash (out of receipt scope) |
| Cashbox transfer | `CashboxHandlers` | `from/to.AccountId ?? CashUsd` | Fallback only |
| Receipt create | WPF/API | N/A | `CompanyId`/`BranchId` not persisted on voucher |
| Reversal | N/A | N/A | Not implemented |
| Tender / payment method | N/A | N/A | Not modeled |

---

## 4. Target design implemented

### 4.1 Domain
- `PaymentMethodKind`, extended `ReceiptVoucher` (Submitted, Reversal metadata)
- `ReceiptTenderLine`, `BankAccount`, `PaymentMethod`
- Extended `Cashbox` (`CompanyId`, `AllowNegativeBalance`, `OpeningDate`)

### 4.2 Migration (additive)
`20260722120000_AddPhase3FinanceModule`:
- `finance.payment_methods` (seeded per company)
- `finance.bank_accounts`
- `finance.receipt_tender_lines`
- Receipt voucher reversal/idempotency columns
- Cashbox `CompanyId`, `AllowNegativeBalance`, `OpeningDate`
- `CustomerAdvances` GL account seed (`a1000014-…`)

### 4.3 Posting rules (new receipts)
- **No `CashUsd` in new receipt postings**
- Debit account resolved from tender → cashbox `AccountId` or bank `GlAccountId`
- `PostingKind.ReceiptVoucherCollection` / `ReceiptVoucherReversal`
- Unallocated amount → `CustomerAdvances` Cr (design hook; Phase 4 full advance engine deferred)
- Validation via `ICashboxPostingValidator` / `IBankAccountPostingValidator`

### 4.4 Services
- `IReceiptPostingService`, `ICashboxBalanceService`, `ICashboxReconciliationService`
- `IReceiptTenderResolver`

### 4.5 Handlers
- `CreateReceiptVoucherHandler` — tender line + company/branch + validation
- `ApproveReceiptVoucherHandler`
- `PostReceiptVoucherHandler` — GL from cashbox, no silent `CashUsd`
- `ReverseReceiptVoucherHandler` — reversal journal + status `Reversed`
- `ApproveSalesInvoiceHandler` — requires cashbox with linked GL for cash sales

### 4.6 API
- `GET /api/v1/finance/cashboxes`
- `GET /api/v1/finance/payment-methods`
- `GET /api/v1/finance/bank-accounts`
- `GET /api/v1/finance/cashboxes/reconciliation`
- `POST /api/v1/finance/receipts/{id}/approve`
- `POST /api/v1/finance/receipts/{id}/reverse`
- Existing `POST /api/v1/receipts` + `/post` updated via new handlers

---

## 5. Tests

| Suite | Result |
|-------|--------|
| `Phase3FinanceAcceptanceTests` | **8 passed, 0 failed** |
| Full 28-scenario live matrix | ⏳ Not yet implemented |
| E2E isolated company | ⏳ Not yet implemented |
| Concurrency (20 parallel) | ⏳ Not yet implemented |

---

## 6. Remaining for `PHASE 3: PASSED`

1. VPS backup + `pg_restore --list` confirmation  
2. Apply migration on staging/production after backup  
3. `Phase3FinanceE2ETestCompanySeeder` + live scenarios A–L  
4. Expand test matrix to 28 cases (live DB)  
5. WPF: cashbox account display, receipt reverse UX, reconciliation page  
6. React: finance receipt flows  
7. PDF receipt template updates  
8. `artifacts/phase3-cashbox-account-mapping-required.md`  
9. `artifacts/phase3-legacy-receipt-analysis.md`  
10. Post baseline + `artifacts/phase3-baseline-diff.md`  
11. `artifacts/phase3-receipt-cross-layer-proof.md`

---

## 7. Rollback

1. Restore: `pg_restore --clean --if-exists -d erp_pro …/erp_pro_pre_phase3_*.dump`
2. Revert migration / git commits
3. Redeploy prior build (`caa3b87`)

---

## 8. Final confirmation

```text
PHASE 3 FINAL ACCEPTANCE: NOT PASSED (core backend ready)
PHASE 4 STARTED: NO
```

**Next step for operator:** Run VPS backup script, then continue Phase 3 E2E gate in a follow-up session.
