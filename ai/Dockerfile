# =============================================================================
# ai — internal AI service local development container image (Python 3.10, full stack).
# Installs the curated requirements.txt (RAG + ML included). The vector store
# and model cache live under /app and are mounted as named volumes by
# docker-compose so they persist across container restarts.
# No secret is baked in; GROQ_API_KEY is supplied at run time.
# =============================================================================
FROM python:3.10-slim

WORKDIR /app

# Build deps occasionally needed by ML wheels; kept minimal.
RUN apt-get update \
    && apt-get install -y --no-install-recommends build-essential \
    && rm -rf /var/lib/apt/lists/*

COPY requirements.txt .
RUN python -m pip install --no-cache-dir --upgrade pip \
    && pip install --no-cache-dir --default-timeout=600 -r requirements.txt

COPY . .

# Persisted, writable local data locations (mounted as volumes in compose).
ENV VECTORSTORE_DIR=/app/vectorstore \
    MODEL_DIR=/app/app/models \
    PORT=8000
RUN mkdir -p /app/vectorstore /app/app/models

# Phase 18 — run as a non-root user. No secret is baked in; the service listens on
# an unprivileged port (8000). The app owns /app so the vectorstore/model dirs are
# writable (mounted volumes should be provisioned with matching ownership in compose).
RUN groupadd --system appgrp \
    && useradd --system --gid appgrp --home-dir /app --shell /usr/sbin/nologin appuser \
    && chown -R appuser:appgrp /app
USER appuser

EXPOSE 8000

# Container-level readiness probe (liveness). Uses Python (no curl dependency).
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD python -c "import urllib.request,sys; sys.exit(0 if urllib.request.urlopen('http://localhost:8000/health/live').status==200 else 1)" || exit 1

CMD ["sh", "-c", "uvicorn app.api:app --host 0.0.0.0 --port ${PORT:-8000}"]
