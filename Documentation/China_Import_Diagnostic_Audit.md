# China Import Module — Full Diagnostic Audit

**Date:** 2026-07-05  
**Scope:** Read-only audit — no code changes  
**Method:** Source-code trace (UI → service → handler → domain → repository → PostgreSQL → accounting/inventory engines)

---

## 1. Executive Summary

The China Orders / China Import module is **architecturally complete** for the core path (create → parse → cost entry → landing cost → approve → warehouse → sale-ready), with handlers registered in DI, PostgreSQL persistence, and integration hooks for `IIntegratedAccountingService` and `IInventoryEngine`.

**The module is not production-ready.** The primary user-reported symptom — *container approval does not execute* — is explained by a combination of **UI gating** (approve button hidden/disabled until strict preconditions are met), **permission checks**, and a **post-save accounting failure path** that can make approval appear to fail even after the container status has already changed in the database.

There is **no evidence** of a missing handler registration, unwired button click handler, or absent `SaveChanges` on the approval path when preconditions are satisfied. The approval pipeline is wired end-to-end; failures are predominantly **precondition**, **permission**, or **post-commit accounting** issues.

---

## 2. Current Status

| Area | Status | Notes |
|------|--------|-------|
| Container CRUD + list | 🟡 Partial | PostgreSQL-backed; list filter missing `LandingCostReviewed` |
| Excel / multi-file parse | 🟡 Partial | Parsers and handlers exist; session state in navigation context |
| Cost entry + landing cost | 🟡 Partial | Transactional `SubmitCostEntryAsync`; sets status `LandingCostReviewed` |
| **Container approval** | 🟡 Partial | Wired and functional when gated conditions pass; split-transaction risk |
| Accounting at approval | 🟡 Partial | Journal posted post-save; zero expenses skip silently; no tax-reserve GL |
| Warehouse transfer + inventory | 🟡 Partial | Transactional; uses `IInventoryEngine.PostContainerImportAsync` |
| Sale pricing | 🟡 Partial | Required when fabric type lines exist; separate screen |
| Ready for sale / sales visibility | 🟡 Partial | Status `InWarehouse` + stock posted required by `ContainerSaleValidator` |
| Distribution / stocktake routes | ❌ Broken | Placeholder UI only |
| InTransit / Arrived workflow | ❌ Broken | Domain methods exist; no handler or UI |
| Reports | 🟡 Partial | Hub via `ModuleReportsViews`; container report template exists |
| Operations Center | 🟡 Partial | PostgreSQL data; quick actions partially fire-and-forget |

---

## 3. Full Workflow Map

### Status enum (`ChinaContainerStatus`)

| Value | Arabic label | Used in live workflow |
|-------|--------------|----------------------|
| `Draft` | مسودة | Create only (immediate → `UnderReview`) |
| `InTransit` | بالطريق | Display/stepper only — **no transition handler** |
| `Arrived` | واصلة | Display/stepper only — **no transition handler** |
| `UnderReview` | قيد المراجعة | After create |
| `LandingCostReviewed` | مراجعة التكلفة | After `CalculateLandingCost` |
| `Approved` | معتمدة | After `ApproveContainer` |
| `InWarehouse` | في المخزن | After `MoveContainerToWarehouse` |
| `Closed` | مغلقة | Domain only — **no handler** |
| `Archived` | مؤرشفة | `ArchiveContainerHandler` |
| `Cancelled` | ملغاة | Unable to determine transition from current source |

---

### Step 1 — Create container

| Field | Detail |
|-------|--------|
| **Current status** | `Draft` → immediately `UnderReview` |
| **Required previous** | Valid parse lines, supplier, branch/company context |
| **Next status** | `UnderReview` |
| **UI** | `NewChinaImportControl` → `PackingListAnalysisControl` → `ChinaImportCostEntryControl` |
| **UI service** | `ContainerUiService.SubmitCostEntryAsync` (create + landing cost in one transaction) |
| **Command** | `CreateChinaContainerCommand` |
| **Handler** | `CreateChinaContainerHandler` |
| **Domain** | `ContainerAggregate.CreateDraft`, `AddItem`, `BeginReview` |
| **Repository** | `ChinaContainerRepository.AddAsync` |
| **Tables** | `china_import.containers`, `china_import.container_items`, `ImportBatches` |
| **Accounting** | None |
| **Inventory** | None |
| **Failure points** | Permission `containers.create`; invalid rows; duplicate container number (unique index); missing fabric catalog links |

---

### Step 2 — Import Excel files

