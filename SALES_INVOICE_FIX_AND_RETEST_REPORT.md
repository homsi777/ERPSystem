# تقرير إصلاح وإعادة اختبار رحلة فاتورة المبيعات

تاريخ التنفيذ: 2026-07-16  
الخادم الحي: `65.21.136.217:2727`  
قاعدة البيانات: `erp_pro`  
الخدمة: `erpsystem-api.service`  
نسخة الإصلاح: `9102f010e7692391a0898725fc9d72059309eacb`

## 1. ملخص تنفيذي

تم إصلاح رحلة فاتورة البيع النقدية ونشرها فعليًا على الخادم. أصبحت الواجهة ترسل `CashboxId`، وأصبح الحجز مرتبطًا بثوب محدد، ولا يقبل التفصيل البيع بالطول فقط، ويُباع الثوب المختار كاملًا ثم يصبح `Sold` بطول متبقٍ صفر. أضيف عكس ذري بصلاحية مدير يعكس قيد الفاتورة وسند القبض والصندوق وذمة العميل وحركة المخزون، ويعيد الثوب المحدد نفسه.

تم تنفيذ اختبار حي كامل بلا أي تعديل SQL للفاتورة الجديدة. الفاتورة `TEST_FIX_INV_20260716180930` باعت الثوب رقم `137` كاملًا بطول `109.7280m`، ثم عُكست. عادت القيم حرفيًا إلى خط الأساس: الصندوق `5000.00`، AR=`0.00`، المخزون التشغيلي `92908.559245248`، وInventory GL=`20541.00`.

لم تُعدّل ملفات CSS أو بنية التصميم. التغيير المرئي الوحيد وظيفي داخل نموذج البيع الحالي: اختيار الصندوق باستخدام نفس مكونات النموذج الموجودة.

## 2. الإصلاحات المنفذة

### Fix 1 — فجوة CashboxId

- `ERPSystem.Api/Endpoints/SalesEndpoints.cs:108-131,288-297`: أضيف `CashboxId` إلى عقد إنشاء الفاتورة وتم تمريره إلى `CreateSalesInvoiceDraftCommand`.
- `web-client/src/api/types.ts:998-1008` و`web-client/src/pages/Sales.tsx:401,426-429,606,638-640,735-739`: تحميل الصناديق، اختيار الصندوق، إرساله، ومنع إرسال بيع نقدي أو دفعة جزئية بدونه.
- تحقق الاعتماد الخلفي بقي عند حد الاعتماد في `SalesInvoiceHandlers.cs:489-500`، لذلك لا يمكن تجاوز الشرط من عميل آخر.
- WPF يستخدم نفس الأمر المشترك الذي كان يدعم `CashboxId` أصلًا؛ لم يحدث تغيير تصميمي أو انحدار في عقده.

### Fix 2 — الحجز والثوب المحدد وقاعدة البيع الكامل

- `ERPSystem.Infrastructure/Services/InventoryEngine.cs:425-483`: أصبح لكل ثوب حجز مستقل يحمل `FabricRollId` و`RollCount=1` واستراتيجية `SpecificRoll`.
- `InventoryEngine.cs:531`: التفصيل يرفض صراحة الإدخال بلا رقم ثوب؛ لم يعد مسار «طول فقط» صالحًا.
- `InventoryEngine.cs:573-606`: عند تبديل الثوب في التفصيل يُحرر الثوب المحجوز سابقًا وتُنقل كمية الحجز إلى الثوب المحدد الجديد.
- `InventoryEngine.cs:672-728`: الاعتماد لا يبحث عن ثوب بديل؛ يلزم `FabricRollId` المحجوز نفسه، ويحوّله دائمًا إلى `Sold` و`RemainingLengthMeters=0`، ثم يجعل سجل الحجز `Sold`.

### Fix 3 — العكس الذري

