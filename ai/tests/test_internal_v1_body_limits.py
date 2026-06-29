"""
Phase 22 / AI-03 — explicit request-body size limits on the internal AI contract.

Proves the large free-text fields reject oversized input both at the schema layer
(Pydantic max_length / list bounds) and at the HTTP boundary (FastAPI -> 422), so a
single client cannot drive unbounded embedding/inference work or memory spikes.

Fully offline: faked store/LLM, no network, no weights.
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
from pydantic import ValidationError

from app.internal.v1 import schemas as S

KEY = "test-bodylimits-signing-key-0123456789abcdef"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope, tenant="tenant-1", **over):
    now = int(time.time())
    claims = {
        "iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
        "scope": scope, "tenantId": tenant, "uid": "u-1",
        "jti": f"jti-{now}-{os.urandom(6).hex()}", "iat": now, "exp": now + 120,
    }
    claims.update(over)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


def auth(scope):
    return {"Authorization": f"Bearer {mint(scope)}"}


# --- schema-layer caps (deterministic; the source of truth FastAPI enforces) --

def test_tutor_message_length_cap():
    S.TutorRequest(correlation_id="c", message="x" * S.MAX_MESSAGE_CHARS)        # boundary accepted
    with pytest.raises(ValidationError):
        S.TutorRequest(correlation_id="c", message="x" * (S.MAX_MESSAGE_CHARS + 1))


def test_conversation_turn_content_cap():
    S.ConversationTurn(role="user", content="x" * S.MAX_MESSAGE_CHARS)            # boundary accepted
    with pytest.raises(ValidationError):
        S.ConversationTurn(role="user", content="x" * (S.MAX_MESSAGE_CHARS + 1))


def test_analysis_conversation_turn_count_cap():
    turn = {"role": "user", "content": "hi"}
    S.AnalysisRequest(correlation_id="c", student_ref="s",
                      conversation=[turn] * S.MAX_ANALYSIS_TURNS)                 # boundary accepted
    with pytest.raises(ValidationError):
        S.AnalysisRequest(correlation_id="c", student_ref="s",
                          conversation=[turn] * (S.MAX_ANALYSIS_TURNS + 1))


def test_analysis_turn_content_cap():
    with pytest.raises(ValidationError):
        S.AnalysisTurn(role="user", content="x" * (S.MAX_MESSAGE_CHARS + 1))


def test_document_content_cap():
    S.IngestDocumentRequest(correlation_id="c", document_id="d", version=1,
                            content="x" * S.MAX_DOCUMENT_CHARS)                   # boundary accepted
    with pytest.raises(ValidationError):
        S.IngestDocumentRequest(correlation_id="c", document_id="d", version=1,
                                content="x" * (S.MAX_DOCUMENT_CHARS + 1))


# --- HTTP boundary: oversized body -> 422 ------------------------------------

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
    from app.rag.store import InMemoryTenantVectorStore
    store = InMemoryTenantVectorStore()
    api.app.dependency_overrides[r.get_tutor_retriever] = lambda: store
    api.app.dependency_overrides[r.get_document_store] = lambda: store
    api.app.dependency_overrides[r.get_tutor_llm] = lambda: (lambda **kw: "ok")
    yield TestClient(api.app)
    api.app.dependency_overrides.clear()


def test_http_tutor_oversized_message_rejected(client):
    body = {"correlation_id": "c", "message": "x" * (S.MAX_MESSAGE_CHARS + 1), "language": "en"}
    assert client.post("/internal/v1/tutor", json=body, headers=auth("ai:tutor")).status_code == 422


def test_http_tutor_oversized_history_turn_rejected(client):
    body = {"correlation_id": "c", "message": "ok", "language": "en",
            "history": [{"role": "user", "content": "x" * (S.MAX_MESSAGE_CHARS + 1)}]}
    assert client.post("/internal/v1/tutor", json=body, headers=auth("ai:tutor")).status_code == 422


def test_http_document_oversized_content_rejected(client):
    body = {"correlation_id": "c", "document_id": "d", "version": 1,
            "content": "x" * (S.MAX_DOCUMENT_CHARS + 1), "language": "en", "material_type": "notes"}
    assert client.post("/internal/v1/documents", json=body, headers=auth("ai:ingest")).status_code == 422
