# =============================================================================
# scripts/_common.ps1 - shared helpers/config for the DerasaX local scripts.
# Dot-source this from the other scripts:  . "$PSScriptRoot\_common.ps1"
#
# Two run modes are supported:
#   docker  - PostgreSQL + backend + ai-rag via docker compose
#   native  - local PostgreSQL (pg_ctl) + `dotnet` + uvicorn venv (no Docker)
# `auto` (default) picks docker when the docker CLI is available, else native.
# =============================================================================

$ErrorActionPreference = "Stop"

# --- Paths -------------------------------------------------------------------
$Workspace        = Split-Path -Parent $PSScriptRoot
$BackendDir       = Join-Path $Workspace "backend"
$BackendApiDir    = Join-Path $BackendDir "src\DerasaX.Api"
$BackendInfraDir  = Join-Path $BackendDir "src\DerasaX.Infrastructure"
$AiRagDir         = Join-Path $Workspace "ai"
# Single unified frontend (Phase 20): the frontend/ app serves the public
# marketing pages, the auth page, and the authenticated portal. The former
# standalone public app was merged in and archived under archive/.
$FrontendAuthDir  = Join-Path $Workspace "frontend"
$LocalDbDir       = Join-Path $Workspace ".localdb"
$PgData           = Join-Path $LocalDbDir "pgdata"
$PgLog            = Join-Path $LocalDbDir "pg.log"

# Gitignored runtime directory for PID/process metadata + per-service logs.
$RuntimeDir       = Join-Path $Workspace ".runtime"
$RuntimeLogDir    = Join-Path $RuntimeDir "logs"

# --- Defaults (overridable via workspace .env) -------------------------------
$Defaults = @{
    POSTGRES_USER         = "derasax"
    POSTGRES_PASSWORD     = "derasax_local_dev"
    POSTGRES_DB           = "derasax_local"
    POSTGRES_PORT         = "5432"
    BACKEND_PORT          = "5155"
    AIRAG_PORT            = "8000"
    FRONTEND_AUTH_PORT    = "5173"
    BACKEND_JWT_KEY       = "local-dev-only-signing-key-not-for-production-0123456789abcdef"
    # Phase 3: shared local-only key for the backend -> school-ai-rag service token.
    # The backend and the AI service MUST use the same value locally.
    SERVICE_AUTH_SIGNING_KEY = "local-dev-only-service-signing-key-not-for-production-abcdef0123456789"
    GROQ_API_KEY          = ""
    SEED_ENABLED          = "true"
}

