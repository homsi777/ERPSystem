# تدقيق حي لمسار فاتورة البيع النقدية والترحيلات المالية وظهورها للمدير

تاريخ الفحص: 2026-07-16 (Asia/Damascus)  
الخادم: `65.21.136.217:2727`  
عنوان الخروج الذي نُفذ منه الفحص: `159.26.98.241`

## ملخص تنفيذي

نجح اعتماد فاتورة نقدية حية بمبلغ `2.00 USD` وأنشأ سند قبض، حركة مخزون، وقيدي GL متوازنين، وبقي رصيد العميل صفراً.  
لكن الرحلة لا تعمل سليمة من طرف إلى طرف: API إنشاء الفاتورة لا يقبل `CashboxId`، واضطر الفحص إلى تعيين صندوق فاتورة الاختبار مباشرة في SQL قبل الاعتماد.  
كما بقي ثوبان محجوزين بعد بيع متر واحد، فانخفض المخزون التشغيلي بمقدار `399.130552512` بدلاً من COGS المتوقع `1.818727`، ولا يوجد مسار عكس فعلي لتنظيف فاتورة مرحّلة.  
ظهرت الفاتورة والصندوق بصورة صحيحة في WPF، وتطابقت استجابات HTTP، لكن تعذّر التحقق البصري من الويب لأن متصفح Codex المدمج لم يكن متاحاً.

## بيئة الفحص

- خدمة API: `erpsystem-api.service` وكانت `active`.
- العملية الحية: `/usr/local/bin/dotnet /opt/erpsystem/api/ERPSystem.Api.dll`، PID وقت الفحص `115145`.
- وقت دخول الخدمة الحالة النشطة: `2026-07-16 13:17:28 UTC`.
- DLL المختبر: `/opt/erpsystem/api/ERPSystem.Api.dll`، الحجم `254976` بايت، mtime=`2026-07-16 13:17:14.573790797 +0000`.
- قاعدة البيانات الفعلية: PostgreSQL 16، قاعدة `erp_pro`.
- الشركة: `11111111-1111-1111-1111-111111111111`، الكود `ALAMAL-AB`، الاسم `الأمل.AB — تجارة أقمشة الجينز`.
- الفرع: `22222222-2222-2222-2222-222222222222` (`MAIN`).
- المستودع: `55555555-5555-5555-5555-555555555555` (`المستودع الرئيسي`).
- الشجرة المنشورة المستخدمة فعلياً: `/opt/erpsystem/src` عند commit `5191e8bdb317a213a1351ba252b50286d6ac2d06`، تاريخ commit `2026-07-16T16:15:57+03:00`.
- موضوع commit: `fix: align delivery permissions and auto-grant warehouse.detailing for delivery roles`.
- نسخة المقارنة `~/ERPSystem` مختلفة: `e9af1a4572497d97ae3391180a32963e87b56a3b` بتاريخ `2026-07-11T02:10:27+03:00`.
- نسخة WPF المحلية التي اتصلت بقاعدة VPS الحية كانت عند commit نفسه `5191e8bd...`، واتصلت عبر `localhost:5433` إلى SSH `65.21.136.217:2727`.
- Nginx يخدم الويب من `/var/www/alamal-ab.org`؛ mtime لملف `index.html` هو `2026-07-16 13:17:26.419795579 +0000`، ويمرر `/api` إلى `127.0.0.1:4066`.

الدليل الخام للبيئة:

```text
=== SERVICE ===
active
MainPID=115145
ExecStart={ path=/usr/local/bin/dotnet ; argv[]=/usr/local/bin/dotnet /opt/erpsystem/api/ERPSystem.Api.dll ... }
ActiveEnterTimestamp=Thu 2026-07-16 13:17:28 UTC

=== DEPLOYED TREE ===
5191e8bdb317a213a1351ba252b50286d6ac2d06
COMMIT=5191e8bdb317a213a1351ba252b50286d6ac2d06
DATE=2026-07-16T16:15:57+03:00
SUBJECT=fix: align delivery permissions and auto-grant warehouse.detailing for delivery roles

=== WORKING COPY ===
e9af1a4572497d97ae3391180a32963e87b56a3b
DATE=2026-07-11T02:10:27+03:00

=== CONNECTION ===
/opt/erpsystem/api/appsettings.json:
Host=localhost;Port=5432;Database=erp_pro;Username=postgres;Password=<REDACTED>
```

