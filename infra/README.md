# infra/ — Infrastructure & deployment configuration

This folder is reserved for infrastructure, deployment, and monitoring configuration that is not
owned by a single application.

## Where things actually live today

- **Local orchestration:** native only, no Docker. `../scripts/start-local.ps1` runs local PostgreSQL
  (via `pg_ctl`), the .NET backend, the AI service (uvicorn), and the Vite frontend directly on the
  host.
- **Local automation scripts:** `../scripts/` (start / stop / verify / backup / restore).

## What belongs here later

Deployment manifests (e.g. cloud/staging/production) and monitoring/alerting configuration, and any
shared infrastructure-as-code that does not belong to one app. This is an intentional, documented
placeholder.
