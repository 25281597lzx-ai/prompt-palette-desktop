$ErrorActionPreference = "Stop"

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$srcDir = Join-Path $root "src"
$dist = Join-Path $root "dist"
$out = Join-Path $dist "PromptPalette.ps1"

$files = Get-ChildItem -Path $srcDir -Filter "*.cs" -Recurse |
    Where-Object { $_.Name -ne "AssemblyInfo.cs" } |
    Sort-Object FullName

$usings = @(
    "using System;",
    "using System.Collections.Generic;",
    "using System.Drawing;",
    "using System.Drawing.Drawing2D;",
    "using System.Drawing.Text;",
    "using System.IO;",
    "using System.Runtime.InteropServices;",
    "using System.Runtime.Serialization;",
    "using System.Runtime.Serialization.Json;",
    "using System.Text;",
    "using System.Windows.Forms;"
)

$chunks = New-Object System.Collections.Generic.List[string]
foreach ($file in $files) {
    $text = Get-Content -Path $file.FullName -Raw -Encoding UTF8
    $lines = $text -split "`r?`n" | Where-Object { $_.TrimStart() -notmatch "^using\s+[A-Za-z_][A-Za-z0-9_.]*\s*;" }
    $chunks.Add(($lines -join "`r`n"))
}

$source = (($usings -join "`n") + "`n`n" + ($chunks -join "`n`n")).TrimEnd()
$source = $source.Replace("`r`n", "`n")

if ($source -match "(?m)^'@") {
    throw "The source contains a PowerShell here-string terminator."
}

$header = @'
# PromptPalette runtime launcher.
# Keeps the full app source in this file and compiles it in memory with Add-Type.
# This path works on machines where Windows Smart App Control blocks unsigned .exe files.
$ErrorActionPreference = "Stop"
$WarningPreference = "SilentlyContinue"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$env:PROMPT_PALETTE_HOME = $root

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;
public static class PPAppID {
    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    public static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
"@
[void][PPAppID]::SetCurrentProcessExplicitAppUserModelID("PromptPalette.Desktop")

$source = @'
'@

$tail = @'
try {
    $types = Add-Type -TypeDefinition $source -Language CSharp -PassThru -ReferencedAssemblies @(
        "System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll",
        "System.Runtime.Serialization.dll", "System.Xml.dll"
    )
    $program = $types | Where-Object { $_.FullName -eq "PromptPalette.Program" } | Select-Object -First 1
    if ($null -eq $program) { throw "PromptPalette.Program was not compiled." }
    $method = $program.GetMethod("Main", [System.Reflection.BindingFlags]::NonPublic -bor [System.Reflection.BindingFlags]::Static)
    if ($null -eq $method) { throw "The app entry point was not found." }

    [void]$method.Invoke($null, @())
}
catch {
    [System.Windows.Forms.MessageBox]::Show(
        "PromptPalette failed to start.`r`n`r`n" + $_.Exception.Message,
        "PromptPalette",
        [System.Windows.Forms.MessageBoxButtons]::OK,
        [System.Windows.Forms.MessageBoxIcon]::Error
    ) | Out-Null
}
'@

$content = $header + "`r`n" + $source + "`r`n'@`r`n`r`n" + $tail
if (-not (Test-Path $dist)) { New-Item -ItemType Directory -Path $dist | Out-Null }
Set-Content -Path $out -Value $content -Encoding UTF8
Write-Host ("Launcher written: {0} ({1:N0} KB)" -f $out, ((Get-Item $out).Length / 1KB)) -ForegroundColor Green
