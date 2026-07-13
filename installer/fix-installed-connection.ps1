# يصلّح appsettings.Local.json للتثبيت الحالي بدون إعادة التثبيت
param(
    [string]$Password = "12345678",
    [string]$InstallDir = "${env:ProgramFiles}\AlamalAB\ERPSystem"
)

$json = @"
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=65.21.136.217;Port=5432;Database=erp_pro;Username=erp_app;Password=$Password;SSL Mode=Prefer;Trust Server Certificate=true"
  },
  "SshTunnel": { "Enabled": false }
}
"@

$path = Join-Path $InstallDir "appsettings.Local.json"
$json | Set-Content -Path $path -Encoding UTF8
Write-Host "تم التحديث: $path" -ForegroundColor Green
Write-Host "Host=65.21.136.217 (وليس alamal-ab.org)"
