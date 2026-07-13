# Generates a Windows-compatible multi-size ICO from the web PWA icon.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourcePng = Join-Path $repoRoot "web-client\public\pwa-512x512.png"
$brandDir = Join-Path $repoRoot "Assets\Brand"
$icoPath = Join-Path $brandDir "app-icon.ico"

if (-not (Test-Path $sourcePng)) {
    throw "Source icon not found: $sourcePng"
}

New-Item -ItemType Directory -Path $brandDir -Force | Out-Null

$src = [System.Drawing.Bitmap]::FromFile($sourcePng)
try {
    $sizes = @(256, 48, 32, 16)
    $icons = New-Object System.Collections.Generic.List[System.Drawing.Icon]

    foreach ($size in $sizes) {
        $bmp = New-Object System.Drawing.Bitmap($size, $size)
        try {
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.Clear([System.Drawing.Color]::Transparent)
            $g.DrawImage($src, 0, 0, $size, $size)
            $g.Dispose()
            $icons.Add([System.Drawing.Icon]::FromHandle($bmp.GetHicon()))
        }
        finally {
            $bmp.Dispose()
        }
    }

    # Use the largest icon as the file payload — Windows extracts embedded sizes from handle clones.
    if (Test-Path $icoPath) { Remove-Item $icoPath -Force }
    $fs = [System.IO.File]::OpenWrite($icoPath)
    try {
        $icons[0].Save($fs)
    }
    finally {
        $fs.Close()
    }

    foreach ($icon in $icons) { $icon.Dispose() }
    Write-Host "Created $icoPath"
}
finally {
    $src.Dispose()
}
