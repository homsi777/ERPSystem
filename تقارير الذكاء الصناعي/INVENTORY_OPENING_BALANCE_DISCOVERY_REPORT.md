# تقرير اكتشاف: دليل الحسابات والبنية الحالية لرصيد افتتاحي للمخزون

> تاريخ الفحص: 2026-07-13 — المهمة تقرير فقط، ولم تُنفّذ أي كتابة أو ترحيل أو تعديل بيانات.

## الخلاصة التنفيذية

- يوجد فعلياً حساب حقوق ملكية صالح للترحيل: **3100 — أرصدة افتتاحية** (`Opening Balance Equity`).
- حساب أصل المخزون التشغيلي هو **1200 — مخزون أقمشة** (`Fabric Inventory`).
- يوجد مساران متوازيان لرصيد المخزون الافتتاحي:
  1. مسار مخزون قديم/مباشر: `InventoryOpeningStockFormControl` → `inventory.opening_stock_documents` و`inventory.opening_stock_lines` → `InventoryEngine.PostOpeningStockAsync`.
  2. مسار مالي موحّد: `OpeningBalanceType.OpeningStock` → `finance.opening_balance_*` → قيد **مدين 1200 / دائن 3100** → إنشاء مستند مخزون داخلي ثم حركة وتقييم.
- قاعدة البيانات المفحوصة تحتوي الجدولين والمسارين، لكن لا تحتوي أي مستند افتتاحي حالياً: العدد صفر في جداول `finance.opening_balance_*` و`inventory.opening_stock_*`.
- شاشة «مواد أول المدة» الحالية **موجودة فعلاً وليست مجرد placeholder**، لكنها لا تُدخل أطوال الرولات منفردة. تدخل إجمالي الأمتار وعدد الرولات ثم ينشئ المحرك رولات متساوية الطول حسابياً. لذلك لا تلبي إدخال نحو 10,000 رول بأطوالها الحقيقية.
- المسار المالي الموحّد للمخزون موجود في الكود ويستخدم الحسابين الصحيحين، لكنه يحل الصنف واللون **بالاسم** ولا يخزن معرفيهما في `finance.opening_balance_lines`، ولا يحفظ أطوال الرولات الفردية.
- آلية تفصيل رولات المبيعات لا تتحقق من «مجموع أطوال الرولات = طول إجمالي مُدخل»؛ لا يوجد طول إجمالي مستقل في سطر فاتورة البيع. الإجمالي يُحسب من مجموع الرولات بعد التفصيل. هذه نقطة مهمة: المرجع الموجود يضمن عدد صفوف الرولات وصحة كل طول، وليس مطابقة مجموع إلى قيمة إجمالية سابقة.
- يوجد عدم توافق حالي بين الكود وقاعدة البيانات في تفصيل المبيعات: الكود يتوقع `DraftRollNumber` و`DraftLengthMeters` وترحيلاتهما موجودة في المستودع، لكنهما غير مطبقين في قاعدة البيانات المفحوصة.
- يوجد أيضاً عدم توافق منطقي في كشف حساب العميل: القيد الجديد يُحفظ بمصدر `FinanceOpeningBalance`، بينما استعلام كشف العميل يبحث عن `CustomerOpeningBalance`. لذلك لا يمكن اعتبار ظهور الرصيد الجديد في الكشف مضموناً من الكود الحالي؛ لا توجد بيانات فعلية لاختباره لأن عدد العملاء والمستندات الافتتاحية صفر.

## نطاق قاعدة البيانات ومصداقية وصفها

التطبيق يحمّل `appsettings.json` ثم `appsettings.Local.json` من مجلد التشغيل (`App.xaml.cs:46-51`). ملف المصدر المحلي يشير إلى `localhost:5433` باسم مستخدم `erp_app`، لكن وقت الفحص لم توجد خدمة PostgreSQL مستمعة على 5433. الخدمة الفعلية الوحيدة كانت PostgreSQL 16.13 على `localhost:5432`، وتم الاتصال بقاعدة `erp_pro` كمستخدم `postgres` بنجاح.