- `ERPSystem.Api/Endpoints/SalesEndpoints.cs:32,183-196`: endpoint جديد `POST /api/v1/sales/invoices/{id}/reverse`.
- `ERPSystem.Infrastructure/Seed/DatabaseSeeder.cs:119,588`: صلاحية `sales.reverse` وتُمنح لدور الإدارة المزروع.
- `ERPSystem.Application/UseCases/Sales/SalesInvoiceHandlers.cs:704-849`: معاملة واحدة تعكس سند القبض والتخصيص والصندوق والعميل والمخزون وقيد الفاتورة، ثم تغيّر حالة الفاتورة إلى `Reversed=10`.
- `ERPSystem.Infrastructure/Services/AccountingPostingEngine.cs:147-190`: تنفيذ فعلي لـ`ReverseAsync` مع idempotency وقيد مقابل يحمل `ReversalOfEntryId`.
- `ERPSystem.Infrastructure/Services/InventoryEngine.cs:842-945`: إعادة الثوب المحدد، حركة مخزون موجبة مقابلة، وإغلاق الحجوزات، إضافةً إلى إصلاح idempotent للحجوزات التراثية القديمة.
- `ERPSystem.Domain/Aggregates/SalesInvoiceAggregate.cs:356-378`: حفظ حالة العكس و`ReversedByJournalEntryId` وسبب العكس.
- `ERPSystem.Infrastructure/Repositories/PartyRepositories.cs:23-28,275-280`: العكس قادر على معالجة فاتورة عميل معطّل من دون إعادة تفعيله.

### Fix 4 — أطوال الأثواب في قائمة الفواتير

- `ERPSystem.Infrastructure/Repositories/AggregateRepositories.cs:449-461`: قائمة الفواتير تحمل `sales_invoice_roll_details` بدل تمرير قائمة فارغة إلى mapper.
- الإثبات الحي: `GET /api/v1/sales/invoices?page=1&pageSize=50` أعاد `totalLengthMeters=109.7280` و`rollLengths=[109.7280]` للفاتورة التجريبية.

### Fix 5 — قيمة المخزون في WPF

- `ERPSystem.Infrastructure/Repositories/InventoryManagementRepository.cs:86-96,223-271,304-330`: القيمة أصبحت مجموع `RemainingLengthMeters * CostPerMeter` للأثواب `Available` نفسها، بدل `AvailableMeters * AverageCost`.
- بناء WPF النهائي نجح بلا أخطاء.
- نفس مصدر البيانات الذي تستهلكه لوحة WPF أعاد حيًا عبر `/api/v1/inventory/dashboard`:

```json
{"totalInventoryValue":92908.5592452480000000,"totalRolls":459,"totalMeters":50255.4240,"reservedMeters":0.0000}
```

وهذا يساوي استعلام SQL التشغيلي حرفيًا: `92908.5592452480000000`.

## 3. أي انحراف عن قاعدة العمل

لا يوجد انحراف في المسار المدعوم بعد الإصلاح:

1. الإنشاء يحدد عدد الأثواب فقط، والإرسال يحجز ثوبًا فعليًا محددًا لكل RollDetail.
2. التفصيل هو المكان الوحيد الذي يستطيع تبديل الثوب وتسجيل قياسه الكامل.
3. أي طلب تفصيل بلا `RollNumber` يُرفض.
4. الاعتماد يستخدم `FabricRollId` المثبت فقط، ولا ينفذ FIFO بديلًا أو خصمًا جزئيًا.

ملاحظة مكتشفة أثناء تنظيف اختبار التدقيق القديم: الفاتورة القديمة سبقت الإصلاح وكانت تحمل حجزًا عامًا بلا `FabricRollId`. عالج مسار العكس الجديد هذه الحالة idempotently، وأعاد الثوبين `1` و`137` إلى `Available/109.7280` قبل بدء الاختبار الجديد.

## 4. نتائج إعادة الاختبار

### بيانات الاختبار

```text
CustomerId = 93feacf2-c4d0-4c52-87ab-120d830a96bc
InvoiceId  = 38b51678-2785-4d51-8f81-2d4eba6f2093
InvoiceNo  = TEST_FIX_INV_20260716180930
CashboxId  = 66666666-6666-6666-6666-666666666666
FabricRoll = 00b9234c-c2bf-4f7d-b10c-af276aeb510a / RollNumber 137
Length     = 109.7280m
UnitPrice  = 2.00
GrandTotal = 219.46
```

### تسلسل HTTP الفعلي

```text
POST /api/v1/customers                                      => 200 + CustomerId
POST /api/v1/sales/invoices                                => 200 + InvoiceId
POST /api/v1/sales/invoices/{id}/send-to-warehouse         => 204
POST /api/v1/detailing/{id}/complete (roll=137,length=109.728) => 204
POST /api/v1/sales/invoices/{id}/approve                   => 204
GET  /api/v1/sales/invoices/{id}                           => Status=4, CashboxId موجود
POST /api/v1/sales/invoices/{id}/reverse                   => 204
POST /api/v1/customers/{customerId}/deactivate             => 204
```

