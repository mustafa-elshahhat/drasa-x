"""
school-ai-rag FastAPI application (Phase 6).

This service is an INTERNAL surface called only by DerasaX-backend over an
authenticated service-token contract (``/internal/v1/*``). The browser never
reaches it. The legacy public prototype endpoints (``/api/chat``,
``/api/build-rag``, ``/api/quiz/*``, ``/api/analyze-chat``, ``/api/predict/*``)
and the global, non-tenant vector store were REMOVED in §18.

Only ``/``, ``/health/live`` and ``/health/ready`` are unauthenticated.
"""
from __future__ import annotations

import logging
import os
from pathlib import Path

from dotenv import load_dotenv
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import JSONResponse

load_dotenv()

logging.basicConfig(
    level=os.environ.get("LOG_LEVEL", "INFO").upper(),
    format="%(asctime)s %(levelname)s %(name)s %(message)s",
)

from app.observability import telemetry

# -----------------------------------------------------------------------------
# Optional / heavy dependencies are imported defensively so the service can
# always start (and serve /health/*) even when the full ML/RAG stack or the
# downloaded models are not present. Each subsystem records whether it is
# available and, if not, a short SANITIZED reason for the readiness endpoint.
# -----------------------------------------------------------------------------
_feature_status: dict[str, str] = {}

# RAG + embeddings (langchain / chroma / sentence-transformers)
try:
    from app.rag.indexer import embedding_model_name  # noqa: F401
    from app.rag.store import ChromaTenantStore  # noqa: F401

    _RAG_AVAILABLE = True
    _feature_status["rag"] = "available"
except Exception as exc:  # pragma: no cover - depends on optional install
    _RAG_AVAILABLE = False
    _feature_status["rag"] = f"missing dependency: {type(exc).__name__}"

# LLM client (groq) — light dependency, imports without an API key.
try:
    from app.llm.groq_client import has_api_key

    _LLM_AVAILABLE = True
    _feature_status["llm"] = "available"
except Exception as exc:  # pragma: no cover
    _LLM_AVAILABLE = False
    _feature_status["llm"] = f"missing dependency: {type(exc).__name__}"

    def has_api_key() -> bool:  # type: ignore
        return bool(os.environ.get("GROQ_API_KEY"))

# Prediction service (joblib / pandas / scikit-learn)
try:
    from app.prediction.service import model_files_present  # noqa: F401

    _PREDICTION_AVAILABLE = True
    _feature_status["prediction"] = "available"
except Exception as exc:  # pragma: no cover
    _PREDICTION_AVAILABLE = False
    _feature_status["prediction"] = f"missing dependency: {type(exc).__name__}"

    def model_files_present() -> bool:  # type: ignore
        return False

# Computer-vision (Phase 15). The vision MODULE imports with zero heavy CV deps
# (it always offers a deterministic stub engine); cv_engine_info() reports
# whether the REAL torch engine + local model files are present.
try:
    from app.vision.service import cv_engine_info, cv_models_ready  # noqa: F401

    _VISION_AVAILABLE = True
    _feature_status["vision"] = "available"
except Exception as exc:  # pragma: no cover
    _VISION_AVAILABLE = False
    _feature_status["vision"] = f"missing dependency: {type(exc).__name__}"

    def cv_models_ready() -> bool:  # type: ignore
        return False

    def cv_engine_info() -> dict:  # type: ignore
        return {"configured": "auto", "active": "unavailable", "models_ready": False,
                "recognition_ready": False, "model_dir_present": False}


# -----------------------------------------------------------------------------
# APP
# -----------------------------------------------------------------------------
app = FastAPI(title="School AI RAG API (internal)")

# CORS origins come from configuration (ALLOWED_ORIGINS, comma-separated) with
# local-frontend defaults. No production origins are hardcoded. NOTE: the browser
# does NOT call this service; CORS is intentionally narrow.
_default_origins = "http://localhost:5173,http://127.0.0.1:5173,http://localhost:4173,http://127.0.0.1:4173"
ALLOWED_ORIGINS = [
    o.strip()
    for o in os.environ.get("ALLOWED_ORIGINS", _default_origins).split(",")
    if o.strip()
]

app.add_middleware(
    CORSMiddleware,
    allow_origins=ALLOWED_ORIGINS,
    allow_credentials=True,
    allow_methods=["POST", "GET", "DELETE", "OPTIONS"],
    allow_headers=["Authorization", "Content-Type", "X-Correlation-Id"],
)

BASE_DIR = Path(__file__).resolve().parent.parent
VECTORSTORE_DIR = Path(os.environ.get("VECTORSTORE_DIR", str(BASE_DIR / "vectorstore")))
MODEL_DIR = Path(os.environ.get("MODEL_DIR", str(BASE_DIR / "app" / "models")))


# -----------------------------------------------------------------------------
# CORRELATION / TELEMETRY MIDDLEWARE (§17)
# Reads X-Correlation-Id (set by the backend) or generates one; echoes it on
# every response INCLUDING errors; counts requests + latency. No payload is logged.
# -----------------------------------------------------------------------------
@app.middleware("http")
async def correlation_middleware(request: Request, call_next):
    cid = telemetry.set_correlation_id(request.headers.get("X-Correlation-Id"))
    rid = telemetry.new_request_id()
    timer = telemetry.Timer()
    try:
        response = await call_next(request)
    except Exception:
        telemetry.metrics.inc("http.unhandled_errors")
        resp = JSONResponse(
            status_code=500,
            content={"detail": {"code": "internal_error", "message": "internal error"}},
        )
        resp.headers["X-Correlation-Id"] = cid
        resp.headers["X-Request-Id"] = rid
        return resp
    response.headers["X-Correlation-Id"] = cid
    response.headers["X-Request-Id"] = rid
    if request.url.path.startswith("/internal/"):
        telemetry.metrics.observe_latency("http", timer.ms())
    return response