لذلك كل نتائج البيانات أدناه هي من **قاعدة `erp_pro` المحلية المتاحة فعلياً على 5432**. لا توجد في المستودع أو حالة التشغيل التي فُحصت قرينة تكفي لتسميتها خادم الإنتاج البعيد. الأرقام التي تؤكد أنها قاعدة حقيقية وليست فارغة بالكامل: مورد واحد، حاوية واحدة، 509 رول، 8 أرصدة تجميعية للمستودع، بإجمالي 47,837 متر في المستودع الرئيسي. العملاء صفر.

آخر ترحيل مسجل في القاعدة ضمن السلسلة ذات الصلة هو `20260716092000_AddMovementReportIndexes`. ترحيلات الكود `20260717120000_AddSalesInvoiceItemLineContainer` و`20260718120000_AddSalesInvoiceRollDetailDraftFields` غير مسجلة فيها.

## 1. دليل الحسابات الكامل كما هو في القاعدة

عدد الحسابات 16. `IsPostable=true` يعني حساب حركة، و`false` يعني جذر تجميعي.

| الكود | الاسم العربي | الاسم الإنكليزي | النوع | الأب | حركة؟ | نشط؟ |
|---|---|---|---|---|---:|---:|
| 1000 | الأصول | Assets | Asset | — | لا | نعم |
| 1010 | الصندوق — USD | Cash USD | Asset | 1000 الأصول | نعم | نعم |
| 1100 | ذمم عملاء | Accounts Receivable | Asset | 1000 الأصول | نعم | نعم |
| 1200 | مخزون أقمشة | Fabric Inventory | Asset | 1000 الأصول | نعم | نعم |
| 1300 | تكاليف وصول معلقة | Landing Cost Clearing | Asset | 1000 الأصول | نعم | نعم |
| 2000 | الخصوم | Liabilities | Liability | — | لا | نعم |
| 2100 | ذمم موردين | Accounts Payable | Liability | 2000 الخصوم | نعم | نعم |
| 3000 | حقوق الملكية | Equity | Equity | — | لا | نعم |
| 3100 | أرصدة افتتاحية | Opening Balance Equity | Equity | 3000 حقوق الملكية | نعم | نعم |
| 3200 | رأس مال الشركاء | Partner Capital | Equity | 3000 حقوق الملكية | نعم | نعم |
| 4000 | الإيرادات | Revenue | Revenue | — | لا | نعم |
| 4100 | إيراد مبيعات | Sales Revenue | Revenue | 4000 الإيرادات | نعم | نعم |
| 4200 | خصم مبيعات | Sales Discounts | Revenue | 4000 الإيرادات | نعم | نعم |
| 5000 | المصروفات | Expenses | Expense | — | لا | نعم |
| 5100 | تكلفة مبيعات | Cost of Goods Sold | Expense | 5000 المصروفات | نعم | نعم |
| 5210 | مصاريف تشغيل | Operating Expenses | Expense | 5000 المصروفات | نعم | نعم |

لا يوجد أي حساب مؤرشف أو غير نشط في القائمة. المعرفات الثابتة المستخدمة في الكود معرفة في `AccountingAccountIds`، ويزرعها `ERPSystem.Infrastructure/Seed/DatabaseSeeder.cs:258-265`.

### الحسابات المطلوبة تحديداً

- **Opening Balance Equity:** موجود، الكود الدقيق `3100`، الاسم الدقيق `أرصدة افتتاحية`، النوع `Equity`، حساب حركة تحت `3000`.
- **Inventory Asset:** `1200 — مخزون أقمشة`، النوع `Asset`، حساب حركة تحت `1000`.
- **مرجع رصيد عميل افتتاحي:** `BuildJournalLinesAsync` ينشئ مدين `1100 ذمم عملاء` ودائن `3100 أرصدة افتتاحية` (`OpeningBalanceEngine.cs:624-627`).
- **رصيد مورد افتتاحي:** مدين `3100` ودائن حساب ذمم المورد، مع fallback إلى `2100` (`OpeningBalanceEngine.cs:629-641`).
- **رصيد مخزون افتتاحي:** مدين `1200` ودائن `3100` (`OpeningBalanceEngine.cs:619-622`). لا يدخل أي مورد أو حساب AP في هذا القيد.
- **تفعيل مخزون حاوية الصين:** مدين `1200` ودائن `1300 تكاليف وصول معلقة`، وليس 3100 (`IntegratedAccountingService.cs:55-74`).