| Field | Detail |
|-------|--------|
| **UI** | `PackingListAnalysisControl`, `NewChinaImportControl` |
| **UI service** | `ParseExcelAsync`, `ParseInvoiceAsync`, `ParsePackingSummaryAsync` |
| **Handlers** | `ImportContainerExcelHandler`, `ParseChinaInvoiceExcelHandler`, `ParseChinaPackingSummaryHandler` |
| **Session** | `ChinaImportNavigationContext` (multi-file session) |
| **Tables** | None until submit (in-memory parse) |
| **Failure points** | Unlinked fabric codes; parse errors; `AppServices` not initialized |

---

### Step 3 — Parse invoice / packing / DPL

| Field | Detail |
|-------|--------|
| **Parsers** | `PackingListExcelParser`, `ChinaInvoiceExcelParser`, `ChinaPackingListSummaryParser`, `ChinaImportCrossFileMatcher`, `ChinaImportDplLinkSuggester` |
| **Output** | `ContainerExcelParseResultDto`, type lines for weighted allocation |
| **Failure points** | Missing alias mappings (`fabric_type_aliases`); unmatched invoice/DPL lines |

---

### Step 4 — Review file analysis

| Field | Detail |
|-------|--------|
| **UI** | `PackingListAnalysisControl` |
| **Route guard** | `ChinaImportWorkflow.CanAccessRoute("FileAnalysis")` requires `hasParseSession` |
| **Failure points** | Navigation context lost on app restart |

---

### Step 5 — Enter costs

| Field | Detail |
|-------|--------|
| **UI** | `ChinaImportCostEntryControl` |
| **UI service** | `ContainerUiService.SubmitCostEntryAsync` |
| **Commands** | `CreateChinaContainerCommand` (if new) + `CalculateLandingCostCommand` |
| **Handler** | `CreateChinaContainerHandler`, `CalculateLandingCostHandler` |
| **Permission** | `containers.create`, `containers.landing-cost` |
| **Tables** | `landing_costs`, `container_fabric_type_lines` (if type lines) |
| **Failure points** | Existing container with landing cost → conflict; zero meters; transaction rollback |

---

### Step 6 — Calculate landing cost

| Field | Detail |
|-------|--------|
| **Current status after** | `LandingCostReviewed` |
| **Landing cost status** | `Reviewed` (via `LandingCost.MarkReviewed`) |
| **Domain** | `ContainerAggregate.SetLandingCost`, `SetFabricTypeLines` |
| **Allocator** | `ChinaImportTypeCostAllocator` (weighted or flat) |
| **UI review** | `ChinaImportLandingCostReviewControl`, OC tab `LandingCost` |
| **Accounting** | None at this step |
| **Failure points** | `TotalMeters <= 0`; validation on `LandingCostValidator` |

---

### Step 7 — Approve container ⚠️ FOCUS

| Field | Detail |
|-------|--------|
| **Required previous status** | `LandingCostReviewed` |
| **Required landing cost status** | `Reviewed` |
| **Next status** | `Approved` |
| **UI** | `ChinaImportLandingCostReviewControl` (button **اعتماد الحاوية**); OC quick action **اعتماد الحاوية** |
| **UI service** | `ContainerUiService.ApproveContainerAsync` |
| **Command** | `ApproveContainerCommand` |
| **Handler** | `ApproveContainerHandler` |
| **Domain** | `ContainerAggregate.Approve(userId)` |
| **Specification** | `ContainerCanBeApprovedSpecification` |
| **Repository** | `ChinaContainerRepository.UpdateAsync` → `SyncLandingCostAsync`, `SyncItemsAsync`, `SyncFabricTypeLinesAsync` |
| **Save** | `unitOfWork.SaveAndDispatchAsync` |
| **Event** | `ContainerApproved` |
| **Tables** | `containers` (status, ApprovedAt, ApprovedByUserId), `landing_costs` (status → Approved) |
| **Accounting** | Post-save via `DomainEventDispatcher` → `PostContainerApprovalAsync` |
| **Inventory** | **None at approval** |
| **Failure points** | See Section 4 |

---

### Step 8 — Post accounting entries (approval)

| Field | Detail |
|-------|--------|
| **Method** | `IIntegratedAccountingService.PostContainerApprovalAsync` |
| **Trigger** | `ContainerApproved` domain event (after DB commit) |
| **Journal** | Dr `LandingCostClearing` / Cr `AccountsPayable` |
| **Amount** | `TotalImportExpenses × ExchangeRateToLocalCurrency` |
| **Idempotency** | `PostIfNotExistsAsync` on `DocumentType.ChinaContainer` + `SourceId` + description contains `"اعتماد حاوية"` |
| **Skip conditions** | `totalExpenses <= 0` → **no journal, no error** |
| **Failure** | `AccountingException` if branch/company context empty or GL accounts missing |

