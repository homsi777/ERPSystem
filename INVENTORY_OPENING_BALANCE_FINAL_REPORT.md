# تقرير التنفيذ النهائي — رصيد المخزون الافتتاحي

> تاريخ التنفيذ: 2026-07-13  
> بيئة التنفيذ والتحقق البرمجي وقاعدة البيانات: محلية، وفق توجيه نبيل بعدم محاولة Codex الاتصال بالسحابة أثناء تشغيل VPN.
> حالة النشر: نُشر commit `e90bc0a` على السحابة بنجاح بواسطة نبيل، وأُضيفت نتيجة التنفيذ الفعلية إلى هذا التقرير.
> لم يتم تشغيل تطبيق WPF.

## النتيجة التنفيذية

تم تنفيذ المطلوب كاملاً، والتحقق منه محلياً، ثم نشره على السحابة بنجاح:

- توحيد `DocumentType` للأرصدة الافتتاحية على `FinanceOpeningBalance` في النشر والقراءة.
- تحويل شاشة «مواد أول المدة» من المسار المخزني المباشر إلى المسار المالي الموحد الذي ينشئ قيد 1200/3100.
- تخزين `FabricItemId` و`FabricColorId` فعلياً على سطر الرصيد الافتتاحي.
- الإبقاء على إدخال عدة أسطر صنف/لون داخل المستند نفسه.
- إنشاء الرولات القديمة دفعة واحدة عبر `AddRange` بدلاً من `AddAsync` لكل رول.
- إضافة وسم صريح `IsLegacyOpeningBalance` وحالة `LegacyLengthConfirmed` على الرول.
- السماح بتصحيح الطول فوق القيمة الافتراضية مرة واحدة فقط للرول القديم، مع استمرار رفض رول الصين للحالة نفسها.
- إضافة مخزون أول المدة إلى مصدر المخزون القابل للاختيار في فاتورة البيع دون تغيير تصميم الشاشة.
- إضافة migration مركّز واختبارات unit واختبار قاعدة بيانات محلية حقيقي داخل transaction ثم rollback.

## 1. الاتصال والنسخة الاحتياطية وخط الأساس

بناءً على التوجيه المباشر، لم تتم أي محاولة اتصال بالسحابة أو SSH. استُخدمت قاعدة `erp_pro` المحلية على `localhost:5432`.

### النسخة الاحتياطية المحلية

- الملف: `artifacts/inventory-opening-balance-final/erp_pro_local_pre_inventory_opening_20260713T015121.dump`
- النوع: PostgreSQL custom-format dump.
- الحجم: `509,116` bytes.
- SHA-256: `4E1CBB5186FA946F5D0186223BAF62022BB951550E9F5CD08B8638098A5650DA`.
- `pg_restore --list`: نجح، 416 سطراً.
- اختبار الاستعادة: نجح في قاعدة منفصلة مؤقتة باسم `erp_pro_restore_iob_015137`.
- تحقق ما بعد الاستعادة: 16 حساباً و509 رولات.
- حُذفت قاعدة الاستعادة المؤقتة بعد نجاح التحقق.

### خط الأساس المحلي قبل التغيير

الشركة: `11111111-1111-1111-1111-111111111111`.

| المؤشر | قبل التنفيذ |
|---|---:|
| AR GL | 0 |
| Operational Inventory | 113297.705258 |
| Inventory GL | 15885.00 |

كانت قاعدة 5432 متأخرة عن migrations المحلية؛ لذلك فشلت أول محاولة لأداة baseline بسبب غياب `accounting.journal_entries.PostingKind`. سُجلت القيم الثلاث مباشرة باستعلامات القراءة نفسها، ثم طُبقت migrations المحلية المعلقة وترحيل هذه المهمة بواسطة SQL idempotent مولّد من EF. بعد ذلك عملت أداة `AccountingBaselineReport` بنجاح.

## 2. Part 1 — إصلاح SourceType / DocumentType

### القرار الصحيح

المسار الموحد ينشئ مستنداً واحداً من `finance.opening_balance_documents` يمكن أن يكون عميل أو مورد أو مخزون أو نقد أو بنك أو رأس مال أو دفتر عام. لذلك مصدر القيد الصحيح هو نوع المستند الموحد نفسه:

`DocumentType.FinanceOpeningBalance`.

أضيفت سياسة واحدة في `ERPSystem.Application/Common/OpeningBalanceDocumentTypePolicy.cs` تمنع عودة الاختلاف لاحقاً.

### التغييرات

- `IntegratedAccountingService.cs:434,444`: النشر والبحث اللاحق عن رقم القيد يستخدمان `OpeningBalanceDocumentTypePolicy.SourceType`.
- `GetCustomerAccountLedgerHandler.cs:35`: كشف حركة العميل يقرأ المصدر الموحد.
- `CustomerQueryHandlers.cs:202`: كشف حساب العميل يقرأ المصدر الموحد.
- `OpeningBalanceRepository.cs:191`: تفاصيل المستند المالي تبحث عن المصدر نفسه.

### الأنواع المتأثرة وعدم الانحدار

- `FinanceOpeningBalance`: النوع الموحد المستخدم الآن في الكتابة والقراءة.
- `CustomerOpeningBalance`: بقي للـ legacy API القديم فقط، ولم يعد قارئ المسار الموحد يعتمد عليه.
- `SupplierOpeningBalance`: بقي للـ legacy API القديم فقط. كشف المورد لا يرشح بهذا النوع؛ يقرأ دفتر حساب المورد حسب الحساب والتاريخ، لذلك لم يتعطل.
- الأنواع الأخرى مثل SalesInvoice وReceiptVoucher وChinaContainer لم تتغير.

### التحقق الحقيقي والتنظيف

اختبار `InventoryOpeningBalanceLocalDbVerificationTests`:

1. فتح transaction حقيقية على `erp_pro` المحلية.
2. استدعى `IIntegratedAccountingService.PostOpeningBalanceDocumentAsync` بقيد متوازن 12.34:
   - مدين ذمم العملاء.
   - دائن أرصدة افتتاحية.
3. حفظ القيد فعلياً داخل transaction.
4. استدعى `GetPartyOpeningBalanceAsync` بالمصدر الموحد وأعاد `12.34` بنجاح.
5. نفذ rollback.
6. تحقق أن عدد القيود ذات وصف الاختبار أصبح صفراً.

## 3. Part 2 — الإدخال التجميعي عبر المسار المالي الموحد

### ربط الشاشة

`Controls/Inventory/InventoryOpeningStockFormControl.cs` احتفظ بنفس الواجهة ونفس جدول الأسطر. لم يتغير تصميم XAML/CSS أو تخطيط الشاشة.

المسار الجديد:

`OpeningBalanceUiService.CreateAsync` → `OpeningBalanceType.OpeningStock` → Submit → Approve → Post/Lock → `OpeningBalanceEngine` → قيد 1200/3100 → `PostFinanceOpeningBalanceStockAsync`.

لم تعد الشاشة تستدعي `InventoryUiService.CreateOpeningStockAsync` أو `PostOpeningStockAsync` مباشرة.

### القيد

لم يتغير منطق القيد الموجود في `OpeningBalanceEngine`:

- مدين `1200 — مخزون أقمشة`.
- دائن `3100 — أرصدة افتتاحية`.

القيمة هي مجموع `Quantity × UnitCost` للأسطر التجميعية. تصحيح طول رول منفرد لاحقاً لا يستدعي خدمة محاسبية ولا يعيد تقييم هذا القيد.

### معرفات الصنف واللون

أضيف الحقلان إلى جميع الطبقات:

- `OpeningBalanceLineInput` وDTO.
- `OpeningBalanceLine` في domain.
- `OpeningBalanceLineEntity` في persistence.
- mapping ذهاباً وإياباً.
- `finance.opening_balance_lines."FabricItemId" uuid`.
- `finance.opening_balance_lines."FabricColorId" uuid`.
- فهرس مركب على الحقلين.

تتحقق `OpeningBalanceEngine` من وجود المعرفين في سطر OpeningStock، ويستخدم `PostFinanceOpeningBalanceStockAsync` المعرفين مباشرة. أزيل حل الصنف واللون بالاسم من مسار الترحيل.

### تعدد الأسطر

الشاشة كانت تحتوي `ObservableCollection<OpeningLineRow>` وDataGrid وتدعم إضافة عدد غير محدود من الأسطر. الربط الجديد يحول المجموعة كاملة إلى `List<OpeningBalanceLineInput>` داخل مستند مالي واحد؛ أي أن كل صنف/لون يبقى سطراً مستقلاً في المستند نفسه.