## 2. تطبيق رصيد العميل الافتتاحي من البداية للنهاية

### الواجهات الحالية

- `Controls/Customers/CustomerOpeningBalanceControl.cs`: قائمة مستندات رصيد العملاء مع KPI وفلاتر العميل والحالة والتاريخ والمبلغ والبحث والاستيراد والتصدير.
- `Controls/Customers/CustomerOpeningBalanceFormControl.cs`: نموذج الإنشاء/التعديل الفعلي.
- لا يوجد ملف باسم `CustomerOpeningBalanceOperationsCenterControl.cs` في checkout الحالي. مركز العمليات العام هو `Controls/Finance/OpeningBalanceOperationsCenterControl.cs`، والنموذج الخاص بالعميل يُفتح ضمن سياق التنقل المالي.

### الحقول التي يجمعها النموذج

من `CustomerOpeningBalanceFormControl.cs:18-25,45-52`:

- العميل (`PartyId`, `PartyName`).
- تاريخ الافتتاح.
- رمز العملة، ويجب أن يكون 3 أحرف.
- اتجاه الرصيد: مدين «ذمة على العميل» أو دائن «رصيد لصالح العميل».
- المبلغ، ويجب أن يكون أكبر من صفر.
- المرجع.
- الملاحظات.

النموذج يبني `OpeningBalanceLineInput` بحيث يكون أحد `Debit` أو `Credit` فقط موجباً (`:168-207`)، ثم ينشئ `CreateOpeningBalanceCommand` بنوع `CustomerReceivable` ومصدر `Manual` وسعر صرف 1 (`:211-257`). حالات العمل: Draft → PendingApproval → Approved → Posted/Locked، مع أزرار حفظ مسودة، إرسال، اعتماد، وترحيل (`:136-151`).

### مسار التنفيذ والترحيل

1. الواجهة تستدعي `OpeningBalanceUiService`.
2. الخدمة توجّه إلى `IOpeningBalanceEngine` (`Services/Finance/OpeningBalanceUiService.cs:199-209`).
3. `OpeningBalanceEngine.CreateAsync` يتحقق، يحل أسماء/معرفات السطر، يولد الرقم، ينشئ المستند وأثر التدقيق ويحفظه (`OpeningBalanceEngine.cs:106-145`).
4. `SubmitAsync` ثم `ApproveAsync` يغيران الحالة ويضيفان events.
5. `PostAsync` لا يسمح بالترحيل إلا من `Approved`، يبني سطور القيد، يستدعي `IIntegratedAccountingService.PostOpeningBalanceDocumentAsync`، ثم يطبق أثر الطرف ويعلّم المستند Posted/Locked (`:221-278`).
6. الاختصار `PostPartyOpeningBalanceAsync` ينفذ Create → Submit → Approve → Post → Lock آلياً، ويرفض التكرار إن كانت `Customers.OpeningBalancePosted=true` (`:281-353`).

### القيد المحاسبي وأثر AR

لرصيد عميل بقيمة `X`، المسار الموحّد الحالي يبني:

- مدين: `1100 ذمم عملاء` بقيمة `X` مع `PartyId=CustomerId`.
- دائن: `3100 أرصدة افتتاحية` بقيمة `X` دون PartyId.

بعد نجاح القيد، `ApplyPartyEffectsAsync` يستدعي `Customer.MarkOpeningBalancePosted(line.Amount)`، فتُضبط العلامة `OpeningBalancePosted=true` ويضاف المبلغ إلى `Customer.Balance` (`OpeningBalanceEngine.cs:714-725` و`PartyEntities.cs:64-72`).

### كيف يفترض أن يظهر في كشف العميل، وما المشكلة الحالية

