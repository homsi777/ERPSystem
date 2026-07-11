# WPF Performance Rescue — Phase B: Remediation Plan (proposal only — not started)

**Status: NOT STARTED.** Per the task's explicit instructions, Phase A stops after producing the diagnostic report and this plan. Nothing below has been implemented. Every item is written so it can be picked up, estimated, and executed independently, with its own before/after measurement using the instrumentation already in place from Phase A.

**Guiding constraint for every item below:** none of these changes may alter a financial, accounting, or inventory result — only *how many round trips* and *when* work happens. Each item states explicitly why the fix is result-neutral.

---

## Priority 1 — Critical: fix the startup seeding N+1

**Target:** `ERPSystem.Infrastructure/Seed/DatabaseSeeder.cs:377-426`

**Problem (measured):** ~180 of the 225 DB round trips in the 47-48s `App.Startup` scope come from `EnsurePermissionsAsync` looping over ~60 permissions and doing 3 sequential round trips each — including re-checking the same `AdminRoleId` existence on every single iteration.

**Fix:**
1. Before the loop: load all existing permission codes into an in-memory `Dictionary<string, Guid>` (`SELECT Code, Id FROM identity.permissions`), check `AdminRoleId` existence **once**, and load all existing `(RoleId, PermissionId)` links for `AdminRoleId` into a `HashSet<Guid>` (`SELECT PermissionId FROM identity.role_permissions WHERE RoleId = @AdminRoleId`).
2. Inside the loop: only touch in-memory collections + `context.Permissions.Add(...)` / `context.RolePermissions.Add(...)` when missing — no DB calls per iteration.
3. One `SaveChangesAsync()` after the loop (unchanged from today).

**Result neutrality:** the exact same permissions and role-permission links end up existing after seeding — this only changes how many queries it takes to get there. Verify with a unit/integration test that seeding an empty DB and seeding a fully-seeded DB both produce byte-identical `identity.permissions`/`identity.role_permissions` tables before/after the change.

**Expected impact:** ~180 round trips → ~3, cutting the measured 38.6s of DB wait in this stage to well under 1s.

**Verification:** re-run the app with the existing `App.Startup` profiler scope; confirm `queries` drops from 225 to roughly 45 and `TotalMs` drops from ~48,000ms to a few seconds (the remaining cost is the other seed steps + migrations check + accounting health validation, themselves candidates for the same fix pattern if they show similar loops — audit them with the same method before declaring this done).

**Risk:** Low. Pure refactor of *how* existing idempotent checks are done; same end state.

---

## Priority 2 — Medium/High: stop blocking the UI thread during SSH tunnel startup

**Target:** `Services/SshTunnelService.cs:83-84`

**Problem:** `for (var attempt = 0; attempt < 40 && !IsPortOpen(...); attempt++) Thread.Sleep(500);` runs synchronously on the UI thread during `App.OnStartup`, before any `await` — up to 20 seconds of a fully frozen UI (no splash screen feedback possible) whenever the tunnel isn't already up.

**Fix:** Change the polling loop to `await Task.Delay(500, cancellationToken)` and make `StartIfConfigured`/its polling helper `async Task`, awaited from `App.OnStartup` (already `async void`, so this is a direct, low-risk change). Consider adding a lightweight "جاري الاتصال..." splash/status text during this wait so users get feedback instead of an apparently-frozen window.

**Result neutrality:** the tunnel readiness check logic (attempt count, 500ms interval, `IsPortOpen` check) is unchanged — only whether the wait blocks the UI thread.

**Verification:** manually kill the tunnel before launch and confirm the app UI remains responsive (e.g. window can be dragged) while the tunnel comes up, instead of "Not Responding" in Task Manager.

**Risk:** Low.

---

## Priority 3 — High: make top-level module construction lazy

**Target:** `MainWindow.xaml.cs:26-48`

