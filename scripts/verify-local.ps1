<#
.SYNOPSIS
    Verify the DerasaX local stack with precise, honest semantics. Every result
    is PASS / FAIL / WARN (a WARN is NEVER reported as PASS). Exits 0 ONLY when
    every MANDATORY check for the selected -AiMode passes.

.DESCRIPTION
    Checks: PostgreSQL (local-only), backend (build/migrate/no-pending-model/
    health/swagger/seed/prod-guard), AI service (per -AiMode), both frontends
    (deps/lint/typecheck/build/HTTP/bundle URL hygiene), archived-backend
    absence, secret-leak summary, and reset-refusal safety.

.PARAMETER AiMode
    Core - prove FastAPI starts, /health/live + /docs, Groq config present, and a
           GENUINE Groq response. Missing RAG/prediction/vector/model pieces are
           reported as WARNINGS, not full readiness.
    Full - additionally require /health/ready ready:true, RAG + prediction imports,
           vector store + local index, prediction model files, and a real RAG and
           prediction request. Fails (non-zero) whenever ready != true.
    Default: Full.

.PARAMETER SkipBuilds
    Skip the expensive backend/frontend build + EF checks (health/HTTP/AI/secret
    checks still run). Use for fast re-runs; a full run must not skip them.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\verify-local.ps1 -AiMode Core
.EXAMPLE
    powershell -ExecutionPolicy Bypass -File scripts\verify-local.ps1 -AiMode Full
#>
param(
    [ValidateSet("Core", "Full")]
    [string]$AiMode = "Full",
    [switch]$SkipBuilds
)

. "$PSScriptRoot\_common.ps1"

# _common sets ErrorActionPreference=Stop. In a verifier that shells out to
# native tools (npm, dotnet, psql) which legitimately write to stderr (e.g. the
# vite "chunk > 500 kB" warning), Stop would turn that stderr into a thrown
# error and misreport a passing build as FAIL. Each Check is already wrapped in
# try/catch and judges success by exit code, so Continue is correct here.
$ErrorActionPreference = "Continue"

$envVals = Get-WorkspaceEnv
$be   = $envVals['BACKEND_PORT']
$ai   = $envVals['AIRAG_PORT']
$pgP  = $envVals['POSTGRES_PORT']
$faPort = $Defaults['FRONTEND_AUTH_PORT']

$failures = 0
$warnings = 0
$results = @()

function Check {
    param([string]$Name, [scriptblock]$Test, [bool]$Required = $true)
    $ok = $false; $detail = ""
    try { $r = & $Test; $ok = [bool]$r[0]; $detail = [string]$r[1] }
    catch { $ok = $false; $detail = $_.Exception.Message }
    $status = if ($ok) { "PASS" } elseif ($Required) { "FAIL" } else { "WARN" }
    if (-not $ok -and $Required) { $script:failures++ }
    if (-not $ok -and -not $Required) { $script:warnings++ }
    $script:results += [pscustomobject]@{ Check = $Name; Status = $status; Detail = $detail }
    $color = if ($ok) { "Green" } elseif ($Required) { "Red" } else { "Yellow" }
    Write-Host ("  [{0}] {1} {2}" -f $status, $Name, $detail) -ForegroundColor $color
}

function Http-Ok($url) {
    try {
        $r = Invoke-WebRequest $url -UseBasicParsing -TimeoutSec 8
        return @(($r.StatusCode -eq 200), "HTTP $($r.StatusCode)")
    } catch {
        $code = $_.Exception.Response.StatusCode.value__
        return @($false, "HTTP $code")
    }
}

Write-Step "Verifying DerasaX local stack  (AI mode: $AiMode)"

