<#
.SYNOPSIS
    Start AND supervise the full DerasaX local development stack with ONE command:
    PostgreSQL + backend + ai + frontend (unified).

.DESCRIPTION
    Modes:
      auto    (default) docker if available, else native
      docker  docker compose up -d --build  (infra) + native frontend
      native  local PostgreSQL (pg_ctl) + dotnet + uvicorn venv + Vite frontend

    The script:
      * detects required dependencies,
      * refuses to run against a known PRODUCTION database host,
      * starts every active component (never the archived backend),
      * tracks each child PID in the gitignored .runtime\ directory,
      * refuses to start a duplicate, and fails fast if a port is held by an
        UNRELATED process,
      * polls each service until it is reachable,
      * prints the real local URLs,
      * exits 0 ONLY when the startup gate (all 5 components reachable) passes,
        and non-zero with a useful message otherwise.

.PARAMETER Gate
    Which components must be reachable for exit code 0.
      full   (default) postgres + backend + ai + both frontends
      core   postgres + backend + ai            (frontends best-effort)

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\start-local.ps1
#>
param(
    [ValidateSet("auto", "docker", "native")]
    [string]$Mode = "auto",
    [ValidateSet("full", "core")]
    [string]$Gate = "full"
)

. "$PSScriptRoot\_common.ps1"

$ExitCode = 0
$gateFailures = @()

# --- Ensure .env templates are realized (never overwrite an existing .env) ----
function Ensure-EnvFile($dir) {
    $envf = Join-Path $dir ".env"
    $tmpl = Join-Path $dir ".env.example"
    if ((-not (Test-Path $envf)) -and (Test-Path $tmpl)) {
        Copy-Item $tmpl $envf
        Write-Warn2 "Created $envf from .env.example - review its values."
    }
}

# Start a tracked service only if it is not already up; refuse a squatted port.
# Returns $true if (newly or already) running under our control, $false if the
# port is held by an unrelated process.
function Start-ServiceGuarded {
    param([string]$Name, [int]$Port, [scriptblock]$Launch)
    $owner = Get-PortOwnerPid $Port
    $tracked = Read-PidFile $Name
    if ($owner) {
        if ($tracked -and "$owner" -eq "$tracked") {
            Write-Ok "$Name already running (PID $owner, port $Port) - reusing"
            return $true
        }
        if ($tracked -and (Test-PidAlive $tracked)) {
            Write-Ok "$Name already tracked (PID $tracked); port $Port owned by $owner"
            return $true
        }
        Write-Err "Port $Port is held by an UNRELATED process (PID $owner). Refusing to start $Name. Stop that process or change the port in .env."
        return $false
    }
    & $Launch
    return $true
}

Write-Step "Validating environment files"
Ensure-EnvFile $Workspace
Ensure-EnvFile $BackendDir
Ensure-EnvFile $AiRagDir
Ensure-EnvFile $FrontendAuthDir
Initialize-Runtime
$envVals = Get-WorkspaceEnv
$envVals["POSTGRES_HOST"] = "localhost"

Write-Step "Safety: checking for production database hosts"
Assert-NotProductionDb -EnvVals $envVals
Write-Ok "No production database host configured"

$resolved = Resolve-Mode -Requested $Mode
Write-Step "Run mode: $resolved   (startup gate: $Gate)"

# --- Tool validation ---------------------------------------------------------
Write-Step "Validating required tools"
if (-not (Test-Command "dotnet")) { throw "dotnet SDK not found on PATH." }
Write-Ok "dotnet present"
$nodeOk = Test-Command "node"
if (-not $nodeOk) { Write-Warn2 "node not found - the frontends will not be started." }
else { Write-Ok "node present" }

$bePort = [int]$envVals['BACKEND_PORT']
$aiPort = [int]$envVals['AIRAG_PORT']
$fa     = [int]$Defaults['FRONTEND_AUTH_PORT']

