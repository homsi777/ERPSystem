# WPF Performance Rescue — Phase B Execution Report

## Executive status

Phase B is **partially completed and safety-verified**. All implemented changes preserve the WPF visual design and no React/web-client file was touched. The production accounting gate passed after every implemented change group. Items that could not be safely completed or measured are explicitly marked below; none is silently treated as complete.

## Mandatory safety gate

### Fresh production backup

- Database: `erp_pro` on production VPS.
- Backup: `/opt/erpsystem/backups/wpf-phase-b/erp_pro_pre_wpf_phase_b_20260711T141617Z.dump`
- Size: `1,111,840` bytes.
- `pg_restore --list`: **463** TOC lines; parsed successfully.
- SHA-256: `50f0d28ad541ebf817ebfc949c3496bd390147f8607f94f86a46b85d2a56f1d6`.

### Real-company baseline

- Company ID: `11111111-1111-1111-1111-111111111111`.
- Company: `الأمل.AB — تجارة أقمشة الجينز`.
- Baseline artifact: `artifacts/wpf-phase-b-production-baseline.json`.
- AR GL: **320.00**.
- Operational Inventory: **104,968.412982**.
- Inventory GL: **15,622.43**.

The live operational-inventory value is lower than the older Phase A reference `105,636.71`; the fresh production value above is the mandatory Phase B reference, captured before code changes with the production company ID passed explicitly.

### Accounting diffs

| Gate | Artifact | AR GL | Operational Inventory | Inventory GL | Result |
|---|---|---:|---:|---:|---|
| Initial | `wpf-phase-b-production-baseline.json` | 320.00 | 104,968.412982 | 15,622.43 | Reference |
| After startup group | `wpf-phase-b-after-startup-group.json` | 320.00 | 104,968.412982 | 15,622.43 | **PASS** |
| After query batching | `wpf-phase-b-after-query-batching.json` | 320.00 | 104,968.412982 | 15,622.43 | **PASS** |
| After navigation/dashboard group | `wpf-phase-b-final-baseline.json` | 320.00 | 104,968.412982 | 15,622.43 | **PASS** |
| After Excel async | `wpf-phase-b-after-excel-async.json` | 320.00 | 104,968.412982 | 15,622.43 | **PASS** |

## Priority-by-priority result

### Priority 1 — Startup permission-seeding N+1: COMPLETED

- Changed `DatabaseSeeder.EnsurePermissionsAsync`.
- Loads requested permission codes once, checks the admin role once, and loads existing role-permission IDs once.
- The loop now operates entirely on dictionaries/hash sets and tracked `Add` calls, followed by one `SaveChangesAsync`.
- Static query count for each permission group changed from approximately `3 × permission count` to three fixed reads plus one save.
- Production baseline diff: **PASS**.

The remaining seeding pipeline was inspected. Several small fixed-size loops remain, but the confirmed loop-per-permission multiplier was the dominant startup defect addressed here.

### Priority 2 — Async SSH tunnel startup: COMPLETED

- Added `StartIfConfiguredAsync` and asynchronous tunnel polling.
- Replaced `Thread.Sleep(500)` with `Task.Delay(500, cancellationToken)`.
- Replaced the blocking socket readiness probe with cancellable `ConnectAsync`.
- `App.OnStartup` now awaits tunnel startup and shows a lightweight `جاري الاتصال...` status window during the wait; the established main-window design is unchanged.
- Production baseline diff: **PASS**.

### Priority 3 — Lazy top-level modules: COMPLETED

- `MainWindow` now builds only `DashboardModule` eagerly.
- Other modules are created on first navigation and retained in a module cache.
- Dashboard `NavigationRequested`/`ActionRequested` wiring remains in the dashboard creation path.
- A repository-wide field-reference check found no external dependency on the removed eager module fields.
- Production baseline diff: **PASS**.

### Priority 4 — Sales Operations Center journal N+1: COMPLETED

- Added `IJournalEntryRepository.GetByIdsAsync`.
- Implementation loads all requested headers in one query and all lines in one query, then preserves requested ordering while grouping in memory.
- `GetSalesInvoiceOperationsCenterHandler` no longer calls `GetByIdAsync` per journal row.
- Expected query shape: `N` detail queries replaced by two fixed queries.
- Production baseline diff: **PASS**.

### Priority 4b — Shared catalog enricher: COMPLETED

- Added batched `GetItemsByIdsAsync` and `GetColorsByIdsAsync` repository operations.
- `SalesInvoiceCatalogEnricher.EnrichLinesAsync` and `EnrichRollsAsync` now perform two catalog queries per batch rather than two per line/roll.
- This changes the shared Customer Ledger, Warehouse Detailing, and Sales Operations Center enrichment path without changing DTO mapping.
- Production baseline diff: **PASS**.

### Priority 4c — Lazy Operations Center tabs: BLOCKED / NOT IMPLEMENTED

`OperationsCenterSpec` currently accepts already-constructed `UIElement Content`; every caller executes `BuildXxxTab(...)` before `OperationsCenterShell.Build` receives the specification. A safe implementation requires changing the shared contract to factories and converting every Operations Center caller together. That broad lifecycle change was not completed in this run; the existing eager behavior remains.

### Priority 5 — Parallel Sales list lookups: SAFETY-BLOCKED / NOT IMPLEMENTED