---

### Step 9 — Move to warehouse

| Field | Detail |
|-------|--------|
| **Required previous** | `Approved` |
| **Next status** | `InWarehouse` |
| **UI** | `ChinaImportWarehouseTransferControl` |
| **UI service** | `ContainerUiService.MoveToWarehouseAsync` |
| **Command** | `MoveContainerToWarehouseCommand` |
| **Handler** | `MoveContainerToWarehouseHandler` (explicit DB transaction) |
| **Permission** | `containers.move-to-warehouse` |
| **Tables** | `containers` status |
| **Failure points** | Not approved; warehouse not selected; permission denied |

---

### Step 10 — Activate inventory stock

| Field | Detail |
|-------|--------|
| **Method** | `IInventoryEngine.PostContainerImportAsync` via `ContainerWarehouseImportService` |
| **Creates** | `FabricBatches`, `FabricRolls`, `WarehouseStocks`, `StockMovements` |
| **Roll status** | `FabricRollStatus.Available`, `InventoryReservationStatus.Available` |
| **Duplicate guard** | Throws if `WarehouseStocks` already exist for `ContainerId` |
| **Accounting** | `PostInventoryActivationAsync` — Dr Inventory Asset / Cr Landing Cost Clearing |
| **Failure points** | Missing warehouse; no valid items; already posted |

---

### Step 11 — Set sale prices

| Field | Detail |
|-------|--------|
| **When required** | When `FabricTypeLines.Count > 0` — **blocks approval** |
| **UI** | `ChinaImportSalePriceControl` |
| **Command** | `SetContainerTypeSalePricesCommand` |
| **Handler** | `SetContainerTypeSalePricesHandler` |
| **Permission** | `containers.landing-cost` |
| **Domain** | `ContainerAggregate.SetTypeSalePrices` |
| **Status change** | Stays `LandingCostReviewed` |

---

### Step 12 — Mark ready for sale

| Field | Detail |
|-------|--------|
| **UI** | `ChinaImportReadyForSaleControl` (display/summary) |
| **Effective gate** | Status `InWarehouse` + stock posted |
| **Validator** | `ContainerSaleValidator.EnsureReadyForSaleAsync` (used by sales module) |
| **Accounting** | None |
| **Inventory** | Stock already active from step 10 |

---

### Step 13 — Make stock visible to sales

| Field | Detail |
|-------|--------|
| **Condition** | `ChinaContainerStatus.InWarehouse` AND `IInventoryRepository.IsStockPostedForContainerAsync` |
| **Sales guard** | `ContainerSaleValidator` |
| **Roll availability** | `AvailableMeters` on `WarehouseStocks`; rolls `RemainingLengthMeters` |

---

## 4. Approval Failure Root Cause

### 4.1 Call chain (verified)

```
[UI] ChinaImportLandingCostReviewControl.ApproveAsync()
  OR ChinaContainerQuickActionRouter.ApproveAsync()  (fire-and-forget)
    ↓
[UI Service] ContainerUiService.ApproveContainerAsync(containerId)
    ↓
[Handler] ApproveContainerHandler.HandleAsync(ApproveContainerCommand)
    ├─ Permission: containers.approve
    ├─ Repository: GetByIdAsync (excludes archived via EF query filter)
    ├─ Spec: ContainerCanBeApprovedSpecification
    ├─ Domain: aggregate.Approve(userId)
    ├─ Repository: UpdateAsync
    ├─ Audit: ContainerAuditRecorder.RecordStatusChangeAsync("ApproveContainer")
    └─ Save: unitOfWork.SaveAndDispatchAsync → SaveChangesAsync THEN DispatchAsync
         ↓
[Event] DomainEventDispatcher.HandleContainerApprovedAsync
    ├─ Reload container (new DI scope)
    ├─ IIntegratedAccountingService.PostContainerApprovalAsync
    └─ INotificationService.PublishAsync(ContainerApprovedNotification)
```

### 4.2 Diagnostic checklist

