# نشر ERPSystem على سحابة VPS (Ubuntu)

دليل شامل لنشر النظام على خادم Ubuntu، وربط الدومين **https://alamal-ab.org**،
وتشغيل قاعدة بيانات واحدة مشتركة بين **تطبيق سطح المكتب (WPF)** و**الواجهة السحابية (web)**.

---

## 1) المعمارية (كيف تعمل الأمور)

```
                          ┌───────────────────────────────────────────────┐
                          │                 VPS Ubuntu                     │
                          │                                                │
  المتصفح (alamal-ab.org) │   Nginx (443/HTTPS)                            │
  ───────────────────────►│    ├── /            →  ملفات web-client الثابتة │
                          │    └── /api/        →  http://127.0.0.1:5218    │
                          │                          (ERPSystem.Api)        │
                          │                              │                  │
  تطبيق سطح المكتب (WPF)   │                              ▼                  │
  ───────5432 (SSL)──────►│                      PostgreSQL (erp_pro)       │
                          │                       نفس القاعدة المشتركة       │
                          └───────────────────────────────────────────────┘
```

- **الواجهة (web-client)**: React/Vite تُبنى لملفات ثابتة ويقدّمها Nginx على الدومين.
- **الـ API**: خدمة ASP.NET Core تعمل داخلياً على المنفذ `5218` خلف Nginx تحت المسار `/api`.
- **قاعدة البيانات**: PostgreSQL واحدة. الـ API يتصل بها محلياً، وتطبيق سطح المكتب يتصل بها **عن بُعد** عبر المنفذ `5432` بتشفير SSL.

> تطبيق سطح المكتب يتصل بقاعدة البيانات مباشرة (EF Core). لذلك نفتح منفذ PostgreSQL بشكل آمن بدل جعله يمرّ عبر الـ API.

---

## 2) المتطلبات قبل البدء

1. خادم **Ubuntu 22.04 أو 24.04** مع صلاحية `root`.
2. **سجل DNS**: وجّه الدومين إلى IP الخادم قبل تشغيل السكربت:
   - `A` record: `alamal-ab.org`  →  `IP-الخادم`
   - `A` record: `www.alamal-ab.org` → `IP-الخادم` (اختياري)
3. إذا كان مستودع GitHub خاصاً: جهّز Personal Access Token لوضعه في `REPO_URL`.

---

## 3) خطوات النشر

على الخادم (بعد رفع المشروع أو استنساخه):

```bash
# 1) انسخ ملف الإعدادات وعدّله
cp deploy/.env.example deploy/.env
nano deploy/.env          # عدّل الدومين، كلمة مرور القاعدة، البريد، رابط المستودع...
chmod 600 deploy/.env

# 2) شغّل الإعداد الكامل (مرة واحدة)
sudo bash deploy/setup-vps.sh
```

السكربت يقوم تلقائياً بـ:
- تثبيت: .NET 9 SDK، Node.js 20، PostgreSQL، Nginx، Certbot، ufw.
- إنشاء قاعدة البيانات ومستخدم التطبيق.
- تطبيق ترحيلات EF Core (بناء المخطط).
- بناء ونشر الـ API كخدمة `systemd`.
- بناء الواجهة ونشرها على Nginx.
- **HTTPS**: إن كانت `MANAGE_SSL="no"` (الشهادة مثبتة مسبقاً) يكتب إعداد Nginx يشير للشهادة الموجودة ويتخطّى الإصدار. وإن كانت `"yes"` يصدر الشهادة عبر Let's Encrypt.
- ضبط جدار الحماية (فتح 80/443/22 و5432 حسب الإعداد).

عند الانتهاء ستظهر بطاقة ملخّص فيها سلسلة اتصال تطبيق سطح المكتب.

---

## 4) ربط تطبيق سطح المكتب بقاعدة البيانات السحابية

في جهاز المستخدم، عدّل سلسلة الاتصال في تطبيق سطح المكتب (`appsettings.json`) إلى:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=alamal-ab.org;Port=5432;Database=erp_pro;Username=erp_app;Password=كلمة-المرور;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

- `SSL Mode=Require` يفرض التشفير.
- `Trust Server Certificate=true` لأن الخادم يستخدم شهادة PostgreSQL الافتراضية (snakeoil).
  لاحقاً يمكنك تركيب شهادة موثوقة وإزالة هذا الخيار.

> **مهم للأمان**: في ملف `deploy/.env` اضبط `DB_REMOTE_ALLOWED_CIDRS` على عنوان IP مكتبك
> (مثال `203.0.113.10/32`) بدل `0.0.0.0/0` حتى لا تكون القاعدة مفتوحة للإنترنت بالكامل.

---

## 5) التحديثات اللاحقة

لأي تحديث للشيفرة (بعد `git push`):

```bash
sudo bash deploy/deploy-app.sh
```

يسحب آخر شيفرة، يعيد بناء الـ API والواجهة، ويعيد تشغيل الخدمات دون لمس إعداد الخادم.

---

## 6) أوامر تشغيل ومراقبة

| الغرض | الأمر |
|------|-------|
| حالة الـ API | `systemctl status erpsystem-api` |
| سجل الـ API الحيّ | `journalctl -u erpsystem-api -f` |
| إعادة تشغيل الـ API | `systemctl restart erpsystem-api` |
| اختبار إعداد Nginx | `nginx -t && systemctl reload nginx` |
| فحص صحة الـ API | `curl https://alamal-ab.org/health` |
| تجديد الشهادة (تلقائي) | `certbot renew --dry-run` |
| حالة جدار الحماية | `ufw status verbose` |

---

## 7) ملاحظات أمنية موصى بها

- احصر منفذ قاعدة البيانات (5432) بعناوين IP معروفة عبر `DB_REMOTE_ALLOWED_CIDRS`.
- استخدم كلمة مرور قوية لـ `DB_APP_PASSWORD`، ولا تشارك ملف `.env`.
- `JWT_SECRET` يُولَّد تلقائياً ويُحفظ في `.env` — لا تغيّره بعد التشغيل وإلا ستُبطَل جلسات الدخول.
- خذ نسخة احتياطية دورية لقاعدة البيانات:
  ```bash
  sudo -u postgres pg_dump erp_pro | gzip > /root/erp_pro_$(date +%F).sql.gz
  ```