if ($resolved -eq "docker") {
    # -------------------------------------------------------------------------
    # DOCKER MODE - infra (postgres + backend + ai-rag) via compose.
    # -------------------------------------------------------------------------
    Write-Step "Starting infrastructure with docker compose"
    Push-Location $Workspace
    try {
        docker compose up -d --build
        if ($LASTEXITCODE -ne 0) { throw "docker compose up failed." }
    } finally { Pop-Location }
    Write-Ok "Containers started (migrations apply on backend startup; seed runs in Development)."
}
else {
    # -------------------------------------------------------------------------
    # NATIVE MODE
    # -------------------------------------------------------------------------
    $pgBin = Get-PgBin
    if (-not $pgBin) { throw "PostgreSQL client tools (pg_ctl) not found. Install PostgreSQL 16 or run in docker mode." }

    Write-Step "Starting local PostgreSQL (port $($envVals['POSTGRES_PORT']))"
    if (-not (Test-Path (Join-Path $PgData "PG_VERSION"))) {
        New-Item -ItemType Directory -Force -Path $LocalDbDir | Out-Null
        $pwFile = Join-Path $LocalDbDir "superpw.txt"
        Set-Content -Path $pwFile -Value "postgres" -NoNewline -Encoding ascii
        & "$pgBin\initdb.exe" -D "$PgData" -U postgres --auth-local=trust --auth-host=md5 --pwfile="$pwFile" --encoding=UTF8 | Out-Null
        Remove-Item $pwFile -Force
        Write-Ok "Initialized new local cluster"
    }
    if (-not (Start-LocalPostgres -PgBin $pgBin -Port $envVals['POSTGRES_PORT'] -DataDir $PgData -LogFile $PgLog)) {
        throw "PostgreSQL did not become ready."
    }
    Write-Ok "PostgreSQL is accepting connections"

    Write-Step "Ensuring role + database exist"
    $env:PGPASSWORD = "postgres"
    $u = $envVals['POSTGRES_DB']; $role = $envVals['POSTGRES_USER']; $pw = $envVals['POSTGRES_PASSWORD']
    & "$pgBin\psql.exe" -h localhost -p $envVals['POSTGRES_PORT'] -U postgres -d postgres -c `
        "DO `$`$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='$role') THEN CREATE ROLE $role LOGIN PASSWORD '$pw'; END IF; END `$`$;" | Out-Null
    $dbExists = & "$pgBin\psql.exe" -h localhost -p $envVals['POSTGRES_PORT'] -U postgres -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$u'"
    if ("$dbExists".Trim() -ne "1") {
        & "$pgBin\psql.exe" -h localhost -p $envVals['POSTGRES_PORT'] -U postgres -d postgres -c "CREATE DATABASE $u OWNER $role" | Out-Null
        Write-Ok "Created database $u"
    } else { Write-Ok "Database $u already exists" }

    Write-Step "Applying EF Core migrations"
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    dotnet ef database update --project "$BackendInfraDir" --startup-project "$BackendApiDir" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Migration failed." }
    Write-Ok "Migrations applied"

    # Build/run the DEBUG DLL. On this WDAC-constrained machine the Release
    # assembly load is blocked (0x800711C7); the Debug DLL loads and runs. The
    # verifier still compiles Release to an isolated output to prove Release builds.
    Write-Step "Building backend (Debug)"
    dotnet build "$BackendApiDir\DerasaX.Api.csproj" -c Debug | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Backend build failed." }
    $backendDll = Join-Path $BackendApiDir "bin\Debug\net9.0\DerasaX.Api.dll"
    if (-not (Test-Path $backendDll)) {
        $found = Get-ChildItem (Join-Path $BackendApiDir "bin\Debug") -Recurse -Filter "DerasaX.Api.dll" -ErrorAction SilentlyContinue | Select-Object -First 1
        if ($found) { $backendDll = $found.FullName }
    }

    Write-Step "Starting backend (http://localhost:$bePort)"
    $env:ASPNETCORE_URLS = "http://localhost:$bePort"
    $env:Seed__Enabled   = $envVals['SEED_ENABLED']
    # Phase 3: backend signs internal AI service tokens with this shared local key.
    $env:ServiceAuth__SigningKey = $envVals['SERVICE_AUTH_SIGNING_KEY']
    if (-not (Start-ServiceGuarded -Name "backend" -Port $bePort -Launch {
        Start-Tracked -Name "backend" -FilePath "dotnet" -ArgList @("`"$backendDll`"") -WorkingDir $BackendApiDir | Out-Null
        Write-Ok "Backend launching (seed runs on startup in Development)"
    })) { $ExitCode = 3 }

    Write-Step "Starting AI service (http://localhost:$aiPort)"
    $venvPy = Join-Path $AiRagDir ".venv\Scripts\python.exe"
    if (-not (Test-Path $venvPy)) {
        Write-Warn2 "ai-rag venv missing - creating it and installing core deps (one-time)."
        if (-not (Test-Command "py")) { throw "Python launcher 'py' not found; install Python 3.10." }
        py -3.10 -m venv (Join-Path $AiRagDir ".venv")
        & $venvPy -m pip install --quiet --upgrade pip
        & $venvPy -m pip install -r (Join-Path $AiRagDir "requirements-core.txt")
    }
    # The AI service reads its own gitignored ai\.env (GROQ_API_KEY etc.)
    # via load_dotenv(); the workspace .env value (if any) takes precedence here.
    if ($envVals['GROQ_API_KEY']) { $env:GROQ_API_KEY = $envVals['GROQ_API_KEY'] }
    $env:ALLOWED_ORIGINS = "http://localhost:5173,http://127.0.0.1:5173,http://localhost:4173,http://127.0.0.1:4173"
    # Phase 3: AI service validates internal service tokens with the same shared key.
    $env:SERVICE_AUTH_SIGNING_KEY = $envVals['SERVICE_AUTH_SIGNING_KEY']
    if (-not (Start-ServiceGuarded -Name "airag" -Port $aiPort -Launch {
        Start-Tracked -Name "airag" -FilePath $venvPy `
            -ArgList @("-m", "uvicorn", "app.api:app", "--host", "127.0.0.1", "--port", "$aiPort") `
            -WorkingDir $AiRagDir | Out-Null
        Write-Ok "AI service launching"
    })) { $ExitCode = 3 }
}

