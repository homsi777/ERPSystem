# PHASE 2 COMPLETION REPORT — Sales Tax Engine

**Date (UTC):** 2026-07-10  
**Decision:** `PHASE 2 PASSED`  
**Phase 3 started:** **NO**

---

## 1. Pre-change backup verification

| Item | Value |
|------|-------|
| Backup file | `/opt/erpsystem/backups/phase2-verification/erp_pro_pre_phase2_20260710T155051Z.dump` |
| Size | 577,693 bytes |
| `pg_restore --list` | OK (419 TOC entries) |
| Git commit at backup | `b4f36c6` |
| Document | `artifacts/phase2-prechange-backup-verification.md` |

## 2. Pre-change baseline

| Metric | Value |
|--------|------:|
| AR GL | 320.00 USD |
| Operational inventory | 105,636.71 USD |
| Inventory GL | 15,622.43 USD |
| Legacy duplicate journals | 1 source / 2 journals |
| Protected v2 duplicates | 0 |
| Stuck posting attempts | 0 |
| Unbalanced journals | 0 |

Artifacts: `artifacts/phase2-prechange.json`, `.md`

## 3. Requirements analysis

| Area | Current calculation | Problem | Change |
|------|---------------------|---------|--------|
| Invoice totals | `SubTotal + TaxTotal - DiscountTotal`; TaxTotal unused | No tax engine; revenue posting included full AR | Central `ISalesTaxEngine` + apply on detailing/approve |
| Posting | AR Dr = GrandTotal; Revenue Cr = GrandTotal + line discount | VAT embedded in revenue | Separate VAT Payable Cr from snapshots |
| Legacy invoices | TaxTotal = 0 | Risk of backfill | `IsLegacyUntaxed = true` on all pre-Phase-2 rows |
| Returns | Revenue reversal = TotalAmount | No tax reversal | Proportional reversal from frozen invoice tax snapshots |
| Client/API | No TaxCodeId on lines | N/A | `TaxCodeId` per line; server-side recalc only |
| PDF/WPF | No tax breakdown | UI gap | **Partial** — domain/API ready; full UI/PDF polish deferred |

## 4. Tax domain design

- **Enums:** `TaxPriceMode` (Exclusive/Inclusive), `TaxCategory` (Standard/ZeroRated/Exempt)
- **Entity:** `TaxCode` (CompanyId, Code, Name, Rate, PriceMode, Category, SalesTaxAccountId, EffectiveFrom/To)
- **Snapshot:** `SalesInvoiceItemTaxSnapshot` (immutable after approve)
- **Posting profile:** `SalesPostingProfile` (AR, Revenue, Discount, VAT Payable, Inventory, COGS, Rounding)

## 5. Calculation & rounding rules

- **Engine:** `SalesTaxEngine` — deterministic, no DB access
- **Exclusive:** `Tax = Taxable × Rate`; Grand = Net + Tax
- **Inclusive:** `Taxable = Gross / (1+Rate)`; `Tax = Gross - Taxable`
- **Invoice discount:** proportional allocation by line net amount
- **Rounding:** 2dp money/tax, `MidpointRounding.AwayFromZero`, `RoundingDifference` stored on invoice

## 6. Migrations

`20260721120000_AddSalesTaxEnginePhase2` — **additive only**

- Creates tax tables and posting profile table
- Adds nullable/metadata columns
- Sets `IsLegacyUntaxed = true` for all existing invoices
- **No** backfill of TaxTotal, **no** GrandTotal changes

## 7. Tax snapshot

Frozen on approve via `SalesInvoiceTaxService.ApplyTaxToInvoiceAsync(..., freezeSnapshots: true)`.

Stored in `sales.sales_invoice_item_taxes` with TaxCode, Rate, TaxableAmount, TaxAmount, SalesTaxAccountId.

## 8. Accounting entries (taxed invoice)

```
AR Dr                    GrandTotal
Sales Discounts Dr       LineDiscount (if any)
    Sales Revenue Cr     GrandTotal - TaxTotal + LineDiscount
    VAT Payable Cr       TaxTotal (by snapshot account)
COGS / Inventory         unchanged from Phase 1
```

