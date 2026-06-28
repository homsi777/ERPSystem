# تقرير تدقيق تقني — وحدة استيراد الصين (China Import)

**التاريخ:** 2026-06-26  
**النطاق:** قراءة فقط — لا تغييرات على الكود  
**الهدف:** توثيق المنطق الحالي (واجهة + Domain + Application) قبل ربط PostgreSQL

---

## ملخص تنفيذي

| الطبقة | الحالة الفعلية |
|--------|----------------|
| **واجهة China Import** (`Views/China/ChinaViews.cs`) | **100% تجريبية** — `ChinaImportSampleData` فقط، بدون رفع ملفات Excel حقيقي |
| **Domain + Infrastructure** | نموذج غني (`ContainerAggregate`, `LandingCost`, …) + جداول PostgreSQL + Repository |
| **Application Handlers** | **4 handlers مكتوبة** (`Create`, `CalculateLandingCost`, `Approve`, `MoveToWarehouse`) — **غير مسجّلة في DI** |
| **Excel Import Handler** | **`ImportContainerExcelCommand` موجود — بدون Handler** |
| **`GetChinaContainerListHandler`** | مسجّل في DI — **يُستدعى من Sales/Dashboard فقط**، وليس من شاشة قائمة الحاويات |

**خطر رئيسي:** واجهة الاستيراد تعرض أعمدة Excel ومعادلات Landing Cost، بينما Domain يستخدم enums ومعادلات مختلفة جزئياً؛ لا يوجد تحويل عملات CNY→SAR؛ لا يُخزَّن «سعر الصين الأصلي» في `CalculateLandingCostCommand`.

---

## 1. منطق استيراد Excel (`NewImport` + `ExcelReview`)

### 1.1 أين يعيش الكود؟

| الملف | الدور |
|-------|------|
| `Views/China/ChinaViews.cs` → `BuildImportForm()` | شاشة «استيراد حاوية جديدة» |
| `BuildExcelReview()` | **alias كامل** — يعيد نفس `BuildImportForm()` |
| `Core/ChinaImport/ChinaImportSampleData.cs` | مصدر بيانات المعاينة والملخص |
| `ERPSystem.Application/Commands/Containers/ContainerCommands.cs` | `ImportContainerExcelCommand` (Domain-ready، بدون UI/Handler) |

### 1.2 أعمدة Excel المتوقعة (من واجهة المعاينة فقط)

**لا يوجد parser Excel في المشروع.** الأعمدة التالية مُعرَّفة فقط في `BuildExcelPreviewGrid()` كـ DataGrid على بيانات وهمية:

```csharp
// Views/China/ChinaViews.cs — BuildExcelPreviewGrid()
AddCol(g, "رقم التوب",     nameof(ContainerFabricLine.BoltNumber),   80);
AddCol(g, "كود القماش",   nameof(ContainerFabricLine.FabricCode),   100);
AddCol(g, "نوع القmاش",   nameof(ContainerFabricLine.FabricType),   "*");
AddCol(g, "اللون",        nameof(ContainerFabricLine.Color),        80);
AddCol(g, "الطول بالمتر", nameof(ContainerFabricLine.LengthMeters), 100, "N2");
AddCol(g, "الوزن بالكغ",  nameof(ContainerFabricLine.WeightKg),     100, "N2");
AddCol(g, "ملاحظة",       nameof(ContainerFabricLine.Note),         100);
AddCol(g, "حالة الصف",    nameof(ContainerFabricLine.RowStatus),    90);
```

**نموذج الصف في UI:**

```csharp
// Core/ChinaImport/ChinaImportModels.cs
public class ContainerFabricLine
{
    public int BoltNumber { get; set; }
    public string FabricCode { get; set; } = "";
    public string FabricType { get; set; } = "";
    public string Color { get; set; } = "";
    public decimal LengthMeters { get; set; }
    public decimal WeightKg { get; set; }
    public string Note { get; set; } = "";
    public string RowStatus { get; set; } = "صحيح";
    // ...
}
```

