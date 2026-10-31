param(
    [switch]$Run
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$dist = Join-Path $root "dist"
$exeFull = Join-Path $dist "PromptPalette.exe"

if (-not (Test-Path $csc)) {
    throw "C# compiler not found: $csc"
}
if (-not (Test-Path (Join-Path $root "assets\app.ico"))) {
    python (Join-Path $root "tools\make_icon.py")
}
if (-not (Test-Path $dist)) {
    New-Item -ItemType Directory -Path $dist | Out-Null
}

Push-Location $root
try {
    $sources = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse |
        Sort-Object FullName |
        ForEach-Object { $_.FullName.Substring($root.Length + 1) }

    # csc (legacy compiler) cannot resolve non-ASCII paths for Win32 resources.
    # Stage resources and the output in an ASCII temp folder before compiling.
    $stage = Join-Path ([System.IO.Path]::GetTempPath()) "promptpalette-build"
    if (-not (Test-Path $stage)) { New-Item -ItemType Directory -Path $stage | Out-Null }
    Copy-Item (Join-Path $root "assets\app.ico") (Join-Path $stage "app.ico") -Force
    Copy-Item (Join-Path $root "app.manifest") (Join-Path $stage "app.manifest") -Force
    $stagedExe = Join-Path $stage "PromptTags.exe"

    $arguments = @(
        "/nologo",
        "/target:winexe",
        "/platform:anycpu",
        "/optimize+",
        "/codepage:65001",
        "/langversion:5",
        "/out:$stagedExe",
        "/win32icon:$stage\app.ico",
        "/win32manifest:$stage\app.manifest",
        "/reference:System.dll",
        "/reference:System.Core.dll",
        "/reference:System.Drawing.dll",
        "/reference:System.Windows.Forms.dll",
        "/reference:System.Runtime.Serialization.dll",
        "/reference:System.Xml.dll"
    ) + $sources

    Write-Host "Building PromptPalette..." -ForegroundColor Cyan
    $output = & $csc $arguments 2>&1
    if ($LASTEXITCODE -ne 0) {
        $output | ForEach-Object { Write-Host $_ -ForegroundColor Red }
        throw "Build failed (exit code $LASTEXITCODE)"
    }
    if ($output) { $output | ForEach-Object { Write-Host $_ } }
    Copy-Item -LiteralPath $stagedExe -Destination $exeFull -Force

    $size = (Get-Item $exeFull).Length / 1KB
    Write-Host ("Build complete: {0}  ({1:N0} KB)" -f $exeFull, $size) -ForegroundColor Green

    # Keep the no-exe fallback launcher and runtime icon in sync with the source.
    $launcher = Join-Path $root "tools\make_launcher.ps1"
    if (Test-Path $launcher) {
        & $launcher
    }
    Copy-Item (Join-Path $root "assets\app.ico") (Join-Path $dist "app.ico") -Force
    $distAssets = Join-Path $dist "assets"
    if (-not (Test-Path $distAssets)) { New-Item -ItemType Directory -Path $distAssets | Out-Null }
    Copy-Item (Join-Path $root "assets\app.ico") (Join-Path $distAssets "app.ico") -Force
}
finally {
    Pop-Location
}

if ($Run) {
    Start-Process -FilePath $exeFull
}