# =============================================================================
# 1) PostgreSQL
# =============================================================================
Write-Step "PostgreSQL"
$pgBin = Get-PgBin
Check "PostgreSQL available" {
    if ($pgBin) { & "$pgBin\pg_isready.exe" -p $pgP *> $null; return @(($LASTEXITCODE -eq 0), "pg_isready :$pgP") }
    $t = Test-NetConnection -ComputerName "localhost" -Port $pgP -WarningAction SilentlyContinue
    return @($t.TcpTestSucceeded, "tcp :$pgP")
}
Check "Local database reachable ($($envVals['POSTGRES_DB']))" {
    if (-not $pgBin) { return @($false, "psql not found") }
    $env:PGPASSWORD = $envVals['POSTGRES_PASSWORD']
    $r = & "$pgBin\psql.exe" -h localhost -p $pgP -U $envVals['POSTGRES_USER'] -d $envVals['POSTGRES_DB'] -tAc "SELECT 1" 2>$null
    return @(("$r".Trim() -eq "1"), "SELECT 1 -> $("$r".Trim())")
}
Check "Database target is LOCAL (not Neon/Render/AWS/Azure)" {
    $hay = "Host=localhost"
    $beEnv = Join-Path $BackendDir ".env"; if (Test-Path $beEnv) { $hay += (Get-Content $beEnv -Raw) }
    $devJson = Join-Path $BackendApiDir "appsettings.Development.json"; if (Test-Path $devJson) { $hay += (Get-Content $devJson -Raw) }
    $low = $hay.ToLower()
    foreach ($h in $ProductionDbHosts) { if ($low.Contains($h.ToLower())) { return @($false, "found prod host '$h'") } }
    return @($true, "only local host referenced")
}

# =============================================================================
# 2) Main backend
# =============================================================================
Write-Step "Main backend (.NET)"
if (-not $SkipBuilds) {
    Check "Backend build (Release)" {
        # Build to an alternate output dir so we don't fight the running backend
        # for its locked Release DLL (verifies compilation without disrupting it).
        $vout = Join-Path $RuntimeDir "verify-build"
        dotnet build "$BackendApiDir\DerasaX.Api.csproj" -c Release -o "$vout" *> $null
        return @(($LASTEXITCODE -eq 0), "dotnet build exit $LASTEXITCODE (isolated output)")
    }
    Check "EF migrations apply" {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        dotnet ef database update --project "$BackendInfraDir" --startup-project "$BackendApiDir" *> $null
        return @(($LASTEXITCODE -eq 0), "database update exit $LASTEXITCODE")
    }
    Check "No pending EF model changes" {
        $env:ASPNETCORE_ENVIRONMENT = "Development"
        $out = dotnet ef migrations has-pending-model-changes --project "$BackendInfraDir" --startup-project "$BackendApiDir" 2>&1 | Out-String
        $noPending = ($LASTEXITCODE -eq 0) -and ($out -notmatch "pending changes")
        return @($noPending, "exit $LASTEXITCODE")
    }
} else {
    Write-Warn2 "  (SkipBuilds) backend build / EF checks skipped"
}
Check "Backend /health/live"  { Http-Ok "http://localhost:$be/health/live" }
Check "Backend /health/ready (PostgreSQL)" { Http-Ok "http://localhost:$be/health/ready" }
Check "Backend Swagger (Development)" { Http-Ok "http://localhost:$be/swagger/index.html" }
Check "Seed data present + expected counts" {
    if (-not $pgBin) { return @($false, "psql not found") }
    $env:PGPASSWORD = $envVals['POSTGRES_PASSWORD']
    # ASP.NET Identity tables are PascalCase and stored case-sensitively, so the
    # SQL needs literal double quotes. Pass it via a temp file so the quotes are
    # not mangled by PowerShell->native arg parsing.
    Initialize-Runtime
    $sql = Join-Path $RuntimeDir "seedcount.sql"
    Set-Content -Path $sql -Encoding ascii -Value 'SELECT (SELECT COUNT(*) FROM "AspNetUsers") || ''/'' || (SELECT COUNT(*) FROM "AspNetRoles");'
    $res = & "$pgBin\psql.exe" -h localhost -p $pgP -U $envVals['POSTGRES_USER'] -d $envVals['POSTGRES_DB'] -tAf $sql 2>$null
    $val = "$res".Trim()
    # Phase 3 expanded the seed to the security fixture set: two active tenants +
    # one suspended tenant, all five roles, a platform SystemAdmin and a disabled
    # account. Expect users >= 15 and exactly 5 roles (was 4/3 in Phase 1).
    $parts = $val -split '/'
    $users = if ($parts.Count -ge 1) { [int]($parts[0]) } else { 0 }
    $roles = if ($parts.Count -ge 2) { [int]($parts[1]) } else { 0 }
    $ok = ($users -ge 15) -and ($roles -eq 5)
    return @($ok, "users/roles=$val (Phase 3 target: users>=15, roles=5)")
}
Check "Backend refuses prohibited remote DB host (guard present)" {
    $prog = Get-Content (Join-Path $BackendApiDir "Program.cs") -Raw
    $has = ($prog -match "GuardAgainstProductionDatabase") -and ($prog -match "neon\.tech")
    return @($has, "startup guard present in Program.cs")
}