**شكل الأمر في Application (ما سيُرسَل للـ DB لاحقاً):**

```csharp
// ERPSystem.Application/Commands/Containers/ContainerCommands.cs
public sealed class ImportContainerLineCommand
{
    public int LineNumber { get; init; }
    public Guid FabricItemId { get; init; }      // resolved IDs — لا strings
    public Guid FabricColorId { get; init; }
    public int RollCount { get; init; }
    public decimal LengthMeters { get; init; }
    public Guid? BuyerCustomerId { get; init; }
    // ⚠️ لا WeightKg، لا BoltNumber، لا FabricCode/Color كـ string
}
```

**فجوة:** UI يعرض 8 أعمدة نصية/رقمية؛ Command يتوقع GUIDs + `RollCount` — **لا جسر تحويل موجود**.

### 1.3 مكتبة Excel / آلية الرفع

| البند | الواقع |
|-------|--------|
| ClosedXML / EPPlus / OpenXML | **غير موجودة** في `.csproj` أو الكود |
| `OpenFileDialog` / `FileStream` | **غير موجود** في `ChinaViews` |
| أزرار «رفع Excel» / «معاينة» / «تأكيد» | `Button` بدون `Click` handlers — **ديكور فقط** |

```csharp
// Views/China/ChinaViews.cs — أزرار بدون ربط
uploadRow.Children.Add(new Button { Content = "رفع ملف Excel", ... });
uploadRow.Children.Add(new Button { Content = "معاينة البيانات", ... });
uploadRow.Children.Add(new Button { Content = "تأكيد الحفظ", ... });
```

### 1.4 التحقق (Validation) — ما هو موجود vs غير موجود

| التحقق | UI (Sample) | Domain | Application |
|--------|-------------|--------|-------------|
| أعمدة Excel مطلوبة | ❌ | ❌ | ❌ |
| أرقام سالبة | ❌ | ✅ `LengthInMeters`, `WeightInKg`, `Money` | ❌ على Import |
| صفوف مكررة | ❌ | ❌ | ❌ |
| خلايا فارغة | ❌ | ❌ | ❌ |
| `FabricCode`/`Color` → GUID | ❌ | يفترض GUID جاهز | ❌ |
| `RowStatus == Valid` | ~10% عشوائي «خطأ» في Sample | `ChinaContainerItem.IsValid` | ❌ |
| `BeginReview()` — لا invalid rows | — | ✅ يرمي إذا `!i.IsValid` | ❌ لا handler |

**Domain — صف غير صالح:**

```csharp
// ERPSystem.Domain/Entities/ChinaImport/ChinaImportEntities.cs
public bool IsValid => RowStatus.Equals("Valid", StringComparison.OrdinalIgnoreCase);
// ⚠️ UI يستخدم "صحيح"/"خطأ" — Domain يتوقع "Valid"
```

**Domain — بدء المراجعة:**

```csharp
// ERPSystem.Domain/Aggregates/ContainerAggregate.cs
public void BeginReview()
{
    if (_items.Any(i => !i.IsValid))
        throw new ContainerApprovalException("Cannot review container with invalid import rows.");
    TransitionTo(ChinaContainerStatus.UnderReview);
}
```

### 1.5 سلوك الصف المعطوب

| السينario | السلوك |
|-----------|--------|
| رفع Excel حقيقي | **غير مُنفَّذ** — لا crash ولا skip؛ لا شيء يحدث |
| Sample data | ~10% صفوف `RowStatus = "خطأ"` للعرض فقط — **لا يُستبعد** من الملخص |
| `GetImportSummary` | يعد `valid` / `errors` لكن **لا يمنع الحفظ** |

```csharp
// ChinaImportSampleData.GetImportSummary
valid:  lines.Count(l => l.RowStatus == "صحيح"),
errors: lines.Count(l => l.RowStatus != "صحيح"),
```

### 1.6 خطوة المعاينة — ما يمكن للمستخدم تعديله؟

