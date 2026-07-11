# Operations Center + Startup Seed Code Fix Report

**Date:** 2026-07-11  
**Commit:** `94c2d32` (pushed to `main`)  
**Company baseline:** `11111111-1111-1111-1111-111111111111`

---

## Part 1 — Sales.OperationsCenter: query trace and batching

### Original 15 queries (code path trace)

Handler: `GetSalesInvoiceOperationsCenterHandler` in `ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs`.

| # | Query | Source (file / method) |
|---|-------|------------------------|
| 1 | Sales invoice header | `SalesInvoiceRepository.LoadFromHeaderAsync` via `GetByIdAsync` — `AggregateRepositories.cs` |
| 2 | Sales invoice line items | same |
| 3 | Sales invoice roll details | same (always loaded, even when invoice is not in detailing status) |
| 4 | Sales invoice item taxes | same |
| 5 | Warehouse detailing session | same |
| 6 | Customer aggregate | `CustomerRepository.GetByIdAsync` — `PartyRepositories.cs` |
| 7 | Fabric items (batched) | `FabricCatalogRepository.GetItemsByIdsAsync` — handler |
| 8 | Fabric colors (batched) | `FabricCatalogRepository.GetColorsByIdsAsync` — handler |
| 9 | Container numbers (when detailing) | `ChinaContainerRepository.GetNumberLookupAsync` — handler |
| 10 | Journal entry headers | `JournalEntryRepository.GetAggregatesBySourceIdAsync` — `RemainingRepositories.cs` |
| 11 | Journal entry lines | same (second query) |
| 12 | Receipt payments + voucher join | `ReceiptInvoicePaymentRepository.GetByInvoiceWithVoucherAsync` |
| 13 | Sales return headers | `SalesReturnRepository.GetListAsync` — `AggregateRepositories.cs` |
| 14+ | Sales return lines (N+1) | `GetListAsync` loop — one query **per return** |
| last | Warehouse aggregate (name only needed) | `WarehouseRepository.GetByIdAsync` — loads warehouse + locations + stocks (**3 queries**) |

Journal entries and payments were already batched at the header/line or join level; returns and warehouse name lookup were the main remaining N+1 / over-fetch sources. Invoice load always pulled rolls/session even for non-detailing invoices.

### Changes applied

| Area | Change | File(s) |
|------|--------|---------|
| Invoice load | New `GetByIdForOperationsCenterAsync` — header + items + taxes always; rolls + session **only** for `AwaitingDetailing` / `Detailed` | `ISalesInvoiceRepository.cs`, `AggregateRepositories.cs`, handler |
| Party display | New `GetInvoicePartyDisplayAsync` — customer name/phone + warehouse name in **one** query (subquery); replaces `GetByIdAsync` + `WarehouseRepository.GetByIdAsync` | `ICustomerRepository.cs`, `PartyRepositories.cs`, handler |
| Journal | `GetAggregatesBySourceIdAsync` merged headers + lines into **one** GroupJoin query | `RemainingRepositories.cs` |
| Returns | `GetListAsync` batches all return lines with `WHERE SalesReturnId IN (...)` | `AggregateRepositories.cs` |
| Handler | Uses new methods; `IWarehouseRepository` dependency removed | `OperationsQueryHandlers.cs` |

### Expected query count (code review, not measured)

Typical **approved/delivered** invoice with one linked return (no detailing):

| Step | Queries |
|------|--------:|
| Invoice (header + items + taxes) | 3 |
| Party display (customer + warehouse name) | 1 |
| Catalog (fabrics + colors) | 2 |
| Journal (headers + lines, single join) | 1 |
| Payments | 1 |
| Returns (headers + batched lines) | 2 |
| **Total** | **~10** |

With **detailing** status, add rolls + session (+2) and container lookup (+1) → **~13**.

Down from **15** (plus extra warehouse/return N+1 queries when multiple returns exist). Further merging (e.g. invoice children in one SQL) was not attempted to avoid over-engineering.

---

## Part 2 — Startup Seed: permission batching

### Confirmation (before fix)

Each of the 12 module seed steps called `DatabaseSeeder.EnsurePermissionsAsync`, which independently executed:

1. `SELECT` existing permissions by code  
2. `AnyAsync` admin role exists  
3. `SELECT` existing role-permission links  
4. `SaveChangesAsync`

`ExpenseModuleSeeder` and `CapitalModuleSeeder` each called `EnsurePermissionsAsync` again — **14 independent read passes** for permissions alone (~42 reads + 14 saves).

### Changes applied

| Change | File(s) |
|--------|---------|
| New `PermissionSeedContext` — load all seed permission codes + admin role links **once** | `PermissionSeedContext.cs` |
| New `EnsureAllModulePermissionsAsync` — all 12 module + expense + capital permission tuples in one in-memory pass + one save | `DatabaseSeeder.cs` |
| Replaced 12 separate `Seed.*Permissions` phases with single `Seed.AllPermissions` | `DatabaseSeeder.cs` |
| Removed duplicate permission calls from `ExpenseModuleSeeder` / `CapitalModuleSeeder` | `ExpenseModuleSeeder.cs`, `CapitalModuleSeeder.cs` |

### Expected query count (code review, not measured)

| Phase | Before (reads + saves) | After |
|-------|------------------------|-------|
| Permission seeding only | ~42 reads + 14 saves | **3 reads + 1 save** |
| Full startup seed phase | ~73 queries (Nabil session) | **~37–40 queries** estimated (remaining seed steps unchanged: accounts, journal books, currency, expense categories, etc.) |

Permission reads drop from **~42 to 3**. Total startup seed should fall materially; reaching exactly 10–15 for the entire seed phase would require batching non-permission seed steps (out of scope for this task).

---

## Build

| Project | Result |
|---------|--------|
| `ERPSystem.Api` | **PASS** — 0 errors, 0 warnings |
| `ERPSystem` (WPF) | **PASS** — 0 errors, 0 warnings |

---

## Accounting baseline diff

Tool: `tools/AccountingBaselineReport` — company `11111111-1111-1111-1111-111111111111`  
Artifact: `artifacts/accounting-baseline-opcenter-seed-fix.md`

| Metric | Expected | Actual | Part 1 | Part 2 |
|--------|----------|--------|--------|--------|
| AR GL balance (USD) | 320.00 | **320.00** | PASS | PASS |
| Operational inventory (USD) | 104,968.412982 | **104,968.41** | PASS | PASS |
| Inventory GL balance (USD) | 15,622.43 (ref) / 15,598.92 (prior baseline) | **15,598.92** | PASS | PASS |

All changes are read-path / seed-idempotency only — no financial writes. **Accounting diff: PASS** (both parts).

---

## Deployment

| Step | Status |
|------|--------|
| Git push to `main` | **Done** — `94c2d32` |
| VPS `sudo bash /opt/erpsystem/src/deploy/deploy-app.sh` | **Blocked** — remote `sudo` requires interactive password (non-interactive SSH cannot complete deploy) |
| `https://alamal-ab.org/health` | **OK** (service healthy; may still be on prior deploy until manual sudo deploy) |

**Action for Nabil:** On the VPS, run:

```bash
sudo bash /opt/erpsystem/src/deploy/deploy-app.sh
```

Then hard-refresh the WPF client (or rebuild locally from `main` at `94c2d32`) before manual testing.

---

## Manual test handoff

**No app testing was performed by Cursor — awaiting Nabil's manual test session.**

After Nabil runs the desktop app normally and closes it, a follow-up task should read the new `session-summary-*.md` and compare Startup Seed / Sales.OperationsCenter query counts and timings against the prior `session-summary-20260711-193729.md` baseline.