| Check | Result |
|-------|--------|
| Button wired? | ✅ Yes — `_approveButton.Click += ApproveAsync` in `ChinaImportLandingCostReviewControl` |
| OC quick action wired? | ✅ Yes — `china:Approve` → `ChinaContainerQuickActionRouter.ApproveAsync` |
| Action enabled for status? | ⚠️ Only when `CanApprove == true` (see gates below) |
| UI calls correct method? | ✅ `ContainerUiService.ApproveContainerAsync` |
| Service calls correct handler? | ✅ `ICommandHandler<ApproveContainerCommand, ApplicationResult>` |
| Handler executes? | ✅ Registered in `ApplicationServiceCollectionExtensions.cs:249` |
| Domain validation blocks? | ✅ Yes — landing cost, valid items, sale prices (if type lines) |
| Exception swallowed? | ⚠️ Handler catches all exceptions → `ToFailureResult()`; OC quick action has **no try/catch** |
| SaveChanges called? | ✅ Yes — inside `SaveAndDispatchAsync` before event dispatch |
| Status updated? | ✅ Yes — `ChinaContainerStatus.Approved` persisted via `UpdateAsync` |
| Accounting before status change? | ❌ No — accounting runs **after** commit |
| Required costs missing? | Blocks at UI (`CanApprove`) and domain/spec |
| Required accounts missing? | Blocks at accounting post (after status saved) |
| Branch/company missing? | Blocks at accounting post (`AccountingException`) |
| Container items missing? | Spec fails if meters ≤ 0 or invalid items |
| Fabric type sale prices missing? | UI disables approve; domain throws Arabic `ContainerApprovalException` |

### 4.3 Root causes ranked by likelihood

#### A. UI gate — button hidden or disabled (most common “does nothing”)

`CanApprove` requires **all** of:

```csharp
aggregate.LandingCost?.Status == LandingCostStatus.Reviewed
&& aggregate.Status == ChinaContainerStatus.LandingCostReviewed
&& (aggregate.FabricTypeLines.Count == 0
    || aggregate.FabricTypeLines.All(l => l.SalePricePerMeterUsd > 0))
```

When `CanApprove` is false:
- `_approveButton.IsEnabled = false`
- `_approveButton.Visibility = Collapsed` (unless already Approved/InWarehouse)

**User perception:** Approval “does not execute” because the button is invisible or greyed out.

**Fix path (not implemented):** Complete sale prices on `ChinaImportSalePriceControl`; ensure cost entry reached `LandingCostReviewed`; navigate with active container in `ChinaImportNavigationContext`.

#### B. Missing sale prices when type lines exist

If weighted/multi-file import created `container_fabric_type_lines`, approval is blocked until margins are set. UI shows **"إدخال أسعار البيع (مطلوب قبل الاعتماد)"**.

#### C. Permission denied

`WpfPermissionService.CanAsync("containers.approve")` returns false if user lacks permission. UI shows **"صلاحية غير كافية"** via `ApplicationResultPresenter`.

Seeded permission exists in `DatabaseSeeder.cs` but role assignment must include it for non-admin users.

#### D. Post-save accounting failure (approval saved, user sees error)

`SaveAndDispatchAsync` commits the container as `Approved` **before** dispatching `ContainerApproved`. Accounting runs in a **new DI scope** and can throw:

- `AccountingException`: branch/company context not set
- `AccountingException`: missing GL accounts (`LandingCostClearing`, `AccountsPayable`)

Handler catch returns `ApplicationResult.Conflict` — **but container is already Approved in PostgreSQL**.

**User perception:** Error dialog after click; status may show Approved on refresh — inconsistent state.

#### E. Zero import expenses — silent accounting skip

If all landing cost expense fields sum to zero, `PostContainerApprovalAsync` returns immediately. Approval succeeds with **no journal**. Not a blocker but may look like accounting “didn't run.”

#### F. Operations Center quick action — no refresh / unobserved errors

```csharp
case "china:Approve":
    _ = ApproveAsync(row.Id);  // fire-and-forget
```

No loading state; OC may not refresh until manual reload. Exceptions in task may be unobserved (no try/catch in `ApproveAsync` for OC path — actually it uses ApplicationResultPresenter which handles result, but unhandled exceptions from scope disposal could occur).

#### G. Archived container

EF query filter `!x.IsArchived` causes `GetByIdAsync` → null → **"Container not found."**

### 4.4 Exception types on approval path

| Exception | Source | Mapped result |
|-----------|--------|---------------|
| `ContainerApprovalException` | Domain `Approve()` | Conflict (Arabic message for missing sale prices) |
| `AccountingException` | `CreateAndPostJournalAsync` | Conflict |
| `InvalidOperationException` | Repository update if header missing | Failure |
| PostgreSQL constraint | Unique container number, FK violations | Failure (with user-friendly mapper for duplicate number) |

No runtime exception log was available in source — **Unable to determine actual runtime SqlState from current source.**

---

## 5. Screen-by-Screen Audit