`GetCustomerStatementHandler` و`GetCustomerAccountLedgerHandler` يبدآن الرصيد الجاري من رصيد افتتاحي مستخرج من قيود الأستاذ إذا كانت علامة العميل مفعلة. بعد ذلك يضيفان الفواتير ويطرحان القبوض/المرتجعات (`CustomerQueryHandlers.cs:184-228` و`GetCustomerAccountLedgerHandler.cs:33-36,155-170`).

لكن `GetPartyOpeningBalanceAsync` يرشح بدقة على `JournalEntries.SourceType` المرسل إليه (`AccountingReportRepository.cs:197-220`). الاستعلامان يرسلان `DocumentType.CustomerOpeningBalance`، بينما `PostOpeningBalanceDocumentAsync` للمسار الموحّد يسجل القيد باسم `DocumentType.FinanceOpeningBalance` (`IntegratedAccountingService.cs:421-445`). النتيجة وفق الكود الحالي: علامة العميل ورصيد entity قد يتحدثان، لكن استعلام كشف الحساب قد يعيد رصيداً افتتاحياً صفراً. لا توجد بيانات فعلية حالياً لاختبار ذلك: العملاء = 0 والمستندات الافتتاحية = 0.

## 3. بنية المخزون الحالية

### ما الموجود باسم رصيد/أول مدة

الموجود فعلياً:

- واجهة `Controls/Inventory/InventoryOpeningStockFormControl.cs` بعنوان «مواد أول المدة».
- أوامر `CreateOpeningStockCommand`, `PostOpeningStockCommand` في `ERPSystem.Application/Commands/Inventory/InventoryCommands.cs`.
- handlers في `ERPSystem.Application/UseCases/Inventory/InventoryHandlers.cs:358-395`.
- repository writer في `InventoryManagementRepository.cs:1458-1482`.
- محرك `InventoryEngine.PostOpeningStockAsync` في `InventoryEngine.cs:856-943`.
- جداول `inventory.opening_stock_documents` و`inventory.opening_stock_lines`.
- نوع حركة `MovementType.OpeningBalance` ونوع مرجع `DocumentType.OpeningBalance`.
- مسار مالي موحد `OpeningBalanceType.OpeningStock` و`PostFinanceOpeningBalanceStockAsync` (`InventoryEngine.cs:945-1021`).

إذن الميزة ليست جديدة كلياً، لكنها جزئية وغير مناسبة بعد لتفصيل أطوال 10,000 رول.

### حقول الكتالوج والرول كما هي في القاعدة

#### `catalog.fabric_items`

`Id uuid`, `CompanyId uuid`, `CategoryId uuid`, `Code text`, `NameAr text`, `NameEn text`, `DefaultUnit text`, ثم حقول التدقيق `CreatedAt timestamptz`, `CreatedByUserId uuid?`, `UpdatedAt timestamptz?`, `UpdatedByUserId uuid?`, `IsActive boolean`, `IsArchived boolean`.

لا يوجد `CostPerMeter` في `fabric_items` الحالي.

#### `catalog.fabric_colors`

`Id uuid`, `FabricItemId uuid`, `Code text`, `NameAr text`, `NameEn text`، ثم حقول التدقيق القياسية نفسها.

#### `public."FabricRolls"`

`Id uuid`, `ContainerId uuid`, `FabricItemId uuid`, `FabricColorId uuid`, `WarehouseId uuid`, `RollNumber integer`, `LengthMeters numeric`, `WeightKg numeric?`, `Status integer`, حقول التدقيق، `ContainerItemId uuid?`, `CostPerMeter numeric NOT NULL DEFAULT 0`, `LotCode text?`, `RemainingLengthMeters numeric NOT NULL DEFAULT 0`, `SalePricePerMeter numeric?`, `FabricBatchId uuid?`, `StorageLocationId uuid?`, `Barcode text?`, `QrCode text?`, `QualityStatus integer NOT NULL DEFAULT 0`, `ReservationStatus integer NOT NULL DEFAULT 0`.

#### `inventory.fabric_batches`

`Id uuid`, `BatchNumber varchar(50)`, `SupplierId uuid?`, `ContainerId uuid?`, `PurchaseInvoiceId uuid?`, `ArrivalDate timestamptz`, `LandingCostPerMeter numeric(18,4)`, `CurrencyCode text`, `TotalMeters numeric(18,4)`, `RollCount integer`, `WarehouseId uuid`, `StorageLocationId uuid?`, `QualityStatus integer`, `Status integer`، ثم حقول التدقيق.