## الفاتورة التجريبية المستخدمة

- العميل: `TEST_AUDIT_20260716141337`.
- Customer ID: `8046e3c6-f8fc-420c-b61d-173db6f1bf58`.
- نوع العميل: نقدي (`Type=0`)؛ الرصيد قبل الاعتماد `0.00`.
- رقم الفاتورة: `TEST_AUDIT_INV_20260716141337`.
- Invoice ID: `e9b1459b-f7f9-4897-9ede-c36f54ea13b2`.
- النوع: نقدي (`PaymentType=0`).
- الصندوق: `CASH-MAIN`، ID=`66666666-6666-6666-6666-666666666666`.
- الثوب الفعلي: ID=`00b9234c-c2bf-4f7d-b10c-af276aeb510a`، الرقم `137`.
- الحاوية: ID=`8747238e-fe5b-43e8-b9e0-b6b1d9a0bb51`، الرقم `124`.
- الصنف: `5531f911-6959-4ce9-9ef7-3d07312327b0`، اللون `89c90db8-4ee8-4b5d-b20b-6bc806cce601`.
- الطول المباع: `1.0000m` من `109.7280m`.
- سعر البيع: `2.00 USD/m`.
- التكلفة: `1.818727 USD/m`.
- الإجمالي المتوقع والفعلي: `2.00 USD`، الضريبة `0.00`.
- COGS المتوقع: `1.818727 USD`؛ القيد رحّل `1.82 USD` بعد التقريب.

الحالة قبل الاعتماد:

```text
Id                                   | InvoiceNumber                  | PaymentType | CashboxId                            | Status | SubTotal | TaxTotal | GrandTotal
e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | TEST_AUDIT_INV_20260716141337 | 0           | 66666666-6666-6666-6666-666666666666 | 2      | 2.00     | 0.00     | 2.00

RollDetailId                         | FabricRollId                         | LengthMeters | RollNumber | RemainingLengthMeters | CostPerMeter   | ExpectedCOGS
432219d1-5d84-458d-8e3f-180d17f99a5e | 00b9234c-c2bf-4f7d-b10c-af276aeb510a | 1.0000       | 137        | 109.7280              | 1.818727000000 | 1.8187270000000000
```

ملاحظة لازمة لإعادة الإنتاج: طلب HTTP لإنشاء الفاتورة نجح لكنه أنشأ `CashboxId=NULL`. جرى تحديث **سجل فاتورة الاختبار وحده** بشرط ID ورقم يبدأ بـ`TEST_AUDIT_` إلى الصندوق `CASH-MAIN` قبل الاعتماد:

```text
=== TEST INVOICE CREATED / API GAP PROOF ===
Invoice ID                           | PaymentType | CashboxId | Status | GrandTotal
e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | 0           |           | 0      | 0.00

=== SET CASHBOX ON TEST INVOICE ONLY ===
UPDATE 1
Invoice ID                           | CashboxId                            | Status
e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | 66666666-6666-6666-6666-666666666666 | 0
```

## نتائج الفحص لكل نقطة تكامل

### 1. الصندوق وسند القبض — ✅ نجح بعد تجاوز فجوة `CashboxId`

قبل الاعتماد:

```text
CashboxId                            | Code      | Balance
66666666-6666-6666-6666-666666666666 | CASH-MAIN | 5000.00

receipt_count
0
```

بعد الاعتماد:

```text
CashboxId                            | Code      | Balance | UpdatedAt
66666666-6666-6666-6666-666666666666 | CASH-MAIN | 5002.00 | 2026-07-16 14:15:44.960233+00

ReceiptVoucherId                     | VoucherNumber   | CustomerId                           | CashboxId                            | Amount | Status | PostedAt
5b105d2e-e045-42c1-8541-348dc9aa2131 | RCP-MAIN-000006 | 8046e3c6-f8fc-420c-b61d-173db6f1bf58 | 66666666-6666-6666-6666-666666666666 | 2.00   | 2      | 2026-07-16 14:15:44.879891+00

TenderLineId                         | ReceiptVoucherId                     | CashboxId                            | Amount | BaseAmount | Currency
fa42441a-c60d-4784-8ae1-2a2f3708d5af | 5b105d2e-e045-42c1-8541-348dc9aa2131 | 66666666-6666-6666-6666-666666666666 | 2.00   | 2.00       | USD

PaymentAllocationId                  | SalesInvoiceId                       | ReceiptVoucherId                     | Amount
9f8be417-c1f5-4e57-932a-4cade5e88f73 | e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | 5b105d2e-e045-42c1-8541-348dc9aa2131 | 2.00
```

قيد سند القبض منفصل عن قيد الفاتورة ومربوط بمصدر سند القبض:

```text
EntryNumber    | SourceType | SourceId                              | Code | Account          | Debit | Credit | PartyId
JE-MAIN-000007 | 4          | 5b105d2e-e045-42c1-8541-348dc9aa2131 | 1010 | الصندوق — USD   | 2.00  | 0.00   |
JE-MAIN-000007 | 4          | 5b105d2e-e045-42c1-8541-348dc9aa2131 | 1100 | ذمم عملاء       | 0.00  | 2.00   | 8046e3c6-f8fc-420c-b61d-173db6f1bf58
```

### 2. المخزون — ❌ حركة الصرف صحيحة، لكن الحجز المتبقي غير صحيح

قبل الاعتماد، بعد الإرسال والتفصيل:

```text
FabricRollId                         | RemainingLengthMeters | Status
00b9234c-c2bf-4f7d-b10c-af276aeb510a | 109.7280              | 1

WarehouseStockId                     | RollCount | TotalMeters | ReservedMeters | AvailableMeters
89adf9b0-2331-4b24-b70e-525b1cddf00e | 229       | 25127.7120  | 109.7280       | 25017.9840
```

بعد الاعتماد:

```text
FabricRollId                         | RemainingLengthMeters | Status | CostPerMeter
00b9234c-c2bf-4f7d-b10c-af276aeb510a | 108.7280              | 1      | 1.818727000000

WarehouseStockId                     | RollCount | TotalMeters | ReservedMeters | AvailableMeters
89adf9b0-2331-4b24-b70e-525b1cddf00e | 229       | 25126.7120  | 108.7280       | 25017.9840

MovementId                           | MovementNumber                                    | Type | ReferenceType | ReferenceId                           | Status
c66ef591-62eb-4384-9f59-8eb0c2689b51 | SAL-TEST_AUDIT_INV_20260716141337-20260716141544 | 2    | 0             | e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | 1

MovementLineId                       | FabricRollId                         | QuantityMeters | UnitCost | TotalValue
bd5110ae-4980-495f-9634-28b8e4e30c06 | 00b9234c-c2bf-4f7d-b10c-af276aeb510a | -1.0000        | 1.8187   | -1.8187
```

المشكلة المثبتة: حجز FIFO الأول أبقى الثوب رقم `1` محجوزاً، ثم التفصيل بالرقم `137` حجز ثوباً آخر. بعد الاعتماد بقي كلاهما `Reserved`:

```text
FabricRollId                         | RollNumber | RemainingLengthMeters | CostPerMeter   | Status | test_detail_length
0452ffc9-91d1-4e47-af12-890f7110e119 | 1          | 109.7280              | 1.818727000000 | 1      | 0
00b9234c-c2bf-4f7d-b10c-af276aeb510a | 137        | 108.7280              | 1.818727000000 | 1      | 1.0000

reserved_rolls | reserved_value
2              | 397.3118255120000000
```

المواضع المنشورة المرتبطة بالخلل:

- `/opt/erpsystem/src/ERPSystem.Infrastructure/Services/InventoryEngine.cs:439-480`: حجز أول أثواب FIFO وتسجيل Reservation بلا `FabricRollId`.
- الملف نفسه `:520-572`: التفصيل يقبل ثوباً `Available` آخر ويحوّله إلى `Reserved` دون تحرير FIFO السابق.
- الملف نفسه `:677-701`: بعد الصرف لا يعود الثوب الجزئي إلى `Available` ولا تُحرر بقية الحجز؛ يتغير إلى `Sold` فقط إن أصبح المتبقي صفراً.