**Problem (measured):** all 12 top-level modules (and their default subpages) are constructed synchronously in `MainWindow()`'s constructor — measured at 3,953ms total for `App.MainWindowConstruction`, of which only 360ms is DB; the rest is pure UI-thread control-tree construction for 11 modules the user has not asked to see yet.

**Fix:**
1. Change the 12 `private readonly XModule _x` fields to lazily-constructed (`XModule? _x; XModule X => _x ??= new XModule();` or `Lazy<XModule>`), or construct on first `NavigateTo` call for that module and cache the instance in a `Dictionary<AppModule, UIElement>`.
2. `MainWindow()`'s constructor only needs to eagerly build `Dashboard` (since `OnLoaded` immediately navigates there).
3. Keep the existing event wiring (`_dashboard.NavigationRequested += ...`) — just move it to wherever `Dashboard` is first constructed.

**Result neutrality:** every module still gets constructed exactly once per app session, the first time it's needed — same end state, just deferred. No behavior difference visible to the user beyond faster startup and a very small (sub-100ms, well within budget) one-time construction cost the *first* time each module is opened.

**Verification:** re-run with the `App.MainWindowConstruction` profiler scope; expect it to drop to well under 500ms (Dashboard only). Add a matching profiler scope around the first navigation into each of the other 11 modules to confirm their one-time construction cost is small and only paid once per session (navigating to the same module twice should not re-pay it).

**Risk:** Low-medium. Needs care that nothing outside the constructor currently assumes all 12 module instances exist immediately (e.g. background timers, event subscriptions across modules) — grep for `_sales.`, `_accounting.`, etc. outside `MainWindow.xaml.cs` before changing field types, and check `Modules/DashboardModule.xaml.cs`'s `ActionRequested`/`NavigationRequested` wiring still fires correctly for lazily-created targets.

---

## Priority 4 — High: fix the Sales Operations Center journal-entry N+1

**Target:** `ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs:236-260` (`GetSalesInvoiceOperationsCenterHandler`)

**Problem (measured):** 5,658ms / 17 queries to open one invoice's Operations Center, because journal entries linked to the invoice are fetched as a summary list, then **re-fetched individually** in a loop.

**Fix:**
1. Add `Task<IReadOnlyList<AccountingAggregate>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct)` to `IJournalEntryRepository` / `JournalEntryRepository`, implemented the same way `SalesInvoiceRepository.MapHeadersToListAggregatesAsync` already does it: one query for headers `WHERE Id = ANY(@ids)`, one query for all their lines `WHERE JournalEntryId = ANY(@ids)`, grouped in-memory.
2. In `GetSalesInvoiceOperationsCenterHandler`, replace the `foreach` + per-row `GetByIdAsync` with a single `GetByIdsAsync(journalRows.Select(r => r.Id))` call.

**Result neutrality:** the same set of journal entries, in the same shape, is returned — this only removes the redundant per-row round trip. Cross-check the DTO produced for a known invoice before/after the change is byte-identical (same journal entry count, same amounts, same ordering).

