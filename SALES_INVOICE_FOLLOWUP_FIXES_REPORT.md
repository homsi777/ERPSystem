# تقرير متابعة إصلاحات فاتورة المبيعات

تاريخ التنفيذ: 2026-07-16  
الخادم الحي: `65.21.136.217:2727`  
نسخة الإصلاح والنشر: `726e20eaea260c731237d3a082b3546d92f91192`

## 1. ملخص تنفيذي

تم حل المشكلتين بالكامل ونشر الإصلاح على الخادم الحي:

1. عاد مشروع `ERPSystem.DocumentEngine` إلى البناء ضمن الحل الكامل بعد إضافة مساحة الأسماء الصحيحة للنوع الموجود فعلًا `RenderContext`، من دون إنشاء نوع وهمي أو حذف قالب صالح.
2. أصبح `GET /api/v1/detailing/{invoiceId}` يعيد `warehouseId` الحقيقي للفاتورة بدل `Guid.Empty`، وأضيف اختبار ارتداد يمنع عودة الخطأ.

نجح البناء النظيف الكامل على VPS بـ `0 Error(s)`، ولا يوجد أي تحذير متعلق بـ `RenderContext` أو بالإصلاحين. بعد النشر كانت `erpsystem-api.service` في الحالة `active` وأعاد `/health` استجابة `HTTP 200 OK` ومحتوى `OK`.

لم تُعدّل الواجهة أو CSS أو بنية صفحة التسليم؛ الإصلاح اقتصر على ربط البيانات الخلفي والاختبار.

## 2. المشكلة 1: فشل بناء الحل الكامل

### التحقيق بالأدلة

- النوع لم يُحذف ولم يُعد تسميته. هو معرّف في `ERPSystem.DocumentEngine/Services/RenderContext.cs:10` ضمن `ERPSystem.DocumentEngine.Services`، ويُنشأ فعليًا في `HtmlRenderer.cs:33` وتستخدمه القوالب والمكوّنات المشتركة.
- أُنشئ `RenderContext` مع الوحدة في commit `2fd6a27816ba6a94ef3c21e6698fa33189ae9540` بتاريخ `2026-07-05T17:14:18+03:00`.
- يثبت `git blame` أن override المكسور `RenderBody(DocumentModel model, RenderContext ctx)` أُضيف إلى `ReceiptVoucherTemplate.cs:12` في commit `925a8bda4e30cbef7a28fd5eac7ec8baa5d50afa` بتاريخ `2026-07-11T01:26:13+03:00`، لكن الملف لم يضف `using ERPSystem.DocumentEngine.Services;`.
- إصلاح فاتورة المبيعات السابق `9102f010e7692391a0898725fc9d72059309eacb` مؤرخ `2026-07-16T18:29:33+03:00`. لذلك كان عطل البناء موجودًا قبل عمل فاتورة المبيعات بخمسة أيام ولم ينتج عنه.
- `ERPSystem.DocumentEngine` مشروع مستقل داخل `ERPSystem.sln:14`. لا يوجد `ProjectReference` إليه من API أو Application أو Infrastructure أو WPF؛ كما يستبعده مشروع WPF صراحة عبر `ERPSystem.csproj:19,27`. لذلك لم يكن للعطل أثر على الـ API أو WPF المنشورين، لكنه كان يمنع البناء الكامل ويؤثر في CI.
- مسار CI في `.github/workflows/dotnet.yml:26` ينفذ `dotnet build --no-restore` من جذر المستودع، أي يبني الحل الكامل.
- مسارات النشر الفعلية لا تبني الحل: `deploy/deploy-app.sh:27` و`deploy/setup-vps.sh:193` ينشران `ERPSystem.Api.csproj` مباشرة، و`deploy/publish-desktop.ps1:13` ينشر `ERPSystem.csproj` مباشرة. هذا يفسر نجاح النشر سابقًا رغم فشل الحل الكامل.

### القرار وسببه

تم إكمال الـ refactor الناقص بإضافة `using ERPSystem.DocumentEngine.Services;`. لم يُحذف القالب لأن الوحدة متماسكة داخليًا ويستخدم محركها النوع الحقيقي، ولم يُنشأ بديل مصطنع لأن التعريف الصحيح موجود أصلًا.

### الإصلاح

```diff
 using ERPSystem.DocumentEngine.Models;
+using ERPSystem.DocumentEngine.Services;
 using ERPSystem.DocumentEngine.Templates.Shared;
```

الإصلاح موجود في commit `726e20eaea260c731237d3a082b3546d92f91192`.

### دليل نجاح البناء الكامل على VPS

لأن VPS يعمل بنظام Linux والحل يحتوي مشروع WPF مستهدفًا Windows، نُفذت الأوامر الصحيحة للحل المختلط:

```bash
dotnet restore ERPSystem.sln -p:EnableWindowsTargeting=true
dotnet clean ERPSystem.sln -p:EnableWindowsTargeting=true
dotnet build ERPSystem.sln --no-restore --disable-build-servers -m:1 -p:EnableWindowsTargeting=true
```

النتيجة الحية:

```text
ERPSystem -> /opt/erpsystem/src/bin/Debug/net9.0-windows/ERPSystem.dll
ERPSystem.DocumentEngine -> /opt/erpsystem/src/ERPSystem.DocumentEngine/bin/Debug/net9.0/ERPSystem.DocumentEngine.dll
ERPSystem.Api -> /opt/erpsystem/src/ERPSystem.Api/bin/Debug/net9.0/ERPSystem.Api.dll
ERPSystem.Application.Tests -> /opt/erpsystem/src/ERPSystem.Application.Tests/bin/Debug/net9.0/ERPSystem.Application.Tests.dll

Build succeeded.
    6 Warning(s)
    0 Error(s)

RenderContext related warnings/errors: 0
```

أما خطوة `clean` نفسها فانتهت بـ `0 Warning(s), 0 Error(s)`. التحذيرات الستة في build قديمة وغير مرتبطة بهذه المشكلة، ومفصلة في القسم 4.

## 3. المشكلة 2: خطأ warehouseId في التفصيل

### التحقيق بالأدلة

المسار الدقيق هو:

`DetailingEndpoints.GetDetailingAsync` → `GetSalesInvoiceOperationsCenterHandler` → `SalesInvoiceMapper.ToOperationsCenterDto` → `SalesInvoiceCatalogEnricher.WithEnrichedRolls`.

- المستودع موجود في قاعدة البيانات وفي `SalesInvoiceAggregate.WarehouseId`.
- `SalesInvoiceMapper.ToDetailingDto` ينسخه صحيحًا إلى DTO.
- عند إثراء أسماء الأقمشة والألوان والحاويات، كانت `WithEnrichedRolls` تنشئ `WarehouseDetailingDto` جديدًا وتنسخ كل الحقول تقريبًا ما عدا `WarehouseId`. ولأن الحقل من نوع `Guid` أخذ القيمة الافتراضية `00000000-0000-0000-0000-000000000000`.
- إذن السبب mapping bug فقط، وليس join ناقصًا ولا فجوة بيانات.
- المستهلك المتأثر فعليًا هو صفحة التسليم `web-client/src/pages/Delivery.tsx:349,420-440`: تمرر `detailing.warehouseId` إلى `getDetailingCandidateRolls`. القيمة الصفرية كانت تمنع `canLookup` أو ترسل مستودعًا غير صالح، فتخفي قائمة الأثواب المرشحة عن المستخدم.

### الإصلاح

أضيف النسخ المفقود في `ERPSystem.Application/Common/SalesInvoiceCatalogEnricher.cs:206`:

```csharp
WarehouseId = detailing.WarehouseId,
```

وأضيف اختبار `WithEnrichedRolls_preserves_warehouse_id` في `ERPSystem.Application.Tests/Common/SalesInvoiceCatalogEnricherTests.cs`. نتيجته المحلية:

```text
Passed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1
```

### الدليل الحي قبل الإصلاح

أُنشئت فاتورة ائتمانية فعلية عبر API ثم أُرسلت إلى المستودع، بلا أي تعديل SQL:

```text
InvoiceId = 6b962b6c-b763-4b7d-a1c9-e5d7345145b9
InvoiceNo = FOLLOWUP_WH_20260716154350
DB WarehouseId = 55555555-5555-5555-5555-555555555555
```

الاستجابة الخام من النسخة القديمة لنفس الفاتورة:

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{"invoiceId":"6b962b6c-b763-4b7d-a1c9-e5d7345145b9","invoiceNumber":"FOLLOWUP_WH_20260716154350","customerName":"دندار مرسل","warehouseId":"00000000-0000-0000-0000-000000000000","chinaContainerId":"8747238e-fe5b-43e8-b9e0-b6b1d9a0bb51","sentToWarehouseAt":"2026-07-16T15:43:50.953561Z","representativeUnitPrice":2.00,"status":0,"rolls":[{"rollDetailId":"7c4a7931-0b42-4ba4-b5a0-8ee7db6ffcbe","salesInvoiceItemId":"569efc28-93be-4fc0-b94d-2cc69c4a9199","rollSequence":1,"fabricItemId":"5531f911-6959-4ce9-9ef7-3d07312327b0","fabricColorId":"89c90db8-4ee8-4b5d-b20b-6bc806cce601","fabricDisplayName":"4130","fabricCode":"4130","colorDisplayName":"DARK BLUE/BACK WHITE","lengthMeters":0,"hasValidLength":false,"chinaContainerId":"8747238e-fe5b-43e8-b9e0-b6b1d9a0bb51","containerDisplay":"124","draftRollNumber":null,"draftLengthMeters":null}]}
```

### الدليل الحي بعد الإصلاح والنشر

الاستجابة الخام لنفس الفاتورة بعد نشر `726e20e`:

```http
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8