### بنية المستند والحركة والتجميع والتقييم

#### `inventory.opening_stock_documents`

`Id uuid`, `DocumentNumber varchar(50)`, `WarehouseId uuid`, `OpeningDate timestamptz`, `Reference text?`, `CurrencyCode text`, `Status integer`, `Notes text?`, `PostedAt timestamptz?`، حقول التدقيق والإلغاء.

#### `inventory.opening_stock_lines`

`Id uuid`, `DocumentId uuid`, `FabricItemId uuid`, `FabricColorId uuid`, `FabricRollId uuid?`, `FabricBatchId uuid?`, `StorageLocationId uuid?`, `QuantityMeters numeric(18,4)`, `RollCount integer`, `UnitCost numeric(18,4)`, `TotalValue numeric(18,4)`، ثم حقول التدقيق.

#### `inventory.warehouse_stocks`

`Id uuid`, `WarehouseId uuid`, `FabricItemId uuid`, `FabricColorId uuid`, `ContainerId uuid`, `RollCount integer`, `TotalMeters numeric(18,4)`, `ReservedMeters numeric(18,4)`, `AvailableMeters numeric(18,4)`، ثم حقول التدقيق. المفتاح التشغيلي في الكود هو المستودع + الصنف + اللون + الحاوية.

#### `public."StockMovements"`

`Id uuid`, `MovementNumber text`, `MovementDate timestamptz`, `Type integer`, `WarehouseId uuid`, `ReferenceType integer?`, `ReferenceId uuid?`, `Status integer`, `PostedAt timestamptz?`، حقول التدقيق والإلغاء، `SourceWarehouseId uuid?`, `DestinationWarehouseId uuid?`, `SourceLocationId uuid?`, `DestinationLocationId uuid?`, `Reason text?`, `UserId uuid?`.

#### `inventory.stock_movement_lines`

`Id uuid`, `MovementId uuid`, `FabricItemId uuid`, `FabricColorId uuid`, `FabricRollId uuid?`, `FabricBatchId uuid?`, `ContainerId uuid`, `RollCount integer`, `QuantityMeters numeric(18,4)`, `UnitCost numeric(18,4)`, `TotalValue numeric(18,4)`, `CurrencyCode text`، ثم حقول التدقيق.

#### `inventory.inventory_valuation_snapshots`

`Id uuid`, `WarehouseId uuid`, `FabricItemId uuid?`, `FabricColorId uuid?`, `ContainerId uuid?`, `Method integer`, `QuantityMeters numeric(18,4)`, `UnitCost numeric(18,4)`, `TotalValue numeric(18,4)`, `CurrencyCode text`, `SnapshotDate timestamptz`, `MovementId uuid?`، ثم حقول التدقيق.

### من يكتب هذه الجداول اليوم

- **اعتماد/تفعيل حاوية الصين:** `InventoryEngine.ActivateContainerAsync` ينشئ batch، رولات `FabricRolls`، أرصدة `warehouse_stocks`، حركة Import وخطوطها، قيد تفعيل المخزون، ثم valuation snapshot (`InventoryEngine.cs:14-171`). هذه هي البيانات الفعلية الحالية: 509 رول و8 أرصدة و8 snapshots؛ كل الحركات المنشورة الموجودة Type=Import وعددها 8 خطوط بإجمالي 47,837 متر وقيمة 15,885.0001.
- **فاتورة شراء مخزنية:** `PostPurchaseInvoiceAsync` ينشئ batch/rrolls/stock/movement ثم snapshot.
- **المبيعات:** الحجز يعدّل `ReservedMeters` و`AvailableMeters` وحالة الرولات؛ اعتماد/صرف البيع ينقص المخزون والرولات ويسجل حركة Sale ثم snapshot ويحسب COGS.
- **مرتجع البيع:** يزيد المخزون ويسجل SaleReturn ثم snapshot.
- **التحويل والجرد والتسويات:** تستخدم المحرك نفسه وتحدث stock/movements/snapshots حسب العملية.
- **الرصيد الافتتاحي المباشر:** ينشئ رولات، يعمل upsert على `warehouse_stocks` بحاوية `Guid.Empty`، يسجل حركة OpeningBalance ثم snapshot AverageCost (`InventoryEngine.cs:856-943`).
- **الرصيد المالي الموحد:** يولد مستنداً داخلياً في `inventory.opening_stock_*` لكل مستودع ثم يستدعي نفس `PostOpeningStockAsync` (`:945-1021`).

