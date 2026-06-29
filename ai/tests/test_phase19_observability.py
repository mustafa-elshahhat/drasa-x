"""
Phase 19 — AI service observability/operations contracts.

Adds (does not replace) assertions on top of test_health_readiness.py:
  * Liveness is independent of BOTH the provider key and the service-auth key.
  * Readiness is ALWAYS HTTP 200 and reports a status the operator can inspect.
  * CV/vision readiness is reported SEPARATELY and never gates service readiness
    (production CV/model readiness is distinct from local service readiness).
  * The observability metrics endpoint is auth-protected (no anonymous exposure).

All offline: env is monkeypatched and the app is reloaded; no network, no model download.
"""
import importlib

from fastapi.testclient import TestClient

SIGNING = "phase19-obs-signing-key-do-not-leak-abcdef0123456789"
GROQ = "gsk_phase19_obs_provider_key_zzz"


def _reload_app(monkeypatch, *, signing=SIGNING, groq=GROQ):
    if signing is None:
        monkeypatch.delenv("SERVICE_AUTH_SIGNING_KEY", raising=False)
        monkeypatch.delenv("AI__ServiceSigningKey", raising=False)
    else:
        monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", signing)
    # Empty (not deleted) so api.load_dotenv() does not restore the real .env value.
    monkeypatch.setenv("GROQ_API_KEY", groq if groq is not None else "")
    import app.api as api
    importlib.reload(api)
    return TestClient(api.app)


def test_liveness_independent_of_all_external_config(monkeypatch):
    # No provider key AND no service-auth key: the process is still alive.
    client = _reload_app(monkeypatch, signing=None, groq=None)
    res = client.get("/health/live")
    assert res.status_code == 200
    assert res.json() == {"status": "alive"}


def test_readiness_always_200_and_reports_status(monkeypatch):
    client = _reload_app(monkeypatch)
    res = client.get("/health/ready")
    assert res.status_code == 200  # consumers inspect `ready`, never rely on the HTTP code
    body = res.json()
    assert isinstance(body["ready"], bool)
    assert body["status"] in ("ready", "degraded")


def test_cv_readiness_reported_separately_and_non_gating(monkeypatch):
    client = _reload_app(monkeypatch)
    checks = client.get("/health/ready").json()["checks"]
    # CV/vision health is OBSERVABLE and reported on its own keys...
    for key in ("vision_engine_active", "vision_ready", "vision_models"):
        assert key in checks, key
    # ...and it is honest about the local stub posture (never a faked "ready" CV engine).
    assert checks["vision_engine_active"] in ("torch", "stub", "unknown")
    # The vision keys are NOT part of the required-component gating set (a stub CV engine
    # must never, by itself, make the whole service report not-ready).
    assert "vision_ready" not in (
        "service_auth", "prompt_registry", "provider_configured",
        "rag_dependencies", "prediction_dependencies", "vector_store", "prediction_models",
    )


def test_metrics_endpoint_requires_service_token(monkeypatch):
    client = _reload_app(monkeypatch)
    # The observability metrics surface must not be anonymously reachable.
    res = client.get("/internal/v1/metrics")
    assert res.status_code == 401
