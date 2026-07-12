# جسر استيراد الصين ↔ المشتريات (China Import ↔ Purchases Bridge)

**التاريخ:** 2026-07-12  
**الحالة:** مُنفَّذ — يتطلب migration على قاعدة البيانات

---

## 1. المشكلة التي حُلّت

| الوحدة | الدور السابق | المشكلة |
|--------|-------------|---------|
| **استيراد الصين** | تشغيلي: Excel → حاوية → مخزن → أثواب | لا يظهر في المشتريات |
| **المشتريات** | مالي: فواتير شراء + ذمم موردين | فارغ رغم وجود حاويات معتمدة |

**السبب:** جدولان منفصلان (`Containers` vs `purchase_invoices`) بدون ربط تلقائي.  
اعتماد الحاوية كان يرحّل **Landing Cost** فقط إلى AP دون **فاتورة المورد الصيني** (فجوة F-01).

---

## 2. الحل المعماري

```
استيراد الصين (وجه تشغيلي)          المشتريات (وجه مالي)
─────────────────────────          ─────────────────────
Excel → Container → Approve   →    PurchaseInvoice (SourceContainerId)
         ↓                              ↓
    استيراد المخزن                 AP + رصيد المورد
    (FabricRolls)                  (ظاهر في فواتير الشراء)
         ↓
    GL: LandingCostClearing → Inventory (عند التحويل للمخزن)
```

### قرارات التصميم

1. **`SourceContainerId`** على `PurchaseInvoice` — فهرس فريد (nullable) يربط 1:1 مع الحاوية.
2. **عند اعتماد الحاوية:** إنشاء + ترحيل `PurchaseInvoice` تلقائياً (فشل الاعتماد إن تعذّر).
3. **إلغاء `PostContainerApprovalAsync`** المنفصل — تكلفة الوصول تُدمج في بنود الفاتورة.
4. **تخطي ترحيل المخزون** من فاتورة الشراء إذا `SourceContainerId` مُعرّف (المخزون يأتي من استيراد الحاوية).
5. **قيود GL للفاتورة من حاوية:** بنود المخزون تُDebit `LandingCostClearing` بدلاً من `InventoryAsset`.
6. **Backfill:** للحاويات المعتمدة سابقاً — إنشاء الفاتورة + رصيد المورد؛ **تخطي GL** إن وُجدت قيود `ChinaContainer` مُرحّلة.

---

## 3. بنود فاتورة الشراء من الحاوية

| المصدر | نوع البند | حساب GL |
|--------|-----------|---------|
| Fabric Type Lines (مبلغ USD + قماش) | Inventory line | LandingCostClearing |
| Fabric Type Lines (بدون FabricItemId) | Expense line | LandingCostClearing |
| Fallback: `ChinaInvoiceAmountUsd` | Expense line | LandingCostClearing |
| Landing Cost (جمارك، شحن، …) | Expense lines | LandingCostClearing |

**العملة:** USD  
**مرجع المورد:** رقم الحاوية  
**ملاحظات:** `فاتورة مورد صيني — حاوية {رقم} — جسر استيراد الصين ↔ المشتريات`

---

## 4. الملفات الرئيسية

| الملف | الدور |
|-------|------|
| `ERPSystem.Domain/.../PurchasingEntities.cs` | `SourceContainerId`, `IsFromChinaContainer` |
| `ERPSystem.Application/.../IChinaContainerPurchaseBridgeService.cs` | واجهة الجسر |
| `ERPSystem.Application/.../ChinaContainerPurchaseBridgeService.cs` | بناء البنود + إنشاء/ترحيل + backfill |
| `ERPSystem.Application/.../ContainerHandlers.cs` | `ApproveContainerHandler` يستدعي الجسر |
| `ERPSystem.Application/.../PurchaseHandlers.cs` | تخطي مخزون؛ handler الـ backfill |
| `ERPSystem.Infrastructure/.../IntegratedAccountingService.cs` | LandingCostClearing للفواتير من حاوية |
| `ERPSystem.Infrastructure/Migrations/20260723130000_AddPurchaseInvoiceSourceContainerId.cs` | Migration |

### واجهة المستخدم (WPF)

| الشاشة | التغيير |
|--------|---------|
| **فواتير الشراء** | عمود «المصدر»؛ زر «ربط حاويات معتمدة»؛ رسالة empty state |
| **مركز عمليات فاتورة الشراء** | عرض الحاوية المرتبطة؛ quick action «فتح الحاوية» |
| **مركز عمليات الحاوية** | quick action «فاتورة الشراء» عند وجود رابط |

---

## 5. خطوات التشغيل للمستخدم

### حاوية جديدة (سير طبيعي)

1. استيراد الصين → إنشاء حاوية → Landing Cost → **اعتماد**
2. تُنشأ فاتورة شراء **مرحّلة** تلقائياً في **المشتريات → فواتير الشراء**
3. المصدر يظهر: `حاوية {رقم}`
4. من مركز عمليات الحاوية: **فاتورة الشراء** يفتح الفاتورة

### حاويات معتمدة سابقاً (Backfill)

1. **المشتريات → فواتير الشراء**
2. زر **«ربط حاويات معتمدة»**
3. تأكيد العملية
4. تُنشأ فواتير للحاويات (Approved / InWarehouse / Closed) بدون فاتورة مرتبطة
5. إن كانت القيود المحاسبية للحاوية مُرحّلة مسبقاً → **لا يُكرّر قيد GL**

---

## 6. Migration

```bash
# على VPS بعد النشر
dotnet ef database update --project ERPSystem.Infrastructure --startup-project ERPSystem.Api
```

أو عبر `deploy-app.sh` إن كان يطبّق migrations تلقائياً.

**عمود جديد:** `purchasing.purchase_invoices.SourceContainerId` (uuid, nullable, unique filtered index)

---

## 7. التحقق المحاسبي

بعد backfill أو اعتماد حاوية جديدة:

- [ ] فاتورة الشراء ظاهرة في قائمة المشتريات
- [ ] `المصدر = حاوية {رقم}`
- [ ] رصيد المورد (AP) يزيد بمبلغ الفاتورة
- [ ] قيد GL: Dr LandingCostClearing / Cr AP (للحاويات الجديدة)
- [ ] **لا** قيد GL مكرر للحاويات التي لها `ChinaContainer` journals مُرحّلة
- [ ] المخزون يُحدَّث عند **تحويل الحاوية للمخزن** وليس من فاتورة الشراء

---

## 8. Baseline متوقع

لا يجب أن يتغيّر رصيد المخزون التشغيلي من الجسر وحده — التغيير في **AP والذمم**.  
Baseline AR / Inv Operational / Inv GL يُقاس قبل/بعد backfill على بيئة اختبار.

---

## 9. API (للمستقبل)

يمكن إضافة endpoints:

- `GET /api/v1/purchases/invoices/by-container/{containerId}`
- `POST /api/v1/purchases/backfill-china-containers`

حالياً المنطق متاح عبر Application Layer وWPF فقط.
