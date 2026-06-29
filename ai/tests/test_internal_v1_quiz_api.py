"""
Internal v1 quiz-draft HTTP contract tests + prototype-removal proof (§11/§18).

Real FastAPI app + real service-token auth; retriever and JSON-LLM overridden.
Also asserts the removed prototype quiz endpoints no longer exist (no route, not
in OpenAPI) and that the AI service holds no quiz store.
"""
import base64
import hashlib
import hmac
import importlib
import json
import os
import time
from pathlib import Path

import pytest

KEY = "test-service-signing-key-quiz-v1-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope="ai:quiz", tenant="tenant-1", **over):
    now = int(time.time())
    claims = {"iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
              "scope": scope, "tenantId": tenant, "uid": "u-1",
              "jti": f"jti-{now}-{os.urandom(5).hex()}", "iat": now, "exp": now + 120}
    claims.update(over)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


class _Chunk:
    def __init__(self, doc_id="doc-1", score=0.9):
        self.chunk_id = f"{doc_id}-c"; self.document_id = doc_id; self.score = score
        self.snippet = "Chlorophyll absorbs sunlight; plants release oxygen."; self.tenant_id = "tenant-1"
        self.title = "Bio U1"; self.grade = 8; self.subject = "science"
        self.unit = self.lesson = self.language = None; self.metadata = {}


class _Retriever:
    def retrieve(self, *, tenant_id, query, k, grade=None, subject=None, unit=None, lesson=None):
        return [_Chunk()]


def _llm_factory():
    def _llm(*, system, context, instruction):
        return json.dumps({"title": "Quiz", "instructions": "Answer.", "questions": [
            {"question_type": "mcq", "question_text": "What does chlorophyll absorb?",
             "options": ["Sunlight", "Water", "Soil", "Air"], "correct_index": 0,
             "explanation": "from text", "points": 2},
        ]})
    return _llm


@pytest.fixture()
def app_mod(monkeypatch):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", KEY)
    monkeypatch.setenv("SERVICE_AUTH_ISSUER", "derasax-backend")
    monkeypatch.setenv("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
    import app.security.service_auth as sa
    importlib.reload(sa); sa._seen_jti.clear()
    import app.api as api
    importlib.reload(api)
    return api


@pytest.fixture()
def client(app_mod):
    from fastapi.testclient import TestClient
    from app.internal.v1 import router as r
    app_mod.app.dependency_overrides[r.get_quiz_retriever] = lambda: _Retriever()
    app_mod.app.dependency_overrides[r.get_quiz_llm] = _llm_factory
    yield TestClient(app_mod.app)
    app_mod.app.dependency_overrides.clear()


def _body(**over):
    b = {"correlation_id": "c1", "num_questions": 1, "language": "en", "grade": 8,
         "subject": "Science", "topic": "photosynthesis", "difficulty": "core", "question_types": ["mcq"]}
    b.update(over)
    return b


def test_valid_draft(client):
    res = client.post("/internal/v1/quiz/draft", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 200, res.text
    d = res.json()
    assert d["grounded"] is True
    assert d["draft"]["question_count"] == 1
    assert d["prompt_version"] == "quiz.v1"
    assert d["draft"]["questions"][0]["source_references"] == ["doc-1"]


def test_missing_token(client):
    assert client.post("/internal/v1/quiz/draft", json=_body()).status_code == 401


def test_wrong_scope(client):
    res = client.post("/internal/v1/quiz/draft", json=_body(), headers={"Authorization": f"Bearer {mint(scope='ai:tutor')}"})
    assert res.status_code == 403


def test_unknown_field_rejected(client):
    res = client.post("/internal/v1/quiz/draft", json=_body(injected="x"), headers={"Authorization": f"Bearer {mint()}"})
    assert res.status_code == 422


# ---- prototype removal proof (§18) ----------------------------------------

def test_prototype_quiz_routes_removed(app_mod):
    paths = {getattr(r, "path", "") for r in app_mod.app.routes}
    assert not any(p.startswith("/api/quiz") for p in paths), f"prototype quiz routes still present: {paths}"


def test_prototype_quiz_absent_from_openapi(app_mod):
    spec = app_mod.app.openapi()
    assert not any(p.startswith("/api/quiz") for p in spec.get("paths", {}))


def test_quiz_store_json_removed():
    assert not (Path(__file__).resolve().parents[1] / "app" / "data" / "quiz_store.json").exists()
