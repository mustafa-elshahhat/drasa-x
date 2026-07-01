<#
.SYNOPSIS
    Stop EVERY process started by start-local.ps1 (backend, AI service, both
    frontends, and local PostgreSQL), using the PID metadata recorded under
    .runtime\. PRESERVES the database data directory.

.DESCRIPTION
    Processes are stopped by their RECORDED PID (and child tree), never by a
    blind port sweep, so an unrelated process that merely shares a port is never
    killed. Safe to run repeatedly. Use reset-local-db.ps1 to wipe data.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\stop-local.ps1
#>

. "$PSScriptRoot\_common.ps1"

$envVals = Get-WorkspaceEnv
Write-Step "Stopping local stack (native)"

# Tracked native processes (backend, AI, unified frontend) - stop by recorded PID.
Write-Step "Stopping tracked services (by PID, incl. child trees)"
Stop-Tracked -Name "frontend-auth"   -Label "frontend (Vite)"
Stop-Tracked -Name "airag"           -Label "ai service (uvicorn)"
Stop-Tracked -Name "backend"         -Label "backend (dotnet)"

# Local PostgreSQL (native) - stop the cluster, KEEP the data directory.
$pgBin = Get-PgBin
if ($pgBin -and (Test-Path (Join-Path $PgData "PG_VERSION"))) {
    & "$pgBin\pg_isready.exe" -p $envVals['POSTGRES_PORT'] *> $null
    if ($LASTEXITCODE -eq 0) {
        Write-Step "Stopping local PostgreSQL (data directory preserved)"
        & "$pgBin\pg_ctl.exe" -D "$PgData" -m fast stop | Out-Null
        Write-Ok "PostgreSQL stopped (data kept at $PgData)"
    } else {
        Write-Host "  (local PostgreSQL not running)" -ForegroundColor Gray
    }
}

# Clear the recorded runtime snapshot (logs are kept for inspection).
$procJson = Join-Path $RuntimeDir "processes.json"
if (Test-Path $procJson) { Remove-Item $procJson -Force -ErrorAction SilentlyContinue }

Write-Ok "Local stack stopped. Database data is preserved."
exit 0
