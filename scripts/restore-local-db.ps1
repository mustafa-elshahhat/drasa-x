<#
.SYNOPSIS
    Phase 19 - SAFE local PostgreSQL restore. REFUSES to run without -ConfirmRestore (exit 2),
    refuses any production DB host, and by DEFAULT restores into an ISOLATED scratch database
    (derasax_restore_test) so it never overwrites developer data. Restoring over the primary dev
    database requires explicitly passing -TargetDb <devdb> together with -ConfirmRestore.

.DESCRIPTION
    Drops + recreates the target database (local only), then pg_restore's the given dump into it
    and prints a sanity row count. Secrets are never printed.

.PARAMETER BackupFile
    Path to a custom-format dump produced by backup-local-db.ps1 (required).

.PARAMETER TargetDb
    Database to restore INTO. Default: derasax_restore_test (isolated scratch DB).

.PARAMETER ConfirmRestore
    Required. Without it the script refuses and exits 2 (safe by default).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\restore-local-db.ps1 -BackupFile X.dump -ConfirmRestore
#>
param(
    [Parameter(Mandatory = $true)][string]$BackupFile,
    [string]$TargetDb = "derasax_restore_test",
    [switch]$ConfirmRestore
)

. "$PSScriptRoot\_common.ps1"
$ErrorActionPreference = "Continue"

if (-not $ConfirmRestore) {
    Write-Err "Refusing to restore without -ConfirmRestore. This recreates the target database '$TargetDb'."
    Write-Host "  Re-run with -ConfirmRestore to proceed (default target is the isolated 'derasax_restore_test')." -ForegroundColor Yellow
    exit 2
}

$envVals = Get-WorkspaceEnv
Assert-NotProductionDb -EnvVals $envVals

if (-not (Test-Path $BackupFile)) { Write-Err "Backup file not found: $BackupFile"; exit 1 }

$pgBin = Get-PgBin
if (-not $pgBin) { Write-Err "PostgreSQL client tools (pg_restore/psql) not found."; exit 1 }

$port = $envVals['POSTGRES_PORT']
$user = $envVals['POSTGRES_USER']
$devDb = $envVals['POSTGRES_DB']

if ($TargetDb -eq $devDb) {
    Write-Warn2 "Target is the PRIMARY dev database ('$devDb'). This DESTROYS current dev data."
}

$env:PGPASSWORD = $envVals['POSTGRES_PASSWORD']
try {
    Write-Step "Recreating target database '$TargetDb' (localhost:$port)"
    # Terminate existing connections, then drop + recreate (run against the maintenance DB).
    $term = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '$TargetDb' AND pid <> pg_backend_pid();"
    & "$pgBin\psql.exe" -h localhost -p $port -U $user -d postgres -v ON_ERROR_STOP=0 -c $term *> $null
    & "$pgBin\psql.exe" -h localhost -p $port -U $user -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS `"$TargetDb`";"
    & "$pgBin\psql.exe" -h localhost -p $port -U $user -d postgres -v ON_ERROR_STOP=1 -c "CREATE DATABASE `"$TargetDb`";"
    if ($LASTEXITCODE -ne 0) {
        Write-Err "Could not (re)create '$TargetDb' as role '$user'."
        Write-Host "  This is expected under DB least-privilege: the application role lacks CREATEDB." -ForegroundColor Yellow
        Write-Host "  Run this restore with the migration/admin DB principal (a CREATEDB-capable role)." -ForegroundColor Yellow
        Write-Host "  The dump's restorability can still be validated locally with: pg_restore --list <dump>" -ForegroundColor Yellow
        exit 3
    }

    Write-Step "Restoring '$BackupFile' -> '$TargetDb'"
    & "$pgBin\pg_restore.exe" -h localhost -p $port -U $user -d $TargetDb --no-owner --no-privileges $BackupFile
    $rc = $LASTEXITCODE
    # pg_restore can emit non-fatal warnings (exit 1) while still restoring; verify by querying.
    $count = & "$pgBin\psql.exe" -h localhost -p $port -U $user -d $TargetDb -tAc 'SELECT COUNT(*) FROM "__EFMigrationsHistory";' 2>$null
    $count = "$count".Trim()

    if ($count -match '^\d+$' -and [int]$count -gt 0) {
        Write-Ok "Restore verified: '$TargetDb' has $count applied migrations (pg_restore exit $rc)."
        exit 0
    } else {
        Write-Err "Restore verification failed (migration count='$count', pg_restore exit $rc)."
        exit 1
    }
} finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}
