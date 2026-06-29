"""
Internal v1 document-lifecycle HTTP contract tests (Phase 6 §7/§24).

Real FastAPI app + real service-token auth; the document store is overridden
with a single shared in-memory store so cross-tenant isolation is exercised
through the wire (different tenant tokens, same backing store).
"""
import base64
import hashlib
import hmac
import importlib
import json
import os
import time

import pytest

KEY = "test-service-signing-key-documents-v1-0123456789"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(scope="ai:ingest", tenant="tenant-1", **over):
    now = int(time.time())
    claims = {
        "iss": "derasax-backend", "aud": "school-ai-rag", "sub": "svc:ai-orchestrator",
        "scope": scope, "tenantId": tenant, "uid": "u-1",
        "jti": f"jti-{now}-{os.urandom(5).hex()}", "iat": now, "exp": now + 120,
    }
    claims.update(over)
    h = _b64url(json.dumps({"alg": "HS256", "typ": "JWT"}).encode())
    p = _b64url(json.dumps(claims).encode())
    sig = hmac.new(KEY.encode(), f"{h}.{p}".encode("ascii"), hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


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
    from fastapi.testclient import TestClient
    from app.internal.v1 import router as r
    from app.rag.store import InMemoryTenantVectorStore

    shared = InMemoryTenantVectorStore()
    api.app.dependency_overrides[r.get_document_store] = lambda: shared
    yield TestClient(api.app)
    api.app.dependency_overrides.clear()


def _doc(**over):
    b = {
        "correlation_id": "c1", "document_id": "doc-1", "version": 1,
        "content": "Photosynthesis converts light energy into chemical energy in plants.",
        "language": "en", "material_type": "textbook", "grade": 8, "subject": "Science",
    }
    b.update(over)
    return b


def _h(scope="ai:ingest", tenant="tenant-1"):
    return {"Authorization": f"Bearer {mint(scope=scope, tenant=tenant)}"}


def test_ingest_valid(client):
    res = client.post("/internal/v1/documents", json=_doc(), headers=_h())
    assert res.status_code == 200, res.text
    body = res.json()
    assert body["status"] == "indexed"
    assert body["chunk_count"] >= 1
    assert body["checksum"]


def test_ingest_missing_token(client):
    assert client.post("/internal/v1/documents", json=_doc()).status_code == 401


def test_ingest_wrong_scope(client):
    res = client.post("/internal/v1/documents", json=_doc(), headers=_h(scope="ai:tutor"))
    assert res.status_code == 403


def test_ingest_unknown_field_rejected(client):
    res = client.post("/internal/v1/documents", json=_doc(bad="x"), headers=_h())
    assert res.status_code == 422


def test_status_then_delete_then_status(client):
    client.post("/internal/v1/documents", json=_doc(), headers=_h())
    st = client.get("/internal/v1/documents/doc-1/status", headers=_h()).json()
    assert st["indexed"] is True and st["chunk_count"] >= 1

    dele = client.request("DELETE", "/internal/v1/documents/doc-1", headers=_h()).json()
    assert dele["status"] == "deleted" and dele["deleted_chunks"] >= 1

    st2 = client.get("/internal/v1/documents/doc-1/status", headers=_h()).json()
    assert st2["indexed"] is False


def test_reindex_mismatched_document_id_rejected(client):
    res = client.post("/internal/v1/documents/other/reindex", json=_doc(document_id="doc-1"), headers=_h())
    assert res.status_code == 400


def test_cross_tenant_isolation_through_http(client):
    # tenant-1 ingests; tenant-2 must not see it.
    client.post("/internal/v1/documents", json=_doc(), headers=_h(tenant="tenant-1"))
    st_other = client.get("/internal/v1/documents/doc-1/status", headers=_h(tenant="tenant-2")).json()
    assert st_other["indexed"] is False
    # tenant-2 deleting the same id removes nothing from tenant-1.
    client.request("DELETE", "/internal/v1/documents/doc-1", headers=_h(tenant="tenant-2"))
    st_own = client.get("/internal/v1/documents/doc-1/status", headers=_h(tenant="tenant-1")).json()
    assert st_own["indexed"] is True
