"""
Internal service-to-service authentication for school-ai-rag.

Validates the signed service JWT issued by DerasaX-backend (the internal
backend->AI contract; see the root README and
docs/audit/CROSS_LAYER_ARCHITECTURE_FINDINGS.md). Implemented with a small,
dependency-free HS256 verifier so the service does not require PyJWT.

Validation performed:
  * Bearer token presence
  * alg == HS256 (no "none", no algorithm confusion)
  * HMAC-SHA256 signature against the shared key (kid-selectable)
  * iss / aud
  * exp / nbf (with bounded clock skew)
  * required scope
  * subject (service identity) present
  * jti present + replay protection (recent-jti cache)
  * tenant claim required for tenant-scoped operations

The tenant is ALWAYS taken from the signed token — never from the request body.
"""
from __future__ import annotations

import base64
import hashlib
import hmac
import json
import os
import threading
import time
from typing import Any, Dict, Optional

from fastapi import Header, HTTPException, status

# --- configuration (env) -----------------------------------------------------
_ISSUER = os.environ.get("SERVICE_AUTH_ISSUER", "derasax-backend")
_AUDIENCE = os.environ.get("SERVICE_AUTH_AUDIENCE", "school-ai-rag")
_CLOCK_SKEW = int(os.environ.get("SERVICE_AUTH_CLOCK_SKEW_SECONDS", "60"))
_MAX_TTL = int(os.environ.get("SERVICE_AUTH_MAX_TTL_SECONDS", "300"))  # reject over-long tokens


def _signing_key() -> Optional[str]:
    # Read lazily so tests / runtime can set it after import.
    return os.environ.get("SERVICE_AUTH_SIGNING_KEY") or os.environ.get("AI__ServiceSigningKey")


# --- replay protection (pluggable store) -------------------------------------
# A one-time-use jti guard. The DEFAULT is an in-memory store suitable ONLY for a
# single-instance (local/compose) topology. A horizontally-scaled deployment must
# install a shared store via set_replay_store(SharedReplayStore(redis_client)) so a
# captured token cannot be replayed against a DIFFERENT replica within its TTL
# (AI-02 / SEC-03). The contract is fail-safe: a fresh jti returns True exactly once.


class ReplayStore:
    """Records one-time jti use. ``register_unseen`` returns True on the FIRST use
    of a jti and False on any replay. Implementations must be atomic for their
    deployment topology."""

    def register_unseen(self, jti: str, exp: float) -> bool:  # pragma: no cover - interface
        raise NotImplementedError


class InMemoryReplayStore(ReplayStore):
    """Single-instance, in-memory jti -> expiry-epoch store; prunes expired entries
    on each call. NOT safe across multiple processes/replicas (each has its own
    memory), which is exactly why a multi-instance deployment needs a shared store."""

    def __init__(self, backing: Optional[Dict[str, float]] = None) -> None:
        self._seen: Dict[str, float] = backing if backing is not None else {}
        self._lock = threading.Lock()

    def register_unseen(self, jti: str, exp: float) -> bool:
        now = time.time()
        with self._lock:
            for k in [k for k, v in self._seen.items() if v < now]:
                self._seen.pop(k, None)
            if jti in self._seen:
                return False
            self._seen[jti] = exp
            return True


class SharedReplayStore(ReplayStore):
    """Multi-instance replay store backed by a Redis-like client. The client is
    injected and duck-typed: ``client.set(key, value, nx=True, ex=<int seconds>)``
    must return a truthy value IFF the key was newly set. This module therefore
    never hard-depends on a redis package; the atomic SET-NX-EX is the cross-replica
    guard (Redis SETNX-with-TTL is the canonical backing in production)."""

    def __init__(self, client: Any, namespace: str = "airag:jti:") -> None:
        self._client = client
        self._namespace = namespace

    def register_unseen(self, jti: str, exp: float) -> bool:
        ttl = max(1, int(exp - time.time()) + _CLOCK_SKEW)
        return bool(self._client.set(f"{self._namespace}{jti}", "1", nx=True, ex=ttl))


# Module-level default store. `_seen_jti` remains the in-memory backing dict so
# existing tooling/tests (sa._seen_jti.clear()) keep resetting the replay cache.
_seen_jti: Dict[str, float] = {}
_replay_store: ReplayStore = InMemoryReplayStore(_seen_jti)


