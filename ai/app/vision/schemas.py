"""
Strict request/response schemas for the internal CV contract (Phase 15).

Mirrors the conventions of ``app/internal/v1/schemas.py``: ``extra="forbid"``,
bounded fields, and — critically — the tenant is NEVER in the body (it comes
from the signed service token). The response carries only NORMALIZED analysis
results + model/version/engine metadata. It returns NO raw image, NO face crop,
NO embedding, and NO DerasaX student identity (only opaque external labels).
"""
from __future__ import annotations

from typing import Dict, List, Literal, Optional

from pydantic import BaseModel, ConfigDict, Field

from .preprocess import MAX_IMAGE_B64_CHARS
from . import SEQUENCE_LENGTH


class _Strict(BaseModel):
    model_config = ConfigDict(extra="forbid")


RecognitionStatus = Literal["candidate", "low_confidence", "unknown"]
EngagementLabel = Literal["Engaged", "Disengaged", "NotReady"]


class VisionAnalyzeRequest(_Strict):
    """Backend-mediated request to analyze ONE ephemeral classroom frame."""
    correlation_id: str = Field(min_length=1, max_length=128)
    session_id: str = Field(min_length=1, max_length=128)
    # Ephemeral frame, base64 (optionally a data URL). Never persisted by the AI.
    image_base64: str = Field(min_length=1, max_length=MAX_IMAGE_B64_CHARS)
    frame_index: int = Field(default=0, ge=0, le=1_000_000)
    capture_label: Optional[str] = Field(default=None, max_length=64)
    want_engagement: bool = True
    recognition_threshold: Optional[float] = Field(default=None, ge=0.0, le=1.0)


class VisionFaceResult(_Strict):
    track_id: str                                  # temporary, session-scoped
    bbox: List[int] = Field(min_length=4, max_length=4)
    external_label: str                            # "Unknown" or an enrolled label (never invented)
    external_label_id: Optional[str] = None        # OPAQUE — NOT a DerasaX student id
    recognition_confidence: float = Field(ge=0.0, le=1.0)
    recognition_status: RecognitionStatus
    emotion: str
    emotion_confidence: float = Field(ge=0.0, le=1.0)
    engagement: EngagementLabel
    engagement_confidence: float = Field(ge=0.0, le=1.0)
    engagement_frames: int = Field(ge=0)
    engagement_frames_required: int = Field(ge=1)
    quality_flags: List[str] = Field(default_factory=list)


class VisionAnalyzeResponse(_Strict):
    correlation_id: str
    session_id: str
    faces_detected: int = Field(ge=0)
    results: List[VisionFaceResult] = Field(default_factory=list)
    engine: str                                    # "stub" | "torch"
    degraded: bool                                 # True when the stub/limited engine ran
    model_version: str
    model_versions: Dict[str, str] = Field(default_factory=dict)
    sequence_length: int = SEQUENCE_LENGTH
    quality_flags: List[str] = Field(default_factory=list)
    generated_at: str


class VisionEndSessionRequest(_Strict):
    correlation_id: str = Field(min_length=1, max_length=128)
    session_id: str = Field(min_length=1, max_length=128)


class VisionEndSessionResponse(_Strict):
    session_id: str
    buffers_cleared: int = Field(ge=0)
