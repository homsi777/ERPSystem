# ERP PRO — خطة تنفيذ تطبيق الويب / PWA (من الألف إلى الياء)

> **الإصدار:** 1.1  
> **التاريخ:** 2026-07-06  
> **الحالة:** معتمدة للتنفيذ — بعد مراجعة الفريق (تعديلات v1.1)  
> **التغييرات v1.1:** تنبيهات أمن WASM، سياسة اتساق الأرقام، إعادة ترتيب المراحل (مخزون قبل التفصيل)، جداول زمنية حسب حجم الفريق  
> **النطاق:** تطبيق ويب/PWA على `https://alamal-ab.org/` يعمل على متصفح (Android / iPhone / Desktop)  
> **ما لا يشمله:** استبدال ERP Desktop (WPF) — الويب **امتداد ميداني** وليس ERP كامل  

---

## جدول المحتويات

1. [الرؤية والأهداف](#1-الرؤية-والأهداف)
2. [تنبيهات ملزمة للمنفّذين ⚠️](#2-تنبيهات-ملزمة-للمنفّذين)
3. [قرارات معمارية ملزمة](#3-قرارات-معمارية-ملزمة)
3. [المكدس التقني والمكتبات](#3-المكدس-التقني-والمكتبات)
4. [هيكل الحل (Solution)](#4-هيكل-الحل-solution)
5. [البنية على VPS والدومين](#5-البنية-على-vps-والدومين)
6. [الأمان والصلاحيات](#6-الأمان-والصلاحيات)
7. [تصميم الواجهة (UX/UI)](#7-تصميم-الواجهة-uxui)
8. [تصميم API](#8-تصميم-api)
9. [الوحدات والشاشات (تفصيل كامل)](#9-الوحدات-والشاشات-تفصيل-كامل)
10. [سير العمل الحرج: تفصيل المستودع](#10-سير-العمل-الحرج-تفصيل-المستودع)
11. [التكامل مع ERP Desktop](#11-التكامل-مع-erp-desktop)
12. [قاعدة البيانات والترحيل](#12-قاعدة-البيانات-والترحيل)
13. [PWA ودعم الموبايل](#13-pwa-ودعم-الموبايل)
14. [الاختبار وضمان الجودة](#14-الاختبار-وضمان-الجودة)
15. [CI/CD و GitHub](#15-cicd-و-github)
16. [المراحل والجدول الزمني](#16-المراحل-والجدول-الزمني)
17. [توزيع الأدوار على الفريق](#17-توزيع-الأدوار-على-الفريق)
18. [المخاطر والتخفيف](#18-المخاطر-والتخفيف)
19. [معايير قبول (Definition of Done)](#19-معايير-قبول-definition-of-done)
20. [ملحق: قائمة NuGet / npm](#20-ملحق-قائمة-nuget--npm)

---

## 1. الرؤية والأهداف

### 1.1 ما المطلوب؟

بناء **تطبيق ويب متقدم** (PWA) يُستخدم من:

| المستخدم | المكان | الاحتياج |
|----------|--------|----------|
| أمين مستودع | المستودع (تابلت/موبايل) | **تفصيل أطوال الأثواب** لفواتير البيع المرسلة من المكتب |
| مدير / محاسب | مكتب أو موبايل | استعلام مخزون، عملاء، مالية |
| (لاحقاً) مبيعات | موبايل | متابعة عملاء، تفاصيل بيع |

### 1.2 تعريف «التسليم» في ERP PRO

> **مهم:** في هذا المشروع، قسم **«التفصيل / التسليم»** في الويب = **فند أطوال Rolls في المستودع** بعد إرسال فاتورة من المحاسب — **وليس** تسليم بضاعة للعميل في الشارع (رغم أن اعتماد الفاتورة قد يُسمّى «اعتماد وتسليم» في Desktop).

### 1.3 أهداف غير قابلة للتفاوض

1. **مصدر واحد للحقيقة:** PostgreSQL واحدة + منطق `ERPSystem.Application` واحد.
2. **موثوقية:** لا فقدان بيانات أثناء التفصيل؛ audit كامل.
3. **اتساق الأرقام:** ما يظهر في الويب = ما يظهر في Desktop — **محسوب من Handlers فقط** (انظر [§2.3](#23-سياسة-اتساق-الأرقام-مع-desktop) و [§8](#8-سياسة-اتساق-الأرقام-مع-desktop)).
4. **RTL / عربي:** واجهة عربية أولاً.
5. **Responsive:** iPhone (375px) + Tablet (768px+) + Desktop browser.
6. **PWA:** إضافة للشاشة الرئيسية على Android/iPhone.
7. **تطوير محلي أولاً:** اعتماد التصميم على `localhost` قبل VPS.

### 1.4 ما لن نفعله في المرحلة الأولى

- ERP ويب كامل (كل وحدات Desktop).
- تطبيق Native (Flutter/React Native).
- قاعدة بيانات منفصلة للويب.
- Offline كامل (مرحلة لاحقة اختيارية).
- طلبات الصين كاملة (مرحلة 4+).

---

## 2. تنبيهات ملزمة للمنفّذين ⚠️

> **يُقرأ هذا القسم قبل أي سطر كود.** ناتج عن مراجعة الفريق — يمنع أخطاء أمنية وسلوكية شائعة في Blazor WASM.

### 2.1 Blazor WASM ≠ أمان — الصلاحيات في الـ API فقط

Blazor **WebAssembly** يُنزَّل ويُنفَّذ **في متصفح المستخدم**. أي كود في `ERPSystem.Web` **مرئي وقابل للتلاعب** (DevTools، تعديل JWT claims محلياً، استدعاء API مباشرة).

| ✅ مسموح في Web (UX فقط) | ❌ ممنوع في Web (أمان) |
|---------------------------|-------------------------|
| إخفاء زر «إكمال التفصيل» إذا `CanDetail=false` | `if (user.IsManager) { save... }` كشرط وحيد |
| تعطيل حقل للقراءة | التحقق من صلاحية دون استدعاء API |
| توجيه المستخدم لصفحة «غير مصرح» | أي قرار يغيّر بيانات أو يتخطى workflow |

**القاعدة:** كل endpoint في `ERPSystem.Api` يستدعي `IPermissionService` **قبل** الـ Handler — حتى لو الواجهة أخفت الزر. اختبار الاختراق = استدعاء API بـ Postman بدون واجهة.

### 2.2 الأرقام: الـ Web يعرض فقط — لا يحسب

انظر [§8 — سياسة اتساق الأرقام](#8-سياسة-اتساق-الأرقام-مع-desktop).

### 2.3 ترتيب التعلم: مخزون (قراءة) → ثم التفصيل

**أول شاشة حقيقية بعد Shell:** استعلام **المخزون** (GET فقط) — لتعلم Blazor/MudBlazor/API على تعقيد منخفض.  
**ثانياً:** **التفصيل** (POST/PUT) — أعقد شاشة في المشروع.  
(انظر [§18 — إعادة ترتيب المراحل](#18-المراحل-والجدول-الزمني).)

### 2.4 الجدول الزمني يعتمد على حجم الفريق

تقدير «10–12 أسبوع» = **فريق 3–5**. مطور واحد أو ثنائي: اضرب التقدير **×2 إلى ×3** ونفّذ **تسلسلياً** (انظر [§18.1](#181-جدول-زمني-حسب-حجم-الفريق)).

---

## 3. قرارات معمارية ملزمة

### 3.1 Clean Architecture — نفس Desktop

```
┌─────────────────────────────────────────────────────────┐
│  ERPSystem.Web (Blazor WASM + MudBlazor)                │  ← Presentation Web
├─────────────────────────────────────────────────────────┤
│  ERPSystem.Api (ASP.NET Core Web API)                   │  ← HTTP + Auth + Swagger
├─────────────────────────────────────────────────────────┤
│  ERPSystem.Application                                  │  ← Commands/Queries/Handlers (موجود)
├─────────────────────────────────────────────────────────┤
│  ERPSystem.Domain                                       │  ← Aggregates (موجود)
├─────────────────────────────────────────────────────────┤
│  ERPSystem.Infrastructure                               │  ← EF + Repos (موجود)
└─────────────────────────────────────────────────────────┘
         ▲                              ▲
         │                              │
   ERPSystem (WPF)              PostgreSQL على VPS
```

### 3.2 قواعد

| # | القاعدة |
|---|---------|
| R1 | **ممنوع** وضع منطق أعمال في Blazor components |
| R2 | **ممنوع** EF Core references في Web أو Api controllers مباشرة — فقط عبر Handlers |
| R3 | Api controllers = thin: validate → handler → DTO |
| R4 | DTOs من `ERPSystem.Application` — لا DTOs مكررة إلا للـ API contract عند الحاجة |
| R5 | نفس `ApplicationResult` pattern → HTTP status mapping موحّد |
| R6 | Idempotency لعمليات التفصيل (حفظ/إكمال) |
| R7 | **ممنوع** أي قرار أمني أو صلاحية **وحيد** في Blazor WASM — API يفرض دائماً (انظر [§2.1](#21-blazor-wasm--أمان--الصلاحيات-في-ال-api-فقط)) |
| R8 | **ممنوع** حساب مبالغ/أرصدة/إجماليات في Web — عرض DTO من Handler فقط (انظر [§8](#8-سياسة-اتساق-الأرقام-مع-desktop)) |

### 3.3 Repo

**Repo واحد** على GitHub (الموجود) + مشروعان جديدان:

- `ERPSystem.Api`
- `ERPSystem.Web`

---

## 3. المكدس التقني والمكتبات

### 3.1 Backend (API)

| المكوّن | التقنية | الإصدار المستهدف |
|---------|---------|------------------|
| Runtime | .NET | **9.0** (متوافق مع المشاريع الحالية) |
| Framework | ASP.NET Core Web API | 9.0 |
| ORM | EF Core | 9.0 (عبر Infrastructure الموجود) |
| Database | PostgreSQL | 15+ (موجود على VPS) |
| Auth | JWT Bearer | Microsoft.AspNetCore.Authentication.JwtBearer |
| Validation | FluentValidation (اختياري) أو Application validators الموجودة | — |
| API Docs | Swashbuckle (Swagger/OpenAPI) | 7.x |
| Logging | Serilog + Console/File | 4.x |
| Health | AspNetCore.HealthChecks.NpgSql | — |
| CORS | م built-in | whitelist `alamal-ab.org` + localhost dev |

**NuGet إضافية مقترحة للـ Api:**

```
Microsoft.AspNetCore.Authentication.JwtBearer
Swashbuckle.AspNetCore
Serilog.AspNetCore
Serilog.Sinks.File
AspNetCore.HealthChecks.NpgSql
AspNetCore.HealthChecks.UI.Client (اختياري)
Microsoft.AspNetCore.RateLimiting (حماية brute-force)
```

### 3.2 Frontend (Web)

| المكوّن | التقنية | السبب |
|---------|---------|--------|
| UI Framework | **Blazor Web App** — Interactive **WebAssembly** | C# واحد، مشاركة types، فريق .NET |
| Component Library | **MudBlazor** | RTL، Material، responsive، ناضج |
| Icons | Material Icons (مدمج مع MudBlazor) | — |
| HTTP Client | `HttpClient` + **Refit** (اختياري) أو typed client يدوي | typed API calls |
| State | **Fluxor** (اختياري) أو scoped services | للتفصيل المعقّد |
| Localization | `IStringLocalizer` + ar-SA default | عربي |
| PWA | `service-worker.js` + `manifest.webmanifest` | الشاشة الرئيسية |
| CSS | MudBlazor theme + CSS variables مخصصة | هوية Alamal |

**NuGet للـ Web:**

```
MudBlazor
Microsoft.AspNetCore.Components.WebAssembly
Microsoft.AspNetCore.Components.WebAssembly.DevServer
Microsoft.Extensions.Http
Fluxor.Blazor.Web (اختياري — مرحلة 2)
```

### 3.3 لماذا Blazor وليس React؟

| المعيار | Blazor WASM | React + TS |
|---------|-------------|------------|
| مشاركة Application | مباشرة (DTOs) | عبر OpenAPI فقط |
| فريق C# موجود | ✅ | يحتاج front-end dedicated |
| PWA | ✅ | ✅ |
| RTL | MudBlazor جيد | ممتاز مع MUI |
| حجم bundle أولي | أكبر | أصغر |

**القرار:** Blazor WASM + MudBlazor — **قابل للمراجعة** إذا الفريق لديه قسم front-end قوي يفضل React؛ في هذه الحالة يبقى Api كما هو.

### 3.4 أدوات التطوير

| الأداة | الاستخدام |
|--------|-----------|
| Visual Studio 2022 / Rider | تطوير |
| Docker (اختياري) | PostgreSQL محلي + parity مع VPS |
| Postman / Bruno | اختبار API |
| Playwright | E2E للويب |
| xUnit | Unit + integration tests |

---

## 4. هيكل الحل (Solution)

### 4.1 مشاريع جديدة

```
ERPSystem/
├── ERPSystem.Api/
│   ├── Program.cs
│   ├── appsettings.json / appsettings.Development.json
│   ├── Controllers/          (thin)
│   ├── Auth/                 (JWT, login endpoint)
│   ├── Mapping/              (ApplicationResult → IActionResult)
│   ├── Middleware/           (exception, correlation id)
│   └── Dockerfile            (اختياري)
│
├── ERPSystem.Web/
│   ├── Program.cs
│   ├── wwwroot/
│   │   ├── manifest.webmanifest
│   │   ├── service-worker.js
│   │   └── icons/
│   ├── Layout/
│   │   ├── MainLayout.razor      (Shell + bottom/top nav)
│   │   └── NavMenu.razor
│   ├── Pages/
│   │   ├── Home/
│   │   ├── Inventory/
│   │   ├── Customers/
│   │   ├── Finance/
│   │   └── Detailing/            (تفصيل المستودع)
│   ├── Components/               (shared UI)
│   ├── Services/                 (ApiClient, AuthState, TokenStorage)
│   └── Theme/                    (MudTheme RTL)
│
├── ERPSystem.Application/        (موجود — بدون تغيير جذري)
├── ERPSystem.Infrastructure/       (موجود)
├── ERPSystem.Domain/               (موجود)
└── ERPSystem/                      (WPF Desktop — موجود)
```

### 4.2 Solution folders (تنظيم VS)

```
📁 src
   📁 Api
   📁 Web
   📁 Application
   📁 Domain
   📁 Infrastructure
   📁 Desktop
📁 tests
   📁 ERPSystem.Api.Tests
   📁 ERPSystem.Web.Tests
   📁 ERPSystem.Application.Tests (موجود أو جديد)
📁 docs
   📁 Documentation/
```

---

## 5. البنية على VPS والدومين

### 5.1 Topology

```
Internet
    │
    ▼
[ Cloudflare / DNS optional ]
    │
    ▼
https://alamal-ab.org  (SSL ✅ موجود)
    │
    ▼
┌──────────────────────────────────────┐
│  VPS (Linux Ubuntu 22.04+ recommended) │
│  ┌────────────┐                      │
│  │   Nginx    │  reverse proxy       │
│  └─────┬──────┘                      │
│        │                              │
│   ┌────┴─────┐    ┌──────────────┐   │
│   │ Web      │    │ API          │   │
│   │ (static  │    │ Kestrel      │   │
│   │  WASM)   │    │ :5000        │   │
│   └──────────┘    └──────┬───────┘   │
│                          │           │
│                   ┌──────▼───────┐   │
│                   │ PostgreSQL   │   │
│                   │ localhost    │   │
│                   │ :5432        │   │
│                   └──────────────┘   │
└──────────────────────────────────────┘
```

### 5.2 Nginx (مقترح)

| المسار | الهدف |
|--------|--------|
| `/` | Blazor WASM static files (`wwwroot` published) |
| `/api/*` | proxy → `http://127.0.0.1:5000` |
| `/health` | health check |

### 5.3 Process management

- **systemd** units: `erp-api.service`, (optional) `erp-web` if not pure static
- أو **Docker Compose** (api + nginx + postgres volume) — للفريق الكبير أنظف

### 5.4 البيئات

| البيئة | URL | DB |
|--------|-----|-----|
| Development | `https://localhost:7xxx` | local PostgreSQL أو Docker |
| Staging (اختياري) | `staging.alamal-ab.org` | DB staging |
| Production | `https://alamal-ab.org` | VPS PostgreSQL |

### 5.5 Secrets

- **لا** connection strings في GitHub
- `appsettings.Production.json` على VPS فقط أو environment variables
- GitHub Secrets: `DB_CONNECTION`, `JWT_SECRET`, `VPS_SSH_KEY`

---

## 6. الأمان والصلاحيات

### 6.1 Authentication

| العنصر | التفاصيل |
|--------|----------|
| Login | POST `/api/auth/login` — username/password (users table موجودة في seed) |
| Token | JWT access token (15–60 min) + refresh token (7 days) |
| Storage (Web) | `localStorage` أو `sessionStorage` — **httpOnly cookie أفضل** (مرحلة 1.5) |
| HTTPS | إلزامي — موجود على الدومين |

### 6.2 Authorization — ربط بـ permissions الموجودة

النظام Desktop يستخدم permission codes مثل:

- `sales.approve`, `warehouse.detail`, `customers.create`, …

**الخطة:** Api يتحقق من نفس `IPermissionService` **قبل** استدعاء Handler — **في كل endpoint بدون استثناء**.

> ⚠️ **Blazor WASM:** إخفاء الأزرار في الواجهة = **UX فقط**. أي مستخدم يمكنه استدعاء `/api/...` مباشرة. لا تُعتبر `AuthorizeView` أو `if (user.CanX)` في Razor أماناً — انظر [§2.1](#21-blazor-wasm--أمان--الصلاحيات-في-ال-api-فقط) و **R7**.

### 6.3 أدوار Web MVP

| الدور | الصلاحيات التقريبية |
|-------|---------------------|
| `warehouse_clerk` | تفصيل، استعلام مخزون |
| `accountant` | عملاء (قراءة)، مالية (قراءة + سندات لاحقاً) |
| `manager` | كل الأقسام Web |
| `admin` | + إعدادات |

### 6.4 حماية إضافية

- Rate limiting على `/api/auth/login`
- CORS: origins محددة فقط
- PostgreSQL: **لا** port 5432 مفتوح للعامة
- Audit log: استخدام `IAuditLogRepository` الموجود لعمليات التفصيل
- Correlation ID في كل request

---

## 8. سياسة اتساق الأرقام مع Desktop

> **متطلب حرج (1.3، 14، 19)** — هذا القسم يشرح **كيف** يتحقق، لا **ماذا** فقط.

### 8.1 المبدأ

**أي رقم يظهر للمستخدم (رصيد، إجمالي، أمتار، قيمة مخزون، سعر متر) يجب أن يكون:**

1. **محسوباً** في `ERPSystem.Application` (Handler / Domain / Engine)
2. **مُرسلاً** في DTO جاهز للعرض
3. **معروضاً** في Blazor **بدون إعادة حساب** — تنسيق `N2` / `ToString` للعرض فقط

### 8.2 ممنوع في Web

| ❌ ممنوع | ✅ بديل |
|---------|---------|
| `items.Sum(x => x.Price * x.Qty)` في Razor | `dto.GrandTotal` من API |
| تقريب محلي `Math.Round` لقرارات | Handler يُرجع القيمة النهائية |
| تحويل عملة في الواجهة | Handler + `Money` في Domain |
| «إجمالي مؤقت» قبل الحفظ | API `preview` endpoint إن لزم (مرحلة لاحقة) |

### 8.3 التحقق (QA)

| # | اختبار |
|---|--------|
| T1 | نفس `customerId` + نفس `from/to` → Web sales-details = Desktop popup |
| T2 | نفس فلاتر مخزون → Web grid = Desktop `InventoryFabricStockPageControl` |
| T3 | بعد complete detailing → `GrandTotal` Web = Desktop invoice |
| T4 | Integration test: Handler output snapshot مقابل golden file |

### 8.4 Code review checklist

- [ ] لا `Sum` / `Average` / `*` مالي في `.razor` أو Web services
- [ ] كل property رقمية في UI من API response
- [ ] Desktop و Web يستدعيان **نفس Handler** (أو query متطابقة)

---

## 7. تصميم الواجهة (UX/UI)

### 7.1 Shell — الشريط الرئيسي

**Desktop/Tablet:** شريط علوي أفقي  
**Mobile (< 640px):** شريط سفلي (Bottom Navigation) — 5 أيقونات

| # | التبويب | Icon (MDL/Material) | Route |
|---|---------|---------------------|-------|
| 1 | رئيسية | Home | `/` |
| 2 | المخزون | Inventory | `/inventory` |
| 3 | العملاء | People | `/customers` |
| 4 | المالية | AccountBalance | `/finance` |
| 5 | التفصيل | Straighten / Warehouse | `/detailing` |

### 7.2 Header مشترك

- شعار Alamal + اسم الشركة
- زر إشعارات (لاحقاً)
- قائمة المستخدم (اسم، تسجيل خروج)
- مؤشر اتصال (online/offline — لاحقاً)

### 7.3 Design tokens (متوافقة مع Desktop)

| Token | قيمة مقترحة |
|-------|-------------|
| Primary | `#2563EB` (أزرق — متوافق مع ERP cards) |
| Success | `#059669` |
| Warning | `#D97706` |
| Danger | `#DC2626` |
| Font | Segoe UI, Tahoma, Arial |
| Direction | RTL (`dir=rtl`) |
| Border radius | 8–10px |
| Touch target | min 44px height (mobile) |

### 7.4 MudBlazor Theme

- `MudTheme` مخصص + `RightToLeft = true`
- Dark mode: **مرحلة لاحقة** (optional)

### 7.5 حالات UI إلزامية

كل شاشة تحتوي:

- Loading skeleton
- Empty state (لا بيانات)
- Error state (مع retry)
- Success snackbar (MudSnackbar)

---

## 8. تصميم API

### 8.1 Conventions

| Convention | Value |
|------------|-------|
| Base path | `/api/v1` |
| JSON | camelCase |
| Dates | ISO 8601 UTC |
| Errors | `{ "code", "message", "validationErrors": [] }` |
| Pagination | `?page=1&pageSize=50` |
| Auth header | `Authorization: Bearer {token}` |

### 8.2 ApplicationResult → HTTP

| ApplicationResultStatus | HTTP |
|-------------------------|------|
| Success | 200 / 201 |
| ValidationFailed | 400 |
| NotFound | 404 |
| PermissionDenied | 403 |
| Conflict | 409 |
| Failure | 500 |

### 8.3 Endpoints MVP (Phase 1–2)

#### Auth
```
POST   /api/v1/auth/login
POST   /api/v1/auth/refresh
POST   /api/v1/auth/logout
GET    /api/v1/auth/me
```

#### Dashboard (رئيسية)
```
GET    /api/v1/dashboard/summary
GET    /api/v1/dashboard/tasks          # فواتير بانتظار التفصيل، تنبيهات
```

#### Detailing (تفصيل — الأهم)
```
GET    /api/v1/detailing/queue          # فواتير AwaitingDetailing
GET    /api/v1/detailing/{invoiceId}    # تفاصيل + rolls expected
PUT    /api/v1/detailing/{invoiceId}/rolls   # حفظ أطوال (draft)
POST   /api/v1/detailing/{invoiceId}/complete  # CompleteWarehouseDetailing
```

> Handlers موجودة: `CompleteWarehouseDetailingCommand`, `GetDetailingQueue` (Operations queries) — **إعادة استخدام**.

#### Inventory
```
GET    /api/v1/inventory/stock          # FabricStockBalanceDto list + filters
GET    /api/v1/inventory/stock/{fabricId}
GET    /api/v1/inventory/containers     # filter options
GET    /api/v1/inventory/warehouses
```

#### Customers
```
GET    /api/v1/customers                # list/search
GET    /api/v1/customers/{id}
GET    /api/v1/customers/{id}/sales-details?from=&to=
GET    /api/v1/customers/{id}/statement?from=&to=
```

#### Finance (قراءة MVP)
```
GET    /api/v1/finance/cashboxes
GET    /api/v1/finance/receipts/recent
GET    /api/v1/finance/journal/recent
```

### 8.4 Swagger

- `/swagger` في Development + Staging فقط
- **مقفل** في Production

---

## 9. الوحدات والشاشات (تفصيل كامل)

### 9.1 رئيسية (`/`)

| الشاشة | المحتوى | API |
|--------|---------|-----|
| Home Dashboard | KPI cards: فواتير معلقة تفصيل، قيمة مخزون، عملاء متأخرين | dashboard/summary |
| Quick actions | اختصار: «تفصيل»، «استعلام مخزون» | — |
| Recent activity | آخر 10 حركات | audit / movements |

**Mobile:** cards عمودية، swipe horizontal للـ KPIs.

---

### 9.2 المخزون (`/inventory`)

| الشاشة | المحتوى |
|--------|---------|
| Stock list | جدول/بطاقات: قماش، كود، لون، حاوية، rolls، أمتار، متاح، قيمة |
| Filters | مستودع + حاوية (نفس `InventoryContainerFilterUi` logic) |
| Stock detail | تفاصيل صنف واحد + حركات حديثة (phase 2) |
| Scan (phase 3) | barcode/QR للroll (optional) |

**Reuse:** `GetFabricStockAsync`, `FabricStockBalanceDto`, filters from Application.

---

### 9.3 العملاء (`/customers`)

| الشاشة | المحتوى |
|--------|---------|
| Customer search | بحث بالاسم/كود |
| Customer card | رصيد، حد ائتمان، نوع (نقدي/آجل) |
| Sales details | تفاصيل بيع + فلتر تاريخ (موجود Desktop) |
| Statement (phase 2) | كشف حساب مبسّط |

**Reuse:** `GetCustomerSalesDetailsQuery`, `GetCustomerStatementQuery`, credit limit policy.

---

### 9.4 المالية (`/finance`)

| الشاشة | MVP | لاحقاً |
|--------|-----|--------|
| Overview | أرصدة صناديق، إجمالي | — |
| Receipts list | قراءة سندات قبض | إنشاء سند |
| Journal | قراءة آخر قيود | — |

**حساس:** صلاحيات صارمة؛ MVP **قراءة فقط**.

---

### 9.5 التفصيل (`/detailing`) — **قلب المشروع**

| الشاشة | المحتوى |
|--------|---------|
| Queue | قائمة فواتير `AwaitingDetailing`: رقم، عميل، تاريخ، rolls count |
| Detailing workspace | لكل roll: اسم توب، لون، كود، **حقل طول (متر)** |
| Summary | إجمالي أمتار، سعر، حالة الإكمال |
| Actions | **حفظ مسودة** / **إكمال التفصيل** |
| Success | رسالة + العودة للقائمة؛ إشعار Desktop (optional SignalR phase 3) |

**Desktop parity:** نفس `WarehouseDetailingWorkspaceControl` / `CompleteWarehouseDetailingCommand`.

---

## 10. سير العمل الحرج: تفصيل المستودع

### 10.1 Sequence

```
محاسب (Desktop)                API/DB                    أمين (Web)
      │                          │                            │
      │ Create draft invoice     │                            │
      │ Send to warehouse        │                            │
      │─────────────────────────►│ Status=AwaitingDetailing   │
      │                          │◄───────────────────────────│ GET queue
      │                          │                            │ Enter roll lengths
      │                          │◄───────────────────────────│ PUT rolls (draft)
      │                          │                            │
      │                          │◄───────────────────────────│ POST complete
      │                          │ Status=Detailed/Ready      │
      │◄── notification ─────────│                            │
      │ Approve + deliver        │                            │
      │ (Desktop)                │                            │
```

### 10.2 قواعد موثوقية

1. **Optimistic concurrency:** `RowVersion` على invoice — رفض إذا Desktop عدّل concurrently.
2. **Validation:** كل roll length > 0 قبل complete (domain rule موجود).
3. **Idempotency-Key** header على POST complete.
4. **Autosave:** PUT rolls every 30s أو on blur (debounced).
5. **Audit:** user id + timestamp لكل save/complete.

### 10.3 Offline (مرحلة لاحقة)

- IndexedDB queue للrolls عند انقطاع النت
- Sync عند عودة الاتصال
- **ليس MVP**

---

## 11. التكامل مع ERP Desktop

### 11.1 الآن (Phase 0)

| العنصر | الحالة |
|--------|--------|
| PostgreSQL | مشترك — Desktop يكتب، Web يقرأ/يكتب |
| Migrations | Infrastructure فقط — `dotnet ef` |
| Users/Permissions | seed مشترك |

### 11.2 لاحقاً (Phase 3+)

| Feature | التقنية |
|---------|---------|
| Desktop يستخدم Api بدل local DI | optional — Desktop as Api client |
| Real-time | SignalR hub `DetailingCompleted` |
| Push notifications | Web Push (PWA) |

### 11.3 تعارضات محتملة

- Desktop `IServiceScopeFactory` vs Web HTTP — **لا تعارض** إذا نفس DB + handlers.
- Desktop migrations on startup — Web Api **لا** يشغّل migrate في Production بدون orchestration — **واحد فقط** (Desktop installer أو CI migration step).

---

## 12. قاعدة البيانات والترحيل

### 12.1 Connection

- Web Api يستخدم نفس `InfrastructureServiceCollectionExtensions` / `ErpDbContext`
- Connection string من environment: `ERP_CONNECTION_STRING`

### 12.2 Migrations

- **مصدر واحد:** `ERPSystem.Infrastructure/Migrations`
- CI step: `dotnet ef database update` قبل deploy Api
- **Backup يومي:** `pg_dump` cron على VPS

### 12.3 Indexes للWeb queries

مراجعة indexes على:

- `sales_invoices(status, customer_id, invoice_date)`
- `inventory_movements(warehouse_id, container_id)`
- audit logs by date

---

## 13. PWA ودعم الموبايل

### 13.1 manifest.webmanifest

```json
{
  "name": "Alamal ERP",
  "short_name": "Alamal",
  "start_url": "/",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#2563EB",
  "lang": "ar",
  "dir": "rtl",
  "icons": [ ... 192, 512 ... ]
}
```

### 13.2 Service Worker (MVP)

- Cache static assets (WASM, CSS, JS)
- **Network-first** لـ `/api/*`
- Update prompt عند deploy جديد

### 13.3 iOS Safari

- «Add to Home Screen» — يعمل مع manifest
- Safe area insets CSS (`env(safe-area-inset-bottom)`) للـ bottom nav

### 13.4 Android Chrome

- PWA install banner
- Fullscreen standalone

---

## 14. الاختبار وضمان الجودة

### 14.1 Unit Tests

| Target | أمثلة |
|--------|-------|
| Application handlers | CompleteDetailing, GetSalesDetails |
| Api mapping | ApplicationResult → status codes |
| Auth | JWT generation/validation |

### 14.2 Integration Tests

- `WebApplicationFactory` + Testcontainers PostgreSQL
- Detailing flow end-to-end

### 14.3 E2E (Playwright)

- Login → Detailing queue → enter lengths → complete
- RTL snapshot tests
- Mobile viewport 375x812

### 14.4 Manual QA checklist

- [ ] RTL layout
- [ ] iPhone Safari PWA
- [ ] Android Chrome PWA
- [ ] أرقام تطابق Desktop (اختبارات T1–T4 من §8)
- [ ] صلاحيات محترمة **في API** (Postman بدون token = 401/403)

---

## 15. CI/CD و GitHub

### 15.1 Branches

| Branch | Purpose |
|--------|---------|
| `main` | Production |
| `develop` | Integration |
| `feature/web-*` | features |

### 15.2 GitHub Actions (مقترح)

**Workflow: `build.yml`** (on PR)

```yaml
- dotnet restore
- dotnet build --configuration Release
- dotnet test
```

**Workflow: `deploy-production.yml`** (on tag / manual)

```yaml
- dotnet publish ERPSystem.Api -c Release -o ./publish/api
- dotnet publish ERPSystem.Web -c Release -o ./publish/web
- scp/rsync to VPS
- systemctl restart erp-api
- nginx reload
```

### 15.3 Rollback

- Keep last 3 releases on VPS (`/var/www/erp/releases/{timestamp}`)
- Symlink `current` → active release

---

## 16. المراحل والجدول الزمني

> **v1.1:** أُعيد ترتيب المراحل — **المخزون (قراءة)** قبل **التفصيل** لتعلم Blazor على شاشة أبسط أولاً.  
> التقديرات أدناه **ليست** «موعد تسليم» — هي نطاقات حسب حجم الفريق.

### 16.1 جدول زمني حسب حجم الفريق

| حجم الفريق | Phase 0–5 (تقريبي) | أسلوب العمل |
|------------|-------------------|-------------|
| **1 مطور** (+ AI/Cursor) | **20–30 أسبوع** | تسلسلي — task صغير ينتهي قبل التالي |
| **2 مطور** | **14–20 أسبوع** | Backend/Web بالتناوب أو توازي محدود |
| **3–5 مطور** | **10–12 أسبوع** | Streams متوازية (انظر §17) |
| **6+ مطور** | **8–10 أسابيع** | Parallel + QA مخصص |

**قاعدة:** فريق صغير **لا** ينسخ جدول 3–5 — ينفّذ Phase 1 (مخزون) كاملاً قبل Phase 2 (تفصيل).

### Phase 0 — Foundation (أسبوع 1–2 | ×2 للفرد)

| # | Task | Output |
|---|------|--------|
| 0.1 | Create Api + Web projects in solution | builds |
| 0.2 | Wire DI (Application + Infrastructure) | health check OK |
| 0.3 | JWT auth + login screen | authenticated session |
| 0.4 | Shell: layout + 5 tabs (placeholder) | localhost demo |
| 0.5 | MudBlazor RTL theme | design approval |
| 0.6 | ApiResult middleware + Swagger | dev docs |

**Exit:** فتح `localhost` → login → تنقل بين 5 tabs.

---

### Phase 1 — Inventory read-only MVP (أسبوع 3–4) 📦 *أول شاشة حقيقية*

> **لماذا أولاً؟** GET فقط، فلاتر، جدول، KPI cards — تعلم Blazor + API + RTL بدون مخاطر كتابة أو concurrency.

| # | Task |
|---|------|
| 1.1 | GET `/api/v1/inventory/stock` + warehouse/container filters |
| 1.2 | Web: `/inventory` — list + filters + summary cards |
| 1.3 | Web: `/` home — KPI placeholders + link to inventory |
| 1.4 | Parity test T2 vs Desktop |
| 1.5 | Design approval على الموبايل |

**Exit:** أمين مستودع يستعلم عن رصيد قماش من الموبايل — أرقام = Desktop.

---

### Phase 2 — Detailing MVP (أسبوع 5–8) ⭐ *الأهم — الأصعب*

| # | Task |
|---|------|
| 2.1 | GET detailing queue API |
| 2.2 | GET invoice detailing workspace API |
| 2.3 | PUT save roll lengths |
| 2.4 | POST complete detailing |
| 2.5 | Web: queue page |
| 2.6 | Web: detailing workspace (mobile-first, tablet) |
| 2.7 | Autosave + validation UX |
| 2.8 | Integration tests + RowVersion conflicts |
| 2.9 | Audit logging |

**Exit:** أمين مستودع يُكمل تفصيل فاتورة من الموبايل → Desktop يرى `Detailed`.

---

### Phase 3 — Customers + Finance read (أسبوع 9–10)

| # | Task |
|---|------|
| 3.1 | Customer search + detail |
| 3.2 | Sales details with date filter (parity T1) |
| 3.3 | Finance overview read-only |

---

### Phase 4 — Production deploy (أسبوع 11–12)

| # | Task |
|---|------|
| 4.1 | Nginx config on VPS |
| 4.2 | GitHub Actions deploy |
| 4.3 | DNS → VPS (alamal-ab.org) |
| 4.4 | PWA manifest + icons |
| 4.5 | SSL verify |
| 4.6 | Backup + monitoring |
| 4.7 | UAT with real users |

---

### Phase 5 — Hardening (أسبوع 13–14)

| # | Task |
|---|------|
| 5.1 | Rate limiting, security review |
| 5.2 | Performance (WASM trim, lazy load) |
| 5.3 | Playwright E2E suite |
| 5.4 | Optional: SignalR notifications |
| 5.5 | Optional: offline draft queue |

---

## 17. توزيع الأدوار على الفريق

### 17.1 فريق 3–5 (متوازي)

| Role | مسؤوليات |
|------|-----------|
| **Tech Lead / Architect** | Api design, DI, review handlers reuse |
| **Backend Dev (×2)** | Controllers, auth, detailing endpoints, tests |
| **Blazor Dev (×2)** | Shell, pages, MudBlazor, PWA |
| **UX/UI** | Wireframes mobile-first, RTL review |
| **DevOps** | VPS, Nginx, GitHub Actions, backups |
| **QA** | Test plans, Playwright, device matrix |
| **Product / Manager** | Accept screens per phase |

### 17.2 مطور واحد أو ثنائي (تسلسلي — موصى به لوضعكم الحالي)

| الترتيب | Task | لا تبدأ قبل |
|---------|------|-------------|
| 1 | Phase 0 كامل | — |
| 2 | Phase 1 Inventory API + UI | Phase 0 معتمد |
| 3 | Parity test T2 | Phase 1 |
| 4 | Phase 2 Detailing API | Phase 1 |
| 5 | Phase 2 Detailing UI | Detailing API يعمل في Swagger |
| 6 | Deploy Phase 4 | Phase 2 UAT |

**قاعدة task:** كل task = **≤ 1–2 يوم** — PR صغير، merge، ثم التالي. **لا** parallel streams.

### Parallel workstreams (فريق 3–5 فقط)

```
Stream A: Api + Auth + Detailing API
Stream B: Web Shell + Theme + Login
Stream C: DevOps + CI
Stream D: QA test cases (from week 1)
```

---

## 18. المخاطر والتخفيف

| Risk | Impact | Mitigation |
|------|--------|------------|
| WASM bundle size slow on 3G | High | Lazy load routes, compress, CDN |
| Concurrent Desktop + Web edit | High | RowVersion + conflict message |
| Lost roll lengths on disconnect | High | Autosave + (later) offline queue |
| WASM false sense of security | High | R7 + API-only auth + Postman penetration test |
| Numbers differ Web vs Desktop | High | R8 + parity tests T1–T4 (§8) |
| Permission only in Blazor UI | High | Code review + R7 |
| Migration conflict Desktop/Web | Medium | Single migration runner in CI |
| RTL bugs in MudBlazor | Medium | QA on real devices |
| JWT theft | Medium | HTTPS, short expiry, httpOnly cookies phase 2 |

---

## 19. معايير قبول (Definition of Done)

### لكل شاشة Web

- [ ] Responsive 375 / 768 / 1280
- [ ] RTL verified
- [ ] Loading / empty / error states
- [ ] API errors shown in Arabic
- [ ] لا `Sum`/حساب مالي في `.razor` (R8)
- [ ] Permission-gated في API (R7) — UI hide فقط UX
- [ ] Manual test on Chrome + Safari mobile

### للنشر Production

- [ ] HTTPS green
- [ ] Health check green
- [ ] DB backup verified
- [ ] Rollback tested
- [ ] 3 real detailing invoices UAT passed
- [ ] Numbers match Desktop report

---

## 20. ملحق: قائمة NuGet / npm

### ERPSystem.Api.csproj (add)

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.*" />
<PackageReference Include="Swashbuckle.AspNetCore" Version="7.*" />
<PackageReference Include="Serilog.AspNetCore" Version="9.*" />
<PackageReference Include="AspNetCore.HealthChecks.NpgSql" Version="9.*" />
```

### ERPSystem.Web.csproj (add)

```xml
<PackageReference Include="MudBlazor" Version="8.*" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly" Version="9.*" />
<PackageReference Include="Microsoft.AspNetCore.Components.WebAssembly.DevServer" Version="9.*" PrivateAssets="all" />
```

### ProjectReferences (both)

```xml
<ProjectReference Include="..\ERPSystem.Application\ERPSystem.Application.csproj" />
<ProjectReference Include="..\ERPSystem.Infrastructure\ERPSystem.Infrastructure.csproj" />
```

> **Note:** Web WASM **لا** ي-reference Infrastructure directly في client — فقط Api calls. Infrastructure reference **Api only**.

---

## ملخص تنفيذي للمدير

1. **نبني محلياً** → نعتمد التصميم → **نرفع VPS** `alamal-ab.org`.
2. **Tech:** ASP.NET Core 9 API + Blazor WASM + MudBlazor + PostgreSQL.
3. **أول شاشة حقيقية:** **استعلام مخزون** (قراءة) — ثم **تفصيل** المستودع.
4. **5 تبويبات:** رئيسية، مخزون، عملاء، مالية، تفصيل.
5. **الجدول:** 10–12 أسبوع (فريق 3–5) | **20–30 أسبوع** (مطور واحد) — انظر §16.1.
6. **Desktop يبقى** للمحاسبة الثقيلة؛ الويب للميدان.
7. **تنبيهات ملزمة:** §2 (أمان WASM، R7/R8، لا حساب في Web).

---

## موافقة الفريق (يُملأ بعد المراجعة)

| الاسم | الدور | الموافقة | تاريخ | ملاحظات |
|-------|-------|----------|-------|---------|
| | | ☐ | | |
| | | ☐ | | |

---

*نهاية الوثيقة — ERP PRO Web Implementation Plan v1.1*
