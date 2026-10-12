# Builds the Rust Week on PEAK BepInEx plugin with the Roslyn compiler.
# Usage: powershell -ExecutionPolicy Bypass -File build.ps1

$ErrorActionPreference = "Stop"

$Root      = Split-Path -Parent $MyInvocation.MyCommand.Path
$Src       = Join-Path $Root "src"
$Build     = Join-Path $Root "build"
$OutDir    = Join-Path $Build "plugins"
$OutDll    = Join-Path $OutDir "RustWeekOnPEAK.dll"
$Csc       = if ($env:ROSLYN_CSC) { $env:ROSLYN_CSC } else { "C:\Users\user\AppData\Local\Temp\opencode\roslyn\x\tasks\net472\csc.exe" }

$PeakManaged = "C:\Program Files (x86)\Steam\steamapps\common\PEAK\PEAK_Data\Managed"
$BepCore     = "C:\Program Files (x86)\Steam\steamapps\common\PEAK\BepInEx\core"

if (-not (Test-Path $Csc)) { throw "Roslyn csc.exe not found at $Csc (set ROSLYN_CSC)." }
if (-not (Test-Path $PeakManaged)) { throw "PEAK managed folder not found at $PeakManaged." }
if (-not (Test-Path $BepCore)) { throw "BepInEx core folder not found at $BepCore." }

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# 0Harmony20 ships Harmony 2.0 and clashes with HarmonyX's HarmonyPatch; skip it.
$skip = @("0Harmony20.dll")
$refs = @()
$refs += (Get-ChildItem $PeakManaged -Filter *.dll | Where-Object { $skip -notcontains $_.Name } | ForEach-Object { $_.FullName })
$refs += (Get-ChildItem $BepCore -Filter *.dll | Where-Object { $skip -notcontains $_.Name } | ForEach-Object { $_.FullName })
$refArgs = $refs | ForEach-Object { "-r:`"$_`"" }

$sources = Get-ChildItem $Src -Filter *.cs | ForEach-Object { $_.FullName }

$args = @(
  "-nologo",
  "-target:library",
  "-nostdlib+",
  "-langversion:7.3",
  "-optimize+",
  "-deterministic",
  "-out:`"$OutDll`""
) + $refArgs + $sources

Write-Host "Compiling $($sources.Count) source files -> $OutDll"
& $Csc @args
if ($LASTEXITCODE -ne 0) { throw "Compilation failed ($LASTEXITCODE)." }

Write-Host "Built $OutDll ($((Get-Item $OutDll).Length) bytes)"
