"""
Phase 17 — AI service contract regression pack.

Two cross-cutting guarantees the rest of the platform depends on, pinned in one
place and run fully OFFLINE (no weights, no network, deterministic):

  1. HONEST CV READINESS. With no local model weights (the documented Phase 15/16
     waiver), /health/ready must report the computer-vision capability as NOT ready
     and NEVER fake it — in the default "auto" engine mode AND when the real engine
     is force-selected (CV_ENGINE=torch) without weights. This is the exact signal
     DerasaX-backend reads to keep CV features honestly disabled.

  2. BACKEND -> AI TUTOR CONTRACT. The request shape the backend orchestrator sends
     (correlation_id + message + language + grade + subject) is accepted, and the
     response carries the fields the backend consumes (grounded, citations/citation_
     count, correlation echo, prompt_version). The schema stays strict (unknown
     fields rejected) so a silent contract drift fails loudly here.

No model weights are downloaded; no import-time network access is introduced.
"""
import base64
import hashlib
import hmac
import importlib
import json
import os
import time

import pytest
from fastapi.testclient import TestClient

# --------------------------------------------------------------------------- #
# 1. Honest CV readiness
# --------------------------------------------------------------------------- #

def _ready_checks(monkeypatch, cv_engine):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", "phase17-contract-signing-key-0123456789")
    import app.api as api
    importlib.reload(api)  # runs load_dotenv(); picks up the signing key
    # Set the engine mode AFTER reload — the vision service reads CV_ENGINE
    # dynamically per request, so no second reload is needed and .env cannot win.
    monkeypatch.setenv("CV_ENGINE", cv_engine)
    body = TestClient(api.app).get("/health/ready").json()
    assert body is not None
    return body["checks"]


def test_readiness_reports_cv_unavailable_honestly_in_auto(monkeypatch):
    checks = _ready_checks(monkeypatch, "auto")
    # Auto mode with no weights => deterministic stub, capability NOT ready, never faked.
    assert checks["vision_engine_configured"] == "auto"
    assert checks["vision_engine_active"] == "stub"
    assert checks["vision_models"] == "absent"
    assert checks["vision_ready"] is False  # a real boolean False, not a truthy string


def test_readiness_under_forced_real_engine_stays_honest(monkeypatch):
    checks = _ready_checks(monkeypatch, "torch")
    # Forcing the production engine WITHOUT local weights must not crash and must
    # not fake readiness — it honestly reports the configured-but-unavailable state.
    assert checks["vision_engine_configured"] == "torch"
    assert checks["vision_models"] == "absent"
    assert checks["vision_ready"] is False


def test_liveness_never_blocked_by_missing_cv(monkeypatch):
    # CV being unavailable must never take the service "not alive".
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", "phase17-contract-signing-key-0123456789")
    import app.api as api
    importlib.reload(api)
    monkeypatch.setenv("CV_ENGINE", "torch")
    res = TestClient(api.app).get("/health/live")
    assert res.status_code == 200
    assert res.json() == {"status": "alive"}


# --------------------------------------------------------------------------- #
# 2. Backend -> AI tutor contract
# --------------------------------------------------------------------------- #

KEY = "phase17-tutor-contract-signing-key-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def _mint(scope="ai:tutor", tenant="tenant-1", **over):
    now = int(time.time())
    claims = {
        "iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
        "scope": scope, "tenantId": tenant, "uid": "u-1",
        "jti": f"jti-{now}-{os.urandom(5).hex()}", "iat": now, "exp": now + 120,
    }
    claims.update(over)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT", "kid": "ai-local"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


class _FakeChunk:
    def __init__(self, doc_id, score, title=None):
        self.chunk_id = f"{doc_id}-c"
        self.document_id = doc_id
        self.score = score
        self.snippet = "The water cycle: evaporation, condensation, precipitation."
        self.tenant_id = "tenant-1"
        self.title = title
        self.grade = self.subject = self.unit = self.lesson = self.language = None
        self.metadata = {}


class _FakeRetriever:
    def __init__(self, chunks):
        self._chunks = chunks

    def retrieve(self, *, tenant_id, query, k, grade=None, subject=None, unit=None, lesson=None):
        return self._chunks


@pytest.fixture()
def tutor_client(monkeypatch):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", KEY)
    monkeypatch.setenv("SERVICE_AUTH_ISSUER", "derasax-backend")
    monkeypatch.setenv("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
    import app.security.service_auth as sa
    importlib.reload(sa)
    sa._seen_jti.clear()
    import app.api as api
    importlib.reload(api)
    from app.internal.v1 import router as r
    api.app.dependency_overrides[r.get_tutor_retriever] = lambda: _FakeRetriever([_FakeChunk("water-1", 0.93, title="Water Cycle")])
    api.app.dependency_overrides[r.get_tutor_llm] = lambda: (lambda **kw: "Evaporation, condensation and precipitation.")
    yield TestClient(api.app)
    api.app.dependency_overrides.clear()


def _backend_tutor_body(**over):
    # Exactly the field set DerasaX-backend's AI orchestrator sends (see verify-local).
    b = {
        "correlation_id": "p17-corr-1",
        "message": "What are the stages of the water cycle?",
        "language": "en",
        "grade": 8,
        "subject": "Science",
    }
    b.update(over)
    return b


def test_backend_to_ai_tutor_contract_is_honored(tutor_client):
    res = tutor_client.post("/internal/v1/tutor", json=_backend_tutor_body(),
                            headers={"Authorization": f"Bearer {_mint()}"})
    assert res.status_code == 200, res.text
    data = res.json()
    # The exact response fields the backend consumes:
    assert data["grounded"] is True
    assert data["citation_count"] >= 1
    assert data["citations"][0]["source_document_id"] == "water-1"
    assert data["correlation_id"] == "p17-corr-1"
    assert data["prompt_version"] == "tutor.v1"


def test_backend_to_ai_tutor_contract_rejects_unknown_fields(tutor_client):
    # Strict schema: a drifted/extra field must be rejected, not silently ignored.
    res = tutor_client.post("/internal/v1/tutor", json=_backend_tutor_body(rogue="x"),
                            headers={"Authorization": f"Bearer {_mint()}"})
    assert res.status_code == 422


def test_backend_to_ai_tutor_requires_correct_scope(tutor_client):
    # A token minted for a different scope must not be able to call the tutor.
    res = tutor_client.post("/internal/v1/tutor", json=_backend_tutor_body(),
                            headers={"Authorization": f"Bearer {_mint(scope='ai:quiz')}"})
    assert res.status_code == 403
