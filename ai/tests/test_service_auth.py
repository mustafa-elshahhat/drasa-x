"""
Service-to-service authentication tests for school-ai-rag (Phase 3 §27).

Covers: missing token, invalid signature, wrong issuer/audience, expired,
excessive expiration, "none"/wrong algorithm, missing/!replayed jti, missing
scope, missing tenant claim, and a browser-style token being rejected as a
service credential.
"""
import base64
import hashlib
import hmac
import importlib
import json
import os
import time

import pytest

KEY = "test-service-signing-key-0123456789abcdef"
OTHER_KEY = "a-different-key-not-the-service-key-9999"


def _b64url(b: bytes) -> str:
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def mint(claims, key=KEY, alg="HS256", header_extra=None):
    header = {"alg": alg, "typ": "JWT"}
    if header_extra:
        header.update(header_extra)
    h = _b64url(json.dumps(header).encode())
    p = _b64url(json.dumps(claims).encode())
    signing_input = f"{h}.{p}".encode("ascii")
    if alg == "none":
        return f"{h}.{p}."
    sig = hmac.new(key.encode(), signing_input, hashlib.sha256).digest()
    return f"{h}.{p}.{_b64url(sig)}"


def base_claims(**over):
    now = int(time.time())
    c = {
        "iss": "derasax-backend",
        "aud": "school-ai-rag",
        "sub": "svc:ai-orchestrator",
        "scope": "ai:chat",
        "tenantId": "tenant-1",
        "uid": "u-123",
        "jti": f"jti-{now}-{os.urandom(4).hex()}",
        "iat": now,
        "exp": now + 120,
    }
    c.update(over)
    return c


@pytest.fixture()
def sa(monkeypatch):
    monkeypatch.setenv("SERVICE_AUTH_SIGNING_KEY", KEY)
    monkeypatch.setenv("SERVICE_AUTH_ISSUER", "derasax-backend")
    monkeypatch.setenv("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
    import app.security.service_auth as m
    importlib.reload(m)
    m._seen_jti.clear()
    return m


def test_valid_token_accepted(sa):
    claims = sa.decode_and_verify(mint(base_claims()))
    assert claims["tenantId"] == "tenant-1"
    assert claims["sub"] == "svc:ai-orchestrator"


def test_missing_bearer_rejected(sa):
    with pytest.raises(sa.ServiceAuthError):
        sa.require_service_token(authorization=None)


def test_invalid_signature_rejected(sa):
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(base_claims(), key=OTHER_KEY))


def test_wrong_issuer_rejected(sa):
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(base_claims(iss="evil")))


def test_wrong_audience_rejected(sa):
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(base_claims(aud="someone-else")))


def test_expired_rejected(sa):
    now = int(time.time())
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(base_claims(iat=now - 600, exp=now - 300)))


def test_excessive_ttl_rejected(sa):
    now = int(time.time())
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(base_claims(iat=now, exp=now + 4000)))


def test_none_algorithm_rejected(sa):
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(base_claims(), alg="none"))


def test_missing_jti_rejected(sa):
    c = base_claims()
    c.pop("jti")
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(mint(c))


def test_replay_rejected(sa):
    token = mint(base_claims())
    sa.decode_and_verify(token)  # first use ok
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(token)  # replay


def test_missing_scope_denied(sa):
    ctx = sa.ServiceContext(sa.decode_and_verify(mint(base_claims(scope=""))))
    with pytest.raises(Exception):
        ctx.require_scope("ai:chat")


def test_wrong_scope_denied(sa):
    ctx = sa.ServiceContext(sa.decode_and_verify(mint(base_claims(scope="ai:build"))))
    with pytest.raises(Exception):
        ctx.require_scope("ai:chat")


def test_missing_tenant_for_tenant_op(sa):
    c = base_claims()
    c.pop("tenantId")
    ctx = sa.ServiceContext(sa.decode_and_verify(mint(c)))
    with pytest.raises(sa.ServiceAuthError):
        ctx.require_tenant()


def test_browser_style_token_rejected(sa):
    # A browser access token: different signing key + different audience/issuer.
    browser = mint(
        {
            "iss": "derasax-backend",
            "aud": "derasax-frontend",
            "sub": "student-user",
            "role": "Student",
            "tenantId": "tenant-1",
            "jti": "abc",
            "iat": int(time.time()),
            "exp": int(time.time()) + 900,
        },
        key=OTHER_KEY,
    )
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(browser)


# --- pluggable replay store (PR-2 / AI-02 / SEC-03) --------------------------

class _FakeSharedRedis:
    """Minimal Redis-like double implementing SET ... NX EX (cross-replica state)."""

    def __init__(self):
        self.store = {}

    def set(self, key, value, nx=False, ex=None):  # noqa: A003 - mirror redis-py signature
        if nx and key in self.store:
            return None          # already set -> NOT newly set (replay)
        self.store[key] = value
        return True              # newly set (first use)


def test_default_replay_store_is_in_memory(sa):
    assert isinstance(sa.get_replay_store(), sa.InMemoryReplayStore)


def test_in_memory_store_misses_cross_instance_replay(sa):
    # Demonstrates WHY a shared store is needed at scale: the in-memory store is
    # per-process. Clearing it simulates a SECOND replica with empty local memory,
    # which would (wrongly) accept the replayed token.
    token = mint(base_claims())
    sa.decode_and_verify(token)            # replica A: first use ok
    sa._seen_jti.clear()                    # replica B: separate process memory (no record of jti)
    sa.decode_and_verify(token)            # NOT rejected by the single-instance cache


def test_shared_store_rejects_cross_instance_replay(sa):
    # With a shared store installed, a captured token is rejected even by a replica
    # that has never seen it locally (its local in-memory cache is empty).
    sa.set_replay_store(sa.SharedReplayStore(_FakeSharedRedis()))
    token = mint(base_claims())
    sa.decode_and_verify(token)            # replica A: first use ok (recorded in the SHARED store)
    sa._seen_jti.clear()                    # replica B: empty LOCAL memory...
    with pytest.raises(sa.ServiceAuthError):
        sa.decode_and_verify(token)        # ...but the SHARED store still rejects the replay


def test_shared_store_still_accepts_a_fresh_token(sa):
    # The shared store must not break legitimate, distinct one-time tokens.
    sa.set_replay_store(sa.SharedReplayStore(_FakeSharedRedis()))
    sa.decode_and_verify(mint(base_claims()))
    sa.decode_and_verify(mint(base_claims()))  # different jti -> accepted