| Screen | PostgreSQL | Button visibility | Status gating | Validation messages | Refresh after action | OC timeline | Consistent UI |
|--------|------------|-------------------|---------------|---------------------|----------------------|---------------|---------------|
| `ContainerListPageControl` | ✅ | ✅ | 🟡 Filter missing `LandingCostReviewed` | 🟡 | ✅ Refresh hub | N/A | ✅ |
| `NewChinaImportControl` | 🟡 Session/draft | ✅ | N/A | 🟡 | 🟡 | N/A | ✅ |
| `PackingListAnalysisControl` | 🟡 Parse only | ✅ | Parse session required | 🟡 | 🟡 | N/A | ✅ |
| `ChinaImportCostEntryControl` | ✅ Submit | ✅ | Parse session | ✅ Conflict on duplicate | ✅ Sets active container | N/A | ✅ |
| `ChinaImportLandingCostReviewControl` | ✅ | 🟡 Approve collapsed if !CanApprove | ✅ Strict | ✅ Presenter + MessageBox | ✅ Reload | N/A | ✅ |
| `ChinaImportSalePriceControl` | ✅ | ✅ | `LandingCostReviewed` only | ✅ | ✅ | N/A | ✅ |
| `ChinaImportWarehouseTransferControl` | ✅ | 🟡 Enabled when Approved | ✅ | ✅ | ✅ | N/A | ✅ |
| `ChinaImportReadyForSaleControl` | ✅ | Display | `InWarehouse` | 🟡 | ✅ | N/A | ✅ |
| `ChinaContainerOperationsCenterControl` | ✅ | 🟡 Quick actions conditional | ✅ | 🟡 OC approve fire-and-forget | 🟡 Partial reload hub | ✅ Audit tab | ✅ |
| `ContainerWorkflowSummaryControl` | ❌ Placeholder | N/A | N/A | N/A | N/A | N/A | ✅ |

**Double-click / context actions:** Container list opens OC — verified via `ContainerListPageControl` pattern (standard ERP list module). Unable to determine all context menu entries without runtime test.

---

## 6. Service / Handler Call Chain Summary

| Operation | UI Service method | Command/Query | Handler |
|-----------|-------------------|---------------|---------|
| List | `GetListAsync` | `GetChinaContainerListQuery` | `GetChinaContainerListHandler` |
| Parse DPL | `ParseExcelAsync` | `ParseContainerExcelQuery` | `ImportContainerExcelHandler` |
| Parse invoice | `ParseInvoiceAsync` | `ParseChinaInvoiceExcelQuery` | `ParseChinaInvoiceExcelHandler` |
| Cost submit | `SubmitCostEntryAsync` | `CreateChinaContainerCommand` + `CalculateLandingCostCommand` | `CreateChinaContainerHandler` + `CalculateLandingCostHandler` |
| Sale prices | `SetTypeSalePricesAsync` | `SetContainerTypeSalePricesCommand` | `SetContainerTypeSalePricesHandler` |
| **Approve** | **`ApproveContainerAsync`** | **`ApproveContainerCommand`** | **`ApproveContainerHandler`** |
| Warehouse | `MoveToWarehouseAsync` | `MoveContainerToWarehouseCommand` | `MoveContainerToWarehouseHandler` |
| Archive | `ArchiveContainerAsync` | `ArchiveContainerCommand` | `ArchiveContainerHandler` |
| OC data | `GetOperationsCenterAsync` | `GetContainerOperationsCenterQuery` | `GetContainerOperationsCenterHandler` |

All handlers above are registered in `ApplicationServiceCollectionExtensions.cs` (lines 247–250, 330).

---

## 7. Database Tables Audit

### Schema `china_import`

| Table | Purpose | FK / constraint risks | Notes |
|-------|---------|----------------------|-------|
| `containers` | Header | Unique `(CompanyId, ContainerNumber)` | Query filter: `IsActive && !IsArchived` |
| `container_items` | Roll/meter lines | Unique `(ContainerId, LineNumber)` | Full replace on every update |
| `landing_costs` | Landing cost | Unique `ContainerId` | Status column for Reviewed/Approved |
| `container_fabric_type_lines` | Per-type allocation | Unique `(ContainerId, LineNumber)` | Sale price required for approval when present |
| `fabric_type_aliases` | DPL ↔ invoice matching | Unique `(CompanyId, SupplierId, DplMatchKey)` | Seeder: `ChinaImportFabricCatalogSeeder` |

### Related tables (other schemas)

| Table | When used |
|-------|-----------|
| `ImportBatches` | Container create with file name |
| `JournalEntries` + lines | Approval + inventory activation |
| `FabricBatches`, `FabricRolls`, `WarehouseStocks`, `StockMovements` | Warehouse transfer |
| `AuditLogs` | Status changes via `ContainerAuditRecorder` |
| `Accounts` | GL validation before journal post |