@app.get("/")
def root():
    return {"service": "school-ai-rag", "surface": "internal"}


# -----------------------------------------------------------------------------
# HEALTH / READINESS (§16)
#   /health/live  -> process is up; NEVER touches external providers/deps.
#   /health/ready -> verifies REQUIRED LOCAL components are configured/present.
#
# Deliberate policy: readiness verifies that the provider is CONFIGURED
# (API key present) but never CALLS the provider. Transient provider
# unreachability does NOT affect liveness or readiness — it is handled per
# request by §15 resilience and the no-fabrication contract. Output is redacted:
# no key values, no secrets, no absolute paths.
# -----------------------------------------------------------------------------
def _vectorstore_ready() -> bool:
    try:
        VECTORSTORE_DIR.mkdir(parents=True, exist_ok=True)
        return os.access(VECTORSTORE_DIR, os.W_OK)
    except Exception:
        return False


def _service_auth_configured() -> bool:
    return bool(os.environ.get("SERVICE_AUTH_SIGNING_KEY") or os.environ.get("AI__ServiceSigningKey"))


def _prompt_registry_ok() -> tuple[bool, dict]:
    try:
        from app.prompts.registry import list_prompts

        prompts = list_prompts()
        return (len(prompts) > 0, prompts)
    except Exception:
        return (False, {})


@app.get("/health/live")
def health_live():
    # Liveness proves ONLY that the process is running. It must never fail
    # because Groq or any external dependency is temporarily unavailable.
    return {"status": "alive"}


@app.get("/health/ready")
def health_ready():
    prompt_ok, prompt_versions = _prompt_registry_ok()

    checks = {
        "process": "alive",
        "service_auth": "configured" if _service_auth_configured() else "missing",
        "prompt_registry": "ok" if prompt_ok else "error",
        "provider_configured": "yes" if has_api_key() else "no",
        "rag_dependencies": "available" if _RAG_AVAILABLE else _feature_status.get("rag", "unknown"),
        "prediction_dependencies": "available" if _PREDICTION_AVAILABLE else _feature_status.get("prediction", "unknown"),
        "vector_store": "ready" if _vectorstore_ready() else "unavailable",
        "prediction_models": "present" if model_files_present() else "absent",
    }

    # Embedding model id is a non-sensitive version string, useful for operators.
    try:
        from app.rag.indexer import embedding_model_name as _emn

        checks["embedding_model"] = _emn()
    except Exception:
        checks["embedding_model"] = "unknown"

    # Computer-vision readiness is reported SEPARATELY (Phase 15 requirement) and
    # deliberately does NOT gate the service-level `ready` flag: the AI service is
    # still "ready" for RAG/prediction when CV runs in degraded/stub mode.
    checks["vision_dependencies"] = "available" if _VISION_AVAILABLE else _feature_status.get("vision", "unknown")
    try:
        _cv = cv_engine_info()
        checks["vision_engine_configured"] = str(_cv.get("configured", "auto"))
        checks["vision_engine_active"] = str(_cv.get("active", "unknown"))
        checks["vision_models"] = "present" if _cv.get("models_ready") else "absent"
        checks["vision_recognition"] = "present" if _cv.get("recognition_ready") else "absent"
        checks["vision_ready"] = bool(_cv.get("models_ready"))
    except Exception:
        checks["vision_engine_active"] = "unknown"
        checks["vision_models"] = "absent"
        checks["vision_ready"] = False

    # READY only when every REQUIRED LOCAL component is configured/present.
    # The provider must be CONFIGURED (key present) but is not pinged here.
    fully_ready = (
        checks["service_auth"] == "configured"
        and prompt_ok
        and checks["provider_configured"] == "yes"
        and _RAG_AVAILABLE
        and _PREDICTION_AVAILABLE
        and checks["vector_store"] == "ready"
        and checks["prediction_models"] == "present"
    )

    return {
        "status": "ready" if fully_ready else "degraded",
        "ready": fully_ready,
        "checks": checks,
        "prompt_versions": prompt_versions,
    }


# -----------------------------------------------------------------------------
# INTERNAL CONTRACT (Phase 6 §4). All real AI operations live here, behind the
# service-token dependency applied per-endpoint (scope + tenant from the token).
# -----------------------------------------------------------------------------
try:
    from app.internal.v1.router import router as internal_v1_router

    app.include_router(internal_v1_router)
    _feature_status["internal_v1_router"] = "loaded"
except Exception as exc:  # pragma: no cover
    _feature_status["internal_v1_router"] = f"not loaded: {type(exc).__name__}"

# Computer-vision router (Phase 15). Mounted on the same authenticated internal
# surface (``/internal/v1/vision/*``, scope ``ai:vision``). No public CV route.
try:
    from app.vision.router import vision_router

    app.include_router(vision_router)
    _feature_status["vision_router"] = "loaded"
except Exception as exc:  # pragma: no cover
    _feature_status["vision_router"] = f"not loaded: {type(exc).__name__}"