### قصور المسار الحالي بالنسبة للمطلوب

- واجهة `InventoryOpeningStockFormControl` تلتقط: المستودع، التاريخ، المرجع، العملة، الملاحظات، ولكل سطر الصنف واللون وإجمالي الأمتار وعدد الرولات وتكلفة المتر (`:16-27,37-52,108-149`).
- لا تلتقط batch/location ولا رقم رول ولا طول رول مفرد.
- `PostOpeningStockAsync` يقسم `QuantityMeters / RollCount` وينشئ كل الرولات بالطول المتساوي (`:886-915`). هذا يفقد أطوال الرولات الحقيقية.
- المسار المباشر لا ينشئ قيداً محاسبياً إطلاقاً؛ handler يستدعي inventory engine فقط (`InventoryHandlers.cs:382-395`). المسار المالي الموحّد هو الذي ينشئ قيد 1200/3100 ثم حركة المخزون.
- مسار `finance.opening_balance_lines` يخزن `ItemName` و`ColorName` كنص ولا يخزن `FabricItemId` أو `FabricColorId`; عند الترحيل يحل الأسماء إلى المعرفات (`InventoryEngine.cs:991-1009`). هذا أقل أماناً من استخدام المعرفات مباشرة.

## 4. تفصيل أطوال رولات فواتير المبيعات

### الجداول والحقول الفعلية في القاعدة

`sales.sales_invoice_roll_details` يحتوي:

- `Id uuid`
- `SalesInvoiceId uuid`
- `SalesInvoiceItemId uuid`
- `RollSequence integer`
- `FabricRollId uuid?`
- `LengthMeters numeric(18,4)`
- `EnteredByUserId uuid?`
- `EnteredAt timestamptz?`
- حقول التدقيق القياسية

الفهرس الفريد معرف في التهيئة على `(SalesInvoiceItemId, RollSequence)` (`SalesConfigurations.cs:44-53`). عدد السجلات الحالي صفر.

الكود الحالي أضاف أيضاً `DraftRollNumber int?` و`DraftLengthMeters decimal?` إلى entity (`SalesEntities.cs:97-108`) ويحفظهما في repository (`AggregateRepositories.cs:489-503`)، لكن العمودين **غير موجودين في قاعدة 5432** لأن ترحيل `20260718120000_AddSalesInvoiceRollDetailDraftFields` غير مطبق.

### آلية العمل والتحقق الفعلية

1. عند إضافة سطر فاتورة بعدد رولات `RollCount`، ينشئ aggregate صف تفصيل لكل تسلسل من 1 إلى العدد (`SalesInvoiceAggregate.cs:96-102`).
2. شاشة `WarehouseDetailingWorkspaceControl` تسمح بإدخال serial و/أو طول. إذا أدخل serial، يحاول handler حل الرول الحقيقي واستخدام طوله المتبقي؛ وإلا يستخدم الطول اليدوي.
3. parser يقبل طول decimal أكبر من صفر فقط، ويقبل serial integer أكبر من صفر فقط (`WarehouseDetailingWorkspaceControl.cs:378-390`).
4. `ValidateAllRolls` يرفض صفاً خالياً تماماً، serial غير صالح، أو طولاً مكتوباً غير موجب (`:395-423`).
5. domain يرفض الإكمال إن بقي أي `SalesInvoiceRollDetail` بلا `LengthMeters > 0` (`SalesInvoiceAggregate.cs:215-229`) ويعيد حساب `LineTotal = Sum(LengthMeters) × UnitPrice` لكل سطر (`SalesEntities.cs:81-90`).
6. الاعتماد يعيد التحقق من صلاحية جميع الأطوال (`SalesInvoiceAggregate.cs:263-268`).

