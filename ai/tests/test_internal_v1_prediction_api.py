"""
Internal v1 prediction HTTP contract tests + prototype-removal proof (§12/§18).
"""
import base64
import hashlib
import hmac
import importlib
import json
import os
import time

import pytest

KEY = "test-service-signing-key-pred-v1-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope="ai:prediction", tenant="tenant-1", **over):
    now = int(time.time())
    claims = {"iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
              "scope": scope, "tenantId": tenant, "uid": "u-1",
              "jti": f"jti-{now}-{os.urandom(5).hex()}", "iat": now, "exp": now + 120}
    claims.update(over)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


class FakePredictor:
    def __init__(self, available=True): self._a = available
    model_version = "rf-2026.06"; feature_schema_version = "perf-v1"; model_name = "rf-performance"
    def available(self): return self._a
    def predict(self, features):
        return {"score": 80.0, "level_index": 2, "level_label": "Strong", "confidence": 0.9,
                "factors": [], "model_name": self.model_name, "model_version": self.model_version,
                "feature_schema_version": self.feature_schema_version}


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


def _client(app_mod, predictor=None):
    from fastapi.testclient import TestClient
    from app.internal.v1 import router as r
    app_mod.app.dependency_overrides[r.get_predictor] = lambda: predictor or FakePredictor()
    return TestClient(app_mod.app)


VALID = dict(age=15, study_hours=10.0, attendance_percentage=85.0, gender="male",
             school_type="public", internet_access="yes", travel_time="<15 min",
             extra_activities="no", study_method="textbook")


def _body(**over):
    b = {"correlation_id": "c1", "student_ref": "stu-1", "prediction_type": "performance",
         "feature_schema_version": "perf-v1", "features": dict(VALID)}
    b.update(over)
    return b


def test_valid_prediction(app_mod):
    c = _client(app_mod)
    try:
        res = c.post("/internal/v1/prediction", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
        assert res.status_code == 200, res.text
        d = res.json()
        assert d["level"] == "Strong" and d["risk_band"] == "low"
        assert d["model_version"] == "rf-2026.06" and d["feature_schema_version"] == "perf-v1"
    finally:
        app_mod.app.dependency_overrides.clear()


def test_missing_token(app_mod):
    c = _client(app_mod)
    try:
        assert c.post("/internal/v1/prediction", json=_body()).status_code == 401
    finally:
        app_mod.app.dependency_overrides.clear()


def test_wrong_scope(app_mod):
    c = _client(app_mod)
    try:
        res = c.post("/internal/v1/prediction", json=_body(), headers={"Authorization": f"Bearer {mint(scope='ai:tutor')}"})
        assert res.status_code == 403
    finally:
        app_mod.app.dependency_overrides.clear()


@pytest.mark.parametrize("body,code", [
    (lambda: {"correlation_id": "c", "student_ref": "s", "feature_schema_version": "perf-v1", "features": dict(VALID), "bad": 1}, 422),
    (lambda: {"correlation_id": "c", "student_ref": "s", "feature_schema_version": "perf-v1", "features": {**VALID, "age": 500}}, 422),
    (lambda: {"correlation_id": "c", "student_ref": "s", "feature_schema_version": "perf-v1", "features": {**VALID, "gender": "x"}}, 422),
    (lambda: {"correlation_id": "c", "student_ref": "s", "feature_schema_version": "perf-v1", "features": {k: v for k, v in VALID.items() if k != "age"}}, 422),
])
def test_invalid_requests_rejected(app_mod, body, code):
    c = _client(app_mod)
    try:
        res = c.post("/internal/v1/prediction", json=body(), headers={"Authorization": f"Bearer {mint()}"})
        assert res.status_code == code, res.text
    finally:
        app_mod.app.dependency_overrides.clear()


def test_unsupported_schema_version(app_mod):
    c = _client(app_mod)
    try:
        res = c.post("/internal/v1/prediction", json=_body(feature_schema_version="perf-v999"),
                     headers={"Authorization": f"Bearer {mint()}"})
        assert res.status_code == 400
    finally:
        app_mod.app.dependency_overrides.clear()


def test_model_unavailable_returns_503(app_mod):
    c = _client(app_mod, predictor=FakePredictor(available=False))
    try:
        res = c.post("/internal/v1/prediction", json=_body(), headers={"Authorization": f"Bearer {mint()}"})
        assert res.status_code == 503
    finally:
        app_mod.app.dependency_overrides.clear()


def test_prototype_predict_routes_removed(app_mod):
    paths = {getattr(r, "path", "") for r in app_mod.app.routes}
    assert not any(p.startswith("/api/predict") for p in paths)
    assert not any(p.startswith("/api/predict") for p in app_mod.app.openapi().get("paths", {}))