- **معاينة:** Grid **read-only** (`ErpUiFactory.BuildGrid(data, false)`).
- **نموذج الرأس:** `TextBox`/`ComboBox`/`DatePicker` — **قيم ثابتة في XAML code-behind**، بدون حفظ.
- **«تأكيد الحفظ»:** بدون handler → **لا persist**.
- **`ExcelReview`:** نفس `NewImport` — لا فرق وظيفي.

**أنواع الملف (Radio):** «ملف نوع قmاش واحد» / «ملف عدة أنواع» / «إدخال يدوي» — **بدون منطق**.

---

## 2. حساب Landing Cost (`LandingCost`)

### 2.1 نسختان من المعادلات (UI Mock vs Domain)

#### أ) UI Mock — `ContainerLandingCost` (ما تراه الشاشة اليوم)

```csharp
// Core/Domain/FabricDomainModels.cs
public class ContainerLandingCost
{
    public decimal TotalLengthFromInvoice { get; set; }
    public decimal ContainerWeightKg { get; set; }
    public decimal ContainerWeightGrams => ContainerWeightKg * 1000;
    public decimal CustomsAmountPaid { get; set; }
    public decimal CustomsCostPerMeter =>
        TotalLengthFromInvoice > 0 ? CustomsAmountPaid / TotalLengthFromInvoice : 0;
    public decimal AvgGramPerMeter =>
        TotalLengthFromInvoice > 0 ? ContainerWeightGrams / TotalLengthFromInvoice : 0;
    public decimal Shipping { get; set; }
    public decimal Clearance { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal TotalImportExpenses =>
        CustomsAmountPaid + Shipping + Clearance + OtherExpenses;
    public decimal ExpenseCostPerMeter =>
        TotalLengthFromInvoice > 0 ? TotalImportExpenses / TotalLengthFromInvoice : 0;
}
```

**قيم ثابتة في الشاشة** (`BuildLandingCost`):

```csharp
var cost = new ContainerLandingCost
{
    TotalLengthFromInvoice = 38500,
    ContainerWeightKg = 18500,
    CustomsAmountPaid = 42000,
    Shipping = 15000,
    Clearance = 8500,
    OtherExpenses = 3200
};
```

#### ب) Domain — `LandingCost` entity (ما يُحسب ويُخزَّن فعلياً)

```csharp
// ERPSystem.Domain/Entities/ChinaImport/ChinaImportEntities.cs
public Money TotalImportExpenses =>
    CustomsAmountPaid.Add(Shipping).Add(Clearance).Add(OtherExpenses);

public decimal CustomsCostPerMeter =>
    TotalLengthFromInvoice.Value > 0
        ? CustomsAmountPaid.Amount / TotalLengthFromInvoice.Value : 0;

public decimal ExpenseCostPerMeter =>
    TotalLengthFromInvoice.Value > 0
        ? TotalImportExpenses.Amount / TotalLengthFromInvoice.Value : 0;

public decimal AvgGramPerMeter =>
    TotalLengthFromInvoice.Value > 0
        ? ContainerWeight.ToGrams().Value / TotalLengthFromInvoice.Value : 0;
```

#### ج) Handler — `CalculateLandingCostHandler`

```csharp
// ERPSystem.Application/UseCases/Containers/ContainerHandlers.cs
var landingCost = LandingCost.Create(
    new LengthInMeters(command.TotalLengthMeters),
    new WeightInKg(command.ContainerWeightKg),
    new Money(command.CustomsAmount),
    new Money(command.Shipping),
    new Money(command.Clearance),
    new Money(command.OtherExpenses));

Domain.Validators.LandingCostValidator.Validate(landingCost);
aggregate.SetLandingCost(landingCost);
```

```csharp
// CalculateLandingCostCommand
public decimal TotalLengthMeters { get; init; }
public decimal ContainerWeightKg { get; init; }
public decimal CustomsAmount { get; init; }
public decimal Shipping { get; init; }
public decimal Clearance { get; init; }
public decimal OtherExpenses { get; init; }
```

#### د) «تكلفة الوصول النهائية للمتر» — `LandingCostCalculator`