### هل يوجد تحقق مجموع الأطوال مقابل إجمالي مدخل؟

**لا.** `sales_invoice_items` لا يحتوي حقل كمية/إجمالي أمتار؛ يحتوي `RollCount`, `UnitPrice`, `LineTotal`. لا توجد قيمة طول إجمالي مستقلة يدخلها المستخدم ليقارن بها النظام. النظام يعتبر مجموع أطوال الرولات هو الإجمالي المرجعي ويحسب منه قيمة السطر. لذلك لا يمكن نسخ «تحقق تطابق المجموع» من المبيعات كما وصفت المهمة؛ الموجود القابل لإعادة الاستخدام هو صفوف الرولات، parsing، حل serial إلى `FabricRollId`، واشتراط طول موجب لكل رول.

## 5. آلية إنشاء القيد اليومية

### المسار القياسي

`OpeningBalanceEngine.PostAsync` → `IntegratedAccountingService.PostOpeningBalanceDocumentAsync` → `PostViaEngineAsync` → `IAccountingPostingEngine.PostAsync` → `IJournalEntryRepository.AddAsync`.

`PostingRequest` يتطلب (`ERPSystem.Application/Posting/PostingModels.cs:15-31`):

- `CompanyId`, `BranchId`
- `SourceType`, `SourceId`, `PostingKind`
- `PostingDate`
- `UserId`
- `Description`, `JournalBookId`
- `Lines`; وكل سطر يتطلب `AccountId`, `Debit`, `Credit`, `Narrative`، مع `PartyId?` و`SourceLineId?` اختياريين.
- حقول اختيارية: `CurrencyId`, `ExchangeRate`, `IdempotencyKey`, `CorrelationId`, `Metadata`.

`PostViaEngineAsync` يأخذ سياق الفرع/الشركة/المستخدم الحالي، يبني الطلب، يستدعي المحرك، ويرمي `AccountingException` إذا فشل (`IntegratedAccountingService.cs:501-554`).

### ضمان التوازن والسلامة

`AccountingPostingEngine.ValidateRequest` (`AccountingPostingEngine.cs:178-206`) يفرض:

- company/branch/source/user غير فارغة.
- سطر واحد على الأقل.
- لا مبالغ سالبة.
- لا يجتمع مدين ودائن موجبان في السطر نفسه.
- `abs(totalDebit - totalCredit) <= 0.01`.

ثم يتحقق من وجود كل الحسابات ونشاطها (`:221-237`). بعد إنشاء aggregate، `AccountingPostingPolicy.EnsureCanPost` يعيد فرض وجود سطور والتوازن ضمن 0.01 قبل تغيير الحالة إلى Posted (`AccountingPostingPolicy.cs:7-22`).

المحرك يحمي من الترحيل المكرر بهوية ثابتة من company + source type + source id + posting kind، ويعيد القيد الموجود عند إعادة المحاولة (`AccountingPostingEngine.cs:27-49,208-249`).

بالنسبة لرصيد المخزون المطلوب، المسار الموجود بالفعل يبني مدين `InventoryAsset` ودائن `OpeningBalanceEquity` لكل سطر ثم يجمع السطور المتشابهة قبل الإرسال (`OpeningBalanceEngine.cs:604-711`).

## 6. سوابق تكلفة المتر والتقييم

لا يوجد حقل تكلفة افتراضية في `catalog.fabric_items`. السوابق الفعلية هي:

- `public."FabricRolls"."CostPerMeter" numeric NOT NULL DEFAULT 0`: تكلفة الرول التشغيلية المستخدمة في البحث والتكلفة والمبيعات.
- `inventory.fabric_batches."LandingCostPerMeter" numeric(18,4)`: تكلفة وصول الدفعة.
- `china_import.container_fabric_type_lines."LandedCostPerMeterUsd" numeric(18,6)`: تكلفة المتر المحملة بالدولار على مستوى نوع القماش في الحاوية.
- `inventory.stock_movement_lines."UnitCost" numeric(18,4)` و`TotalValue numeric(18,4)`: تكلفة الحركة وقيمتها.
- `inventory.inventory_valuation_snapshots."UnitCost" numeric(18,4)` و`TotalValue numeric(18,4)`: تكلفة لقطة التقييم.
- `inventory.opening_stock_lines."UnitCost" numeric(18,4)`: السابقة المباشرة لرصيد أول المدة.
- `finance.opening_balance_lines."UnitCost" numeric(18,4) NULL`: السابقة في المسار المالي الموحّد.