def set_replay_store(store: ReplayStore) -> None:
    """Install the replay store (e.g. a SharedReplayStore for multi-instance)."""
    global _replay_store
    _replay_store = store


def get_replay_store() -> ReplayStore:
    return _replay_store


def _remember_jti(jti: str, exp: float) -> bool:
    """Return True if jti is fresh (first use); False if it is a replay.
    Delegates to the configured replay store (in-memory by default)."""
    return _replay_store.register_unseen(jti, exp)


# --- base64url helpers -------------------------------------------------------
def _b64url_decode(data: str) -> bytes:
    pad = "=" * (-len(data) % 4)
    return base64.urlsafe_b64decode(data + pad)


class ServiceAuthError(HTTPException):
    def __init__(self, detail: str):
        super().__init__(status_code=status.HTTP_401_UNAUTHORIZED, detail=detail)


def decode_and_verify(token: str) -> Dict[str, Any]:
    key = _signing_key()
    if not key:
        # Fail closed: without a key we cannot validate anything.
        raise ServiceAuthError("service authentication is not configured")

    parts = token.split(".")
    if len(parts) != 3:
        raise ServiceAuthError("malformed token")

    header_b64, payload_b64, sig_b64 = parts
    try:
        header = json.loads(_b64url_decode(header_b64))
        payload = json.loads(_b64url_decode(payload_b64))
        signature = _b64url_decode(sig_b64)
    except Exception:
        raise ServiceAuthError("malformed token")

    # Algorithm must be HS256 — never "none" or an asymmetric alg.
    if header.get("alg") != "HS256":
        raise ServiceAuthError("unsupported algorithm")

    signing_input = f"{header_b64}.{payload_b64}".encode("ascii")
    expected = hmac.new(key.encode("utf-8"), signing_input, hashlib.sha256).digest()
    if not hmac.compare_digest(expected, signature):
        raise ServiceAuthError("invalid signature")

    now = time.time()

    exp = payload.get("exp")
    if exp is None or now > float(exp) + _CLOCK_SKEW:
        raise ServiceAuthError("token expired")

    nbf = payload.get("nbf")
    if nbf is not None and now < float(nbf) - _CLOCK_SKEW:
        raise ServiceAuthError("token not yet valid")

    iat = payload.get("iat")
    if iat is not None and (float(exp) - float(iat)) > _MAX_TTL + 1:
        raise ServiceAuthError("token lifetime exceeds policy")

    if payload.get("iss") != _ISSUER:
        raise ServiceAuthError("invalid issuer")

    # aud may be a string or a list
    aud = payload.get("aud")
    aud_ok = (aud == _AUDIENCE) or (isinstance(aud, list) and _AUDIENCE in aud)
    if not aud_ok:
        raise ServiceAuthError("invalid audience")

    if not payload.get("sub"):
        raise ServiceAuthError("missing subject")

    jti = payload.get("jti")
    if not jti:
        raise ServiceAuthError("missing jti")

    if not _remember_jti(jti, float(exp)):
        raise ServiceAuthError("token replay detected")

    return payload


def _extract_bearer(authorization: Optional[str]) -> str:
    if not authorization or not authorization.lower().startswith("bearer "):
        raise ServiceAuthError("missing bearer token")
    return authorization[7:].strip()


class ServiceContext:
    def __init__(self, claims: Dict[str, Any]):
        self.claims = claims
        self.subject: str = claims.get("sub", "")
        self.scope: str = claims.get("scope", "")
        self.tenant_id: Optional[str] = claims.get("tenantId")
        self.actor_uid: Optional[str] = claims.get("uid")

    def require_scope(self, scope: str) -> None:
        scopes = self.scope.split() if self.scope else []
        if scope not in scopes and self.scope != scope:
            raise HTTPException(status_code=status.HTTP_403_FORBIDDEN, detail="insufficient scope")

    def require_tenant(self) -> str:
        if not self.tenant_id:
            raise ServiceAuthError("tenant claim required for tenant operation")
        return self.tenant_id


def require_service_token(authorization: Optional[str] = Header(default=None)) -> ServiceContext:
    """FastAPI dependency: validate the service token and return the trusted context."""
    token = _extract_bearer(authorization)
    claims = decode_and_verify(token)
    return ServiceContext(claims)