```csharp
// ERPSystem.Domain/Services/LandingCostCalculator.cs
public static Money CalculateLandedCostPerMeter(LandingCost landingCost, Money fabricUnitCost)
{
    var perMeter = landingCost.ExpenseCostPerMeter;
    return fabricUnitCost.Add(new Money(perMeter));
}
```

**⚠️ `fabricUnitCost` (سعر الصين/سعر القمash) غير موجود في:**
- `CalculateLandingCostCommand`
- شاشة `BuildLandingCost`
- `LandingCost` entity

**الاستنتاج:** المعادلة الحالية = **توزيع مصاريف الاستيراد على إجمالي الأمتار فقط**؛ «سعر الصين الأصلي» **غير م wired**.

### 2.2 مكوّنات التكلفة الم included

| المكوّن | UI | Domain/Command | ملاحظات |
|---------|-----|----------------|---------|
| سعر وحدة القمash من الصين | ❌ | ❌ | فقط في `CalculateLandedCostPerMeter(..., fabricUnitCost)` |
| Customs / الجمارك | ✅ `CustomsAmountPaid` | ✅ `CustomsAmountPaid` | |
| Shipping / الشحن | ✅ | ✅ | |
| Clearance / التخليص | ✅ | ✅ | |
| Other / مصاريف أخرى | ✅ | ✅ | |
| Insurance | ❌ | ❌ | |
| Local transport | ❌ | ❌ | يمكن وضعه تحت Other |
| Overhead % | ❌ | ❌ | |
| `LandingCostExpense` (قائمة تفصيلية) | ❌ UI | ✅ entity | **غير مستخدم** في Handler — حقول flat فقط |

### 2.3 تحويل العملات

| البند | الواقع |
|-------|--------|
| CNY / USD → SAR | **غير موجود** |
| `Money` default currency | `"SAR"` hardcoded |
| Exchange rate input | ❌ |
| Rate per container | ❌ — لا حقل في `LandingCostEntity` |
| `ImportCost` على UI model | mock فقط — **لا يدخل** في `LandingCost` |

```csharp
// ERPSystem.Domain/ValueObjects/Money.cs
public Money(decimal amount, string currency = "SAR")
```

### 2.4 آلية توزيع المصاريف على الأصناف

**القاعدة الوحيدة المُطبَّقة:** قسمة **بالتساوي على إجمالي أمتار الفاتورة** (meter-based flat rate).

```
CustomsCostPerMeter     = CustomsAmountPaid / TotalLengthFromInvoice
ExpenseCostPerMeter     = TotalImportExpenses / TotalLengthFromInvoice
                          (TotalImportExpenses includes Customs — أي الجمارك داخل المجموع)
AvgGramPerMeter         = (ContainerWeightKg × 1000) / TotalLengthFromInvoice  // للتحقق فقط
```

| أساس التوزيع | مستخدم؟ |
|--------------|---------|
| Weight | ❌ (الوزن للتحقق فقط — BR-23 في docs) |
| Volume | ❌ |
| Value | ❌ |
| Quantity (rolls) | ❌ |
| Meters (total) | ✅ |

**لا توزيع per-line-item** في الكود — كل الحاوية meter-rate واحد.

### 2.5 التقريب (Rounding)

| النوع | الق rule | الأماكن |
|-------|----------|---------|
| `Money.Amount` | `Round(..., 2, AwayFromZero)` | `Money` ctor |
| `LengthInMeters` | `Round(..., 4, AwayFromZero)` | `LengthInMeters` ctor |
| `WeightInKg` / grams | `Round(..., 4, AwayFromZero)` | `WeightInKg` ctor |
| `CustomsCostPerMeter` etc. | **لا rounding صريح** — `decimal` division كامل | computed properties |
| UI display | `N0`, `N2`, `N4` في XAML grids | عرض فقط |

**ملاحظة:** `ExpenseCostPerMeter` **يشمل** `CustomsAmountPaid` في البسط — إذا أضفت `CustomsCostPerMeter + ExpenseCostPerMeter` **تُحسب الجمارك مرتين**.

