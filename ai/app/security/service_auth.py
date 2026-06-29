"""
Internal service-to-service authentication for school-ai-rag.

Validates the signed service JWT issued by DerasaX-backend
(see docs/phase2/SERVICE_AUTHENTICATION.md). Implemented with a small,
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


# --- replay protection (recent jti cache) ------------------------------------
# Single-instance, in-memory store keyed by jti -> token expiry epoch. Suitable
# for the local/compose topology. A multi-instance deployment must replace this
# with a shared store (e.g. Redis SETNX with TTL) — documented in SERVICE_AUTHENTICATION.
_seen_jti: Dict[str, float] = {}
_jti_lock = threading.Lock()


def _remember_jti(jti: str, exp: float) -> bool:
    """Return True if jti is fresh (first use); False if it is a replay."""
    now = time.time()
    with _jti_lock:
        # prune expired entries
        for k in [k for k, v in _seen_jti.items() if v < now]:
            _seen_jti.pop(k, None)
        if jti in _seen_jti:
            return False
        _seen_jti[jti] = exp
        return True


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
