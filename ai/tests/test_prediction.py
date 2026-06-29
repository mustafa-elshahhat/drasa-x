"""
Prediction service tests (Phase 6 §12) — deterministic, no real model load.

Drives ``run_prediction`` with a fake Predictor to prove schema-version checks,
output shape, genuine-confidence passthrough, explicit failures (no fabricated
fallback), and tenant requirement. Also asserts the strict feature schema.
"""
import math

import pytest
from pydantic import ValidationError

from app.internal.v1.prediction import PredictionError, run_prediction
from app.internal.v1.schemas import PredictionFeatures, PredictionRequest

VALID_FEATURES = dict(
    age=15, study_hours=10.0, attendance_percentage=85.0, gender="male",
    school_type="public", internet_access="yes", travel_time="<15 min",
    extra_activities="no", study_method="textbook",
)


class FakePredictor:
    def __init__(self, available=True, confidence=0.82, raise_on_predict=None):
        self._available = available
        self._confidence = confidence
        self._raise = raise_on_predict

    model_version = "rf-2026.06"
    feature_schema_version = "perf-v1"
    model_name = "rf-performance"

    def available(self):
        return self._available

    def predict(self, features):
        if self._raise:
            raise self._raise
        return {
            "score": 78.5, "level_index": 1, "level_label": "Medium",
            "confidence": self._confidence,
            "factors": [{"feature": "attendance_percentage", "importance": 0.4, "kind": "global_importance"}],
            "model_name": self.model_name, "model_version": self.model_version,
            "feature_schema_version": self.feature_schema_version,
        }


def _req(**over):
    base = dict(correlation_id="c1", student_ref="stu-1", prediction_type="performance",
                feature_schema_version="perf-v1", features=VALID_FEATURES)
    base.update(over)
    return PredictionRequest(**base)


def test_valid_prediction():
    resp = run_prediction(_req(), "tenant-1", predictor=FakePredictor())
    assert resp.score == 78.5
    assert resp.level == "Medium"
    assert resp.risk_band == "medium"
    assert resp.confidence == 0.82
    assert resp.model_version == "rf-2026.06"
    assert resp.feature_schema_version == "perf-v1"
    assert resp.limitations and any("human review" in l.lower() for l in resp.limitations)
    assert resp.factors[0].kind == "global_importance"


def test_unsupported_schema_version_rejected():
    with pytest.raises(PredictionError) as ei:
        run_prediction(_req(feature_schema_version="perf-v999"), "tenant-1", predictor=FakePredictor())
    assert ei.value.category == "unsupported_schema_version"


def test_unsupported_model_version_rejected():
    with pytest.raises(PredictionError) as ei:
        run_prediction(_req(model_version="rf-9999"), "tenant-1", predictor=FakePredictor())
    assert ei.value.category == "unsupported_model_version"


def test_model_unavailable_is_explicit_failure():
    with pytest.raises(PredictionError) as ei:
        run_prediction(_req(), "tenant-1", predictor=FakePredictor(available=False))
    assert ei.value.category == "model_unavailable"


def test_inference_failure_propagates_no_fallback():
    pred = FakePredictor(raise_on_predict=PredictionError("inference_failed", "boom"))
    with pytest.raises(PredictionError) as ei:
        run_prediction(_req(), "tenant-1", predictor=pred)
    assert ei.value.category == "inference_failed"


def test_confidence_none_is_not_invented():
    resp = run_prediction(_req(), "tenant-1", predictor=FakePredictor(confidence=None))
    assert resp.confidence is None  # never fabricated


def test_missing_tenant_rejected():
    with pytest.raises(ValueError):
        run_prediction(_req(), "", predictor=FakePredictor())


# ---- strict feature schema -------------------------------------------------

def test_feature_schema_rejects_out_of_range_age():
    with pytest.raises(ValidationError):
        PredictionFeatures(**{**VALID_FEATURES, "age": 200})


def test_feature_schema_rejects_nan_and_inf():
    with pytest.raises(ValidationError):
        PredictionFeatures(**{**VALID_FEATURES, "attendance_percentage": math.nan})
    with pytest.raises(ValidationError):
        PredictionFeatures(**{**VALID_FEATURES, "study_hours": math.inf})


def test_feature_schema_rejects_unknown_category():
    with pytest.raises(ValidationError):
        PredictionFeatures(**{**VALID_FEATURES, "gender": "robot"})


def test_request_rejects_unknown_field():
    with pytest.raises(ValidationError):
        _req(injected="x")
