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
