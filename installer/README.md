# Desktop installer — اتصال مباشر SSL بـ alamal-ab.org (بدون SSH)

## بناء المثبّت

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

الناتج: `publish\AlamalAB-ERP-Setup.exe`

## ما يفعله المثبّت

1. يثبّت التطبيق مع أيقونة سطح المكتب (نفس أيقونة الموقع).
2. يطلب **كلمة مرور erp_app** أثناء التثبيت.
3. يكتب `appsettings.Local.json` باتصال مباشر:
   - `Host=alamal-ab.org;Port=5432;SSL Mode=Require`
   - **بدون نفق SSH**
4. يتحقق التطبيق من الاتصال قبل التشغيل ويعرض رسالة واضحة عند الخطأ.

## SSH tunnel

للمطورين فقط: فعّل `SshTunnel.Enabled=true` مع `Host=localhost` في `appsettings.Local.json`.
أجهزة الشركة لا تحتاج SSH.