### 2.6 الهالك (Waste %)

```csharp
// Views/China/ChinaViews.cs — BuildImportSummaryPanel
("الوزن الصافي بعد الهالك", $"{s.weight * 0.97m:N0} كغ")  // ⚠️ 0.97 ثابت = 3% هالك
```

- حقل «نسبة الهالك %» في نموذج الاستيراد — **لا يُستخدم** في أي حساب Domain.
- `ContainerAggregate` — **لا** `WastePercent`.

---

## 3. التوزيع (`Distribution`)

### 3.1 UI الحالي

```csharp
// Views/China/ChinaViews.cs — BuildDistribution()
new ContainerCustomerDistribution {
    CustomerName = "أحمد الحمصي", FabricCode = "FAB-101", Color = "أبيض",
    Rolls = 12, Meters = 720
}
```

- جدول **ثابت** — بدون إدخال أو حفظ.
- **توزيع على العملاء** (CustomerName) — **ليس** على مستودعات/فروع.

### 3.2 Domain

```csharp
// ERPSystem.Domain/Entities/ChinaImport/ChinaImportEntities.cs
public static ContainerCustomerDistribution Create(
    Guid customerId, Guid fabricItemId, Guid fabricColorId,
    int rollCount, LengthInMeters meters)

// ContainerAggregate.cs
public void AddDistribution(ContainerCustomerDistribution distribution) =>
    _distributions.Add(distribution);
```

| السؤال | الجواب |
|--------|--------|
| warehouses / branches؟ | **لا** — `ContainerDistributionEntity` مرتبط بـ `CustomerId` |
| توزيع تلقائي؟ | **لا** — `AddDistribution` يدوي فقط، **لا handler** |
| الكمية المتبقية؟ | **لا منطق** — لا validation أن Σ rolls/meters = إجمالي الحاوية |
| `BuyerCustomerId` على `ChinaContainerItem` | اختياري عند Import — **غير مستخدم** في UI |

---

## 4. جرد الحاوية (`Stocktake`)

### 4.1 UI

```csharp
// Views/China/ChinaViews.cs — BuildStocktake()
new { البند = "الوارد", القيمة = "450" },
new { البند = "المتوقع", القيمة = "448" },
new { البند = "المعدود", القيمة = "446" },
new { البند = "الفرق", القيمة = "-2" },
// + مبيعات، حجوزات، إرجاع، هالك، مناقلات — كلها أرقام ثابتة
```

### 4.2 Domain / Application

| البند | موجود؟ |
|-------|--------|
| `StocktakeSession` في `Core/Domain/FabricDomainModels.cs` | UI mock عام للمخزون — **غير مربوط** بالحاوية |
| Container stocktake aggregate | ❌ |
| معادلة variance | ❌ — «الفرق = -2» hardcoded في UI |
| مقارنة Excel vs physical | **غير مُنفَّذ** |

**الاستنتاج:** الجرد **Placeholder** — لا formula ولا persistence.

---

## 5. دورة حياة الحاوية / Status Flow

### 5.1 enum Domain (المصدر المعتمد للـ DB)

```csharp
// ERPSystem.Domain/Enums/ChinaContainerStatus.cs
public enum ChinaContainerStatus
{
    Draft = 0,
    InTransit = 1,
    Arrived = 2,
    UnderReview = 3,
    LandingCostReviewed = 4,
    Approved = 5,
    InWarehouse = 6,
    Closed = 7,
    Archived = 8,
    Cancelled = 9
}
```

### 5.2 enum UI (منفصل — **لا يطابق Domain**)

```csharp
// Core/ChinaImport/ChinaImportModels.cs
public enum ContainerStatus
{
    InTransit, Arrived, Customs, Distributed, Approved, Archived, Closed
}
```

**⚠️ خطر:** قائمة الحاويات في China Import تستخدم `ContainerStatus` + `StatusDisplay` عربي مختلف عن `ChinaContainerStatus` في PostgreSQL.

### 5.3 انتقالات الحالة — Domain methods

