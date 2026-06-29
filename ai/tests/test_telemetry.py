"""
Telemetry & correlation tests (Phase 6 §17).

Proves:
  * Correlation id propagates (header in → same header out) and is generated when absent.
  * Correlation id is returned even on error responses (401).
  * Service-local metrics increment on success and failure.
  * Secret masking redacts known secret values.
  * The metrics snapshot exposes only aggregate counters (no payloads/ids).
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

KEY = "test-service-signing-key-telemetry-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope="ai:tutor", tenant="tenant-1", **over):
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
    def __init__(self, doc_id, score):
        self.chunk_id = f"{doc_id}-c"
        self.document_id = doc_id
        self.score = score
        self.snippet = "grounded content"
        self.tenant_id = "tenant-1"
        self.title = None
        self.grade = self.subject = self.unit = self.lesson = self.language = None
        self.metadata = {}


class _FakeRetriever:
    def __init__(self, chunks):
        self._chunks = chunks

    def retrieve(self, *, tenant_id, query, k, grade=None, subject=None, unit=None, lesson=None):
        return self._chunks


@pytest.fixture()
def client(monkeypatch):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", KEY)
    monkeypatch.setenv("SERVICE_AUTH_ISSUER", "derasax-backend")
    monkeypatch.setenv("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
    import app.security.service_auth as sa
    importlib.reload(sa)
    sa._seen_jti.clear()
    import app.api as api
    importlib.reload(api)
    from app.internal.v1 import router as r
    from app.observability import telemetry

    telemetry.metrics.reset()
    api.app.dependency_overrides[r.get_tutor_retriever] = lambda: _FakeRetriever([_FakeChunk("d1", 0.9)])
    api.app.dependency_overrides[r.get_tutor_llm] = lambda: (lambda **kw: "A grounded answer.")
    yield TestClient(api.app), api, r, telemetry
    api.app.dependency_overrides.clear()


def _body(**over):
    b = {"correlation_id": "corr-1", "message": "What is photosynthesis?", "language": "en"}
    b.update(over)
    return b


def test_correlation_header_propagates(client):
    c, api, r, telemetry = client
    res = c.post("/internal/v1/tutor", json=_body(),
                 headers={"Authorization": f"Bearer {mint()}", "X-Correlation-Id": "trace-xyz"})
    assert res.status_code == 200
    assert res.headers["X-Correlation-Id"] == "trace-xyz"
    assert res.headers.get("X-Request-Id", "").startswith("req-")


def test_correlation_generated_when_absent(client):
    c, api, r, telemetry = client
    res = c.post("/internal/v1/tutor", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 200
    assert res.headers["X-Correlation-Id"]  # non-empty generated id


def test_correlation_returned_on_error(client):
    c, api, r, telemetry = client
    res = c.post("/internal/v1/tutor", json=_body())  # no token → 401
    assert res.status_code == 401
    assert res.headers["X-Correlation-Id"]  # present even on error


def test_metrics_increment_on_success(client):
    c, api, r, telemetry = client
    c.post("/internal/v1/tutor", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    snap = telemetry.metrics.snapshot()
    assert snap["counters"].get("requests.tutor", 0) >= 1
    assert snap["counters"].get("success.tutor", 0) >= 1


def test_metrics_increment_on_failure(client):
    c, api, r, telemetry = client
    # Force a provider failure: llm raises.
    def _boom(**kw):
        raise RuntimeError("provider down")
    api.app.dependency_overrides[r.get_tutor_llm] = lambda: _boom
    res = c.post("/internal/v1/tutor", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 502
    snap = telemetry.metrics.snapshot()
    assert snap["counters"].get("failure.tutor", 0) >= 1


def test_metrics_endpoint_exposes_only_aggregates(client):
    c, api, r, telemetry = client
    c.post("/internal/v1/tutor", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    res = c.get("/internal/v1/metrics", headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 200
    body = res.json()
    assert set(body.keys()) == {"counters", "latency_avg_ms", "latency_count"}
    # No payloads / message content / tenant ids leak into metrics.
    raw = res.text
    assert "photosynthesis" not in raw
    assert "tenant-1" not in raw


def test_metrics_endpoint_requires_token(client):
    c, api, r, telemetry = client
    assert c.get("/internal/v1/metrics").status_code == 401


def test_mask_secrets_redacts_known_values(client, monkeypatch):
    c, api, r, telemetry = client
    monkeypatch.setenv("GROQ_API_KEY", "gsk_LIVE_SECRET_VALUE_123456")
    masked = telemetry.mask_secrets("error contacting provider with gsk_LIVE_SECRET_VALUE_123456 now")
    assert "gsk_LIVE_SECRET_VALUE_123456" not in masked
    assert "***REDACTED***" in masked


def test_tenant_fingerprint_is_non_reversible(client):
    c, api, r, telemetry = client
    fp = telemetry.tenant_fingerprint("tenant-1")
    assert fp and fp.startswith("t#")
    assert "tenant-1" not in fp
