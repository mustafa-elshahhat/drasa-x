"""
Internal v1 tutor HTTP contract tests (Phase 6 §4/§5/§20).

Exercises the real FastAPI app + real service-token dependency, with the
retriever and LLM ports overridden by deterministic fakes. Proves: valid
backend call succeeds with normalized citations, missing/!scoped tokens are
rejected, unknown body fields and oversized input are rejected (strict schema),
and ungrounded retrieval yields a safe no-answer.
"""
import base64
import hashlib
import hmac
import importlib
import json
import os
import time

import pytest

KEY = "test-service-signing-key-internal-v1-0123456789"


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
    header = {"alg": "HS256", "typ": "JWT", "kid": "ai-local"}
    h = _b64url(json.dumps(header).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


class _FakeChunk:
    def __init__(self, doc_id, score, snippet="grounded content", title=None):
        self.chunk_id = f"{doc_id}-c"
        self.document_id = doc_id
        self.score = score
        self.snippet = snippet
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
def client(monkeypatch):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", KEY)
    monkeypatch.setenv("SERVICE_AUTH_ISSUER", "derasax-backend")
    monkeypatch.setenv("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
    # Reload auth module so issuer/audience constants pick up the env.
    import app.security.service_auth as sa
    importlib.reload(sa)
    sa._seen_jti.clear()
    import app.api as api
    importlib.reload(api)
    from fastapi.testclient import TestClient
    from app.internal.v1 import router as r

    api.app.dependency_overrides[r.get_tutor_retriever] = lambda: _FakeRetriever([_FakeChunk("d1", 0.9, title="Bio")])
    api.app.dependency_overrides[r.get_tutor_llm] = lambda: (lambda **kw: "A grounded answer.")
    yield TestClient(api.app), api, r
    api.app.dependency_overrides.clear()


def _body(**over):
    b = {"correlation_id": "corr-1", "message": "What is photosynthesis?", "language": "en"}
    b.update(over)
    return b


def test_valid_tutor_request(client):
    c, api, r = client
    res = c.post("/internal/v1/tutor", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 200, res.text
    data = res.json()
    assert data["grounded"] is True
    assert data["citation_count"] == 1
    assert data["citations"][0]["source_document_id"] == "d1"
    assert data["prompt_version"] == "tutor.v1"
    assert data["correlation_id"] == "corr-1"


def test_missing_token_rejected(client):
    c, api, r = client
    res = c.post("/internal/v1/tutor", json=_body())
    assert res.status_code == 401


def test_wrong_scope_rejected(client):
    c, api, r = client
    res = c.post("/internal/v1/tutor", json=_body(),
                 headers={"Authorization": f"Bearer {mint(scope='ai:chat')}"})
    assert res.status_code == 403


def test_unknown_field_rejected(client):
    c, api, r = client
    res = c.post("/internal/v1/tutor", json=_body(injected="x"),
                 headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 422  # extra="forbid"


def test_oversized_message_rejected(client):
    c, api, r = client
    res = c.post("/internal/v1/tutor", json=_body(message="x" * 5000),
                 headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 422


def test_ungrounded_returns_no_answer(client):
    c, api, r = client
    api.app.dependency_overrides[r.get_tutor_retriever] = lambda: _FakeRetriever([])
    res = c.post("/internal/v1/tutor", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 200
    data = res.json()
    assert data["grounded"] is False
    assert data["no_answer_reason"] == "no_relevant_material"
    assert data["citations"] == []
