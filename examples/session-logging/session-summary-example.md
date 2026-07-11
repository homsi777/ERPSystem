# ملخص جلسة الأداء — الأمل.AB

## ملخص الجلسة
- **وقت البداية:** 2026-07-11 19:04:33
- **وقت آخر شاشة:** 2026-07-11 19:04:43
- **عدد تحميلات الشاشات:** 4
- **إجمالي وقت التحميل:** 11309.8 ms
- **إجمالي استعلامات قاعدة البيانات:** 28
- **ملف السجل الخام:** `C:\Users\Homsi\AppData\Local\ERPSystem\perf-logs\wpf-performance-session-20260711-160433-cc5f67a26210.jsonl`

## عتبات الأداء
- **تحذير (Warning):** ≥ 100 ms
- **مرتفع (High):** ≥ 500 ms
- **حرج (Critical):** ≥ 1000 ms

## الشاشات المفتوحة (بالترتيب)
| # | الشاشة | الوقت | الاستعلامات | الشدة |
|---:|---|---:|---:|---|
| 1 | Sales.InvoiceList | 6923.6 ms | 6 | حرج |
| 2 | Customers.List | 372.4 ms | 2 | تحذير |
| 3 | Purchases.Invoices | 551.9 ms | 3 | مرتفع |
| 4 | Sales.OperationsCenter | 3461.9 ms | 17 | حرج |

## إجمالي الوقت لكل شاشة (مجمّع)
| الشاشة | مرات الفتح | إجمالي الوقت | إجمالي الاستعلامات | أبطأ مرة |
|---|---:|---:|---:|---:|
| Sales.InvoiceList | 1 | 6923.6 ms | 6 | 6923.6 ms |
| Sales.OperationsCenter | 1 | 3461.9 ms | 17 | 3461.9 ms |
| Purchases.Invoices | 1 | 551.9 ms | 3 | 551.9 ms |
| Customers.List | 1 | 372.4 ms | 2 | 372.4 ms |

## الشاشات الأبطأ (حسب أبطأ مرة واحدة)
| الشاشة | أبطأ مرة | إجمالي الوقت | مرات الفتح |
|---|---:|---:|---:|
| Sales.InvoiceList | 6923.6 ms | 6923.6 ms | 1 |
| Sales.OperationsCenter | 3461.9 ms | 3461.9 ms | 1 |
| Purchases.Invoices | 551.9 ms | 551.9 ms | 1 |
| Customers.List | 372.4 ms | 372.4 ms | 1 |

## أبطأ شاشة في الجلسة
- **الشاشة:** Sales.InvoiceList
- **الوقت:** 6923.6 ms (حرج)
- **الاستعلامات:** 6
- **السبب المحتمل:** Unbounded Loading / Over-fetching
- **معرّف التتبع:** `1037156936ba`

## تنبيهات الأداء (≥ 100 ms)
| الشاشة | الوقت | الشدة | الاستعلامات | السبب المحتمل |
|---|---:|---|---:|---|
| Sales.InvoiceList | 6923.6 ms | حرج | 6 | Unbounded Loading / Over-fetching |
| Sales.OperationsCenter | 3461.9 ms | حرج | 17 | N+1 (high query count) |
| Purchases.Invoices | 551.9 ms | مرتفع | 3 | Sequential Calls / Slow Query |
| Customers.List | 372.4 ms | تحذير | 2 | Sequential Calls / Slow Query |

---
*تم إنشاء هذا الملخص تلقائياً عند إغلاق التطبيق.*