### 3. ذمة العميل — ✅ صافي التغير صفر

```text
قبل الاعتماد:
CustomerId                           | Balance | Type | Status
8046e3c6-f8fc-420c-b61d-173db6f1bf58 | 0.00    | 0    | 0

بعد الاعتماد:
CustomerId                           | Balance | UpdatedAt
8046e3c6-f8fc-420c-b61d-173db6f1bf58 | 0.00    | 2026-07-16 14:15:45.009501+00
```

قيد الفاتورة مدين للذمم `2.00` وقيد سند القبض دائن للذمم `2.00` لنفس `PartyId`؛ لذلك بقي صافي AR للعميل صفراً كما هو متوقع للبيع النقدي.

### 4. دفتر الأستاذ والقيد — ✅ متوازن

استجابة الاعتماد من API العام:

```http
HTTP/1.1 204 No Content
Date: Thu, 16 Jul 2026 14:15:45 GMT
```

قيد الفاتورة المرتبط مباشرة بـ`SourceId=InvoiceId`:

```text
EntryId                              | EntryNumber    | Status | SourceType | SourceId                              | PostedAt
77dfafb4-75f8-0f50-914f-3f30fa44ba23 | JE-MAIN-000006 | 2      | 0          | e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | 2026-07-16 14:15:44.818789+00

EntryNumber    | AccountCode | Account       | Debit | Credit | PartyId
JE-MAIN-000006 | 1100        | ذمم عملاء    | 2.00  | 0.00   | 8046e3c6-f8fc-420c-b61d-173db6f1bf58
JE-MAIN-000006 | 5100        | تكلفة مبيعات | 1.82  | 0.00   |
JE-MAIN-000006 | 1200        | مخزون أقمشة  | 0.00  | 1.82   |
JE-MAIN-000006 | 4100        | إيراد مبيعات | 0.00  | 2.00   |

EntryNumber    | debit_total | credit_total | difference
JE-MAIN-000006 | 3.82        | 3.82         | 0.00
```

## مقارنة شاشة المدير: ويب مقابل سطح مكتب

حساب الفحص في WPF وHTTP هو `admin`، الاسم الظاهر `مدير النظام`، الدور الظاهر في WPF `مسؤول`، ودور API `Administrator` مع صلاحية `security.general-manager`.

| المؤشر لنفس الفاتورة/الصندوق | الويب المرئي | HTTP الحي الذي يغذي الويب | WPF المرئي | SQL ground truth | النتيجة |
|---|---:|---:|---:|---:|---|
| الفاتورة `TEST_AUDIT_INV_...` | تعذّر التحقق بصرياً | `2.00`، status `4` | `2.00`، «معتمدة» | `2.00`، status `4` | HTTP وWPF مطابقان للـDB |
| مبيعات اليوم | تعذّر التحقق بصرياً | `todaySalesTotal=2.00` | قائمة البيع تعرض الفاتورة `2.00` | مجموع فاتورة اليوم `2.00` | مطابق على مستوى الرقم المتاح |
| صندوق `CASH-MAIN` | تعذّر التحقق بصرياً | `5002.00` | `5,002.00 USD` | `5002.00` | مطابق |
| رصيد عميل الاختبار | تعذّر التحقق بصرياً | كشف الفاتورة/السند متاح عبر API، ولا بطاقة مرئية موثقة | فاتورة `+2.00`، سند قبض `-2.00`، رصيد تراكمي `0.00` | `0.00` | WPF مطابق |
| قيمة المخزون | لا يعرضها `/dashboard/summary` | غير متاح في ملخص المدير | لوحة المخزون `$92,709` | baseline التشغيلي `92,509.428692736` | فرق عرض/تعريف يقارب `199.57` |

استجابات HTTP الخام ذات الصلة:

```json
GET /api/v1/dashboard/summary
{
  "todaySalesTotal": 2.00,
  "totalPostedReceipts": 5002.00,
  "readyForApprovalInvoicesCount": 0,
  "awaitingDetailingCount": 0
}

GET /api/v1/finance/cashboxes
{
  "value": [
    {"id":"66666666-6666-6666-6666-666666666666","code":"CASH-MAIN","balance":5002.00,"balanceDisplay":"5,002.00 USD"}
  ],
  "isSuccess": true
}

GET /api/v1/sales/invoices?page=1&pageSize=50
{
  "items": [
    {"id":"e9b1459b-f7f9-4897-9ede-c36f54ea13b2","invoiceNumber":"TEST_AUDIT_INV_20260716141337","status":4,"grandTotal":2.00,"cashboxId":"66666666-6666-6666-6666-666666666666"}
  ],
  "totalCount":1
}
```

وصف مكافئ لما ظهر فعلياً في WPF:

```text
المستخدم: مدير النظام — مسؤول
المبيعات › قائمة فواتير البيع
TEST_AUDIT_INV_20260716141337 | TEST_AUDIT_20260716141337 | المستودع الرئيسي | 124 | 1 | 2.00 | معتمدة | 2026/07/16

المالية › الصناديق
CASH-MAIN | صندوق حلب الرئيسي | 5,002.00 USD | USD | نشط

المخزون › لوحة المخزون
$92,709 قيمة المخزون | 458 إجمالي الأثواب | 50,254 الأمتار | 109 محجوز
```

تعذّر توثيق الويب بصرياً فقط لأن جلسة الفحص لم تعرض أي متصفح مدمج متاح. لم يُستبدل ذلك باستنتاج من الكود؛ لذلك عمود «الويب المرئي» مصنف صراحةً «تعذّر التحقق».

## حالة النتائج الخمسة من التحليل الثابت السابق

1. **CONFIRMED LIVE — أثر البيع النقدي موجود، مع فجوة في مدخل الصندوق وخلل حجز.** بعد تعيين `CashboxId` لفاتورة الاختبار فقط، زاد الصندوق `2.00`، نقص متر واحد، أُنشئ Stock Movement، ورُحّل قيد الفاتورة وقيد سند القبض. لكن API الويب لا يمكنه إرسال `CashboxId` من عقد الإنشاء الحالي، والحجز لم يتحرر بصورة سليمة.
2. **CONFIRMED LIVE — `CashboxMovement` كيان غير مخزن، والسجل مُركب من جداول السندات والتحويلات.** لا يوجد أي جدول يطابق `%cashbox%movement%`. الملف المنشور `FinanceEntities.cs:304-328` يحتوي الكيان، بينما `RemainingRepositories.cs:721-766` يجمع `ReceiptVouchers` و`PaymentVouchers` و`CashboxTransfers`. قائمة WPF أظهرت رصيد الصندوق الصحيح `5,002.00 USD`.
3. **CONFIRMED LIVE — العكس غير منفذ، لكن الإلغاء لم يفشل بصمت.** `AccountingPostingEngine.cs:147-148` يعيد `NotImplemented()`. طلب الإلغاء الحي أعاد بوضوح `409 Conflict` ورسالة `Posted invoices must be reversed, not cancelled.` ولا يوجد endpoint لعكس فاتورة مبيعات؛ أي إن المستخدم لا يُضلل برسالة نجاح، لكنه يُحال إلى إجراء غير متاح.
4. **CONFIRMED LIVE لهذه المحاولة — لا نتيجة فشل كاذبة بعد commit.** طلب الاعتماد أعاد `204`، وكل الصفوف كانت موجودة ومترابطة بعده. هذا لا يختبر حالة إجبار خدمة الإشعار على رمي استثناء؛ لذلك الاستنتاج محدود للمحاولة الحية المنفذة.
5. **CONFIRMED LIVE — الشجرة المنشورة تختلف عن `~/ERPSystem`.** المختبر الفعلي هو `/opt/erpsystem/src@5191e8bd...` وDLL المبني بعده مباشرة، بينما `~/ERPSystem@e9af1a45...`. النسخة المحلية لـWPF تطابق الشجرة المنشورة عند `5191e8bd...`.