The three repositories are scoped through the same handler scope and share the same scoped `ErpDbContext`. Running their EF operations concurrently with `Task.WhenAll` would violate EF Core's single-operation-per-context rule. No unsafe parallelization was added. A future implementation needs an explicit context factory/read-model service with independently owned contexts.

### Priority 0 — DataGrid/ScrollViewer virtualization: NOT IMPLEMENTED

No XAML layout was changed. Removing outer scrollers across six custom screens requires visual regression testing for each screen; runtime UI verification was unavailable in this run. Existing frontend design remains untouched.

### Priority 8 — Server-side Aging query: NOT IMPLEMENTED

A dedicated server-side aging read model and DTO were not introduced. The current 1,000/5,000 loads remain. This was not approximated with a client filter because that would fail the requested database-side bounding guarantee.

### Sales Return critical per-line N+1: COMPLETED

- Both list and detail handlers collect distinct fabric/color IDs and batch-load them.
- DTO fields and ordering are unchanged.
- Production baseline diff: **PASS**.

### Receipt Voucher picker N+1: COMPLETED

- Added `GetCollectedTotalsAsync(IEnumerable<Guid>)`, implemented as one grouped SQL query.
- `FinanceUiService.GetOpenInvoicesForCustomerAsync` now filters eligible invoices first and resolves all collected totals in one call.
- Remaining-balance formula and threshold are unchanged.
- Production baseline diff: **PASS**.

### Finding #11 — per-row warehouse stock: COMPLETED

- `ReloadAllLineStockOptionsAsync` groups rows by unique `(container, warehouse)` pair.
- Each unique pair is loaded once and its identical option result is reused for all rows in that group.
- Individual-row refresh remains available for genuine single-row changes.
- Production baseline diff: **PASS**.

### Purchase Invoice/Order/Return list batching: NOT IMPLEMENTED

The purchase handlers still contain separate per-row supplier/original-invoice lookups. They require repository batch APIs beyond the catalog and journal APIs added in this run.

### Priority 6 — Subpage cache: PARTIALLY COMPLETED, SAFETY-SCOPED

- Added a per-shell LRU-style cache capped at five pages with a 45-second TTL.
- Caching is enabled only for HR and Settings.
- Sales, Purchases, Accounting, Inventory, Customers, Suppliers, Expenses, Capital, China, and Reports are deliberately always-fresh because they can display money, balances, inventory, or operational state.
- Production baseline diff: **PASS**.

### Priority 6b — Refresh-hub leaks: NOT IMPLEMENTED

The anonymous lambda subscriptions in the listed controls require conversion to named handlers before they can be unsubscribed correctly. This conversion was not completed across all listed controls.

### Priority 6c — Dashboard reload triggers: COMPLETED

- Data refresh events now enter a cancellable 250ms debounce.
- Language changes re-render labels without re-querying dashboard data.
- Loaded/data-change paths retain normal operational/KPI refresh behavior.
- Production baseline diff: **PASS**.

### Priority 6d — Warehouses/tax/branch caching: NOT IMPLEMENTED

No new global reference catalogs or invalidation hooks were added. Implementing cache invalidation correctly requires wiring every edit/save path; serving stale branch/tax/warehouse choices was considered unsafe.

### Priority 6e — Duplicate New Sales Invoice lookup: COMPLETED

- Removed the `IsVisibleChanged` subscription that reloaded customers after the full `OnLoaded` lookup load.
- Explicit refresh-hub-driven customer refresh remains intact.
- Production baseline diff: **PASS**.

### Priority 7 — Excel sync-over-async: COMPLETED

- Both China invoice and packing-summary handlers are genuinely async.
- `Task.Run(...).GetAwaiter().GetResult()` was replaced by awaited `Task.Run(...)` with the existing cancellation token.
- Parse validation and returned DTOs are unchanged.
- Production baseline diff after this final change: **PASS**.

## Build and runtime verification

- `dotnet build ERPSystem.csproj --no-restore`: **PASS**, 0 warnings, 0 errors.
- Full solution build: blocked by a pre-existing unrelated `ERPSystem.DocumentEngine/Templates/ReceiptVoucher/ReceiptVoucherTemplate.cs` error (`RenderContext` type not found).
- WPF profiler after-run measurement: **BLOCKED**. The attempted app launch logged a connection failure to `erp_pro` on `localhost:5433` and exited before producing a new profiler JSONL record. Therefore no fabricated after-times are reported.
- Phase A before-measurements remain: App.Startup 47.1–48.1s / 225 queries; MainWindow construction 3,953ms; Sales list 1.0–1.5s / 6 queries; Sales Operations Center 5,658ms / 17 queries.
- Static expected changes: permission checks become fixed-query batches; module construction becomes dashboard-only at startup; journal details become two batched queries; catalog enrichment becomes two batched queries; receipt allocations become one grouped query. Actual after-times remain pending a successful WPF runtime session.

## Files and scope confirmation

- 17 C# files changed in WPF/Application/Infrastructure paths.
- No React, `web-client`, CSS, or web frontend file changed.
- No schema migration was added.
- No production financial, accounting, or inventory write was performed by the remediation verification; every production baseline run was read-only.

## Final gate result

Accounting result-neutrality: **PASS** for all implemented work.

Full task acceptance: **PARTIAL**, because Priority 4c, Priority 0, Priority 8, Purchase list batching, Priority 6b, and Priority 6d remain unimplemented; Priority 5 is explicitly safety-blocked; profiler after-measurements are blocked by the failed WPF runtime connection attempt. These items require a subsequent continuation and must not be considered completed from this report.
