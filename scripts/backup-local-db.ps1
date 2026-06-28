<#
.SYNOPSIS
    Phase 19 - SAFE local PostgreSQL backup. Runs pg_dump (read-only) against the LOCAL
    database only and writes a timestamped custom-format dump. Refuses any production DB host.
    Non-destructive by construction: pg_dump never modifies the source database.

.DESCRIPTION
    Produces a compressed custom-format dump (-F c) suitable for pg_restore. The output goes
    under .runtime\backups\ by default (gitignored), or -OutDir. Secrets are never printed.

.PARAMETER OutDir
    Directory for the dump file. Default: <workspace>\.runtime\backups.

.PARAMETER TargetDb
    Database to back up. Default: POSTGRES_DB from the workspace .env (derasax_local).

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\backup-local-db.ps1
#>
param(
    [string]$OutDir,
    [string]$TargetDb
)

. "$PSScriptRoot\_common.ps1"
$ErrorActionPreference = "Continue"

$envVals = Get-WorkspaceEnv
Assert-NotProductionDb -EnvVals $envVals   # refuse a production DB host

$pgBin = Get-PgBin
if (-not $pgBin) { Write-Err "PostgreSQL client tools (pg_dump) not found."; exit 1 }

$host_ = "localhost"
$port  = $envVals['POSTGRES_PORT']
$user  = $envVals['POSTGRES_USER']
$db    = if ($TargetDb) { $TargetDb } else { $envVals['POSTGRES_DB'] }

if (-not $OutDir) { $OutDir = Join-Path $RuntimeDir "backups" }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$outFile = Join-Path $OutDir "$db-$stamp.dump"

Write-Step "Backing up LOCAL database '$db' (localhost:$port) -> $outFile"
$env:PGPASSWORD = $envVals['POSTGRES_PASSWORD']
& "$pgBin\pg_dump.exe" -h $host_ -p $port -U $user -d $db -F c -f $outFile
$code = $LASTEXITCODE
Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue

if ($code -eq 0 -and (Test-Path $outFile)) {
    $sizeKb = [math]::Round((Get-Item $outFile).Length / 1KB, 1)
    Write-Ok "Backup complete: $outFile ($sizeKb KB)"
    exit 0
} else {
    Write-Err "pg_dump failed (exit $code)."
    exit 1
}