## الأرقام المحاسبية الأساسية قبل وبعد الاختبار

الاستعلام المستخدم يطابق منطق `/opt/erpsystem/src/ERPSystem.Infrastructure/Services/AccountingBaselineReadService.cs:305-317`؛ AR وInventory GL هما مجموع `(Debit-Credit)` للقيود `Status=Posted`، والمخزون التشغيلي هو `RemainingLengthMeters * CostPerMeter` للأثواب `Status=Available` فقط.

| المرحلة | AR GL | Operational Inventory | Inventory GL |
|---|---:|---:|---:|
| المرجع السابق المذكور في المهمة | 320.00 | 104,968.412982 | 15,622.43 |
| قبل الاختبار الحي | 0.00 | 92,908.559245248 | 20,541.00 |
| الفرق عن المرجع القديم | -320.00 | -12,059.853736752 | +4,918.57 |
| بعد الاعتماد | 0.00 | 92,509.428692736 | 20,539.18 |
| بعد محاولة الإلغاء وتعطيل عميل الاختبار | 0.00 | 92,509.428692736 | 20,539.18 |
| الفرق النهائي عن ما قبل الاختبار | 0.00 | -399.130552512 | -1.82 |

المرجع السابق لا يمثل حالة البيانات الحالية: سجل الشركة الحالي منشأ في `2026-07-13`، وكانت قاعدة الإنتاج قبل الاختبار تحتوي `0` فواتير، بينما تضمنت ثلاثة قيود تشغيلية من شراء/تفعيل حاوية/سند قبض. لا يوجد دليل حي يحدد من نفذ إعادة التهيئة أو سببها؛ لذلك لا يُنسب الفرق إلى شخص أو عملية غير مثبتة.

المخرجات الخام قبل الاختبار:

```text
 AR GL | Operational Inventory  | Inventory GL
-------+------------------------+-------------
     0 | 92908.5592452480000000 | 20541.00
```

المخرجات الخام بعد محاولة التنظيف:

```text
 AR GL | Operational Inventory  | Inventory GL
-------+------------------------+-------------
  0.00 | 92509.4286927360000000 | 20539.18
```

لو نُقص متر واحد فقط مع بقاء بقية الأثواب متاحة، كان المتوقع للمخزون التشغيلي `92906.740518248`. الفرق الإضافي `397.311825512` يساوي تماماً قيمة المتبقي في الثوبين اللذين بقيا `Reserved`.

التنظيف المنفذ والرصيد المتبقي:

```http
POST /api/v1/sales/invoices/e9b1459b-f7f9-4897-9ede-c36f54ea13b2/cancel
HTTP 409
{"code":"Conflict","message":"Posted invoices must be reversed, not cancelled.","validationErrors":[]}

POST /api/v1/customers/8046e3c6-f8fc-420c-b61d-173db6f1bf58/deactivate
HTTP 204
```

```text
Invoice: e9b1459b-f7f9-4897-9ede-c36f54ea13b2 | Status=4 | GrandTotal=2.00 | CancelledAt=NULL
Customer: TEST_AUDIT_20260716141337 | Balance=0.00 | IsActive=false
Cashbox CASH-MAIN: 5002.00
Roll 137: RemainingLengthMeters=108.7280 | Status=1
```

لم يُنفذ حذف مباشر أو تعديل يدوي للقيود/السندات بعد فشل الإلغاء، حتى لا تُطمس مشكلة العكس أو يُنشأ تاريخ محاسبي غير مشروع. الفاتورة والعميل وجميع المراجع تحمل `TEST_AUDIT_` بوضوح، والعميل معطل. بقي انحراف موثق لأن التطبيق لا يوفر عكساً مدعوماً.

## قائمة الأخطاء أو الفجوات المكتشفة