**Expected impact:** 17 queries → ~6-8; 5,658ms → an estimated 1.5-2.5s at the measured ~250ms/round-trip cost (further reducible once Priority 5's parallelization pattern is applied here too).

**Verification:** re-run with the existing `Sales.OperationsCenter` profiler scope on the same test invoice; confirm `queries` and `TotalMs` both drop, and manually compare the rendered Operations Center screen (journal entries tab, GL amounts) before/after to confirm identical output.

**Risk:** Low. Same pattern as an already-proven-good repository method elsewhere in this codebase.

---

## Priority 5 — Medium, low-risk, quick win: parallelize the Sales invoice list's independent lookups

**Target:** `ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs:160-170` (`GetSalesInvoiceListHandler`)

**Problem (measured):** customer-name, warehouse-name, and container-number lookups are 3 independent batched queries, currently `await`ed one after another — each pays the full ~250ms network round trip in series (part of why the already-well-batched list still measures 1.0-1.3s for 6 total queries).

**Fix:**
```csharp
var customerTask = customerRepository.GetNameLookupAsync(customerIds, cancellationToken);
var warehouseTask = warehouseRepository.GetNameLookupAsync(warehouseIds, cancellationToken);
var containerTask = containerRepository.GetNumberLookupAsync(containerIds, cancellationToken);
await Task.WhenAll(customerTask, warehouseTask, containerTask);
var customerNames = customerTask.Result;
var warehouseNames = warehouseTask.Result;
var containerNumbers = containerTask.Result;
```
**Important:** `ErpDbContext` is not thread-safe for concurrent operations on the *same* `DbContext` instance — verify (or ensure) each repository call in this handler resolves its own scoped `DbContext`/connection before parallelizing (check current DI lifetime for these repositories; if they share one context instance per request scope, either give the lookup repositories their own short-lived context for this call, or keep this fix but confirm via a quick load test that no `InvalidOperationException` ("A second operation was started...") occurs before shipping).

**Result neutrality:** identical three dictionaries are produced — order of execution has no bearing on the result.

**Expected impact:** removes ~2 round trips' worth of *serial* latency (roughly 400-500ms) from every list load.

**Verification:** re-run with the `Sales.InvoiceList` profiler scope; `TotalMs` should drop by roughly the two extra round-trips' latency while `queries` stays at 6.

**Risk:** Low, contingent on the `DbContext` thread-safety check above.

---

## Priority 6 — Medium: cache subpages across navigation

**Target:** `Controls/ModuleShellControl.xaml.cs:42-57`, `Views/SubmoduleViewFactory.cs`

**Problem:** navigating away from a subpage and back rebuilds it from scratch and re-fetches all its data, even if the user just switched tabs and back within seconds.

**Fix:** keep a small `Dictionary<string, UserControl>` per `ModuleShellControl` instance (keyed by subpage key), capped to e.g. the last 3-5 visited subpages (evict oldest on overflow to bound memory). On `SelectSubpage`, check the cache before calling `SubmoduleViewFactory.Create`. Optionally add a `DateTime`-based TTL so a cached page older than N seconds still triggers a fresh load (balances responsiveness against staleness — needs a product decision on acceptable staleness window per screen, e.g. 30-60s for list screens, 0s/always-fresh for anything showing balances/money if there is any concern about acting on stale figures).

**Result neutrality:** users still see fresh data on TTL expiry or first visit; no change to what a fresh load returns.

**Risk:** Medium. Needs care with pages that have side effects tied to being "re-Loaded" (e.g. anything relying on `Loaded` firing every visit to reset transient UI state like open filters/selection) — audit each subpage control before enabling caching for it; safest to opt in module-by-module rather than globally on day one.

---

## Priority 7 — High for large files, no current-usage impact: fix Excel-parsing sync-over-async

**Target:** `ERPSystem.Application/UseCases/Containers/ChinaMultiFileParseHandlers.cs:29-34, 71-76`

**Fix:** make `HandleAsync` genuinely `async` and `await Task.Run(...)` instead of `Task.Run(...).GetAwaiter().GetResult()` — copy the already-correct pattern in `ImportContainerExcelHandler.cs:38`.

**Result neutrality:** identical parse result returned; only removes the UI-thread block while parsing.

**Risk:** Low.

---

## Priority 8 — High, grows worse over time: bound the Aging report's page sizes

**Target:** `Controls/Accounting/AgingListControls.cs:62-63`

**Fix:** replace the two unconditional full-page loads (`GetListAsync(null, 1, 1000)` for customers, `GetListAsync(null, null, 1, 5000)` for invoices) with a dedicated server-side aging query that filters to `RemainingBalance > 0` in SQL and returns only the rows the report actually displays.

**Result neutrality:** the aging report shows the same customers/invoices (those with an outstanding balance) — just computed server-side instead of client-side-filtered from a full page.

**Risk:** Medium — needs its own query handler + DTO; more effort than the other items, but bounded and isolated to this one screen.

---

## Additional Priority items surfaced after cross-validation (independent automated audit)

Two independent automated audit passes (N+1 and DataGrid/navigation) confirmed every finding above and surfaced several more that share root causes with Priority 4-6 — folding them in here rather than as a separate track, since fixing the shared helpers below resolves multiple symptoms at once.

### Priority 4b — High, high-leverage: batch `SalesInvoiceCatalogEnricher` (fixes 3+ screens at once)

**Target:** `ERPSystem.Application/Common/SalesInvoiceCatalogEnricher.cs:18-21,97-98` (`EnrichLinesAsync`, `EnrichRollsAsync`)

**Problem:** both methods call `fabricCatalog.GetItemByIdAsync`/`GetColorByIdAsync` per line/roll. This single shared helper is the root cause behind the Sales Operations Center N+1 (Priority 4), the Customer Account Ledger N+1 (critical — invoices × lines × 2 + returns × lines × 2, potentially hundreds/thousands of round trips for long-history customers), and the Warehouse Detailing queue N+1 (critical — per queued invoice).

**Fix:** add `GetItemsByIdsAsync`/`GetColorsByIdsAsync` to `IFabricCatalogRepository` (single `WHERE Id IN (...)` each); change the enricher to collect all distinct IDs across every line/roll it's given, batch-load once, and map in-memory. Same result-neutrality argument as Priority 4.

**Why fix this before the narrower Priority 4:** one change here improves all three screens simultaneously instead of fixing them one at a time.

### Priority 4c — High: lazy tab construction in `OperationsCenterShell`

**Target:** `Controls/OperationsCenter/OperationsCenterShell.cs:205-212`

**Problem:** every Operations Center screen (Sales, Purchases, Customers, Suppliers, China containers) builds **all** tabs' content at shell-construction time instead of on first tab selection — e.g. opening a Customer's Operations Center Overview tab also eagerly builds the Statement tab (triggering the Customer Ledger N+1 above), a scoped invoice list, and a receipts grid, none of which the user has asked to see yet.

**Fix:** set each `TabItem.Content = null` at construction and populate it lazily in a `SelectionChanged` handler, caching the built content per tab so revisiting a tab within the same Operations Center session doesn't rebuild it.

**Result neutrality:** same content is eventually shown; only the timing of when it's built changes.

### Priority 6b — High: fix `*RefreshHub` subscription leaks

**Target:** 9 list controls — see `artifacts/wpf-datagrid-and-navigation-audit.md` for the full file list (Cashbox, CashboxTransfer, OpeningBalance, InventoryWarehouse, 3 classes in `InventoryFabricStockPageControl.cs`, InventoryFabricCategories, PurchaseInvoiceList, Employee, Department lists).

**Problem:** these subscribe to a refresh hub in their constructor without unsubscribing in `Unloaded`. Combined with Priority 6 (subpages are recreated on every navigation), every abandoned instance keeps its handler alive, so refresh events fire redundant load logic on every stale instance in addition to the current one — a growing leak over a session.

**Fix:** add `Unloaded += (_, _) => XListRefreshHub.RefreshRequested -= OnRefreshRequested;` matching the constructor subscription, mirroring the already-correct pattern in `SalesInvoiceListPageControl`, `CustomerListPageControl`, etc.

**Risk:** Low — purely additive cleanup, no behavior change while the control is alive.

### Priority 6c — High: consolidate Dashboard's triple reload trigger

**Target:** `Modules/DashboardModule.xaml.cs` (`Loaded`, `LanguageChanged`, `ErpDataRefreshHub`)

**Fix:** route all three through one debounced `RefreshDashboardAsync()`; `LanguageChanged` specifically should only re-render existing data with new labels, not re-query the database.

### Priority 6d — Medium: extend the `CurrencyCatalog` caching pattern to warehouses/tax codes/branches

**Target:** `Services/Settings/CurrencyCatalog.cs` is the reference pattern (load once at startup, read synchronously). Apply the same shape to warehouses, tax codes, and branches, which today are re-fetched from the DB on every form/screen that needs them (`NewSalesInvoiceControl` notably loads them twice — see Priority 6e).

### Priority 6e — High, quick fix: remove duplicate lookup load in `NewSalesInvoiceControl`

**Target:** `Controls/Sales/NewSalesInvoiceControl.xaml.cs:563-580` (`OnLoaded`) and `605-611` (`OnIsVisibleChanged`)

**Problem:** both handlers call the lookup-loading methods for a new invoice, so opening this form loads customers/warehouses/containers/cashboxes/tax codes **twice**.

**Fix:** guard with an `_lookupsLoaded` flag, or remove the `OnIsVisibleChanged` reload entirely once Priority 6d's caching lands (a cached lookup makes the second call nearly free anyway, but removing the redundant call is still the cleaner fix).

### Priority 0 (new) — Critical/High: fix DataGrid-in-`ScrollViewer` virtualization breakage

**Target:** `Controls/Sales/NewSalesInvoiceControl.xaml:175,518-652` (Critical), `Controls/Customers/CustomerAccountStatementControl.xaml:72,152-179` (High), `Controls/Sales/WarehouseDetailingPageControl.cs:75-98` (High), `Controls/Accounting/AgingListControls.cs:26-54,116-129` (High), `Controls/Reports/ModuleReportViewControl.cs:101-105,184-187` (High), `Controls/Sales/SalesTaxReportPageControl.cs:57-62` (Medium).

**Problem:** each of these wraps its `DataGrid` in an outer `ScrollViewer`, which disables row virtualization regardless of `EnableRowVirtualization` — every row is measured/realized up front. For the invoice line-items grid and the customer ledger grid especially, this means a form or statement with hundreds of rows pays full realization cost on open.

**Fix:** remove the outer vertical `ScrollViewer` around each grid; let the `DataGrid` fill a `*`-sized row/panel (it has its own internal virtualizing scroller) and keep any header/summary content in a separate fixed area above it. Pure layout change — no data or computation is affected.

**Suggested sequencing:** do this alongside Priority 6 (navigation caching) since both touch the same screens' layout/lifecycle.

---

## Suggested execution order & sequencing notes

1. **Do Priority 1, 2, 3 together first** — they are all "startup path" fixes, independent of each other, and collectively address the majority of the "3 minutes before I can do anything" complaint. Re-measure `App.Startup` and `App.MainWindowConstruction` after each to confirm the expected drop before moving on.
2. **Then Priority 4b and 4c** (shared-helper fixes) **before** the narrower Priority 4 and 5 — fixing `SalesInvoiceCatalogEnricher` and `OperationsCenterShell` resolves the Sales Operations Center N+1 plus the Customer Ledger and Detailing Queue N+1s in one pass, and lazy-tabs benefits every Operations Center screen, not just Sales.
3. **Priority 0 (DataGrid-in-ScrollViewer) and Priority 6/6b/6c/6d/6e** can be scheduled afterward, independently, in any order — each is scoped to a single screen/handler, though 0 and 6 touch overlapping screens so doing them together is efficient.
4. **Priority 7 and 8** remain last — narrowest scope, least urgent given today's data volumes.
4. After each priority item lands, re-run the accounting baseline tool (`tools/AccountingBaselineReport`) and diff its output against `artifacts/wpf-performance-prechange-baseline-accounting.json`/`-health.json` from this phase — any diff other than expected new activity (new invoices/entries created during testing) should block the change until explained.
5. Extend the `IWpfPerformanceProfiler` instrumentation to the remaining screens named in the original task (Customers, Suppliers, Purchases, Inventory, Accounting/Journal Entries, China Import, HR, Capital Partners, Expenses, Reports, Dashboard widgets) as each is touched, so every fix has its own measured before/after — the pattern is already established in `Controls/Sales/SalesInvoiceListPageControl.cs` and `SalesInvoiceOperationsCenterControl.cs` and is a 5-10 line addition per screen.