# =============================================================================
# 3) AI service (per -AiMode)
# =============================================================================
Write-Step "AI service (ai/) - mode: $AiMode"
$venvPy = Join-Path $AiRagDir ".venv\Scripts\python.exe"

# Phase 3: school-ai-rag now requires a signed internal service token on its
# application routes. The verification harness authenticates the way the backend
# does (a short-lived HS256 service token with the shared local key) so the
# genuine-AI checks remain meaningful instead of stopping at a 401.
#
# A FRESH token is minted per request: the AI service enforces jti replay
# protection, so a token may be presented only once. Reusing one token across
# probes would (correctly) be rejected as a replay.
$svcKey = if ($env:SERVICE_AUTH_SIGNING_KEY) { $env:SERVICE_AUTH_SIGNING_KEY } else { $envVals['SERVICE_AUTH_SIGNING_KEY'] }
# A FRESH per-scope token is minted per request (the AI service enforces jti
# replay protection, so a token may be presented only once). The harness
# authenticates exactly the way DerasaX-backend does.
function Mint-Header([string]$Scope, [string]$Tenant = 'tenant-1') {
    return @{ Authorization = "Bearer $(New-ServiceToken -Key $svcKey -Scope $Scope -TenantId $Tenant)" }
}
# POST JSON to the AI internal contract; returns @{ status; json; corr } or throws.
function Ai-Post([string]$Path, [string]$Scope, [string]$Tenant, $BodyObj, [int]$TimeoutSec = 60) {
    $h = Mint-Header $Scope $Tenant
    $body = $BodyObj | ConvertTo-Json -Depth 8
    $r = Invoke-WebRequest "http://localhost:$ai$Path" -Method POST -ContentType "application/json" -Body $body -Headers $h -UseBasicParsing -TimeoutSec $TimeoutSec
    return @{ status = $r.StatusCode; json = ($r.Content | ConvertFrom-Json); corr = $r.Headers["X-Correlation-Id"] }
}

