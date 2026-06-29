# infra/ — Infrastructure & deployment configuration

This folder is reserved for infrastructure, deployment, and monitoring configuration that is not
owned by a single application.

## Where things actually live today

- **Local orchestration:** `../docker-compose.yml` lives at the repository root (a root-level file;
  see the root `README.md`). It builds the `backend` and `ai` services and runs PostgreSQL.
- **Per-app container builds:** each app keeps its own `Dockerfile` next to its source, because that
  `Dockerfile` is the build context referenced by `docker-compose.yml`:
  - `../backend/Dockerfile`
  - `../ai/Dockerfile`
- **Local automation scripts:** `../scripts/` (start / stop / verify / backup / restore).

## What belongs here later

Deployment manifests (e.g. cloud/staging/production), monitoring/alerting configuration, and any
shared infrastructure-as-code that does not belong to one app. Nothing functional has been moved here
to avoid breaking the docker-compose build contexts; this is an intentional, documented placeholder.