### أداء 10,000 رول

`InventoryEngine.PostOpeningStockAsync` يبني `List<FabricRollEntity>` بسعة محسوبة من مجموع `RollCount`، ثم يستخدم:

`context.FabricRolls.AddRange(rollsToInsert)` (`InventoryEngine.cs:948`).

لا يوجد `AddAsync` لكل رول في مسار الرصيد الافتتاحي الجديد، ولا يوجد `SaveChanges` داخل حلقة الرولات. الحفظ النهائي يستخدم batching الخاص بمزود Npgsql/EF.

## 4. Part 3 — وسم الرول القديم وتصحيح أول طول حقيقي

### آلية الوسم

لم يعد الاعتماد على `ContainerId=Guid.Empty` وحده. أضيف إلى `FabricRolls`:

- `IsLegacyOpeningBalance boolean NOT NULL DEFAULT false`.
- `LegacyLengthConfirmed boolean NOT NULL DEFAULT false`.

الرولات التي ينشئها `PostOpeningStockAsync` تحمل:

- `ContainerId = Guid.Empty` للتوافق مع تجميع المخزون الحالي.
- `IsLegacyOpeningBalance = true`.
- `LegacyLengthConfirmed = false`.

رولات الصين التي ينشئها `PostContainerImportAsync` تحمل `IsLegacyOpeningBalance=false` و`LegacyLengthConfirmed=true` صراحة.

الـ migration يوسم الرولات القديمة الموجودة ذات `ContainerId=Guid.Empty` دون تغيير أي طول أو تكلفة أو رصيد.

### إتاحة الرولات القديمة في المبيعات

- `InventoryRepository.GetSellableContainerIdsAsync` لم يعد يستبعد `Guid.Empty`.
- قائمة الحاويات الموجودة أضيف إليها خيار «مخزون أول المدة» عند وجود رولات قديمة متاحة (`NewSalesInvoiceControl.xaml.cs:762`).
- لم يتغير شكل القائمة أو تصميم الشاشة.
- `InventoryOperationsService.ValidateInvoiceLinesAsync` يتجاوز تحقق حاوية الصين فقط عند `Guid.Empty`، ويشترط عندها أن تكون الرولات موسومة `IsLegacyOpeningBalance=true`.
- حاويات الصين غير الفارغة تستمر عبر `ContainerSaleValidator` نفسه دون تغيير.

### شرط التجاوز الدقيق

السياسة في `LegacyOpeningBalanceRollLengthPolicy.cs` تسمح بالاستبدال فقط إذا:

`roll.IsLegacyOpeningBalance && !roll.LegacyLengthConfirmed && enteredLengthMeters > 0`.

عندها فقط:

- `LengthMeters = enteredLengthMeters`.
- `RemainingLengthMeters = enteredLengthMeters`.
- `LegacyLengthConfirmed = true`.

بعد التأكيد الأول لا يمكن تطبيق التصحيح ثانية. رول الصين لا يدخل هذا الفرع مطلقاً، ويبقى شرط `entered length <= RemainingLengthMeters` فعالاً كما كان.

### المحاسبة والتقييم

سياسة التصحيح تعدل كيان `FabricRollEntity` فقط. لا تستدعي:

- `IIntegratedAccountingService`.
- `OpeningBalanceEngine`.
- `RecordValuationSnapshotAsync`.
- أي إعادة حساب لقيد 1200/3100.

بعد ذلك، عند بيع الرول، تستمر حركة البيع والاستهلاك والتكلفة المعتادة كما لأي رول آخر.

### اختبار الرولين والتنظيف

داخل transaction قاعدة البيانات نفسها:

- رول قديم: افتراضي 20 متر، أُدخل 27.5 متر؛ قُبل وخُزن `LengthMeters=27.5`, `RemainingLengthMeters=27.5`, `LegacyLengthConfirmed=true`.
- رول الصين: 20 متر، أُدخل 27.5 متر؛ رُفض بـ`InventoryException` وبقيت قيمته 20.
- نُفذ rollback.
- تحقق SQL بعد الاختبار: صفر قيود اختبار وصفر رولات اختبار.

