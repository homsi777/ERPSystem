# Publish self-contained WPF desktop for company installation (win-x64).
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $repoRoot "installer\convert-app-icon.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$outDir = Join-Path $repoRoot "publish\desktop"
$installerDir = Join-Path $repoRoot "publish\installer-input"

Write-Host "Publishing ERPSystem (win-x64, self-contained)..." -ForegroundColor Cyan
dotnet publish (Join-Path $repoRoot "ERPSystem.csproj") `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishReadyToRun=true `
    -o $outDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Stage installer payload (folder publish is more reliable for WPF than single-file).
if (Test-Path $installerDir) { Remove-Item $installerDir -Recurse -Force }
New-Item -ItemType Directory -Path $installerDir | Out-Null
Copy-Item -Path (Join-Path $outDir "*") -Destination $installerDir -Recurse -Force

$iconFile = Join-Path $repoRoot "Assets\Brand\app-icon.ico"
if (Test-Path $iconFile) {
    $brandOut = Join-Path $outDir "Assets\Brand"
    $brandInstaller = Join-Path $installerDir "Assets\Brand"
    New-Item -ItemType Directory -Path $brandOut -Force | Out-Null
    New-Item -ItemType Directory -Path $brandInstaller -Force | Out-Null
    Copy-Item $iconFile $brandOut -Force
    Copy-Item $iconFile $brandInstaller -Force
}

$productionSettings = Join-Path $repoRoot "installer\appsettings.Production.json"
if (Test-Path $productionSettings) {
    Copy-Item $productionSettings (Join-Path $outDir "appsettings.json") -Force
    Copy-Item $productionSettings (Join-Path $installerDir "appsettings.json") -Force
}

$template = Join-Path $repoRoot "installer\appsettings.Company.template.json"
if (Test-Path $template) {
    Copy-Item $template (Join-Path $installerDir "appsettings.Company.template.json") -Force
}

Write-Host ""
Write-Host "Published to: $outDir" -ForegroundColor Green
Write-Host "Installer input: $installerDir" -ForegroundColor Green
