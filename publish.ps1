<#
    publish.ps1 — Publish script for ZProbe (Dual Mode: Full & Lite)
    Adheres to AgentOption .NET Publish Release standard & ZeroUniverse rules.
#>
[CmdletBinding()]
param(
    [ValidateSet('Full', 'Lite', 'All')]
    [string]$Mode = 'All',
    [string]$Configuration = 'Release',
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$Root = $PSScriptRoot
$Proj = Join-Path $Root "src\ZProbe.UI\ZProbe.UI.csproj"
$Dist = Join-Path $Root "publish"
$EngineExe = Join-Path $Root "engine.exe"

if (Test-Path $Dist) {
    Remove-Item $Dist -Recurse -Force -ErrorAction SilentlyContinue
}

if ($Mode -eq 'Full' -or $Mode -eq 'All') {
    Write-Host ">>> Publishing ZProbe FULL (Self-Contained Single File)..." -ForegroundColor Cyan
    $outFull = Join-Path $Dist "full"
    dotnet publish $Proj -c $Configuration -r $Runtime --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:EnableCompressionInSingleFile=true `
        -o $outFull
    if (Test-Path $EngineExe) {
        Copy-Item $EngineExe -Destination $outFull -Force
        Write-Host "  ✔ Bundled engine.exe into Full package" -ForegroundColor Green
    }
    Write-Host "  ✔ Full build generated at: $outFull\ZProbe.exe" -ForegroundColor Green
}

if ($Mode -eq 'Lite' -or $Mode -eq 'All') {
    Write-Host ">>> Publishing ZProbe LITE (Framework-Dependent Single File)..." -ForegroundColor Cyan
    $outLite = Join-Path $Dist "lite"
    dotnet publish $Proj -c $Configuration -r $Runtime --self-contained false `
        -p:PublishSingleFile=true `
        -o $outLite
    if (Test-Path $EngineExe) {
        Copy-Item $EngineExe -Destination $outLite -Force
        Write-Host "  ✔ Bundled engine.exe into Lite package" -ForegroundColor Green
    }
    Write-Host "  ✔ Lite build generated at: $outLite\ZProbe.exe" -ForegroundColor Green
}

Write-Host ">>> ZProbe publish completed successfully!" -ForegroundColor Green