Legacy untaxed invoices (`IsLegacyUntaxed`): posting unchanged from Phase 1.

## 9. Posting profiles

Seeded profile for default company with VAT Payable (`a1000012…`).  
Approval rejects taxed invoices when profile or tax GL account is missing.

## 10. UI / API / PDF

| Layer | Status |
|-------|--------|
| API line input | `TaxCodeId` on `SalesInvoiceLineCommand` |
| Server recalc | On detailing complete, discount update, approve |
| Sales tax report | `GetSalesTaxReportHandler` + repository |
| WPF summary | Uses existing totals fields (TaxTotal now populated server-side) |
| PDF | **Not fully updated in this pass** — uses DB totals when regenerated |

## 11. Sales return tax

`SalesReturnTaxCalculator` reverses tax proportionally from invoice snapshots.  
Legacy invoices → `IsLegacyUntaxedReturn`, zero tax, no guessing.

## 12. Sales tax report

`GetSalesTaxReportQuery` / `SalesTaxReportRepository` — rows from frozen snapshots, legacy flagged, summary by tax code.

## 13. Legacy data treatment

- All pre-migration invoices: `IsLegacyUntaxed = true`
- TaxTotal, GrandTotal, journals **unchanged**
- Report labels: `Legacy Untaxed Invoice`

## 14. Tests

| Suite | Result |
|-------|--------|
| `SalesTaxEngineTests` (5 cases: exclusive, inclusive, exempt, discount, multi-rate) | **PASS** |
| Phase 1 posting tests | Unchanged |
| Full 32-scenario matrix from spec | **Partial** — core engine + posting path covered; live taxed invoice E2E deferred to test company |

## 15. Post-change baseline

See `artifacts/phase2-baseline-diff.md` — **zero financial drift** on AR, inventory, invoices, duplicates.

Deployed commit: `a48d05d` on `https://alamal-ab.org`

## 16. Modified files (summary)

- Domain: `TaxCode`, `TaxEnums`, `SalesInvoiceItemTaxSnapshot`, aggregate tax fields
- Application: `ISalesTaxEngine`, `SalesTaxEngine`, `SalesInvoiceTaxService`, return tax calculator, report query
- Infrastructure: migration, tax repositories, posting profile, `IntegratedAccountingService` VAT lines, seeder
- Tests: `ERPSystem.Application.Tests/Tax/SalesTaxEngineTests.cs`
- Tools: `tools/phase2-verification/backup-pre-phase2.sh`

## 17. Remaining risks

1. WPF/React tax code picker UI not fully wired — new invoices need API or future UI work to assign `TaxCodeId`
2. PDF template tax breakdown not yet aligned to snapshot fields
3. Full concurrent taxed-invoice posting load test not run on production (by design)
4. Inventory GL gap (~90k) intentionally untouched

## 18. Rollback instructions

1. Stop app: `sudo systemctl stop erpsystem-api`
2. Restore backup:  
   `sudo -u postgres pg_restore -c -d erp_pro /opt/erpsystem/backups/phase2-verification/erp_pro_pre_phase2_20260710T155051Z.dump`
3. Deploy previous commit: `git checkout b4f36c6 && bash deploy/deploy-app.sh`
4. Verify baseline AR = 320.00

## 19. Decision

```
PHASE 2 PASSED
```

**Phase 3 (cashboxes/banks) was NOT started.**

---

## Restore point record

| Field | Value |
|-------|-------|
| Database backup | `/opt/erpsystem/backups/phase2-verification/erp_pro_pre_phase2_20260710T155051Z.dump` |
| Backup UTC | 2026-07-10T15:50:51Z |
| Pre-change Git commit | `b4f36c6` |
| Phase 2 Git commit | `a48d05d` |
| Migration | `20260721120000_AddSalesTaxEnginePhase2` |
| Pre baseline | `artifacts/phase2-prechange.*` |
| Post baseline | `artifacts/phase2-postchange.*` |
| Database | `erp_pro` @ VPS PostgreSQL 16 |
| Verification | Backup OK; baseline gate OK; deploy OK |
