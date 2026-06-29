"""
Telemetry & correlation for school-ai-rag (Phase 6 §17).

Goals:
  * Propagate a correlation id across the request (set by the backend via the
    ``X-Correlation-Id`` header; generated if absent) and echo it on responses
    AND on error responses.
  * Emit ONE structured, *safe* telemetry event per AI operation containing only
    non-sensitive fields (ids, versions, durations, counts, status) — never raw
    prompts, documents, conversations, correct answers, sensitive features, or
    unmasked cross-tenant identifiers.
  * Maintain service-local counters/metrics that are usable now (centralized
    monitoring is Phase 19).

Nothing here ever raises into the request path: telemetry is best-effort.
"""
from __future__ import annotations

import hashlib
import json
import logging
import os
import threading
import time
import uuid
from contextvars import ContextVar
from typing import Any, Dict, Optional

_log = logging.getLogger("ai.telemetry")

# --- correlation context -----------------------------------------------------
_correlation_id: ContextVar[Optional[str]] = ContextVar("correlation_id", default=None)
_request_id: ContextVar[Optional[str]] = ContextVar("request_id", default=None)


def set_correlation_id(value: Optional[str]) -> str:
    cid = (value or "").strip() or f"cid-{uuid.uuid4().hex}"
    _correlation_id.set(cid)
    return cid


def get_correlation_id() -> Optional[str]:
    return _correlation_id.get()


def new_request_id() -> str:
    rid = f"req-{uuid.uuid4().hex}"
    _request_id.set(rid)
    return rid


def get_request_id() -> Optional[str]:
    return _request_id.get()


# --- safe identifiers / masking ----------------------------------------------
def tenant_fingerprint(tenant_id: Optional[str]) -> Optional[str]:
    """A stable, non-reversible short fingerprint of the tenant id for logs."""
    if not tenant_id:
        return None
    return "t#" + hashlib.sha256(tenant_id.encode("utf-8")).hexdigest()[:12]


_SECRET_ENV_KEYS = ("GROQ_API_KEY", "SERVICE_AUTH_SIGNING_KEY", "AI__ServiceSigningKey")


def mask_secrets(text: str) -> str:
    """Redact any known secret values that might appear in a string."""
    if not text:
        return text
    out = text
    for key in _SECRET_ENV_KEYS:
        val = os.environ.get(key)
        if val and len(val) >= 6 and val in out:
            out = out.replace(val, "***REDACTED***")
    return out


# --- service-local metrics ---------------------------------------------------
class _Metrics:
    """Thread-safe in-memory counters + latency sums. Snapshot-able for tests."""

    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._counters: Dict[str, int] = {}
        self._latency_ms: Dict[str, float] = {}
        self._latency_n: Dict[str, int] = {}

    def inc(self, name: str, by: int = 1) -> None:
        with self._lock:
            self._counters[name] = self._counters.get(name, 0) + by

    def observe_latency(self, operation: str, ms: float) -> None:
        with self._lock:
            self._latency_ms[operation] = self._latency_ms.get(operation, 0.0) + ms
            self._latency_n[operation] = self._latency_n.get(operation, 0) + 1

    def snapshot(self) -> Dict[str, Any]:
        with self._lock:
            avg = {
                op: round(self._latency_ms[op] / self._latency_n[op], 2)
                for op in self._latency_ms
                if self._latency_n.get(op)
            }
            return {
                "counters": dict(self._counters),
                "latency_avg_ms": avg,
                "latency_count": dict(self._latency_n),
            }

    def reset(self) -> None:
        with self._lock:
            self._counters.clear()
            self._latency_ms.clear()
            self._latency_n.clear()


metrics = _Metrics()


# --- structured AI operation event -------------------------------------------
def record_ai_event(
    operation: str,
    *,
    status: str,
    tenant_id: Optional[str] = None,
    duration_ms: Optional[int] = None,
    prompt_version: Optional[str] = None,
    model_version: Optional[str] = None,
    error_category: Optional[str] = None,
    retrieval_count: Optional[int] = None,
    citation_count: Optional[int] = None,
    tokens: Optional[int] = None,
    no_answer: Optional[bool] = None,
    schema_validation_failed: Optional[bool] = None,
) -> None:
    """Emit one safe telemetry event and update counters. Never raises."""
    try:
        metrics.inc(f"requests.{operation}")
        metrics.inc("requests.total")
        if status == "success":
            metrics.inc(f"success.{operation}")
            metrics.inc("success.total")
        else:
            metrics.inc(f"failure.{operation}")
            metrics.inc("failure.total")
        if error_category:
            metrics.inc(f"error.{error_category}")
            if error_category in ("timeout", "provider_error", "provider_unavailable"):
                metrics.inc("provider_errors.total")
            if error_category == "timeout":
                metrics.inc("timeouts.total")
        if no_answer:
            metrics.inc(f"no_answer.{operation}")
        if schema_validation_failed:
            metrics.inc("schema_validation_failures.total")
        if duration_ms is not None:
            metrics.observe_latency(operation, float(duration_ms))

        event: Dict[str, Any] = {
            "event": "ai_operation",
            "operation": operation,
            "status": status,
            "correlation_id": get_correlation_id(),
            "request_id": get_request_id(),
            "tenant": tenant_fingerprint(tenant_id),
            "prompt_version": prompt_version,
            "model_version": model_version,
            "duration_ms": duration_ms,
            "error_category": error_category,
            "retrieval_count": retrieval_count,
            "citation_count": citation_count,
            "tokens": tokens,
            "no_answer": no_answer,
            "schema_validation_failed": schema_validation_failed,
        }
        # Drop None values to keep the line compact; never include raw content.
        event = {k: v for k, v in event.items() if v is not None}
        _log.info(mask_secrets(json.dumps(event, ensure_ascii=False)))
    except Exception:  # pragma: no cover - telemetry must never break a request
        pass


class Timer:
    """Monotonic elapsed-ms helper for telemetry durations."""

    def __init__(self) -> None:
        self._start = time.monotonic()

    def ms(self) -> int:
        return int((time.monotonic() - self._start) * 1000)
