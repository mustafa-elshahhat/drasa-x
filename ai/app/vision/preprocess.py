"""
CV preprocessing utilities (Phase 15).

Adapts the transform constants from the reference repo's
``utils/{recognition,emotion,engagement}_preprocess.py`` (ImageNet mean/std,
160x160 recognition crop, 224x224 emotion/engagement crop). Pure pillow+numpy
here; torch/torchvision transforms are built lazily inside the real engine so
this module imports without the heavy CV stack.
"""
from __future__ import annotations

import base64
import binascii
import hashlib
import io
from typing import Tuple

# Reused from the reference preprocessing (ImageNet normalization).
IMAGENET_MEAN = (0.485, 0.456, 0.406)
IMAGENET_STD = (0.229, 0.224, 0.225)
RECOGNITION_SIZE = (160, 160)   # FaceNet input (reference detector.py)
EMOTION_SIZE = (224, 224)       # ResNet50 emotion input
ENGAGEMENT_SIZE = (224, 224)    # ResNet50+BiLSTM per-frame input

# ~9 MB of raw image once base64-decoded; a defensive upper bound for a single
# classroom frame. Larger uploads are rejected before any model runs.
MAX_IMAGE_B64_CHARS = 12_000_000


class InvalidImageError(ValueError):
    """Raised when the uploaded payload is not a decodable image."""


def _strip_data_url(image_base64: str) -> str:
    """Accept either a bare base64 string or a ``data:image/...;base64,...`` URL."""
    s = image_base64.strip()
    if s.startswith("data:") and "," in s:
        return s.split(",", 1)[1]
    return s


def decode_image_bytes(image_base64: str) -> bytes:
    """Decode the base64 payload to raw bytes. Raises InvalidImageError."""
    if not image_base64 or not image_base64.strip():
        raise InvalidImageError("empty image payload")
    if len(image_base64) > MAX_IMAGE_B64_CHARS:
        raise InvalidImageError("image payload too large")
    try:
        return base64.b64decode(_strip_data_url(image_base64), validate=True)
    except (binascii.Error, ValueError) as exc:
        raise InvalidImageError("payload is not valid base64") from exc


def decode_image(image_base64: str):
    """
    Decode + validate the image. Returns ``(PIL.Image RGB, raw_bytes)``.

    PIL is imported lazily so the module loads even in a minimal env; pillow is
    a transitive dependency of the existing stack and present in the venv.
    """
    raw = decode_image_bytes(image_base64)
    try:
        from PIL import Image  # lazy: pillow is present transitively
    except Exception as exc:  # pragma: no cover - pillow always present here
        raise InvalidImageError("image backend unavailable") from exc
    try:
        img = Image.open(io.BytesIO(raw))
        img.verify()  # validate without fully decoding
        img = Image.open(io.BytesIO(raw)).convert("RGB")
    except Exception as exc:
        raise InvalidImageError("payload is not a decodable image") from exc
    return img, raw


def image_signature(raw_bytes: bytes) -> str:
    """Stable, non-reversible content hash. Drives the deterministic stub engine
    and is safe to log/return (never the image itself)."""
    return hashlib.sha256(raw_bytes).hexdigest()


def image_dimensions(img) -> Tuple[int, int]:
    return (int(img.width), int(img.height))