{"invoiceId":"6b962b6c-b763-4b7d-a1c9-e5d7345145b9","invoiceNumber":"FOLLOWUP_WH_20260716154350","customerName":"دندار مرسل","warehouseId":"55555555-5555-5555-5555-555555555555","chinaContainerId":"8747238e-fe5b-43e8-b9e0-b6b1d9a0bb51","sentToWarehouseAt":"2026-07-16T15:43:50.953561Z","representativeUnitPrice":2.00,"status":0,"rolls":[{"rollDetailId":"7c4a7931-0b42-4ba4-b5a0-8ee7db6ffcbe","salesInvoiceItemId":"569efc28-93be-4fc0-b94d-2cc69c4a9199","rollSequence":1,"fabricItemId":"5531f911-6959-4ce9-9ef7-3d07312327b0","fabricColorId":"89c90db8-4ee8-4b5d-b20b-6bc806cce601","fabricDisplayName":"4130","fabricCode":"4130","colorDisplayName":"DARK BLUE/BACK WHITE","lengthMeters":0,"hasValidLength":false,"chinaContainerId":"8747238e-fe5b-43e8-b9e0-b6b1d9a0bb51","containerDisplay":"124","draftRollNumber":null,"draftLengthMeters":null}]}
```

استخدم اختبار الواجهة نفس `warehouseId` المصحح في طلب `GET /api/v1/inventory/detailing-candidate-rolls`؛ عاد `HTTP 200 OK` وقائمة فعلية من 229 ثوبًا مرشحًا، ومنها الثوب رقم 1. هذا يثبت أن مسار صفحة التسليم الذي كان متأثرًا أصبح يعمل، من دون تغيير التصميم.

بعد انتهاء الاختبار أُلغيت الفاتورة عبر `POST /api/v1/sales/invoices/{id}/cancel` وكانت النتيجة `204 No Content`. تحقق SQL النهائي:

```text
Invoice Status = 7 (Cancelled)
WarehouseId = 55555555-5555-5555-5555-555555555555
Reservation Status = 7 (Cancelled)
```

## 4. أي مشاكل جديدة اكتُشفت أثناء هذا الفحص

1. كان `/opt/erpsystem/src` عند بدء المهمة على `5191e8bdb317a213a1351ba252b50286d6ac2d06`، أي أقدم من commit إصلاح فاتورة المبيعات `9102f010...`، رغم أن ملفات API المنشورة كانت قد حُدّثت سابقًا. صُحح عدم التطابق؛ الآن `HEAD` على VPS و`origin/main` كلاهما `726e20eaea260c731237d3a082b3546d92f91192`، والملف الوحيد غير المتتبع على الخادم هو `deploy/.env` المقصود محليًا.
2. البناء الكامل على Linux يحتاج restore كاملًا وتمرير `EnableWindowsTargeting=true` بسبب مشروع WPF. محاولة `clean` بلا ذلك أثبتت `NETSDK1100` وحزمًا غير موجودة في cache، ثم نجح restore/clean/build الكامل بعد تمرير الخيار. هذا قيد بيئي، لا عطل شيفرة.
3. بقيت 6 تحذيرات قديمة وغير مرتبطة بالإصلاح: تحذير `CS8321` لدالة `Describe` غير مستخدمة في `tools/ChinaImportCatalogTest`، تعارض `Microsoft.EntityFrameworkCore.Relational` 9.0.1/9.0.6 في مشروع الاختبارات، وأربعة تحذيرات xUnit (`xUnit2012` و`xUnit2013`). لم تُعدّل لأنها خارج المشكلتين المطلوبتين، ولا يوجد تحذير واحد متعلق بـ `RenderContext` أو `WarehouseId`.

## 5. حالة النشر النهائية

```text
Local HEAD       = 726e20eaea260c731237d3a082b3546d92f91192
origin/main      = 726e20eaea260c731237d3a082b3546d92f91192
VPS source HEAD  = 726e20eaea260c731237d3a082b3546d92f91192
erpsystem-api.service = active
```

فحص الصحة النهائي بعد النشر:

```http
HTTP/1.1 200 OK
Content-Type: text/plain; charset=utf-8

OK
```

النتيجة النهائية: الإصلاح منشور ويعمل حيًا، البناء الكامل بلا أخطاء، وبيانات `warehouseId` صحيحة في endpoint وفي مسار صفحة التسليم.