### Migrations (China-related)

- `20260626235435_InitialCreate` — schema + core tables
- `20260628203527_AddContainerExchangeRate`
- `20260628204959_AddContainerItemLotCode`
- `20260628220000_AddChinaInvoiceAmountUsd` — tax reserve field (not GL)
- `20260701120000_AddContainerFabricTypeLines`
- `20260702120000_AddFabricTypeAliases`
- `20260710120000_AddInventoryEngineModule`

### Missing / null risks

- Containers created without linked `FabricItemId`/`FabricColorId` on items → inventory posting may group incorrectly
- `FinancialTaxReservePostedLocal` stored on container — **not** a journal entry
- Soft delete: `IsArchived` on container; archived records invisible to repository

---

## 8. Accounting Integration Audit

### At approval

| Item | Detail |
|------|--------|
| **Method** | `PostContainerApprovalAsync` |
| **Debit** | `AccountingAccountIds.LandingCostClearing` — total import expenses (local) |
| **Credit** | `AccountingAccountIds.AccountsPayable` |
| **Amount** | `LandingCost.TotalImportExpenses × ExchangeRateToLocalCurrency` |
| **DocumentType** | `DocumentType.ChinaContainer` |
| **SourceId** | `container.Id` |
| **Idempotency** | Yes — skips if journal exists with matching source + description fragment `"اعتماد حاوية"` |
| **Double-click approve** | Second approve blocked by spec (landing cost no longer `Reviewed`) |
| **Tax reserve 2%** | Calculated in `SetChinaInvoiceFinancials` → stored in `FinancialTaxReservePostedLocal` only — **no GL posting** |
| **UI claim** | MessageBox says tax reserve was posted — **misleading** |

### At warehouse transfer

| Item | Detail |
|------|--------|
| **Method** | `PostInventoryActivationAsync` |
| **Debit** | Inventory Asset |
| **Credit** | Landing Cost Clearing |
| **Wrapped in** | Same handler transaction as inventory engine call |

### Risk

**Split transaction:** Container approved in DB even if accounting throws afterward. Unlike `MoveContainerToWarehouseHandler`, approval does **not** use `BeginTransactionAsync` wrapping accounting.

---

## 9. Inventory Integration Audit

| Item | Detail |
|------|--------|
| **Engine method** | `IInventoryEngine.PostContainerImportAsync` |
| **When** | Only on `MoveContainerToWarehouseHandler` — **not at approval** |
| **Warehouse** | User-selected in `ChinaImportWarehouseTransferControl` |
| **Stock state** | Rolls: `Available`; Reservation: `Available`; Batch: `Active` |
| **Sale price on rolls** | From type line `SalePricePerMeterUsd × exchange rate` when set |
| **Duplicate prevention** | Checks existing `WarehouseStocks` for `ContainerId` |
| **Sales visibility** | After `InWarehouse` + `IsStockPostedForContainerAsync` |

---

## 10. Missing Data / Configuration

| Item | Impact |
|------|--------|
| GL accounts not seeded / missing IDs | Accounting failure after approval |
| User without `containers.approve` | Permission denied |
| User without `containers.landing-cost` | Cannot set sale prices |
| User without `containers.move-to-warehouse` | Cannot post stock |
| Fabric catalog not linked in parse | Invalid items → blocks review/approval |
| Fabric type lines without sale prices | Blocks approval UI |
| Zero expense amounts | Approval succeeds, no approval journal |
| Branch not selected | `WpfCurrentBranchService` falls back to `DefaultBranchId` — usually OK |
| Stale docs (`docs/china-import-technical-audit.md`) | Claims handlers not in DI — **incorrect as of current source** |

---

## 11. Bugs Found

| ID | Severity | Bug | Evidence |
|----|----------|-----|----------|
| B1 | **HIGH** | Split transaction: status saved before accounting; failure leaves approved container without journal | `DomainEventSaveExtensions.SaveAndDispatchAsync` |
| B2 | **HIGH** | Misleading UI: claims 2% tax reserve posted to accounting | `ChinaImportLandingCostReviewControl` MessageBox vs `PostContainerApprovalAsync` |
| B3 | **MEDIUM** | List filter omits `LandingCostReviewed` — containers awaiting approval hard to find | `ChinaContainerStatusDisplay.FromArabicFilter`, `ContainerListPageControl` filter combo |
| B4 | **MEDIUM** | OC approve is fire-and-forget without loading/disabled state | `ChinaContainerQuickActionRouter` line 31 |
| B5 | **MEDIUM** | `ContainerCanBeApprovedSpecification` does not check container status or sale prices (UI does) | `DomainSpecifications.cs` vs `DomainMappers.cs` |
| B6 | **LOW** | `InTransit` / `Arrived` dead workflow states | `ContainerAggregate.MarkInTransit/MarkArrived` — no callers |
| B7 | **LOW** | Distribution / Stocktake routes are placeholders | `ContainerWorkflowSummaryControl` |
| B8 | **LOW** | Full item replace on every container update — performance/concurrency risk | `SyncItemsAsync` remove + re-add |
| B9 | **INFO** | Zero expenses silently skip approval journal | `PostContainerApprovalAsync` early return |