| Method | Status بعد | شروط |
|--------|------------|------|
| `CreateDraft(...)` | `Draft` | — |
| `MarkInTransit()` | `InTransit` | **بدون** تحقق سابق |
| `MarkArrived(date)` | `Arrived` | يضبط `ArrivalDate` |
| `AddItem(...)` | — | **يُمنع** إذا `Approved`/`InWarehouse`/`Closed` |
| `BeginReview()` | `UnderReview` | كل items `IsValid` |
| `SetLandingCost(lc)` | `LandingCostReviewed` | `TotalMeters > 0`؛ يستدعي `lc.MarkReviewed()` |
| `Approve(userId)` | `Approved` | `LandingCost.Status == Reviewed`؛ items valid |
| `MoveToWarehouse()` | `InWarehouse` | يجب `Approved` |
| `Close()` | `Closed` | **بدون** pre-check |
| `Archive()` | `Archived` | `IsArchived = true` |

```csharp
// ContainerAggregate.Approve
if (_landingCost is null || _landingCost.Status != LandingCostStatus.Reviewed)
    throw new ContainerApprovalException("Landing cost must be reviewed before container approval.");
```

```csharp
// ContainerCanBeApprovedSpecification
candidate.LandingCost?.Status != LandingCostStatus.Reviewed → fail
candidate.Items.Any(i => !i.IsValid) → fail
candidate.TotalMeters.Value <= 0 → fail
```

### 5.4 LandingCost sub-status

```csharp
// ERPSystem.Domain/Enums/ApprovalStatus.cs (partial file)
public enum LandingCostStatus { Draft = 0, Reviewed = 1, Approved = 2 }
```

`SetLandingCost` → يقفز مباشرة إلى `Reviewed` (يتخطى `Draft`).

### 5.5 هل يمكن تخطي مراحل؟

| Mechanism | الواقع |
|-----------|--------|
| `TransitionTo()` | **لا FSM** — أي method يمكن استدعاؤها دون التحقق من الحالة السابقة (ما عدا `MoveToWarehouse`, `Approve`, `AddItem`) |
| Stocktake قبل Landing Cost | **غير مطلوب** في Domain |
| Excel import قبل Arrived | **غير م enforced** |

### 5.6 Workflow موثّق (docs) vs كود

من `docs/ERP_PRO_DOMAIN_FOUNDATION.md` §6.1 — يطابق Domain intent، لكن **UI لا تنفّذ** الخطوات 4–8.

---

## 6. نموذج البيانات (Data Model)

### 6.1 Aggregate root

```csharp
// ERPSystem.Domain/Aggregates/ContainerAggregate.cs
public sealed class ContainerAggregate : AggregateRoot
{
    public ContainerNumber ContainerNumber { get; private set; }
    public Guid CompanyId, BranchId, SupplierId { get; private set; }
    public Guid? ChinaOrderId { get; private set; }          // PO reference — optional
    public ChinaContainerStatus Status { get; private set; }
    public DateTime ShipmentDate { get; private set; }
    public DateTime? ExpectedArrival, ArrivalDate { get; private set; }
    public int TotalRolls { get; private set; }
    public LengthInMeters TotalMeters { get; private set; }
    public WeightInKg? TotalWeight { get; private set; }
    public string? Port, Notes { get; private set; }
    // Collections: Items, ImportBatches, Distributions, LandingCost
}
```

### 6.2 كيانات فرعية — cost / qty / weight

| Class | حقول رئيسية |
|-------|-------------|
| `ChinaContainerItem` | `FabricItemId`, `FabricColorId`, `RollCount`, `LengthMeters`, `WeightKg?`, `BuyerCustomerId?`, `RowStatus` |
| `ChinaImportBatch` | `BatchNumber`, `FileName`, `ValidRowCount`, `ErrorRowCount` |
| `ChinaImportRow` | `RawData`, `ValidationErrors`, `IsAccepted` — **audit trail — غير مستخدم في UI** |
| `LandingCost` | `TotalLengthFromInvoice`, `ContainerWeight`, `CustomsAmountPaid`, `Shipping`, `Clearance`, `OtherExpenses`, computed per-meter fields, `Status` |
| `LandingCostExpense` | `ExpenseType`, `Amount` — **entity موجود، Handler يستخدم flat fields** |
| `ContainerCustomerDistribution` | `CustomerId`, `FabricItemId`, `FabricColorId`, `RollCount`, `Meters` |
| `ChinaOrder` | `OrderNumber`, `ChinaSupplierId`, `OrderDate`, `ApprovalStatus` — **منفصل عن Container** |

