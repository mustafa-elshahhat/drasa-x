"""
Phase 15 — internal CV endpoint contract tests.

Proves: service-token auth (missing/scope/tenant), invalid-image handling,
deterministic stub analysis, sequence engagement NotReady->Ready, tenant
isolation of engagement buffers, forced-real-engine 503 (model-not-ready),
end-session buffer clearing, no startup crash, and CV readiness reported
SEPARATELY on /health/ready. No heavy CV deps, no model files, no network.
"""
import base64
import hashlib
import hmac
import importlib
import io
import json
import os
import time

import pytest

KEY = "test-service-signing-key-vision-v1-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope="ai:vision", tenant="tenant-1", **over):
    now = int(time.time())
    claims = {"iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
              "scope": scope, "tenantId": tenant, "uid": "u-1",
              "jti": f"jti-{now}-{os.urandom(5).hex()}", "iat": now, "exp": now + 120}
    claims.update(over)
    if claims.get("tenantId") is None:
        claims.pop("tenantId", None)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


def _png_b64(color=(120, 30, 200), size=(80, 80)) -> str:
    from PIL import Image

    buf = io.BytesIO()
    Image.new("RGB", size, color).save(buf, format="PNG")
    return base64.b64encode(buf.getvalue()).decode("ascii")


@pytest.fixture()
def app_mod(monkeypatch):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", KEY)
    monkeypatch.setenv("SERVICE_AUTH_ISSUER", "derasax-backend")
    monkeypatch.setenv("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
    monkeypatch.delenv("CV_ENGINE", raising=False)  # default 'auto' -> stub locally
    import app.security.service_auth as sa
    importlib.reload(sa); sa._seen_jti.clear()
    import app.api as api
    importlib.reload(api)
    return api


def _client(app_mod):
    from fastapi.testclient import TestClient
    return TestClient(app_mod.app)


def _body(session_id="sess-1", **over):
    b = {"correlation_id": "c1", "session_id": session_id, "image_base64": _png_b64(),
         "frame_index": 0, "want_engagement": True}
    b.update(over)
    return b


def _hdr(token=None):
    return {"Authorization": f"Bearer {token or mint()}"}


# --- auth -------------------------------------------------------------------
def test_missing_token_401(app_mod):
    c = _client(app_mod)
    assert c.post("/internal/v1/vision/analyze", json=_body()).status_code == 401


def test_wrong_scope_403(app_mod):
    c = _client(app_mod)
    res = c.post("/internal/v1/vision/analyze", json=_body(), headers=_hdr(mint(scope="ai:tutor")))
    assert res.status_code == 403


def test_tenant_required_401(app_mod):
    c = _client(app_mod)
    res = c.post("/internal/v1/vision/analyze", json=_body(), headers=_hdr(mint(tenant=None)))
    assert res.status_code == 401


# --- validation -------------------------------------------------------------
def test_invalid_image_400(app_mod):
    c = _client(app_mod)
    res = c.post("/internal/v1/vision/analyze",
                 json=_body(image_base64="not-an-image"), headers=_hdr())
    assert res.status_code == 400
    assert res.json()["detail"]["code"] == "invalid_image"


def test_unknown_field_rejected_422(app_mod):
    c = _client(app_mod)
    res = c.post("/internal/v1/vision/analyze",
                 json={**_body(), "surprise": 1}, headers=_hdr())
    assert res.status_code == 422


# --- analysis (stub) --------------------------------------------------------
def test_valid_stub_analysis(app_mod):
    c = _client(app_mod)
    res = c.post("/internal/v1/vision/analyze", json=_body(session_id="sess-valid"), headers=_hdr())
    assert res.status_code == 200, res.text
    d = res.json()
    assert d["engine"] == "stub" and d["degraded"] is True
    assert d["faces_detected"] >= 1 and len(d["results"]) == d["faces_detected"]
    assert d["sequence_length"] == 16
    f = d["results"][0]
    # honest contract: opaque external label, never a DerasaX student, no biometrics returned
    assert f["external_label"] == "Unknown"
    assert "embedding" not in f and "image" not in f and "face_crop" not in f
    assert f["recognition_status"] in ("candidate", "low_confidence", "unknown")
    assert f["engagement"] == "NotReady"          # a single frame can never be engaged
    assert f["engagement_frames_required"] == 16
    assert 0.0 <= f["recognition_confidence"] <= 1.0


def test_engagement_not_ready_until_full_sequence(app_mod):
    c = _client(app_mod)
    body = _body(session_id="sess-seq")
    last = None
    for i in range(16):
        res = c.post("/internal/v1/vision/analyze", json={**body, "frame_index": i}, headers=_hdr())
        assert res.status_code == 200, res.text
        last = res.json()
        if i < 15:
            assert all(r["engagement"] == "NotReady" for r in last["results"]), f"frame {i}"
    # 16th frame completes the sequence for each tracked face
    assert all(r["engagement"] in ("Engaged", "Disengaged") for r in last["results"])
    assert all(r["engagement_frames"] == 16 for r in last["results"])
    assert all(0.5 <= r["engagement_confidence"] <= 1.0 for r in last["results"])


def test_engagement_buffer_is_tenant_isolated(app_mod):
    c = _client(app_mod)
    body = _body(session_id="sess-iso")
    # tenant-1 fills the sequence
    for i in range(16):
        c.post("/internal/v1/vision/analyze", json={**body, "frame_index": i}, headers=_hdr(mint(tenant="tenant-1")))
    # tenant-2, same session id + image => independent buffer, still NotReady
    res = c.post("/internal/v1/vision/analyze", json=body, headers=_hdr(mint(tenant="tenant-2")))
    d = res.json()
    assert all(r["engagement"] == "NotReady" for r in d["results"])
    assert all(r["engagement_frames"] == 1 for r in d["results"])


def test_end_session_clears_buffers(app_mod):
    c = _client(app_mod)
    body = _body(session_id="sess-end")
    for i in range(5):
        c.post("/internal/v1/vision/analyze", json={**body, "frame_index": i}, headers=_hdr())
    res = c.post("/internal/v1/vision/end-session",
                 json={"correlation_id": "c1", "session_id": "sess-end"}, headers=_hdr())
    assert res.status_code == 200
    assert res.json()["buffers_cleared"] >= 1
    # after clearing, the next frame starts a fresh sequence at count 1
    res2 = c.post("/internal/v1/vision/analyze", json=body, headers=_hdr())
    assert all(r["engagement_frames"] == 1 for r in res2.json()["results"])


# --- forced real engine unavailable -> 503 (model-not-ready) ----------------
def test_forced_torch_engine_returns_503(app_mod, monkeypatch):
    monkeypatch.setenv("CV_ENGINE", "torch")  # real engine forced but deps/models absent
    c = _client(app_mod)
    res = c.post("/internal/v1/vision/analyze", json=_body(session_id="sess-503"), headers=_hdr())
    assert res.status_code == 503
    assert res.json()["detail"]["code"] == "model_unavailable"


# --- no startup crash + CV readiness reported separately --------------------
def test_no_startup_crash_liveness(app_mod):
    c = _client(app_mod)
    assert c.get("/health/live").json() == {"status": "alive"}


def test_readiness_reports_cv_separately(app_mod):
    c = _client(app_mod)
    body = c.get("/health/ready").json()
    checks = body["checks"]
    assert "vision_engine_active" in checks
    assert "vision_models" in checks
    assert "vision_ready" in checks
    # locally the real engine is absent => stub active, CV not ready, but the
    # process is alive and the response is well-formed (no crash).
    assert checks["vision_models"] == "absent"
    assert checks["vision_ready"] is False
    assert checks["vision_engine_active"] in ("stub", "torch", "unavailable")


def test_vision_route_registered(app_mod):
    paths = {getattr(r, "path", "") for r in app_mod.app.routes}
    assert "/internal/v1/vision/analyze" in paths
    # and there is NO unauthenticated public /analyze passthrough
    assert "/analyze" not in paths and "/analyze/" not in paths