## 5. Migration

الملف:

`ERPSystem.Infrastructure/Migrations/20260724120000_AddInventoryOpeningBalanceLegacyRollSupport.cs`.

الترحيل مركّز فقط على:

- معرفي الصنف واللون في opening-balance lines.
- حقلي وسم/تأكيد الرول.
- فهرسي البحث.
- backfill metadata للرولات القديمة ذات الحاوية الفارغة.

تم رفض migration تلقائي ضخم ولّده EF بسبب قدم model snapshot، وحُذف بالكامل قبل المتابعة. لم تُقبل أي عملية حذف أو إعادة تسمية أو تعديل جداول غير مرتبطة بالمهمة.

## 6. Accounting baseline diffs

| البوابة | AR GL | Operational Inventory | Inventory GL | النتيجة |
|---|---:|---:|---:|---|
| قبل التغيير | 0 | 113297.705258 | 15885.00 | مرجع |
| بعد migrations | 0 | 113297.705258 | 15885.00 | PASS — بلا فرق |
| بعد اختبار SourceType والقيود مع rollback | 0 | 113297.705258 | 15885.00 | PASS — بلا فرق |
| بعد اختبار الرولين مع rollback | 0 | 113297.705258 | 15885.00 | PASS — بلا فرق |
| نهائي | 0 | 113297.705258 | 15885.00 | PASS — بلا فرق |

Artifacts النهائية:

- `artifacts/inventory-opening-balance-local-after-migrations.json`
- `artifacts/inventory-opening-balance-local-final.json`
- ملفات health المقابلة.

أداة health تعرض 3 checks فاشلة (2 critical) وIssueCount=1 في البيانات المحلية؛ العدد بقي ثابتاً ولم تنشئ هذه المهمة drift جديداً.

## 7. الاختبارات والبناء

### اختبارات المهمة

- `InventoryOpeningBalancePolicyTests`: سياسة SourceType، حفظ IDs، قبول الرول القديم، رفض رول الصين، ومنع التصحيح الثاني.
- `InventoryOpeningBalanceLocalDbVerificationTests`: قيد حقيقي + قراءة كشف + رول قديم + رول الصين + rollback + إثبات cleanup.
- النتيجة النهائية بعد إضافة اختبار الإدخال اليدوي: **8 passed, 0 failed**.

مشروع الاختبارات يعرض تحذيرات موجودة مسبقاً بسبب اختلاف إصداري EF Relational 9.0.1/9.0.6 وتحذير xUnit قديم في `AccountingPostingEngineLiveDbTests.cs`. ليست ناتجة عن كود المهمة.

### Build التطبيق

`dotnet build ERPSystem.csproj --no-restore`

- **نجح**.
- **0 errors**.
- **0 warnings**.

## 8. حالة النشر

### نتيجة النشر السحابي الفعلية

نفّذ نبيل على الخادم:

```bash
cd /opt/erpsystem/src && sudo bash deploy/deploy-app.sh
```

وكانت النتيجة:

| المرحلة | النتيجة | الدليل من خرج النشر |
|---|---|---|
| سحب آخر شيفرة من `main` | **PASS** | انتقل الخادم من `dbe8f2f` إلى `e90bc0a` |
| تثبيت النسخة المطلوبة | **PASS** | `HEAD is now at e90bc0a feat: Implement legacy opening balance support for inventory management` |
| Restore لمشروعات .NET | **PASS** | `Restore complete` |
| بناء ونشر Domain | **PASS** | خرج `ERPSystem.Domain.dll` بنجاح |
| بناء ونشر Application | **PASS** | خرج `ERPSystem.Application.dll` بنجاح |
| بناء ونشر Infrastructure | **PASS** | خرج `ERPSystem.Infrastructure.dll` بنجاح |
| نشر API | **PASS** | خرج النشر إلى `/opt/erpsystem/api/` |
| بناء .NET الكلي | **PASS** | `Build succeeded in 16.8s` |
| تثبيت حزم web-client | **PASS** | أضيفت 380 حزمة، ونتيجة التدقيق `found 0 vulnerabilities` |
| فحص TypeScript وبناء Vite | **PASS** | حُولت 387 وحدة، واكتمل البناء خلال `3.34s` |
| توليد PWA | **PASS** | تولد `dist/sw.js` و`dist/workbox-dcde9eb3.js` مع 11 عنصر precache |
| إعادة تشغيل الخدمات | **PASS** | اكتملت مرحلة «إعادة تشغيل الخدمات» |
| فحص تشغيل API داخل سكربت النشر | **PASS** | ظهر صراحة: `الـ API يعمل ✅` ثم `تم التحديث.` |

