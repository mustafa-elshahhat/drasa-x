"""
Tenant- and session-scoped sequence frame buffers (Phase 15).

Adapts the per-track engagement buffer state machine from the reference repo
(`services/student_state.py` + `services/student_manager.py`): collect up to
SEQUENCE_LENGTH frames for a tracked face, then expose the full sequence once
and clear it ("strict no-overlap" cycle), with a per-track timeout reset.

CRITICAL FIX vs the reference: the reference uses a single PROCESS-GLOBAL
``student_buffers`` keyed by bare ``track_id``/``student_id``. That leaks frame
state across tenants and classrooms. Here every buffer is keyed by the full
``(tenant_id, session_id, track_id)`` tuple, so no buffer is ever shared across
tenants or sessions. Buffers are in-memory only (never persisted) and hold
small per-frame tokens, never raw pixels.
"""
from __future__ import annotations

import threading
import time
from collections import deque
from typing import Any, Deque, Dict, List, Optional, Tuple

from . import SEQUENCE_LENGTH

DEFAULT_TTL_SECONDS = 300  # evict a track that has not been seen for 5 minutes


class SequenceBufferStore:
    """Thread-safe (tenant, session, track) -> bounded frame deque."""

    def __init__(self, sequence_length: int = SEQUENCE_LENGTH, ttl_seconds: int = DEFAULT_TTL_SECONDS):
        self._seq = int(sequence_length)
        self._ttl = int(ttl_seconds)
        self._lock = threading.Lock()
        self._buffers: Dict[Tuple[str, str, str], Dict[str, Any]] = {}

    @staticmethod
    def _now() -> float:
        return time.time()

    def add_frame(self, tenant_id: str, session_id: str, track_id: str, token: Any) -> int:
        """Append one frame token for a tracked face; return the new frame count.

        A per-track timeout reset (as in the reference) clears a stale buffer
        before appending so an old, abandoned sequence never mixes with a new one.
        """
        key = (tenant_id, session_id, track_id)
        now = self._now()
        with self._lock:
            entry = self._buffers.get(key)
            if entry is None:
                entry = {"frames": deque(maxlen=self._seq), "last_seen": now}
                self._buffers[key] = entry
            elif (now - entry["last_seen"]) > self._ttl:
                entry["frames"].clear()
            entry["last_seen"] = now
            frames: Deque[Any] = entry["frames"]
            frames.append(token)
            return len(frames)

    def is_ready(self, tenant_id: str, session_id: str, track_id: str) -> bool:
        with self._lock:
            entry = self._buffers.get((tenant_id, session_id, track_id))
            return bool(entry) and len(entry["frames"]) >= self._seq

    def take_sequence(self, tenant_id: str, session_id: str, track_id: str) -> Optional[List[Any]]:
        """Return the full sequence and CLEAR it (strict no-overlap), or None if
        not yet complete."""
        key = (tenant_id, session_id, track_id)
        with self._lock:
            entry = self._buffers.get(key)
            if entry is None or len(entry["frames"]) < self._seq:
                return None
            seq = list(entry["frames"])
            entry["frames"].clear()
            return seq

    def count(self, tenant_id: str, session_id: str, track_id: str) -> int:
        with self._lock:
            entry = self._buffers.get((tenant_id, session_id, track_id))
            return len(entry["frames"]) if entry else 0

    def clear_session(self, tenant_id: str, session_id: str) -> int:
        """Drop all track buffers for one (tenant, session). Returns count removed.
        Used when the backend ends a CV session."""
        with self._lock:
            keys = [k for k in self._buffers if k[0] == tenant_id and k[1] == session_id]
            for k in keys:
                self._buffers.pop(k, None)
            return len(keys)

    def cleanup(self, now: Optional[float] = None) -> int:
        """Evict tracks idle beyond the TTL. Returns count removed."""
        now = self._now() if now is None else now
        with self._lock:
            keys = [k for k, v in self._buffers.items() if (now - v["last_seen"]) > self._ttl]
            for k in keys:
                self._buffers.pop(k, None)
            return len(keys)

    def size(self) -> int:
        with self._lock:
            return len(self._buffers)