---

## 12. Risks

1. **Financial inconsistency** — Approved containers without matching GL entries if accounting fails post-commit.
2. **User confusion** — Approve button invisible when prerequisites incomplete; no inline explanation on collapsed button.
3. **Operational** — Cannot filter list by “awaiting approval” status.
4. **Compliance** — Tax reserve tracked on container row only, not in ledger.
5. **Data integrity** — Re-approving after partial failure blocked (landing cost already Approved) — manual intervention may be needed.
6. **Inventory** — Warehouse step is separate; sales blocked until transfer completes.

---

## 13. Required Fixes (recommended, not implemented)

### Phase 1 — Unblock approval UX (critical)

1. Show approve button as **disabled with reason** instead of collapsed when `!CanApprove`.
2. Add `LandingCostReviewed` to list filter combo and `FromArabicFilter`.
3. Wrap approval accounting in same transaction as status change **or** compensate/revert on accounting failure.
4. Fix tax reserve MessageBox text to match actual behavior (field storage vs GL).
5. Add try/catch + OC refresh to quick-action approve path.

### Phase 2 — Accounting integrity

1. Post tax reserve journal (if business requires) or remove “posted” language.
2. Validate GL accounts exist **before** domain approve (fail fast).
3. Surface warning when `totalExpenses <= 0` and approval journal skipped.

### Phase 3 — Workflow completion

1. Wire or remove `InTransit` / `Arrived` / `Closed` transitions.
2. Implement Distribution and Stocktake backends or remove routes.
3. Align specification with UI gates (`CanApprove` rules in spec).

### Phase 4 — Hardening

1. Optimistic concurrency on container header instead of full item sync.
2. Integration tests for full import → approve → warehouse path (`tools/ChinaImportCatalogTest` exists as starting point).

---

## 14. Recommended Implementation Phases

| Phase | Goal | Effort |
|-------|------|--------|
| 1 | Approval UX + transaction safety | 2–3 days |
| 2 | Accounting completeness + tax reserve | 1–2 days |
| 3 | Dead workflow + placeholders | 3–5 days |
| 4 | Tests + performance | 2–3 days |

---

## 15. Exact Files Inspected

### Navigation / shell
- `Core/Navigation/SubmoduleRegistry.cs`
- `Views/China/ChinaViews.cs`
- `Modules/ChinaImportModule.xaml.cs`
- `Core/ChinaImport/ChinaImportWorkflow.cs`
- `Core/ChinaImport/ChinaContainerStatusDisplay.cs`
- `App.xaml.cs`

### UI controls
- `Controls/China/ContainerListPageControl.cs`
- `Controls/China/NewChinaImportControl.cs`
- `Controls/China/PackingListAnalysisControl.cs`
- `Controls/China/ChinaImportCostEntryControl.cs`
- `Controls/China/ChinaImportLandingCostReviewControl.cs`
- `Controls/China/ChinaImportSalePriceControl.cs`
- `Controls/China/ChinaImportWarehouseTransferControl.cs`
- `Controls/China/ChinaImportReadyForSaleControl.cs`
- `Controls/China/ChinaContainerOperationsCenterControl.cs`
- `Controls/China/ContainerWorkflowSummaryControl.cs`
- `Controls/OperationsCenter/OperationsCenterShell.cs`

### Services
- `Services/China/ContainerUiService.cs`
- `Services/China/ChinaContainerQuickActionRouter.cs`
- `Services/China/ChinaImportNavigation.cs`
- `Services/China/ChinaImportNavigationContext.cs`
- `Services/ApplicationResultPresenter.cs`
- `Services/WpfPermissionService.cs`
- `Services/WpfSessionServices.cs`

