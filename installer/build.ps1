<#
.SYNOPSIS
  Compila el instalador MSI del SAD SyncService.

.DESCRIPTION
  1) Publica SyncService y SyncService.ConfigTool en Release/win-x64/self-contained.
  2) Copia ambos publishes a una carpeta de staging.
  3) Compila el .wixproj contra esa staging para generar el MSI.

  Requiere:
    - .NET 9 SDK
    - WiX v5 (dotnet tool install --global wix)

.PARAMETER Version
  Versión del producto (default: 1.0.0.0).

.PARAMETER Configuration
  Configuración de build (default: Release).
#>

[CmdletBinding()]
param(
  [string]$Version = "1.0.0.0",
  [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$here     = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $here
$staging  = Join-Path $here "staging"
$output   = Join-Path $here "output"

Write-Host "=== SAD SyncService installer build ===" -ForegroundColor Cyan
Write-Host "Repo root : $repoRoot"
Write-Host "Staging   : $staging"
Write-Host "Output    : $output"
Write-Host "Version   : $Version"
Write-Host ""

# ---------- Limpiar staging ----------
if (Test-Path $staging) {
  Remove-Item -Recurse -Force $staging
}
New-Item -ItemType Directory -Path $staging | Out-Null
New-Item -ItemType Directory -Path $output -Force | Out-Null

# ---------- Publish SyncService ----------
Write-Host "[1/3] Publicando SyncService..." -ForegroundColor Yellow
dotnet publish (Join-Path $repoRoot "SyncService\SyncService.csproj") `
  -c $Configuration `
  -r win-x64 `
  --self-contained true `
  -o $staging `
  -p:Version=$Version `
  --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish SyncService falló." }

# ---------- Publish ConfigTool ----------
Write-Host "[2/3] Publicando SyncService.ConfigTool..." -ForegroundColor Yellow
dotnet publish (Join-Path $repoRoot "SyncService.ConfigTool\SyncService.ConfigTool.csproj") `
  -c $Configuration `
  -r win-x64 `
  --self-contained true `
  -o $staging `
  -p:Version=$Version `
  --nologo
if ($LASTEXITCODE -ne 0) { throw "Publish ConfigTool falló." }

# Limpiar archivos que no deben ir al MSI
Get-ChildItem -Path $staging -Filter "*.pdb" -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $staging "appsettings.Development.json") -Force -ErrorAction SilentlyContinue
# appsettings.json del repo tiene credenciales reales; el MSI usa appsettings.template.json
Remove-Item -Path (Join-Path $staging "appsettings.json") -Force -ErrorAction SilentlyContinue

Write-Host "Staging contiene $((Get-ChildItem -Path $staging -Recurse -File).Count) archivos."

# ---------- Build MSI ----------
Write-Host "[3/3] Compilando MSI con WiX..." -ForegroundColor Yellow

# WiX v5 corre como dotnet tool — verificar instalación
$wixVersion = & wix --version 2>$null
if ($LASTEXITCODE -ne 0) {
  Write-Host "WiX no está instalado. Instalalo con:" -ForegroundColor Red
  Write-Host "    dotnet tool install --global wix" -ForegroundColor Red
  throw "WiX missing"
}
Write-Host "WiX version: $wixVersion"

# El .wixproj usa MSBuild — pasamos StagingDir y ProductVersion como properties
dotnet build (Join-Path $here "SADSyncService.wixproj") `
  -c $Configuration `
  -p:StagingDir=$staging `
  -p:ProductVersion=$Version `
  -p:OutputPath=$output `
  --nologo
if ($LASTEXITCODE -ne 0) { throw "Build MSI falló." }

$msi = Get-ChildItem -Path $output -Filter "*.msi" -Recurse | Select-Object -First 1
if ($msi) {
  Write-Host ""
  Write-Host "OK: $($msi.FullName)" -ForegroundColor Green
  Write-Host "Tamaño: $([math]::Round($msi.Length / 1MB, 1)) MB"
} else {
  throw "No se generó el MSI."
}
