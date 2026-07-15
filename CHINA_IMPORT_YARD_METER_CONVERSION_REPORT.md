# China Import — Yard/Meter Unit Selection & Conversion Report

**Date:** 2026-07-15  
**Scope:** Import-time only — future container imports via WPF desktop.  
**Sample files:** `DPL-(114C).xls`, `INVOICE-(114C).xlsx`, `PL-(114C).xlsx` (container LZM114C26 equivalent)

---

## 1. UI step — explicit DPL unit selection

**Route:** `NewImport` → **`DplUnitSelection`** → `FileAnalysis` → `CostEntry` …

**Control:** `Controls/China/ChinaImportDplUnitSelectionControl.cs`

After uploading Invoice + PL + DPL on step 1, the user must open **«تحديد وحدة DPL»**:

- Two radio options (neither pre-selected):
  - **متر (M / MTS)** — values treated as meters
  - **ياردة (YDS)** — each length × **0.9144** → meters for inventory/costing
- Auto-detected unit from file headers is shown **for reference only** (not applied until confirm)
- **Confirm** applies conversion and navigates to file analysis

**Cost-per-meter is never converted** — banner states invoice unit price remains USD/M.

---

## 2. Conversion logic — factor 0.9144 (exact)

**File:** `ERPSystem.Application/Common/DplQuantityConverter.cs`

```csharp
public const decimal YardsToMetersFactor = 0.9144m;
public static decimal ToMeters(decimal nativeQuantity, DplQuantityUnit unit) =>
    unit == DplQuantityUnit.Yards
        ? Math.Round(nativeQuantity * YardsToMetersFactor, 4)
        : nativeQuantity;
```

**Re-application:** `DplQuantityUnitApplicator.Apply()` recomputes all roll `QuantityMeters` from unchanged `QuantityNative` when user confirms unit.

**Storage:** Canonical field remains **`LengthMeters`** on container items / fabric rolls. No parallel storage unit on `FabricRoll`.

**Selected unit** persisted on container header: `containers.DplQuantityUnit`  
**Audit per line:** `container_items.DplQuantityNative` + `DplQuantityUnit` (original value + unit)

---

## 3. Cross-validation — DPL vs Invoice/PL (per fabric/color)

**File:** `DplInvoicePlGroupValidator.cs`  
**Tolerance:** `max(0.5 m, 0.1% × expected meters)` — see `DplCrossValidationTolerance.cs`

| Component | Justification |
|-----------|---------------|
| **0.5 m floor** | Per-roll conversion rounds to 4 decimals; 229 rolls × 120 yd → ~25,127.712 m vs 25,128 m declared ≈ **0.29 m** drift |
| **0.1% relative** | Scales with group size without opening a wide gap |
| **Wrong unit ~9%** | Treating yards as meters (54960 vs 50256) far exceeds tolerance → **blocked** |

**UI:** `PackingListAnalysisControl` shows validation panel. Failed groups block **Continue** until user clicks **«تأكيد والمتابعة رغم الاختلاف»** (explicit override).

When **Meter** is selected on a yard DPL file, validation **fails** (automated test confirms).

---

## 4. Test results — 114C sample files (automated)

**Tests:** `DplQuantityUnitApplicatorTests` + `PackingListExcelParserTests`  
**Result:** 6/6 passed

| Check | Result |
|-------|--------|
| DPL auto-detect yards (114C) | ✅ |
| 458 rolls, ~50,256 m grand total | ✅ |
| Sample roll 120 yd → 109.728 m | ✅ |
| Yard selection + Invoice/PL cross-validation (2 color groups) | ✅ both pass |
| Wrong unit (Meter on yard file) | ✅ validation fails |
| 126C meter DPL unchanged | ✅ |

**Per-color reconciliation (114C, yard selected):**

- Expected (Invoice/PL): **25,128 MTS** per color × 2 colors
- DPL converted sum per group: **≈ 25,127.7 m** (within 0.5 m tolerance)

---

## 5. Audit trail

Each imported roll line stores:

| Field | Example |
|-------|---------|
| `DplQuantityNative` | `120` |
| `DplQuantityUnit` | `Yards` |
| `LengthMeters` | `109.728` |

Grid column **«الكمية (DPL)»** shows e.g. `120.00 yd (109.73 m)` via `PackingListRollDto.QuantityDisplay`.

---

## 6. Existing data — NOT touched

- No migration altering existing container/roll rows
- No batch update / backfill job
- Logic runs only on **new** import session after unit confirmation
- Already-imported containers unchanged

---

## 7. Cost-per-meter — unchanged

- `LandedCostPerMeterUsd` / invoice `UnitPriceUsd` remain **per meter**
- Only **roll length** is converted at import when user selects **يارد**
- Weighted cost allocation uses meter totals as before

---

## 8. Accounting baseline

**Status:** Not executed in this session (requires production DB + `tools/AccountingBaselineReport` against company `11111111-1111-1111-1111-111111111111`).

**Expected impact:** **None** until a new container is imported through the updated flow. No posting/valuation code paths changed.

**Recommended before deploy:** Nabil runs baseline → deploy → baseline diff (should be identical if no test import).

---

## 9. Build result

```
dotnet build ERPSystem.csproj
→ Build succeeded. 0 Warning(s), 0 Error(s)

dotnet test --filter DplQuantityUnit|PackingListExcelParser
→ Passed: 6, Failed: 0
```

---

## 10. Deployment status

**Not deployed** — code ready for Nabil to commit/push/deploy per project workflow.

---

## 11. Manual testing

**No WPF app testing performed** — awaiting Nabil's manual test:

1. Upload 114C Invoice + PL + DPL
2. On unit screen select **يارد** → confirm
3. Verify analysis shows cross-validation ✅ for both colors
4. Try selecting **متر** on same files → validation should fail unless manually confirmed
5. Complete import and verify container items show native yd + meter equivalent

---

## Files changed (summary)

| Area | Files |
|------|-------|
| Unit applicator | `DplQuantityUnitApplicator.cs` |
| Cross-validation | `DplInvoicePlGroupValidator.cs`, `DplCrossValidationTolerance.cs` |
| DTO | `ContainerDtos.cs` (+ `SelectedQuantityUnit`), `DplGroupCrossValidationResult.cs` |
| Navigation | `ChinaImportNavigationContext.cs`, `ChinaImportWorkflow.cs`, `ChinaViews.cs` |
| WPF UI | `ChinaImportDplUnitSelectionControl.cs`, `PackingListAnalysisControl.cs`, `NewChinaImportControl.cs` |
| Import | `PackingListImportLineBuilder.cs`, `ContainerUiService.cs` |
| Tests | `DplQuantityUnitApplicatorTests.cs` |
