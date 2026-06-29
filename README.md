# DerasaX

DerasaX is a multi-tenant education platform built as a small monorepo of **three active
applications** plus shared orchestration. The backend is the system of record; the browser talks
only to the backend; the backend mediates all AI traffic over a signed internal contract.

## The three apps

| App | Path | Stack | Local URL |
|-----|------|-------|-----------|
| **Backend API** | `backend/` | .NET 9 (Api / Application / Domain / Infrastructure) | http://localhost:5155 (`/swagger`, `/health/live`, `/health/ready`) |
| **Frontend** | `frontend/` | React + Vite SPA (feature-organized) | http://localhost:5173 (preview: http://localhost:4173) |
| **AI service** | `ai/` | FastAPI internal RAG/tutor service | http://localhost:8000 (`/docs`, `/health/live`, `/health/ready`) |

Supporting folders: `scripts/` (local orchestration), `infra/` (deployment placeholder),
`docs/` (audit + design + phase reports), `docker-compose.yml` (root-level local stack:
PostgreSQL + backend + ai; the frontend runs as a native Vite server, not in compose).

PostgreSQL runs locally on `localhost:5432` (database `derasax_local`, role `derasax`).

## Prerequisites (local development)

- **.NET SDK 9.0.x** (pinned via `backend/global.json`; `dotnet --version` should resolve a 9.0.x).
- **Node.js** (v20+; repo developed on v24) and npm.
- **Python 3.10** (the AI service uses `ai/.venv`; created automatically on first start).
- **PostgreSQL 16** client + server tools on `PATH` (`psql`, `pg_ctl`, `pg_restore`) for native mode,
  **or** Docker for compose mode.

Copy the env templates before first run (the start script does this automatically if missing):
`.env.example` → `.env` at the repo root, in `ai/`, and in `frontend/`. **Real `.env` files are
gitignored; never commit secrets.**

## Local stack — start / stop / verify

All scripts live in `scripts/` (PowerShell). Run them from the repo root.

```powershell
scripts/start-local.ps1            # start PostgreSQL + backend + ai + frontend (auto: docker if up, else native)
scripts/verify-local.ps1 -AiMode Full   # health + seed + genuine backend->AI flow checks
scripts/stop-local.ps1             # stop tracked child processes; PRESERVES the database
```

`start-local.ps1 -Mode native` forces the native (non-Docker) path; `-Gate core` requires only
backend + AI to be reachable. `verify-local.ps1 -AiMode Core` is a faster check that does not require
the full RAG/prediction stack.

## Database — reset / backup / restore

```powershell
scripts/reset-local-db.ps1 -ConfirmReset   # DESTRUCTIVE: drop+recreate derasax_local, migrate, reseed (local only)
scripts/backup-local-db.ps1                # pg_dump custom-format snapshot into .runtime/backups/
scripts/restore-local-db.ps1 -BackupFile <dump> -ConfirmRestore   # restore into an isolated scratch DB by default
```

These refuse to operate on anything that does not look like a local database.

## Architecture invariants (do not weaken)

- **Browser → backend only.** The SPA's single API client (`frontend/src/lib/api/client.js`) rejects
  absolute URLs, and the frontend config (`frontend/src/config/env.js`) holds **no AI-service URL**.
  The browser never calls the AI service.
- **Backend → AI via a signed internal token.** The backend mints a short-lived, scoped HS256 service
  token per call (`backend/.../Services/Ai/AiRagClient.cs`); the AI service fail-closed-validates it
  (`ai/app/security/service_auth.py`). All AI traffic is server-to-server.
- **Tenant context comes from trusted claims**, never from the request body — at every hop
  (backend `HttpTenantContext`, AI `service_auth` + `rag/tenant.py`). Tenant isolation is enforced in
  the DB query filters, file storage, AI vector collections, SignalR groups, and audit.
- **The backend is the only system of record.** The AI service is stateless per request and returns
  inference only.
- **Correlation IDs flow end-to-end** (browser → backend → AI) and are echoed on every response.

## Secrets & local dev keys

- Only `*.env.example` templates are committed; real `.env` files are gitignored. No real secret is
  embedded in tracked files.
- `backend/src/DerasaX.Api/appsettings.Development.json` contains **throwaway local-only** signing keys
  and DB password, clearly labelled `local-dev-only-...-not-for-production`. They are **never** valid in
  Production: `ProductionConfigValidator` (`Program.cs`) fails closed on placeholder/empty secrets, and
  `GuardAgainstProductionDatabase` blocks non-local DB hosts in Development. Do not copy these keys into
  any deployed configuration.

## Testing

```powershell
# Backend (.NET)
cd backend; dotnet build DerasaX.sln -c Debug
dotnet test DerasaX.sln --settings tests/DerasaX.Tests/serial.runsettings   # needs a running, seeded local PostgreSQL

# Frontend
cd frontend; npm run test    # Vitest
npm run lint; npm run build
npm run test:e2e             # Playwright (requires the full local stack up)

# AI service
cd ai; .venv/Scripts/python -m pytest
```

> **Known local constraint (Windows / Smart App Control):** on a host where Smart App Control / WDAC is
> enforcing, loading freshly-built unsigned .NET assemblies is blocked (`0x800711C7`), which prevents
> `dotnet test`, `dotnet ef`, and the backend runtime from running locally. `dotnet build` still works.
> Run the backend suite in an environment without that policy (or sign / allow-list the build output).
> See `docs/phase22/PHASE22_REMAINING_GAPS.md` for the full evidence.

## Source-control policy for this project

**This project is currently worked on as uncommitted local changes only.** Do **not** create Git
commits, branches, tags, pushes, or history rewrites unless the policy is explicitly reversed. Phase
work is verified locally and captured under `docs/` (see `docs/phase22/`), not via commits.

## Documentation

- `docs/audit/` — the Phase 21 full-stack audit and refactor findings.
- `docs/phase22/` — Phase 22 (Wave-0/A) execution report, remaining gaps, and command logs.
- `docs/phase22b/` — Phase 22B execution: Step 0 unlock, Wave 1/2/3 reports, and command logs.
- `docs/contracts/` — checked-in API contract snapshots (backend OpenAPI route surface, AI internal v1) with drift tests.
- `docs/design/` — design/prototype specs.
- `infra/README.md` — infrastructure/deployment placeholder notes.

## Frontend hosting (`frontend/vercel.json`)

`frontend/vercel.json` is the **intentional** static-hosting config for deploying the unified frontend as a
static SPA on Vercel (FE-04 resolution): SPA history-fallback rewrites plus the same security headers (CSP /
HSTS / `frame-ancestors 'none'`) the app enforces locally. It is the documented frontend hosting target —
keep it in sync with the app's security headers. If a different static host is adopted, replace it with that
host's equivalent and preserve the headers.