# Mint a short-lived internal service JWT (HS256) the way DerasaX-backend does,
# so local verification can authenticate to school-ai-rag (Phase 3). Local only.
function New-ServiceToken {
    param(
        [Parameter(Mandatory)][string]$Key,
        [string]$Scope = "ai:chat",
        [string]$TenantId = "tenant-1",
        [int]$TtlSeconds = 120
    )
    function ConvertTo-B64Url([byte[]]$bytes) {
        [Convert]::ToBase64String($bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')
    }
    $now = [int][double]::Parse((Get-Date -UFormat %s))
    $header  = @{ alg = 'HS256'; typ = 'JWT'; kid = 'ai-local' } | ConvertTo-Json -Compress
    $payload = @{
        iss = 'derasax-backend'; aud = 'school-ai-rag'; sub = 'svc:ai-orchestrator'
        scope = $Scope; tenantId = $TenantId; uid = 'verify'
        jti = [guid]::NewGuid().ToString('N'); iat = $now; exp = ($now + $TtlSeconds)
    } | ConvertTo-Json -Compress
    $h = ConvertTo-B64Url ([Text.Encoding]::UTF8.GetBytes($header))
    $p = ConvertTo-B64Url ([Text.Encoding]::UTF8.GetBytes($payload))
    $signingInput = "$h.$p"
    $hmac = [System.Security.Cryptography.HMACSHA256]::new([Text.Encoding]::UTF8.GetBytes($Key))
    try {
        $sig = ConvertTo-B64Url ($hmac.ComputeHash([Text.Encoding]::UTF8.GetBytes($signingInput)))
    } finally { $hmac.Dispose() }
    return "$signingInput.$sig"
}

# Hosts that indicate a PRODUCTION database - scripts must refuse these locally.
$ProductionDbHosts = @("neon.tech", "render.com", "onrender.com", "amazonaws.com", "azure.com", "rds.")

# --- Output helpers ----------------------------------------------------------
function Write-Step($msg) { Write-Host "==> $msg" -ForegroundColor Cyan }
function Write-Ok($msg)   { Write-Host "[ OK ] $msg" -ForegroundColor Green }
function Write-Warn2($msg) { Write-Host "[WARN] $msg" -ForegroundColor Yellow }
function Write-Err($msg)   { Write-Host "[FAIL] $msg" -ForegroundColor Red }

# --- .env loading ------------------------------------------------------------
function Import-DotEnv {
    param([string]$Path)
    $values = @{}
    foreach ($k in $Defaults.Keys) { $values[$k] = $Defaults[$k] }
    if (Test-Path $Path) {
        foreach ($line in Get-Content $Path) {
            $t = $line.Trim()
            if ($t -eq "" -or $t.StartsWith("#")) { continue }
            $idx = $t.IndexOf("=")
            if ($idx -lt 1) { continue }
            $key = $t.Substring(0, $idx).Trim()
            $val = $t.Substring($idx + 1).Trim()
            $values[$key] = $val
        }
    }
    return $values
}

function Get-WorkspaceEnv {
    return Import-DotEnv -Path (Join-Path $Workspace ".env")
}

# --- Tool / mode detection ---------------------------------------------------
function Test-Command($name) {
    $c = Get-Command $name -ErrorAction SilentlyContinue
    return [bool]$c
}

function Get-DockerAvailable {
    if (-not (Test-Command "docker")) { return $false }
    try { docker info *> $null; return ($LASTEXITCODE -eq 0) } catch { return $false }
}

function Resolve-Mode {
    param([string]$Requested)
    if ($Requested -eq "docker") { return "docker" }
    if ($Requested -eq "native") { return "native" }
    if (Get-DockerAvailable) { return "docker" }
    return "native"
}

function Get-PgBin {
    # Prefer pg_ctl on PATH, else the scoop postgresql16 location.
    $cmd = Get-Command "pg_ctl" -ErrorAction SilentlyContinue
    if ($cmd) { return (Split-Path -Parent $cmd.Source) }
    $scoop = Join-Path $env:USERPROFILE "scoop\apps\postgresql16\current\bin"
    if (Test-Path (Join-Path $scoop "pg_ctl.exe")) { return $scoop }
    return $null
}

function Test-PgReady {
    param([string]$PgBin, [string]$Port)
    & "$PgBin\pg_isready.exe" -p $Port *> $null
    return ($LASTEXITCODE -eq 0)
}

# Start the local PostgreSQL cluster if it is not already accepting connections.
# IMPORTANT (two Windows gotchas this avoids):
#   1) Do NOT pipe pg_ctl (e.g. `| Out-Null`): the spawned postgres daemon
#      inherits the pipe handle and the pipeline never sees EOF -> hangs forever.
#   2) Do NOT use `Start-Process -Wait`: -Wait waits for the whole process tree,
#      and the postgres daemon never exits -> hangs forever.
# So we launch pg_ctl detached (no -w, no -Wait) and poll pg_isready ourselves.
function Start-LocalPostgres {
    param([string]$PgBin, [string]$Port, [string]$DataDir, [string]$LogFile)

    if (Test-PgReady -PgBin $PgBin -Port $Port) { return $true }

    $argline = "-D `"$DataDir`" -l `"$LogFile`" -o `"-p $Port`" start"
    Start-Process -FilePath "$PgBin\pg_ctl.exe" -ArgumentList $argline -WindowStyle Hidden | Out-Null

    for ($i = 0; $i -lt 40; $i++) {
        if (Test-PgReady -PgBin $PgBin -Port $Port) { return $true }
        Start-Sleep -Milliseconds 500
    }
    return (Test-PgReady -PgBin $PgBin -Port $Port)
}

# --- Safety: refuse production DB host --------------------------------------
function Assert-NotProductionDb {
    param([hashtable]$EnvVals)
    $haystacks = @()
    $haystacks += "Host=$($EnvVals['POSTGRES_HOST'])"
    # Also scan backend connection-string override if present in its .env
    $backendEnv = Join-Path $BackendDir ".env"
    if (Test-Path $backendEnv) { $haystacks += (Get-Content $backendEnv -Raw) }
    $combined = ($haystacks -join "`n").ToLower()
    foreach ($h in $ProductionDbHosts) {
        if ($combined.Contains($h.ToLower())) {
            throw "Refusing to run: a PRODUCTION database host ('$h') is configured. Local scripts only operate on a local PostgreSQL."
        }
    }
}

# --- Runtime / process supervision ------------------------------------------
# All long-lived child processes started by start-local.ps1 are tracked by a
# PID file under .runtime/ so stop-local.ps1 can stop EXACTLY those processes
# (and their child trees) without ever touching unrelated processes that merely
# happen to share a port.

function Initialize-Runtime {
    New-Item -ItemType Directory -Force -Path $RuntimeDir    | Out-Null
    New-Item -ItemType Directory -Force -Path $RuntimeLogDir | Out-Null
}

function Get-PidFile($Name) { return (Join-Path $RuntimeDir "$Name.pid") }

function Write-PidFile($Name, $ProcId) {
    Set-Content -Path (Get-PidFile $Name) -Value "$ProcId" -NoNewline -Encoding ascii
}

function Read-PidFile($Name) {
    $f = Get-PidFile $Name
    if (Test-Path $f) { return (Get-Content $f -Raw).Trim() }
    return $null
}

function Test-PidAlive($ProcId) {
    if (-not $ProcId) { return $false }
    $p = Get-Process -Id $ProcId -ErrorAction SilentlyContinue
    return [bool]$p
}

# PID of the process LISTENING on a TCP port (or $null). Used to distinguish
# "our service is already up" from "an unrelated process squats this port".
function Get-PortOwnerPid($Port) {
    $c = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($c) { return ($c | Select-Object -First 1 -ExpandProperty OwningProcess) }
    return $null
}

function Test-PortListening($Port) { return [bool](Get-PortOwnerPid $Port) }

# Start a tracked, detached child process. Output is redirected to log files so
# the child never inherits PowerShell's console handles (which would otherwise
# make startup block forever). Returns the started System.Diagnostics.Process.
function Start-Tracked {
    param(
        [string]$Name,
        [string]$FilePath,
        [string[]]$ArgList,
        [string]$WorkingDir,
        [hashtable]$EnvVars
    )
    Initialize-Runtime
    $out = Join-Path $RuntimeLogDir "$Name.out.log"
    $err = Join-Path $RuntimeLogDir "$Name.err.log"
    if ($EnvVars) { foreach ($k in $EnvVars.Keys) { Set-Item -Path "Env:$k" -Value $EnvVars[$k] } }
    $p = Start-Process -FilePath $FilePath -ArgumentList $ArgList `
        -WorkingDirectory $WorkingDir -WindowStyle Hidden -PassThru `
        -RedirectStandardOutput $out -RedirectStandardError $err
    Write-PidFile -Name $Name -ProcId $p.Id
    return $p
}

# Stop a tracked process (and its whole child tree, e.g. node->vite) by PID file.
# Only ever targets the recorded PID — never a port-based guess.
function Stop-Tracked {
    param([string]$Name, [string]$Label)
    if (-not $Label) { $Label = $Name }
    $procId = Read-PidFile $Name
    if (-not $procId) { Write-Host "  ($Label not tracked / not running)" -ForegroundColor Gray; return }
    if (Test-PidAlive $procId) {
        & taskkill /PID $procId /T /F *> $null
        if ($LASTEXITCODE -eq 0) { Write-Ok "Stopped $Label (PID $procId, incl. child tree)" }
        else { Write-Warn2 "Could not fully stop $Label (PID $procId)" }
    } else {
        Write-Host "  ($Label PID $procId already exited)" -ForegroundColor Gray
    }
    Remove-Item (Get-PidFile $Name) -Force -ErrorAction SilentlyContinue
}

# Poll an HTTP endpoint until it returns 200 or the timeout elapses.
function Wait-HttpOk {
    param([string]$Url, [int]$TimeoutSec = 60)
    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    while ((Get-Date) -lt $deadline) {
        try {
            $r = Invoke-WebRequest $Url -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { return $true }
        } catch { Start-Sleep -Milliseconds 700 }
    }
    return $false
}

# Resolve the local Vite launcher (node + node_modules/vite/bin/vite.js) so the
# tracked PID is the real node process (a clean kill target), not an npm wrapper.
function Get-ViteEntry($RepoDir) {
    $v = Join-Path $RepoDir "node_modules\vite\bin\vite.js"
    if (Test-Path $v) { return $v }
    return $null
}

# --- Local URLs --------------------------------------------------------------
function Get-LocalUrls {
    param([hashtable]$EnvVals)
    return [ordered]@{
        "Unified frontend (frontend/)"           = "http://localhost:$($Defaults['FRONTEND_AUTH_PORT'])  (preview: http://localhost:4173)"
        "Backend Swagger"                        = "http://localhost:$($EnvVals['BACKEND_PORT'])/swagger"
        "Backend health"                         = "http://localhost:$($EnvVals['BACKEND_PORT'])/health/live , /health/ready"
        "AI service docs"                        = "http://localhost:$($EnvVals['AIRAG_PORT'])/docs"
        "AI service health"                      = "http://localhost:$($EnvVals['AIRAG_PORT'])/health/live , /health/ready"
        "PostgreSQL"                             = "localhost:$($EnvVals['POSTGRES_PORT'])  db=$($EnvVals['POSTGRES_DB'])  user=$($EnvVals['POSTGRES_USER'])"
    }
}