في استيراد الصين، المحرك يضع تكلفة الرول من `LandedCostPerMeterUsd × ExchangeRateToLocalCurrency` عند توفر type line، ويضعها في `FabricRoll.CostPerMeter`; بينما خطوط الحركة والتقييم تستعمل تكلفة الحركة/المتوسط (`InventoryEngine.cs:95-121,148-170`). في الرصيد المباشر، `OpeningStockLine.UnitCost` ينتقل إلى كل رول وإلى خط الحركة، ثم تُنشأ لقطة `AverageCost` (`:909-941`).

إذن الاسم المتسق لإدخال تكلفة المتر في ميزة الرصيد هو **`UnitCost` على سطر المستند**، ثم يُرحّل إلى **`CostPerMeter` على الرول**؛ ولا ينبغي اختراع تكلفة على `FabricItem`.

## الموجود مقابل المطلوب بناؤه لاحقاً

| المجال | موجود الآن | النقص بالنسبة لإدخال المخزون الحقيقي |
|---|---|---|
| حسابات GL | 1200 و3100 موجودان ونشطان | لا حاجة لإنشاء حساب جديد |
| قيد المخزون الافتتاحي | مدين 1200 / دائن 3100 موجود في المحرك الموحد | يجب اعتماد المسار الموحد وعدم استعمال المسار المخزني المباشر وحده |
| مستند أول مدة | واجهة وجداول وhandlers ومحرك موجودة | لا توجد دورة اعتماد في المسار المباشر ولا قيد GL |
| إنشاء الرولات | ينشئ عدد الرولات المطلوب | يقسم الإجمالي بالتساوي؛ لا يقبل أطوالاً فردية أو أرقام رولات حقيقية |
| تجميع المخزون | upsert إلى `warehouse_stocks` موجود | يجب تغذيته من مجموع الأطوال الحقيقية |
| التقييم | movement + snapshot موجودان | يجب الحفاظ على `UnitCost` والتقييم نفسه |
| تفصيل الرولات | نمط المبيعات موجود | لا يوجد تحقق مجموع مقابل إجمالي سابق، وترحيلات draft غير مطبقة |
| حل الصنف/اللون | المسار القديم يستخدم IDs؛ المالي يستخدم أسماء | الأفضل في التصميم اللاحق توحيد المسار على IDs، لكن هذا التقرير لا ينفذ التصميم |
| الأداء لـ10,000 رول | لا يوجد مسار bulk ظاهر؛ الإضافة تتم داخل loops عبر EF | يحتاج تصميم إدخال/استيراد وترحيل bulk لاحقاً |
| كشف العميل كمرجع | واجهة ودورة وقيد موجودة | يوجد mismatch في SourceType يجب حسمه قبل نسخه كمرجع موثوق |

## نتيجة الاكتشاف

الأساس المحاسبي المطلوب موجود بالفعل وبالأسماء الصحيحة: **المخزون 1200 مقابل أرصدة افتتاحية 3100**، ولا ينبغي إشراك مورد أو AP. كذلك توجد بنية حركة وتقييم ورولات ومخزون افتتاحي قابلة لإعادة الاستخدام. ما يحتاج بناءً جديداً ليس الحساب أو محرك الحركة الأساسي، بل طبقة إدخال وتخزين آمنة لأطوال الرولات الفردية، وربطها بالمسار المالي الموحد، مع معالجة فروقات المسارين وعدم توافق كشف العميل/SourceType وترحيلات قاعدة البيانات قبل التنفيذ.

هذا التقرير لم ينشئ أو يعدل حساباً أو قيداً أو سجلاً أو migration، ولم يغير أي واجهة.