### 6.3 Persistence (PostgreSQL tables)

`ERPSystem.Infrastructure/Persistence/Models/ChinaImport/`:
- `ContainerEntity`, `ContainerItemEntity`
- `LandingCostEntity`, `LandingCostExpenseEntity`
- `ImportBatchEntity`, `ContainerDistributionEntity`

### 6.4 المورد و PO

| الربط | الواقع |
|-------|--------|
| `SupplierId` على Container | **required** في `CreateChinaContainerCommand` |
| `ChinaOrderId` | **optional** — لا UI لربط PO |
| Purchases module | **منفصل** — `PurchaseInvoice` enums موجودة لكن **لا integration** مع China Import في الكود |

---

## 7. الاتصال الحالي بـ PostgreSQL

### 7.1 ما هو مربوط فعلياً

| Handler / Repository | مسجّل DI؟ | أين يُستدعى؟ |
|---------------------|-----------|--------------|
| `IChinaContainerRepository` / `ChinaContainerRepository` | ✅ Infrastructure | Queries/Commands |
| `GetChinaContainerListHandler` | ✅ | **Sales** (`NewSalesInvoiceControl`, `SalesInvoiceListPageControl`, `WarehouseDetailingPageControl`) |
| `GetContainerOperationsCenterHandler` | ❌ **غير مسجّل** | — |
| `GetDashboardSummaryHandler` | ✅ | Dashboard — `PendingContainersCount` من DB |
| `CreateChinaContainerHandler` | ❌ | — |
| `CalculateLandingCostHandler` | ❌ | — |
| `ApproveContainerHandler` | ❌ | — |
| `MoveContainerToWarehouseHandler` | ❌ | — |
| `ImportContainerExcelHandler` | ❌ **غير موجود** | — |

### 7.2 شاشة China Import (`ChinaViews`)

```csharp
// Views/China/ChinaViews.cs
private static List<ImportContainerModel> _containers = ChinaImportSampleData.Generate(30);
// ...
page.BindData(_containers.Cast<object>().ToList());  // ⚠️ mock — لا GetChinaContainerListHandler
```

**تصحيح على التقرير السابق:** «جلب القائمة فقط» **لا ينطبق** على submodule `Containers` في China Import — القائمة **mock**.  
`GetChinaContainerListHandler` **موجود ويقرأ PostgreSQL** لكن **consumers خارج** وحدة China Import UI.

### 7.3 Dashboard KPI للحاويات (DB حقيقي)

```csharp
// GetDashboardSummaryHandler
PendingContainersCount = containers.Count(c =>
    c.Status is ChinaContainerStatus.Draft or
    ChinaContainerStatus.UnderReview or
    ChinaContainerStatus.LandingCostReviewed),
```

---

## 8. مشاكل معروفة / Tech Debt

### 8.1 TODO / توثيق صريح

| المصدر | النص |
|--------|------|
| `docs/ERP_PRO_APPLICATION_LAYER_REPORT.md` | «Add `ImportContainerExcelHandler` implementation» |
| `docs/ui-audit-report.md` | «⏳ دمج ExcelReview مع NewImport» |
| `Services/SalesDetailingQueueHub.cs` | تعليق legacy — خارج نطاق China |

**لا TODO comments** داخل ملفات `Views/China/` أو `ContainerHandlers.cs`.

### 8.2 Fragile / Hardcoded

