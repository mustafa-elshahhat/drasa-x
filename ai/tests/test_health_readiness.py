"""
Health & readiness tests (Phase 6 §16).

Proves:
  * Liveness is independent of external provider availability.
  * Readiness reports each required local component and redacts secrets.
  * Missing required configuration flips ready=false.
"""
import importlib

import pytest
from fastapi.testclient import TestClient

SECRET_KEY = "super-secret-signing-key-do-not-leak-abcdef0123456789"
SECRET_GROQ = "gsk_SECRET_PROVIDER_KEY_should_never_appear_in_readiness_zzz"


def _reload_app(monkeypatch, *, signing=SECRET_KEY, groq=SECRET_GROQ):
    if signing is None:
        monkeypatch.delenv("SERVICE_AUTH_SIGNING_KEY", raising=False)
        monkeypatch.delenv("AI__ServiceSigningKey", raising=False)
    else:
        monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", signing)
    if groq is None:
        # Set empty (not delete): api.load_dotenv() would otherwise restore the
        # real .env value. An empty value is "present" so dotenv won't override it,
        # and has_api_key() treats empty as not-configured.
        monkeypatch.setenv("GROQ_API_KEY", "")
    else:
        monkeypatch.setenv("GROQ_API_KEY", groq)
    import app.api as api
    importlib.reload(api)
    return TestClient(api.app)


def test_liveness_is_independent_of_provider(monkeypatch):
    # Even with NO provider key configured, liveness must report alive.
    client = _reload_app(monkeypatch, groq=None)
    res = client.get("/health/live")
    assert res.status_code == 200
    assert res.json() == {"status": "alive"}


def test_readiness_reports_required_components(monkeypatch):
    client = _reload_app(monkeypatch)
    res = client.get("/health/ready")
    assert res.status_code == 200  # always 200; consumers inspect `ready`
    body = res.json()
    checks = body["checks"]
    for key in (
        "service_auth", "prompt_registry", "provider_configured",
        "rag_dependencies", "prediction_dependencies", "vector_store",
        "prediction_models", "embedding_model",
    ):
        assert key in checks, key
    assert checks["service_auth"] == "configured"
    assert checks["prompt_registry"] == "ok"
    assert checks["provider_configured"] == "yes"
    # Prompt versions are non-sensitive and useful to operators.
    assert "tutor.v1" in body["prompt_versions"]


def test_readiness_redacts_secrets(monkeypatch):
    client = _reload_app(monkeypatch)
    res = client.get("/health/ready")
    raw = res.text
    # Neither the signing key nor the provider key value may appear anywhere.
    assert SECRET_KEY not in raw
    assert SECRET_GROQ not in raw


def test_missing_service_auth_flips_not_ready(monkeypatch):
    client = _reload_app(monkeypatch, signing=None)
    body = client.get("/health/ready").json()
    assert body["checks"]["service_auth"] == "missing"
    assert body["ready"] is False


def test_missing_provider_key_flips_not_ready(monkeypatch):
    # Provider must be CONFIGURED for readiness (key present), though it is never pinged.
    client = _reload_app(monkeypatch, groq=None)
    body = client.get("/health/ready").json()
    assert body["checks"]["provider_configured"] == "no"
    assert body["ready"] is False
