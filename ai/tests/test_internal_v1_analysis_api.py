"""
Internal v1 analysis HTTP contract tests + prototype-removal proof (§13/§18).
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

KEY = "test-service-signing-key-analysis-v1-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope="ai:analyze", tenant="tenant-1", **over):
    now = int(time.time())
    claims = {"iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
              "scope": scope, "tenantId": tenant, "uid": "u-1",
              "jti": f"jti-{now}-{os.urandom(5).hex()}", "iat": now, "exp": now + 120}
    claims.update(over)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


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


def _client(app_mod):
    from fastapi.testclient import TestClient
    from app.internal.v1 import router as r
    app_mod.app.dependency_overrides[r.get_analysis_llm] = lambda: (lambda **kw: json.dumps(
        {"pain_point_category": "concept", "evidence_summary": "struggles with fractions",
         "recommendation": "practice fractions", "confidence": 0.6, "escalation_level": "monitor", "signals": ["x"]}))
    return TestClient(app_mod.app)


def _body(**over):
    b = {"correlation_id": "c1", "student_ref": "stu-1", "language": "en", "subject": "Math",
         "conversation": [{"role": "user", "content": "I don't get fractions"}]}
    b.update(over)
    return b


def test_valid_analysis(app_mod):
    c = _client(app_mod)
    try:
        res = c.post("/internal/v1/analysis", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
        assert res.status_code == 200, res.text
        d = res.json()
        assert d["pain_point_category"] == "concept"
        assert d["human_review_required"] is True
        assert d["prompt_version"] == "analysis.v1"
    finally:
        app_mod.app.dependency_overrides.clear()


def test_missing_token(app_mod):
    c = _client(app_mod)
    try:
        assert c.post("/internal/v1/analysis", json=_body()).status_code == 401
    finally:
        app_mod.app.dependency_overrides.clear()


def test_wrong_scope(app_mod):
    c = _client(app_mod)
    try:
        res = c.post("/internal/v1/analysis", json=_body(), headers={"Authorization": f"Bearer {mint(scope='ai:tutor')}"})
        assert res.status_code == 403
    finally:
        app_mod.app.dependency_overrides.clear()


def test_unknown_field_and_empty_conversation_rejected(app_mod):
    c = _client(app_mod)
    try:
        assert c.post("/internal/v1/analysis", json=_body(bad=1), headers={"Authorization": f"Bearer {mint()}"}).status_code == 422
        assert c.post("/internal/v1/analysis", json=_body(conversation=[]), headers={"Authorization": f"Bearer {mint()}"}).status_code == 422
    finally:
        app_mod.app.dependency_overrides.clear()


def test_prototype_analyze_routes_removed(app_mod):
    paths = {getattr(r, "path", "") for r in app_mod.app.routes}
    assert not any(p.startswith("/api/analyze") for p in paths)
    assert not any(p.startswith("/api/analyze") for p in app_mod.app.openapi().get("paths", {}))


def test_student_profile_store_removed():
    assert not (Path(__file__).resolve().parents[1] / "app" / "data" / "student_profile_store.json").exists()