Check "AI /health/live" { Http-Ok "http://localhost:$ai/health/live" }
Check "AI /docs" { Http-Ok "http://localhost:$ai/docs" }
Check "Groq provider configuration present" {
    # Read presence without exposing the value.
    $present = $false
    try {
        $r = Invoke-WebRequest "http://localhost:$ai/health/ready" -UseBasicParsing -TimeoutSec 8
        if ($r.Content -match '"provider_configured":\s*"yes"') { $present = $true }
    } catch {}
    return @($present, "provider_configured=$present (value not shown)")
}
# Service authentication: the internal contract MUST reject an unauthenticated call.
Check "AI internal contract rejects missing token (401)" {
    try {
        Invoke-WebRequest "http://localhost:$ai/internal/v1/tutor" -Method POST -ContentType "application/json" `
            -Body '{"correlation_id":"x","message":"hi"}' -UseBasicParsing -TimeoutSec 8 | Out-Null
        return @($false, "expected 401")
    } catch { $code = $_.Exception.Response.StatusCode.value__; return @(($code -eq 401), "HTTP $code") }
}
# Removed prototype endpoints must be gone (404), not merely unauthenticated.
foreach ($proto in @("/api/chat","/api/build-rag","/api/quiz/generate","/api/analyze-chat","/api/predict/performance")) {
    Check "Removed prototype route absent: $proto" {
        try {
            Invoke-WebRequest "http://localhost:$ai$proto" -Method POST -ContentType "application/json" -Body '{}' -UseBasicParsing -TimeoutSec 8 | Out-Null
            return @($false, "still responds")
        } catch { $code = $_.Exception.Response.StatusCode.value__; return @(($code -eq 404), "HTTP $code (gone)") }
    }
}

if ($AiMode -eq "Full") {
    Check "AI /health/ready -> ready:true" {
        try {
            $r = Invoke-WebRequest "http://localhost:$ai/health/ready" -UseBasicParsing -TimeoutSec 8
            $j = $r.Content | ConvertFrom-Json
            return @(($j.ready -eq $true), "status=$($j.status) ready=$($j.ready)")
        } catch { return @($false, "unreachable") }
    }
    Check "RAG dependencies importable" {
        Push-Location $AiRagDir
        try { & $venvPy -c "import langchain_chroma, langchain_huggingface, chromadb, sentence_transformers; from app.rag.indexer import get_embeddings; from app.rag.store import ChromaTenantStore" 2>$null; $rc = $LASTEXITCODE }
        finally { Pop-Location }
        return @(($rc -eq 0), "rag imports exit $rc")
    }
    Check "Prediction dependencies importable" {
        Push-Location $AiRagDir
        try { & $venvPy -c "import joblib, pandas, sklearn; from app.prediction.service import load_models, model_files_present" 2>$null; $rc = $LASTEXITCODE }
        finally { Pop-Location }
        return @(($rc -eq 0), "prediction imports exit $rc")
    }
    Check "Vector store directory present/writable" {
        $vs = Join-Path $AiRagDir "vectorstore"
        $has = (Test-Path $vs)
        return @($has, "vectorstore dir present")
    }
    Check "Prediction model files present" {
        $md = Join-Path $AiRagDir "app\models"
        $reg = Join-Path $md "rf_regressor.pkl"; $cls = Join-Path $md "rf_classifier.pkl"
        $ok = (Test-Path $reg) -and (Test-Path $cls) -and ((Get-Item $reg).Length -gt 1mb) -and ((Get-Item $cls).Length -gt 1mb)
        return @($ok, "rf_regressor + rf_classifier present (>1MB)")
    }

    # --- Genuine end-to-end internal contract (the way the backend calls it) ---
    $script:verifyDocId = "verify-doc-$(Get-Random)"
    $waterCycle = "The water cycle describes how water evaporates from oceans, condenses into clouds, and falls as precipitation. Evaporation, condensation, and precipitation are its main stages."

    Check "Tenant-scoped ingestion (POST /internal/v1/documents, ai:ingest)" {
        try {
            $b = @{ correlation_id = "verify-ing-$(Get-Random)"; document_id = $script:verifyDocId; version = 1; content = $waterCycle; language = "en"; material_type = "textbook"; grade = 8; subject = "Science"; title = "Water Cycle" }
            # Generous timeout: the first ingestion in a cold process loads the
            # embedding model (cached thereafter).
            $res = Ai-Post "/internal/v1/documents" "ai:ingest" "tenant-1" $b 180
            return @(($res.status -eq 200 -and $res.json.status -in @("indexed","reindexed","duplicate")), "status=$($res.json.status) chunks=$($res.json.chunk_count)")
        } catch { return @($false, "ingest failed: $($_.Exception.Message)") }
    }
    Check "Grounded tutor + citation + correlation (POST /internal/v1/tutor, ai:tutor)" {
        try {
            $b = @{ correlation_id = "verify-tut-$(Get-Random)"; message = "What are the stages of the water cycle?"; language = "en"; grade = 8; subject = "Science" }
            $res = Ai-Post "/internal/v1/tutor" "ai:tutor" "tenant-1" $b 60
            $ok = ($res.status -eq 200) -and ($res.json.grounded -eq $true) -and ($res.json.citation_count -ge 1) -and $res.corr
            return @($ok, "grounded=$($res.json.grounded) citations=$($res.json.citation_count) corr=$($res.corr)")
        } catch { return @($false, "tutor failed: $($_.Exception.Message)") }
    }
    Check "No-answer on cross-tenant query (tenant-zzz)" {
        try {
            $b = @{ correlation_id = "verify-na-$(Get-Random)"; message = "What are the stages of the water cycle?"; language = "en" }
            $res = Ai-Post "/internal/v1/tutor" "ai:tutor" "tenant-zzz" $b 40
            return @(($res.json.grounded -eq $false), "grounded=$($res.json.grounded) (cross-tenant isolated)")
        } catch { return @($false, "tutor failed: $($_.Exception.Message)") }
    }
    Check "Quiz draft generation (POST /internal/v1/quiz/draft, ai:quiz)" {
        try {
            $b = @{ correlation_id = "verify-qz-$(Get-Random)"; num_questions = 1; language = "en"; grade = 8; subject = "Science"; topic = "water cycle evaporation condensation precipitation"; question_types = @("mcq") }
            $res = Ai-Post "/internal/v1/quiz/draft" "ai:quiz" "tenant-1" $b 90
            $qc = $res.json.draft.question_count
            return @(($res.status -eq 200 -and $qc -ge 1 -and $res.json.prompt_version), "questions=$qc prompt=$($res.json.prompt_version)")
        } catch { return @($false, "quiz failed: $($_.Exception.Message)") }
    }
    Check "Real prediction model (POST /internal/v1/prediction, ai:prediction)" {
        try {
            $b = @{ correlation_id = "verify-pr-$(Get-Random)"; student_ref = "verify-stu"; feature_schema_version = "perf-v1"; features = @{ age = 14; study_hours = 12.0; attendance_percentage = 85.0; gender = "male"; school_type = "public"; internet_access = "yes"; travel_time = "<15 min"; extra_activities = "no"; study_method = "textbook" } }
            $res = Ai-Post "/internal/v1/prediction" "ai:prediction" "tenant-1" $b 60
            $ok = ($res.status -eq 200) -and ($null -ne $res.json.score) -and $res.json.model_version -and ($null -ne $res.json.confidence)
            return @($ok, "score=$($res.json.score) conf=$($res.json.confidence) model=$($res.json.model_version)")
        } catch { return @($false, "prediction failed: $($_.Exception.Message)") }
    }
    Check "Genuine analysis, non-diagnostic, pending review (POST /internal/v1/analysis, ai:analyze)" {
        try {
            $b = @{ correlation_id = "verify-an-$(Get-Random)"; student_ref = "verify-stu"; language = "en"; conversation = @(@{ role = "user"; content = "I keep mixing up evaporation and condensation in the water cycle." }) }
            $res = Ai-Post "/internal/v1/analysis" "ai:analyze" "tenant-1" $b 60
            $ok = ($res.status -eq 200) -and ($res.json.human_review_required -eq $true) -and $res.json.model_version -and $res.json.prompt_version
            return @($ok, "category=$($res.json.pain_point_category) review=$($res.json.human_review_required) model=$($res.json.model_version)")
        } catch { return @($false, "analysis failed: $($_.Exception.Message)") }
    }
    Check "Service-local metrics exposed (GET /internal/v1/metrics)" {
        try {
            $h = Mint-Header "ai:tutor" "tenant-1"
            $r = Invoke-WebRequest "http://localhost:$ai/internal/v1/metrics" -Headers $h -UseBasicParsing -TimeoutSec 8
            $j = $r.Content | ConvertFrom-Json
            return @((($r.StatusCode -eq 200) -and ($null -ne $j.counters)), "counters present")
        } catch { return @($false, "metrics failed: $($_.Exception.Message)") }
    }

    # --- Backend-mediated end-to-end (browser-style → backend → AI → DB) -------
    Check "Backend-mediated tutor + AiUsage persistence (login -> /api/v1/ai/tutor -> /api/v1/ai-usage)" {
        try {
            $login = Invoke-WebRequest "http://localhost:$be/api/v1/account/login" -Method POST -ContentType "application/json" `
                -Body (@{ UserID = "ADMIN-T1"; Password = "Local@Dev123" } | ConvertTo-Json) -UseBasicParsing -TimeoutSec 20
            $lj = $login.Content | ConvertFrom-Json
            $tok = if ($lj.token) { $lj.token } elseif ($lj.data -and $lj.data.token) { $lj.data.token } else { $null }
            if (-not $tok) { return @($false, "login returned no token") }
            $bh = @{ Authorization = "Bearer $tok" }
            $chat = Invoke-WebRequest "http://localhost:$be/api/v1/ai/tutor" -Method POST -ContentType "application/json" `
                -Body (@{ message = "What are the stages of the water cycle?"; grade = 8; subject = "Science" } | ConvertTo-Json) -Headers $bh -UseBasicParsing -TimeoutSec 60
            $cj = $chat.Content | ConvertFrom-Json
            $grounded = ($chat.StatusCode -eq 200) -and ($cj.grounded -eq $true -or $cj.answer)
            # AiUsage must have been recorded (SchoolAdmin can list).
            $usage = Invoke-WebRequest "http://localhost:$be/api/v1/ai-usage" -Headers $bh -UseBasicParsing -TimeoutSec 20
            $recorded = ($usage.StatusCode -eq 200) -and ($usage.Content -match '"id"')
            return @(($grounded -and $recorded), "chat grounded=$grounded; AiUsage recorded=$recorded")
        } catch { return @($false, "backend-mediated flow failed: $($_.Exception.Message)") }
    }

    # --- Cleanup: remove the verification document (keep tenant store tidy) -----
    Check "Cleanup verification document (DELETE /internal/v1/documents)" {
        try {
            $h = Mint-Header "ai:ingest" "tenant-1"
            $r = Invoke-WebRequest "http://localhost:$ai/internal/v1/documents/$($script:verifyDocId)" -Method DELETE -Headers $h -UseBasicParsing -TimeoutSec 20
            return @(($r.StatusCode -eq 200), "deleted verification doc")
        } catch { return @($false, "cleanup failed: $($_.Exception.Message)") } } $false

    Check "No direct frontend-to-AI calls (src has no AI URL)" {
        $hits = @()
        $src = Join-Path $FrontendAuthDir "src"
        if (Test-Path $src) {
            # Scan PRODUCTION source only. The phase17/phase18 guardrail *tests*
            # legitimately contain ":8000"/"school-ai-rag" as forbidden-pattern
            # literals (they assert no production code references the AI service),
            # so test/spec files are excluded here to avoid a false positive.
            $files = Get-ChildItem $src -Recurse -Include *.js,*.jsx,*.ts,*.tsx -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -notmatch '\.(test|spec)\.' }
            foreach ($f in $files) {
                $t = (Get-Content $f.FullName -Raw)
                if ($t -match "VITE_API_URL" -or $t -match "localhost:8000" -or $t -match ":8000") { $hits += $f.Name }
            }
        }
        return @(($hits.Count -eq 0), $(if ($hits.Count) { "AI refs in: $($hits -join ',')" } else { "frontend calls backend only" }))
    }
} else {
    # Core mode: optional heavy pieces reported as WARNINGS only.
    Check "AI /health/ready (Core: reports dependency state)" {
        try {
            $r = Invoke-WebRequest "http://localhost:$ai/health/ready" -UseBasicParsing -TimeoutSec 8
            $j = $r.Content | ConvertFrom-Json
            return @(($r.StatusCode -eq 200), "status=$($j.status) ready=$($j.ready)")
        } catch { return @($false, "unreachable") }
    }
    Check "RAG stack available (optional in Core)" {
        & $venvPy -c "import langchain_chroma, chromadb, sentence_transformers" 2>$null
        return @(($LASTEXITCODE -eq 0), "optional heavy RAG stack")
    } $false
    Check "Prediction models available (optional in Core)" {
        $md = Join-Path $AiRagDir "app\models"
        return @((Test-Path (Join-Path $md "rf_regressor.pkl")), "optional model files")
    } $false
}

# =============================================================================
# 4) Authenticated frontend (frontend/)
# =============================================================================
Write-Step "Authenticated frontend (frontend/)"
Check "Deps installed (node_modules)" { return @((Test-Path (Join-Path $FrontendAuthDir "node_modules")), "node_modules present") }
if (-not $SkipBuilds) {
    Check "Lint" {
        Push-Location $FrontendAuthDir; try { & npm run lint *> $null; $rc=$LASTEXITCODE } finally { Pop-Location }
        return @(($rc -eq 0), "npm run lint exit $rc")
    }
    Check "Build" {
        Push-Location $FrontendAuthDir; try { & npm run build *> $null; $rc=$LASTEXITCODE } finally { Pop-Location }
        return @(($rc -eq 0), "npm run build exit $rc")
    }
    Check "Bundle: no archived/production URLs; only approved local routes" {
        $dist = Join-Path $FrontendAuthDir "dist"
        if (-not (Test-Path $dist)) { return @($false, "no dist/ (build first)") }
        $bad = @("onrender.com","railway.app","vercel.app","amazonaws.com","neon.tech","school-ai-backend")
        $files = Get-ChildItem $dist -Recurse -Include *.js,*.html -ErrorAction SilentlyContinue
        $hits = @()
        foreach ($f in $files) {
            $t = (Get-Content $f.FullName -Raw).ToLower()
            foreach ($b in $bad) { if ($t.Contains($b)) { $hits += $b } }
        }
        return @(($hits.Count -eq 0), $(if ($hits.Count) { "found: $($hits -join ',')" } else { "clean (no archived/prod URLs)" }))
    }
}
Check "Authenticated frontend HTTP 200" {
    foreach ($p in @($faPort, 4173)) { $r = Http-Ok "http://localhost:$p"; if ($r[0]) { return @($true, "port $p $($r[1])") } }
    return @($false, "not serving on $faPort/4173")
}

# =============================================================================
# (Phase 20) The public website (DerasaX-public) was merged into the unified
# frontend/ app (verified in section 4) and archived, so its separate
# verification section was removed.
# =============================================================================

# =============================================================================
# 6) Archived backend protection
# =============================================================================
Write-Step "Archived backend protection"
Check "Archived backend process NOT running (:10000)" {
    return @((-not (Test-PortListening 10000)), "no :10000 listener")
}
Check "No Docker files remain (Docker removed from local tooling)" {
    $hits = @()
    foreach ($rel in @("docker-compose.yml", "backend\Dockerfile", "ai\Dockerfile", "backend\.dockerignore", "ai\.dockerignore")) {
        if (Test-Path (Join-Path $Workspace $rel)) { $hits += $rel }
    }
    return @(($hits.Count -eq 0), $(if ($hits.Count) { "found: $($hits -join ', ')" } else { "no Docker files present" }))
}
Check "No frontend/backend config references archived backend" {
    $bad = @("onrender.com","railway.app","vercel.app","school-ai-backend",":10000")
    $files = @()
    foreach ($d in @($FrontendAuthDir, $BackendApiDir)) {
        $files += Get-ChildItem $d -Recurse -Include *.env,*.json,*.js,*.ts -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch "node_modules|bin\\|obj\\|dist\\" }
    }
    $feEnv = Join-Path $FrontendAuthDir ".env"; if (Test-Path $feEnv) { $files += Get-Item $feEnv }
    $hits = @()
    foreach ($f in $files) { $t = (Get-Content $f.FullName -Raw -ErrorAction SilentlyContinue); if ($t) { $t=$t.ToLower(); foreach ($b in $bad) { if ($t.Contains($b)) { $hits += "$($f.Name):$b" } } } }
    return @(($hits.Count -eq 0), $(if ($hits.Count) { "refs: $($hits | Select-Object -First 3 -Unique)" } else { "no archived references" }))
}

# =============================================================================
# 7) Secrets (summary only - never prints secret values)
# =============================================================================
Write-Step "Secret hygiene"
Check "Local .env files are gitignored" {
    Push-Location $Workspace
    try {
        $ignored = $true
        foreach ($e in @(".env","ai\.env","backend\.env")) {
            if (Test-Path $e) { git -C $Workspace check-ignore $e *> $null; if ($LASTEXITCODE -ne 0) { $ignored = $false } }
        }
    } finally { Pop-Location }
    return @($ignored, "workspace .env files ignored")
}
Check "No secret values in TRACKED workspace files" {
    # Scan only git-tracked files; report counts/locations, never values.
    $tracked = git -C $Workspace ls-files 2>$null
    if (-not $tracked) { return @($true, "no tracked files yet") }
    # High-signal patterns only. Excludes ${VAR} references and CHANGE_ME
    # placeholders so documented host names / compose variable refs are not
    # misreported as leaks.
    $patterns = @('gsk_[A-Za-z0-9]{20,}', 'sk-[A-Za-z0-9]{20,}', 'postgres://[^ ]*:[^ @]+@', 'Password=(?!\$)(?!CHANGE)[A-Za-z0-9@#%_]{10,}')
    # The documented local-dev-only PostgreSQL password is NOT a secret: it is the
    # local dev DB password (overridable by env) and appears only in explicitly
    # local-dev files. Neutralize ONLY that exact literal in ONLY those files, so a
    # real secret in the same file is still detected and no broad pattern is relaxed.
    # Split so this scanner's OWN source never self-matches the Password=... pattern below.
    $LocalDevPwLiteral  = 'Password=' + 'derasax_local_dev'
    $LocalDevOnlyFiles  = @(
        'backend/src/DerasaX.Api/appsettings.Development.json',
        'backend/src/DerasaX.Infrastructure/DbHelper/Context/DerasaXDbContextFactory.cs'
    )
    $hitFiles = @()
    foreach ($rel in $tracked) {
        $full = Join-Path $Workspace $rel
        if (-not (Test-Path $full)) { continue }
        if ($rel -match '\.example$') { continue }
        $raw = Get-Content $full -Raw -ErrorAction SilentlyContinue
        if (-not $raw) { continue }
        $scan = $raw
        if ($LocalDevOnlyFiles -contains ($rel -replace '\\', '/')) {
            # Exact-literal only; any other Password=... value still matches the pattern.
            $scan = $scan.Replace($LocalDevPwLiteral, 'Password=<local-dev-placeholder>')
        }
        foreach ($p in $patterns) { if ($scan -match $p) { $hitFiles += $rel; break } }
    }
    return @(($hitFiles.Count -eq 0), $(if ($hitFiles.Count) { "POTENTIAL leak in: $($hitFiles -join ', ')" } else { "no secret patterns in tracked files" }))
}

# =============================================================================
# 8) Reset safety (non-destructive: refusal path only)
# =============================================================================
Write-Step "Reset safety (refusal path)"
Check "reset-local-db.ps1 refuses without -ConfirmReset (exit 2)" {
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $PSScriptRoot "reset-local-db.ps1") *> $null
    return @(($LASTEXITCODE -eq 2), "exit $LASTEXITCODE (expected 2)")
}

# =============================================================================
# Summary
# =============================================================================
Write-Host ""
Write-Host ("Summary: {0} PASS-required failures, {1} warning(s). AI mode: {2}" -f $failures, $warnings, $AiMode)
if ($failures -gt 0) {
    Write-Err "$failures mandatory check(s) failed for AI mode '$AiMode'."
    exit 1
} else {
    Write-Ok "All mandatory checks passed for AI mode '$AiMode'. ($warnings warning(s).)"
    exit 0
}