### الـ migration على السحابة

التطبيق يستدعي EF Core migrations عند بدء الـ API. نجاح إعادة تشغيل الـ API ووصول سكربت النشر إلى فحص «الـ API يعمل» يعني أن بدء التطبيق لم يتوقف بخطأ migration. مع ذلك، خرج النشر المرسل لا يحتوي استعلاماً مباشراً لجدول migrations أو فحصاً مستقلاً لأعمدة قاعدة السحابة؛ لذلك يُسجل الوضع بدقة كالتالي:

- تطبيق migration محلياً: **مؤكد ومتحقق**.
- بدء API السحابي بعد نشر الكود الجديد: **مؤكد وناجح**.
- عدم ظهور خطأ migration أثناء بدء الخدمة: **مؤكد من اكتمال النشر**.
- فحص بنية قاعدة السحابة باستعلام SQL مستقل: **غير منفذ ضمن الأدلة المرسلة**.

### تحذيرات النشر غير المانعة

- ظهر تحذير deprecation للحزمتين `source-map@0.8.0-beta.0` و`glob@11.1.0`، لكنه لم يمنع التثبيت أو البناء، وأظهر التدقيق **0 vulnerabilities**.
- أنشأ Vite الحزمة `index-DL8AoFk_.js` بحجم `867.77 kB` قبل الضغط، لذلك ظهر تحذير أن بعض chunks أكبر من 500 kB. هذا تحذير أداء وتحسين code-splitting، وليس فشل نشر.

### الحالة النهائية

- التنفيذ البرمجي: **مكتمل**.
- الاختبارات المحلية المركزة: **8 passed, 0 failed**.
- بناء تطبيق WPF محلياً: **PASS — 0 errors, 0 warnings**.
- نشر API وweb-client على السحابة: **مكتمل وناجح على commit `e90bc0a`**.
- تشغيل API بعد إعادة الخدمات: **مؤكد من سكربت النشر**.
- تغيير تصميم الواجهة: **لم يحدث**.
- اتصال Codex بالسحابة: **لم يحدث**؛ نفّذ نبيل أمر النشر وأرسل النتيجة.
- تشغيل WPF بواسطة Codex: **لم يحدث**.
- الاختبار الوظيفي اليدوي لشاشة «مواد أول المدة» على النسخة المنشورة: **ما يزال مطلوباً**؛ نجاح البناء وhealth check لا يثبت وحده دورة إدخال فعلية من الواجهة حتى الترحيل والبيع.

**الخلاصة: المهمة منفذة ومختبرة محلياً ومنشورة سحابياً بنجاح؛ المتبقي فقط اختبار قبول يدوي وظيفي على الشاشة المنشورة، وليس عملاً برمجياً أو خطوة نشر.**

## 9. نتيجة الاختبار المرئي بعد النشر وإصلاح سطر الإدخال

أظهر الاختبار المرئي الذي أرسله نبيل بعد نشر `e90bc0a` أن جدول «القماش» ظاهر، لكن سطر إدخال المادة أسفل الجدول كان منكمشاً: حقول الأمتار والرولات والتكلفة ظهرت كمربعات صغيرة ومن دون عناوين واضحة، ولذلك لم يكن سطر إضافة مواد أول المدة قابلاً للاستخدام بصورة مفهومة.

### السبب

كان السطر مبنياً بواسطة `Horizontal StackPanel`، بينما حقول الإدخال الرقمية لا تملك عرضاً ثابتاً أو شبكة توزع المساحة؛ فحصل كل حقل على عرض النص القصير داخله فقط (`0` أو `1`).

### الإصلاح المحلي

عُدّل `Controls/Inventory/InventoryOpeningStockFormControl.cs` فقط:

- استُبدل حاوي السطر من `Horizontal StackPanel` إلى `Grid` مرن.
- أُعطي اختيار القماش المساحة المرنة الرئيسية.
- أُعطيت حقول اللون والأمتار والرولات والتكلفة عروضاً واضحة.
- أُضيف عنوان ظاهر فوق كل حقل: القماش، اللون، الأمتار، Rolls، التكلفة/م.
- بقي زر «إضافة» والمسار البرمجي للحفظ والترحيل كما هما.
- لم يتغير تصميم بقية الشاشة، ولم تتغير الواجهة الخلفية أو قاعدة البيانات أو المحاسبة.

### تحقق الإصلاح

`dotnet build ERPSystem.csproj --no-restore`

- **PASS**.
- **0 errors**.
- **0 warnings**.
- لم يشغّل Codex تطبيق WPF ولم يتصل بالسحابة.

### حالة هذا الإصلاح

- إصلاح سطر الإدخال: **مكتمل محلياً**.
- النسخة السحابية `e90bc0a`: **لا تحتوي هذا الإصلاح اللاحق بعد**.
- المطلوب لإكماله سحابياً: رفع التعديل إلى GitHub ثم إعادة تنفيذ `sudo bash deploy/deploy-app.sh` على الخادم، وبعدها إعادة الاختبار المرئي للشاشة.

**الحالة النهائية المحدّثة: منطق المهمة منشور، وعيب ظهور سطر الإدخال أُصلح محلياً وبُني بنجاح، لكنه ينتظر النشر السحابي وإعادة الاختبار المرئي.**

## 10. تصحيح مصدر القماش واللون إلى إدخال يدوي كامل

أوضح نبيل بعد ظهور السطر أن القماش واللون لا يجب أن يكونا قائمتين منسدلتين من المخزون العام؛ شاشة «مواد أول المدة» هي شاشة تأسيس، ويجب أن يكتب الموظف بياناتها يدوياً حتى عندما لا يكون القماش أو اللون معرفاً سابقاً.

### واجهة الإدخال

- استُبدلت قائمة القماش المنسدلة بخانة `TextBox` فارغة.
- استُبدلت قائمة اللون التابعة لها بخانة `TextBox` فارغة.
- بقيت الأمتار وعدد الرولات والتكلفة خانات كتابة.
- بعد الضغط على «إضافة» يُضاف السطر إلى الجدول وتُفرغ الخانات استعداداً للسطر التالي.
- لم تتغير بقية بنية الشاشة أو تصميمها أو أزرار الحفظ والترحيل.

### سلامة الحفظ الخلفي

لأن سطر المخزون يجب أن يخزن `FabricItemId` و`FabricColorId` صحيحين، أضيف مسار حل تلقائي داخل وحدة عمل إنشاء مستند الرصيد الافتتاحي:

- إذا طابق الاسم المكتوب قماشاً/لوناً معرفاً، يعاد استخدام معرفه ولا ينشأ تكرار.
- إذا كان الاسم جديداً، ينشأ تعريف القماش واللون تحت تصنيف مخصص `OPENING_BALANCE` باسم «مواد أول المدة».
- تُحفظ المعرّفات الناتجة على `finance.opening_balance_lines` قبل الترحيل للمخزون.
- الإنشاء وحفظ المستند يتمان ضمن وحدة العمل نفسها؛ لا يعتمد المسار على اختيار من المخزون العام.
- يدعم الحل عدة أسطر ويحتفظ بكاش داخل الدفعة لمنع تكرار القماش أو اللون المكتوب أكثر من مرة في المستند نفسه.

### التحقق

- `dotnet build ERPSystem.csproj -c Release --no-restore`: **PASS — 0 errors, 0 warnings**.
- الاختبارات المركزة: **8 passed, 0 failed**.
- اختبار PostgreSQL محلي حقيقي بإدخال أسماء دون IDs: **PASS**.
- تحقق الاختبار من إنشاء IDs وحفظها على سطر المستند، ثم نفذ rollback وتأكد من عدم بقاء مستند أو قماش أو لون اختباري.
- لم يتصل Codex بالسحابة ولم يشغّل تطبيق WPF.

**الحالة الأحدث: الإدخال أصبح يدوياً بالكامل كما طلب نبيل، مع حفظ معرفات صحيحة خلفياً. هذا التصحيح محلي وينتظر الرفع إلى GitHub وإعادة النشر السحابي.**