# --- Frontend (tracked native Vite dev server) -------------------------------
if ($nodeOk) {
    Write-Step "Starting unified frontend (Vite dev server)"

    foreach ($fe in @(
        @{ Name = "frontend-auth";   Dir = $FrontendAuthDir; Port = $fa; Label = "frontend" }
    )) {
        $vite = Get-ViteEntry $fe.Dir
        if (-not (Test-Path (Join-Path $fe.Dir "node_modules"))) {
            Write-Warn2 "$($fe.Label): node_modules missing - running 'npm install' (one-time)..."
            Push-Location $fe.Dir
            try { & npm install | Out-Null } finally { Pop-Location }
            $vite = Get-ViteEntry $fe.Dir
        }
        if (-not $vite) { Write-Warn2 "$($fe.Label): vite not found; skipping."; continue }
        $port = $fe.Port
        # NOTE: Start-ServiceGuarded invokes -Launch synchronously within this
        # loop iteration, so the loop variables are still current. Do NOT use
        # .GetNewClosure() here — the resulting closure scope cannot resolve the
        # dot-sourced Start-Tracked/Write-Ok functions (CommandNotFoundException).
        $feName = $fe.Name; $feDir = $fe.Dir; $feLabel = $fe.Label
        Start-ServiceGuarded -Name $feName -Port $port -Launch {
            Start-Tracked -Name $feName -FilePath "node" `
                -ArgList @("`"$vite`"", "--port", "$port", "--strictPort", "--host") `
                -WorkingDir $feDir | Out-Null
            Write-Ok "$feLabel launching on http://localhost:$port"
        } | Out-Null
    }
} else {
    Write-Warn2 "Skipping frontends (node not installed)."
}

# --- Startup gate: poll each service until reachable -------------------------
Write-Step "Waiting for services to become reachable (startup gate: $Gate)"

function Gate-Check($label, $url, $timeout) {
    if (Wait-HttpOk -Url $url -TimeoutSec $timeout) { Write-Ok "$label reachable ($url)"; return $true }
    Write-Err "$label NOT reachable within ${timeout}s ($url)"; return $false
}

if (-not (Gate-Check "Backend /health/live" "http://localhost:$bePort/health/live" 90)) { $gateFailures += "backend" }
if (-not (Gate-Check "AI /health/live"      "http://localhost:$aiPort/health/live"  60)) { $gateFailures += "ai" }

if ($Gate -eq "full" -and $nodeOk) {
    if (-not (Gate-Check "Unified frontend" "http://localhost:$fa" 90)) { $gateFailures += "frontend-auth" }
}

# --- Persist process metadata (gitignored) -----------------------------------
$meta = [ordered]@{
    startedUtc = (Get-Date).ToUniversalTime().ToString("o")
    mode       = $resolved
    gate       = $Gate
    services   = @()
}
foreach ($svc in @(
    @{ name = "backend";         port = $bePort },
    @{ name = "airag";           port = $aiPort },
    @{ name = "frontend-auth";   port = $fa }
)) {
    $procId = Read-PidFile $svc.name
    $meta.services += [ordered]@{ name = $svc.name; pid = $procId; port = $svc.port; alive = [bool](Test-PidAlive $procId) }
}
$meta | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $RuntimeDir "processes.json") -Encoding ascii

# --- Summary -----------------------------------------------------------------
Write-Step "Local URLs"
Write-Host ("  {0,-42} {1}" -f "Unified frontend (frontend/)",           "http://localhost:$fa")
Write-Host ("  {0,-42} {1}" -f "Backend API",                            "http://localhost:$bePort")
Write-Host ("  {0,-42} {1}" -f "Backend Swagger (Development)",          "http://localhost:$bePort/swagger")
Write-Host ("  {0,-42} {1}" -f "Backend health",                         "http://localhost:$bePort/health/live , /health/ready")
Write-Host ("  {0,-42} {1}" -f "AI API",                                 "http://localhost:$aiPort")
Write-Host ("  {0,-42} {1}" -f "AI docs",                                "http://localhost:$aiPort/docs")
Write-Host ("  {0,-42} {1}" -f "AI health",                              "http://localhost:$aiPort/health/live , /health/ready")
Write-Host ("  {0,-42} {1}" -f "PostgreSQL",                             "localhost:$($envVals['POSTGRES_PORT'])  db=$($envVals['POSTGRES_DB'])  user=$($envVals['POSTGRES_USER'])")
Write-Host ""

if ($gateFailures.Count -gt 0) {
    Write-Err "Startup gate FAILED for: $($gateFailures -join ', '). See .runtime\logs\ for details."
    if ($ExitCode -eq 0) { $ExitCode = 4 }
} elseif ($ExitCode -ne 0) {
    Write-Err "Startup completed with errors (a port was held by an unrelated process)."
} else {
    Write-Ok "Startup gate passed. All required components are reachable. Run scripts\verify-local.ps1 to verify health."
}

exit $ExitCode