1. **حرج — سلامة مسار العمل:** عقد إنشاء فاتورة الويب لا يحتوي `CashboxId` في `/opt/erpsystem/src/ERPSystem.Api/Endpoints/SalesEndpoints.cs:272-280`، ولا يمرره في `:119-143`، بينما المعالج يضبط `command.CashboxId` في `SalesInvoiceHandlers.cs:116` ويرفض الاعتماد النقدي بدونه في `:489-492`. الأثر: فاتورة نقدية منشأة من API لا يمكن اعتمادها دون تدخل آخر.
2. **حرج — سلامة بيانات المخزون:** `InventoryEngine.cs:439-480` يحجز FIFO، ثم `:520-572` يسمح بتثبيت ثوب آخر حسب الرقم دون تحرير الأول، و`:677-701` لا يعيد المتبقي الجزئي إلى Available. الجداول المثبتة: `public."FabricRolls"` و`inventory.inventory_reservations` و`inventory.warehouse_stocks`. الأثر الحي: ثوبان محجوزان وقيمة تشغيلية ناقصة `397.311825512` إضافة إلى COGS الفعلي.
3. **حرج — سلامة مالية/تنظيف:** `/opt/erpsystem/src/ERPSystem.Infrastructure/Services/AccountingPostingEngine.cs:147-148` يعيد `NotImplemented()` ولا يوجد endpoint لعكس فاتورة مبيعات. الإلغاء يعيد 409 صريحاً، لكن لا توجد وسيلة مدعومة لاسترجاع الصندوق والمخزون وAR وGL لفاتورة مرحّلة.
4. **متوسط — عرض/تقارير:** استجابة `GET /api/v1/sales/invoices` أعادت `totalLengthMeters=0` و`rollLengths=[]` رغم وجود صف حي `sales.sales_invoice_roll_details` بطول `1.0000`. السبب المنشور في `AggregateRepositories.cs:441-455`: استعلام القائمة يحمّل البنود ثم يمرر قائمة أثواب فارغة إلى mapper، بينما `DomainMappers.cs:287-322` يحسب الأطوال من RollDetails. الأثر Display-only على القائمة، لكنه قد يضلل المدير بشأن الأطوال.
5. **متوسط — اتساق شاشة المخزون:** WPF عرض قيمة مخزون مقربة `$92,709` بينما baseline التشغيلي الخام بعد الاختبار `92,509.428692736`. جزء من الفرق مرتبط باختلاف معالجة الأثواب المحجوزة؛ يجب توحيد تعريف المؤشر وتسميته.
6. **قيد تحقق وليس خطأ منتج:** تعذّر فتح الويب بصرياً بسبب عدم توفر متصفح مدمج في جلسة Codex. استجابات HTTP الحية وWPF موثقة، لكن لا توجد شهادة بصرية للصفحة نفسها.

## التوصية بالخطوة التالية

1. تنفيذ مسار عكس معتمد لفاتورة البيع ينسق، في معاملة واحدة، عكس قيد الفاتورة وقيد سند القبض، عكس/تحرير السند والتخصيص، استرجاع الصندوق، استرجاع المخزون، وتحرير الحجوزات؛ ثم استخدامه لعكس فاتورة `TEST_AUDIT_INV_20260716141337` وإعادة قياس baseline.
2. إصلاح ربط الحجز بثوب محدد: تخزين `FabricRollId` في `inventory_reservations` أو استبدال حجز FIFO عند اختيار رقم ثوب آخر، وإعادة الثوب الجزئي إلى `Available` بعد الاعتماد، مع اختبار يثبت عدم بقاء أي حجز للفاتورة.
3. إضافة `CashboxId` إلى `CreateSalesInvoiceRequest` وتمريره إلى `CreateSalesInvoiceDraftCommand`، ثم اختبار نقدي حقيقي عبر API بلا SQL.
4. تحميل `sales_invoice_roll_details` ضمن قائمة الفواتير أو فصل DTO القائمة عن التفاصيل بطريقة لا تعرض أطوالاً صفرية كأنها حقيقة.
5. توحيد تعريف قيمة المخزون بين baseline، WPF، وتقارير المدير، وإظهار Available/Reserved كلٌ على حدة.
6. بعد الإصلاحات، إعادة نفس الاختبار بفوترة صغيرة، والتحقق بصرياً من الويب وWPF، ثم تنفيذ العكس المدعوم والتأكد أن الأرقام الثلاثة تعود حرفياً إلى ما قبل الاختبار.
