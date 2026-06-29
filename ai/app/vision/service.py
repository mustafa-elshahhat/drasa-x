"""
CV analysis orchestration (Phase 15) — selects an engine, runs detection +
recognition + emotion per face, and drives the sequence-based engagement model
through the tenant/session-scoped buffer store.

Follows the same discipline as ``app/prediction/service.py``: configured
behaviour only, NO downloads, explicit failure when a forced engine is
unavailable (affects readiness, not liveness), and NEVER a fabricated result.
"""
from __future__ import annotations

import os
import threading
from datetime import datetime, timezone
from pathlib import Path

from . import DEFAULT_RECOGNITION_THRESHOLD, SEQUENCE_LENGTH
from .engine import StubVisionEngine, TorchVisionEngine, VisionEngine
from .preprocess import decode_image
from .schemas import (
    VisionAnalyzeRequest,
    VisionAnalyzeResponse,
    VisionFaceResult,
)
from .state import SequenceBufferStore

# --- configuration (env) -----------------------------------------------------
# CV_ENGINE / threshold are read DYNAMICALLY (per call) so operators — and tests
# — can change behaviour via the environment without reimporting the module.
_MODEL_DIR = Path(os.environ.get("MODEL_DIR", str(Path(__file__).resolve().parent.parent / "models")))
CV_MODEL_DIR = Path(os.environ.get("CV_MODEL_DIR", str(_MODEL_DIR / "vision")))
CV_MODEL_VERSION = os.environ.get("CV_MODEL_VERSION", "cv-2026.06")

_buffer_store = SequenceBufferStore(SEQUENCE_LENGTH)
_stub = StubVisionEngine()
_torch_lock = threading.Lock()
_torch: TorchVisionEngine | None = None


class VisionUnavailableError(RuntimeError):
    """Raised when a forced engine (CV_ENGINE=torch) is not available — no fallback."""


def _engine_mode() -> str:
    return os.environ.get("CV_ENGINE", "auto").strip().lower()


def _threshold(override: float | None = None) -> float:
    if override is not None:
        return override
    return float(os.environ.get("CV_RECOGNITION_THRESHOLD", str(DEFAULT_RECOGNITION_THRESHOLD)))


def _get_torch() -> TorchVisionEngine:
    global _torch
    if _torch is None:
        with _torch_lock:
            if _torch is None:
                _torch = TorchVisionEngine(CV_MODEL_DIR)
    return _torch


def select_engine() -> VisionEngine:
    mode = _engine_mode()
    if mode == "stub":
        return _stub
    if mode == "torch":
        eng = _get_torch()
        if not eng.available():
            raise VisionUnavailableError(
                f"CV_ENGINE=torch but the real engine is unavailable "
                f"(deps/model files missing in {CV_MODEL_DIR})"
            )
        return eng
    # auto: prefer the real engine when fully available, else the stub.
    eng = _get_torch()
    return eng if eng.available() else _stub


def cv_models_ready() -> bool:
    """True only when the REAL engine (deps + local model files) is ready."""
    return _get_torch().available()


def active_engine_name() -> str:
    try:
        return select_engine().name
    except VisionUnavailableError:
        return "torch"  # configured but unavailable


def cv_engine_info() -> dict:
    torch_engine = _get_torch()
    return {
        "configured": _engine_mode(),
        "active": active_engine_name(),
        "models_ready": torch_engine.available(),
        "recognition_ready": torch_engine.recognition_present(),
        "model_dir_present": CV_MODEL_DIR.exists(),
        "sequence_length": SEQUENCE_LENGTH,
        "model_version": CV_MODEL_VERSION,
    }


def reset_session(tenant_id: str, session_id: str) -> int:
    """Clear all per-track engagement buffers for one (tenant, session)."""
    return _buffer_store.clear_session(tenant_id, session_id)


def buffer_store() -> SequenceBufferStore:
    """Exposed for tests."""
    return _buffer_store


def _recognition_status(confidence: float, threshold: float) -> str:
    if confidence >= threshold:
        return "candidate"
    if confidence <= 0.0:
        return "unknown"
    return "low_confidence"


def analyze(req: VisionAnalyzeRequest, tenant_id: str) -> VisionAnalyzeResponse:
    """Run one frame through the active engine. Raises InvalidImageError (->400)
    or VisionUnavailableError (->503); never returns a fabricated result."""
    image, raw = decode_image(req.image_base64)  # raises InvalidImageError on bad input
    engine = select_engine()
    threshold = _threshold(req.recognition_threshold)

    raw_faces = engine.analyze_frame(image, raw, threshold)
    results = []
    for idx, face in enumerate(raw_faces):
        track_id = face.track_key or f"anon-{idx}"

        engagement_label = "NotReady"
        engagement_conf = 0.0
        frames = 0
        if req.want_engagement:
            frames = _buffer_store.add_frame(tenant_id, req.session_id, track_id, face.engagement_token)
            seq = _buffer_store.take_sequence(tenant_id, req.session_id, track_id)
            if seq is not None:
                engagement_label, engagement_conf = engine.predict_engagement(seq)
                frames = SEQUENCE_LENGTH

        results.append(
            VisionFaceResult(
                track_id=track_id,
                bbox=[int(v) for v in face.bbox],
                external_label=face.external_label,
                external_label_id=face.external_label_id,
                recognition_confidence=float(face.recognition_confidence),
                recognition_status=_recognition_status(float(face.recognition_confidence), threshold),
                emotion=face.emotion,
                emotion_confidence=float(face.emotion_confidence),
                engagement=engagement_label,
                engagement_confidence=float(engagement_conf),
                engagement_frames=int(frames),
                engagement_frames_required=SEQUENCE_LENGTH,
                quality_flags=list(face.quality_flags),
            )
        )

    return VisionAnalyzeResponse(
        correlation_id=req.correlation_id,
        session_id=req.session_id,
        faces_detected=len(results),
        results=results,
        engine=engine.name,
        degraded=(engine.name != "torch"),
        model_version=getattr(engine, "model_version", CV_MODEL_VERSION),
        model_versions=engine.model_versions(),
        sequence_length=SEQUENCE_LENGTH,
        quality_flags=(["degraded_stub_engine"] if engine.name != "torch" else []),
        generated_at=datetime.now(timezone.utc).isoformat(),
    )