### Application layer
- `ERPSystem.Application/Commands/Containers/ContainerCommands.cs`
- `ERPSystem.Application/Queries/Containers/ContainerQueries.cs`
- `ERPSystem.Application/UseCases/Containers/ContainerHandlers.cs`
- `ERPSystem.Application/UseCases/Containers/ImportContainerExcelHandler.cs`
- `ERPSystem.Application/UseCases/Containers/ChinaMultiFileParseHandlers.cs`
- `ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs`
- `ERPSystem.Application/Mapping/DomainMappers.cs`
- `ERPSystem.Application/DomainEvents/DomainEventDispatcher.cs`
- `ERPSystem.Application/Common/DomainEventSaveExtensions.cs`
- `ERPSystem.Application/Common/ContainerAuditRecorder.cs`
- `ERPSystem.Application/Common/ContainerSaleValidator.cs`
- `ERPSystem.Application/Common/ApplicationExceptionMapper.cs`
- `ERPSystem.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs`

### Domain
- `ERPSystem.Domain/Aggregates/ContainerAggregate.cs`
- `ERPSystem.Domain/Entities/ChinaImport/ChinaImportEntities.cs`
- `ERPSystem.Domain/Enums/ChinaContainerStatus.cs`
- `ERPSystem.Domain/Enums/ApprovalStatus.cs`
- `ERPSystem.Domain/Specifications/DomainSpecifications.cs`
- `ERPSystem.Domain/Events/ChinaImport/ChinaImportEvents.cs`
- `ERPSystem.Domain/Services/ChinaImportFinancials.cs`

### Infrastructure
- `ERPSystem.Infrastructure/Repositories/AggregateRepositories.cs`
- `ERPSystem.Infrastructure/Configurations/ContainerConfigurations.cs`
- `ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs`
- `ERPSystem.Infrastructure/Services/InventoryEngine.cs`
- `ERPSystem.Infrastructure/Services/ContainerWarehouseImportService.cs`
- `ERPSystem.Infrastructure/Seed/DatabaseSeeder.cs`
- `ERPSystem.Infrastructure/Migrations/` (China-related migrations listed in Section 7)

---

## 16. Exact Files That Need Changes

| Priority | File | Change |
|----------|------|--------|
| P0 | `ERPSystem.Application/Common/DomainEventSaveExtensions.cs` or `ApproveContainerHandler` | Transactional approval + accounting |
| P0 | `ERPSystem.Application/DomainEvents/DomainEventDispatcher.cs` | Propagate accounting failure handling strategy |
| P0 | `Controls/China/ChinaImportLandingCostReviewControl.cs` | Disabled button + reason; fix tax reserve message |
| P1 | `Core/ChinaImport/ChinaContainerStatusDisplay.cs` | Add `LandingCostReviewed` filter mapping |
| P1 | `Controls/China/ContainerListPageControl.cs` | Add filter option "مراجعة التكلفة" |
| P1 | `Services/China/ChinaContainerQuickActionRouter.cs` | Await approve; refresh OC; error handling |
| P1 | `ERPSystem.Domain/Specifications/DomainSpecifications.cs` | Align spec with `CanApprove` |
| P2 | `ERPSystem.Infrastructure/Services/IntegratedAccountingService.cs` | Tax reserve journal (if required) |
| P2 | `ERPSystem.Application/Mapping/DomainMappers.cs` | Optional: expose `ApproveBlockedReason` DTO |
| P3 | `Controls/China/ContainerWorkflowSummaryControl.cs` | Implement or remove placeholder routes |
| P3 | `ContainerAggregate.cs` + handlers | Wire InTransit/Arrived/Close or remove from stepper |

---

## 17. Production Readiness Checklist

| Category | Status |
|----------|--------|
| Workflow | 🟡 Partial — core path works; dead states and placeholders remain |
| UI | 🟡 Partial — approve gating confusing; filter gaps |
| PostgreSQL | ✅ Ready — schema and migrations present |
| Accounting | 🟡 Partial — split transaction; tax reserve not in GL |
| Inventory | 🟡 Partial — works after warehouse step |
| Validation | 🟡 Partial — spec/UI mismatch |
| Audit | ✅ Ready — `ContainerAuditRecorder` + OC timeline |
| Permissions | 🟡 Partial — seeded but must be assigned per role |
| Idempotency | ✅ Ready — journal dedup by source |
| Error handling | 🟡 Partial — post-save accounting errors inconsistent |
| Reports | 🟡 Partial — template exists; full report UX unclear |
| Performance | 🟡 Partial — full item sync on update |

---

## 18. Build Result

| Project | Result |
|---------|--------|
| `ERPSystem.Application.csproj` | ✅ **Build succeeded** — 0 errors, 0 warnings |
| `ERPSystem.csproj` (full WPF) | ❌ **Failed** — MSB3027 file lock (ERPSystem.exe + Visual Studio holding DLLs). **Not a compile error** — close running app and rebuild. |

---

*End of audit. No code was modified. All findings verified from source unless marked "Unable to determine from current source."*
