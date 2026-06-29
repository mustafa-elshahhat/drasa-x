"""
Phase 15 — CV preprocessing + deterministic-stub unit tests.
No network, no heavy CV deps, no model files.
"""
import base64
import io

import pytest

from app.vision.preprocess import (
    InvalidImageError,
    decode_image,
    decode_image_bytes,
    image_signature,
)
from app.vision.engine import StubVisionEngine


def _png_b64(color=(120, 30, 200), size=(64, 64)) -> str:
    from PIL import Image

    buf = io.BytesIO()
    Image.new("RGB", size, color).save(buf, format="PNG")
    return base64.b64encode(buf.getvalue()).decode("ascii")


def test_decode_valid_png():
    img, raw = decode_image(_png_b64())
    assert img.mode == "RGB"
    assert img.width == 64 and img.height == 64
    assert isinstance(raw, bytes) and len(raw) > 0


def test_decode_accepts_data_url_prefix():
    b64 = _png_b64()
    img, _ = decode_image("data:image/png;base64," + b64)
    assert img.width == 64


def test_decode_rejects_non_base64():
    with pytest.raises(InvalidImageError):
        decode_image("@@@not base64@@@")


def test_decode_rejects_non_image_base64():
    junk = base64.b64encode(b"this is not an image").decode("ascii")
    with pytest.raises(InvalidImageError):
        decode_image(junk)


def test_decode_rejects_empty():
    with pytest.raises(InvalidImageError):
        decode_image("")


def test_signature_is_stable_and_content_addressed():
    raw1 = decode_image_bytes(_png_b64(color=(1, 2, 3)))
    raw2 = decode_image_bytes(_png_b64(color=(1, 2, 3)))
    raw3 = decode_image_bytes(_png_b64(color=(9, 9, 9)))
    assert image_signature(raw1) == image_signature(raw2)
    assert image_signature(raw1) != image_signature(raw3)


def test_stub_engine_is_deterministic():
    eng = StubVisionEngine()
    img, raw = decode_image(_png_b64(color=(50, 60, 70)))
    a = eng.analyze_frame(img, raw, threshold=0.5)
    b = eng.analyze_frame(img, raw, threshold=0.5)
    assert len(a) == len(b) and len(a) >= 1
    for fa, fb in zip(a, b):
        assert fa.track_key == fb.track_key
        assert fa.external_label == "Unknown"          # never invents a student name
        assert fa.external_label_id and fa.external_label_id.startswith("ext-")
        assert fa.emotion == fb.emotion
        assert 0.0 <= fa.recognition_confidence <= 1.0
        assert 0.0 <= fa.emotion_confidence <= 1.0


def test_stub_engine_engagement_label_is_bounded():
    eng = StubVisionEngine()
    label, conf = eng.predict_engagement([0.9] * 16)
    assert label in ("Engaged", "Disengaged")
    assert 0.5 <= conf <= 1.0
