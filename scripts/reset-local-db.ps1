<#
.SYNOPSIS
    Reset the LOCAL development database: drop + recreate, reapply migrations,
    and (when enabled) reapply development seed data.

.DESCRIPTION
    DESTRUCTIVE. Requires the explicit -ConfirmReset switch. Refuses to operate
    on anything that is not a local database.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\reset-local-db.ps1 -ConfirmReset
#>
param(
    [switch]$ConfirmReset
)

. "$PSScriptRoot\_common.ps1"

if (-not $ConfirmReset) {
    Write-Err "Refusing to reset without explicit confirmation."
    Write-Host "Re-run with the -ConfirmReset switch:"
    Write-Host "    scripts\reset-local-db.ps1 -ConfirmReset"
    exit 2
}

$envVals = Get-WorkspaceEnv
$envVals["POSTGRES_HOST"] = "localhost"

# --- Safety: only ever touch a LOCAL database --------------------------------
Write-Step "Safety checks"
Assert-NotProductionDb -EnvVals $envVals
$dbName = $envVals['POSTGRES_DB']
if ($dbName -notmatch "local") {
    throw "Refusing to reset database '$dbName': name does not look local (expected to contain 'local')."
}
Write-Ok "Target is a local database: $dbName"

$pgBin = Get-PgBin
if (-not $pgBin) { throw "PostgreSQL client tools (pg_ctl) not found." }

if (-not (Test-PgReady -PgBin $pgBin -Port $envVals['POSTGRES_PORT'])) {
    Write-Step "Starting local PostgreSQL"
    if (-not (Start-LocalPostgres -PgBin $pgBin -Port $envVals['POSTGRES_PORT'] -DataDir $PgData -LogFile $PgLog)) {
        throw "PostgreSQL did not become ready."
    }
}

$env:PGPASSWORD = "postgres"
$role = $envVals['POSTGRES_USER']

Write-Step "Dropping and recreating database '$dbName'"
& "$pgBin\psql.exe" -h localhost -p $envVals['POSTGRES_PORT'] -U postgres -d postgres -c `
    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$dbName' AND pid<>pg_backend_pid();" | Out-Null
& "$pgBin\psql.exe" -h localhost -p $envVals['POSTGRES_PORT'] -U postgres -d postgres -c "DROP DATABASE IF EXISTS $dbName;" | Out-Null
& "$pgBin\psql.exe" -h localhost -p $envVals['POSTGRES_PORT'] -U postgres -d postgres -c "CREATE DATABASE $dbName OWNER $role;" | Out-Null
Write-Ok "Database recreated (empty)"

Write-Step "Reapplying EF Core migrations"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project "$BackendInfraDir" --startup-project "$BackendApiDir" | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Migration failed." }
Write-Ok "Migrations applied"

if ($envVals['SEED_ENABLED'] -eq "true") {
    Write-Step "Reapplying development seed data (one-shot backend run)"
    # The backend seeds idempotently on startup in Development. We build once and
    # run the published DLL directly (a single, easy-to-stop process - unlike
    # `dotnet run`, which leaves a build-server/launcher behind), wait for the
    # seed to complete (the server reaches /health/ready), then stop it by port.
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ASPNETCORE_URLS = "http://localhost:$($envVals['BACKEND_PORT'])"
    $env:Seed__Enabled = "true"

    dotnet build "$BackendApiDir\DerasaX.Api.csproj" -c Release | Out-Null
    $dll = Join-Path $BackendApiDir "bin\Release\net9.0\DerasaX.Api.dll"

    $proc = Start-Process -FilePath "dotnet" -ArgumentList @("`"$dll`"") `
        -WorkingDirectory $BackendApiDir -WindowStyle Minimized -PassThru
    $ready = $false
    for ($i = 0; $i -lt 80; $i++) {
        try {
            $r = Invoke-WebRequest "http://localhost:$($envVals['BACKEND_PORT'])/health/ready" -UseBasicParsing -TimeoutSec 3
            if ($r.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if ($ready) { Write-Ok "Seed completed (backend reached ready state)" }
    else { Write-Warn2 "Backend did not reach ready state; check logs." }

    # Stop the one-shot process (and anything left on the port).
    try { Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue } catch {}
    $conn = Get-NetTCPConnection -LocalPort $envVals['BACKEND_PORT'] -State Listen -ErrorAction SilentlyContinue
    if ($conn) { $conn | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue } }
    Write-Ok "One-shot seed backend stopped"
} else {
    Write-Warn2 "SEED_ENABLED is not 'true' - skipped reseeding."
}

Write-Ok "Local database reset complete."