### الدليل الخام بعد الاعتماد

```text
Invoice Status=4 | CashboxId=6666... | GrandTotal=219.46
RollDetail FabricRollId=00b9234c... | LengthMeters=109.7280
FabricRoll 137 RemainingLengthMeters=0 | Status=2 (Sold) | ReservationStatus=5 (Sold)
Reservation FabricRollId=00b9234c... | ReservedMeters=109.7280 | RollCount=1 | Status=5 | Strategy=3
Cashbox Balance=5219.46
Customer Balance=0.00
Sale movement QuantityMeters=-109.7280 | TotalValue=-199.5653
Invoice journal JE-MAIN-000010 | Debit=419.03 | Credit=419.03
List API totalLengthMeters=109.7280 | rollLengths=[109.7280]
```

لم يبق أي ثوب `Reserved` لهذا الاختبار بعد الاعتماد؛ الثوب FIFO رقم `1` حُرر عند اختيار الثوب `137`.

## 5. نتيجة اختبار العكس

| المؤشر | قبل الفاتورة | بعد الاعتماد | بعد العكس |
|---|---:|---:|---:|
| الصندوق CASH-MAIN | 5000.00 | 5219.46 | 5000.00 |
| AR GL | 0.00 | صافي الفاتورة النقدية 0.00 للعميل؛ الإجمالي العام تأثر بقيود الاختبار القديم قبل إصلاحه | 0.00 |
| المخزون التشغيلي | 92908.559245248 | 92708.993968992 | 92908.559245248 |
| Inventory GL | 20541.00 | 20343.25 | 20541.00 |
| الثوب 137 | Available / 109.7280 | Sold / 0 | Available / 109.7280 |
| العميل التجريبي | 0.00 | 0.00 | 0.00 ثم معطّل |

الدليل الخام النهائي:

```text
Invoice Status=10 (Reversed)
ReversedByJournalEntryId=4cd733a3-086d-42c0-b9f1-767a09b91eb6

SAL-TEST_FIX_INV...    Type=2  Quantity=-109.7280 Value=-199.5653
SALREV-TEST_FIX_INV... Type=14 Quantity=+109.7280 Value=+199.5653

JE-MAIN-000010 Status=2 Debit=419.03 Credit=419.03
JE-MAIN-000013 Status=2 Debit=419.03 Credit=419.03
JE-MAIN-000013.ReversalOfEntryId=ec1070d4-f576-a658-8e93-89a0cb2272fe

Cashbox=5000.00
AR GL=0.00
Operational Inventory=92908.5592452480000000
Inventory GL=20541.00
ReservedMeters=0.0000
```

إعادة إرسال طلب العكس أعادت `204` ولم تنشئ أثرًا ماليًا أو مخزنيًا مضاعفًا؛ استُخدمت فقط للتحقق من idempotency واتساق الحجز.

## 6. مشاكل متبقية أو مؤجلة وسببها

1. بناء المشاريع المطلوبة نجح: `ERPSystem.Api` وWPF وReact. كما نجحت مجموعة اختبارات التطبيق قبل النشر: `102 passed / 0 failed / 1 skipped`. إعادة تشغيل المجموعة لاحقًا علقت بسبب اختبار/عملية بيئة محلية ولم تنتج فشل assertion؛ لم يُعتبر ذلك فشلًا وظيفيًا لأن الاختبار الحي الكامل وعمليات البناء النهائية نجحت.
2. بناء الحل الكامل `ERPSystem.sln` ما زال يفشل في مشروع مستقل غير داخل نطاق رحلة البيع: `ERPSystem.DocumentEngine/Templates/ReceiptVoucher/ReceiptVoucherTemplate.cs` بسبب النوع المفقود `RenderContext`. لم يُعدّل هذا المشروع.
3. `GET /api/v1/detailing/{invoiceId}` أعاد `warehouseId=00000000-...` رغم أن الفاتورة تحمل المستودع الصحيح. لم يؤثر ذلك على الحجز أو الاعتماد لأن العمليات استخدمت `invoice.WarehouseId` الصحيح، لكنه عيب DTO عرضي مؤجل خارج الإصلاحات الخمسة.

الخدمة النهائية `active` و`/health` أعاد `OK` بعد آخر نشر. الويب منشور في `/var/www/alamal-ab.org` والـAPI في `/opt/erpsystem/api`.
