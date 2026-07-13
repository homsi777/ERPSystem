# Build desktop publish + Inno Setup installer (if ISCC is available).
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
& (Join-Path $repoRoot "deploy\publish-desktop.ps1")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$isccCandidates = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
    "iscc"
)

$iscc = $isccCandidates | Where-Object { Test-Path $_ -ErrorAction SilentlyContinue } | Select-Object -First 1
if (-not $iscc) {
    $cmd = Get-Command iscc -ErrorAction SilentlyContinue
    if ($cmd) { $iscc = $cmd.Source }
}

if (-not $iscc) {
    Write-Host ""
    Write-Host "Inno Setup not found. Published folder is ready at publish\desktop" -ForegroundColor Yellow
    Write-Host "Install Inno Setup 6, then run: iscc installer\ERPSystem.iss" -ForegroundColor Yellow

    $zipPath = Join-Path $repoRoot "publish\AlamalAB-ERP-Portable-win-x64.zip"
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    Compress-Archive -Path (Join-Path $repoRoot "publish\desktop\*") -DestinationPath $zipPath -Force
    Write-Host "Created portable zip: $zipPath" -ForegroundColor Green
    exit 0
}

Write-Host "Building installer with Inno Setup..." -ForegroundColor Cyan
& $iscc (Join-Path $repoRoot "installer\ERPSystem.iss")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$setupExe = Join-Path $repoRoot "publish\AlamalAB-ERP-Setup.exe"
Write-Host ""
Write-Host "Installer ready: $setupExe" -ForegroundColor Green