| # | الم issue |
|---|-----------|
| 1 | **Dual status enums** — UI `ContainerStatus` ≠ Domain `ChinaContainerStatus` |
| 2 | **`RowStatus` mismatch** — UI «صحيح» vs Domain `"Valid"` |
| 3 | **Waste 0.97** hardcoded في summary بينما form فيه «نسبة الهالك %» |
| 4 | **`OperationsCenterFactory`**: `CustomsAmountPaid = c.ImportCost * 0.4m` — **40% magic** |
| 5 | **لا Excel parser** رغم وجود Command/entities جاهزة |
| 6 | **Handlers غير مسجّلة** — Domain جاهز لكن Application DI ناقص |
| 7 | **`SetLandingCost` يستدعي `MarkReviewed(Guid.Empty)`** — user audit trail فارغ |
| 8 | **`TransitionTo` بدون FSM** — قفزات حالة غير منضبطة |
| 9 | **`CalculateLandedCostPerMeter`** يحتاج `fabricUnitCost` — **غير موجود** في flow الاستيراد |
| 10 | **`ExpenseCostPerMeter` includes customs** — خطر double-count إذا UI جمعت customs منفصلة + expenses |
| 11 | **`ImportContainerLineCommand` بدون `WeightKg`** رغم وجوده في Excel UI |
| 12 | **`LandingCostExpense` table** — entity منفصلة غير متزامنة مع flat fields في Handler |
| 13 | **Distribution / Stocktake** — zero backend |
| 14 | **Seed DB** — لا containers seeded (فقط warehouse/fabric) — قائمة DB فارغة افتراضياً |

### 8.3 Handlers موجودة vs ناقصة

| Command | Handler |
|---------|---------|
| `CreateChinaContainerCommand` | ✅ `CreateChinaContainerHandler` |
| `CalculateLandingCostCommand` | ✅ `CalculateLandingCostHandler` |
| `ApproveContainerCommand` | ✅ `ApproveContainerHandler` |
| `MoveContainerToWarehouseCommand` | ✅ `MoveContainerToWarehouseHandler` |
| `ImportContainerExcelCommand` | ❌ **missing** |
| `BeginReview` / `MarkInTransit` / … | ❌ **no commands** |

---

## ملحق: خريطة ملفات رئيسية

```
Views/China/ChinaViews.cs              ← كل UI China Import (mock)
Core/ChinaImport/ChinaImportModels.cs  ← UI models + ChinaImportSampleData
Core/Domain/FabricDomainModels.cs      ← ContainerLandingCost (UI formulas)
ERPSystem.Domain/Aggregates/ContainerAggregate.cs
ERPSystem.Domain/Entities/ChinaImport/ChinaImportEntities.cs
ERPSystem.Domain/Services/LandingCostCalculator.cs
ERPSystem.Application/Commands/Containers/ContainerCommands.cs
ERPSystem.Application/UseCases/Containers/ContainerHandlers.cs
ERPSystem.Application/UseCases/Queries/OperationsQueryHandlers.cs  ← GetChinaContainerListHandler
ERPSystem.Infrastructure/Repositories/AggregateRepositories.cs     ← ChinaContainerRepository
```

---

## توصيات للـ Vertical Slice القادم (خارج نطاق هذا التقرير)

1. توحيد `ContainerStatus` UI مع `ChinaContainerStatus` + mapper عربي.
2. تنفيذ `ImportContainerExcelHandler` + parser + mapping FabricCode→GUID.
3. تسجيل Container handlers في DI + `ContainerUiService` (نمط Customers).
4. استبدال `_containers` mock في `ChinaViews` بـ `GetChinaContainerListHandler`.
5. حسم: هل `fabricUnitCost` (CNY) جزء من Landing Cost؟ إذا نعم — إضافة حقول + exchange rate per container.
6. توضيح semantics `ExpenseCostPerMeter` vs `CustomsCostPerMeter` لتجنب double-count.
7. ربط `WastePercent` أو حذفه من UI.
8. Distribution/Stocktake: إما تأجيل أو تعريف commands/domain rules قبل UI.

---

*نهاية التقرير — تم إنتاجه من قراءة الكود المصدري فقط، بدون تعديلات.*
