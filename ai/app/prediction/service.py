"""
Performance-prediction inference (Phase 6 §12) — inference ONLY.

The AI service loads the approved model from a CONFIGURED directory and executes
it. It never downloads or replaces a model during a request, never fabricates a
fallback result, and never persists predictions. Missing/invalid model files
produce an explicit failure (and affect readiness, not liveness).

Feature schema v1 (raw features the backend derives from authoritative records):
  age (years, int)              study_hours (hours/week, float)
  attendance_percentage (0-100) gender (male|female)
  school_type (public|private)  internet_access (yes|no)
  travel_time (<15 min|15-30 min|30-60 min|>60 min)
  extra_activities (yes|no)
  study_method (textbook|notes|online videos|group study|mixed)
These are one-hot encoded to the model's 16 trained columns.
"""
from __future__ import annotations

import hashlib
import os
import threading
from pathlib import Path
from typing import Any, Dict, List, Optional

FEATURE_SCHEMA_VERSION = os.environ.get("FEATURE_SCHEMA_VERSION", "perf-v1")
MODEL_NAME = os.environ.get("AI_PREDICTION_MODEL_NAME", "rf-performance")
MODEL_VERSION = os.environ.get("AI_PREDICTION_MODEL_VERSION", "rf-2026.06")

_MODEL_DIR = Path(os.environ.get("MODEL_DIR", str(Path(__file__).resolve().parent.parent / "models")))
_REG_FILE = os.environ.get("MODEL_REGRESSOR_FILENAME", "rf_regressor.pkl")
_CLS_FILE = os.environ.get("MODEL_CLASSIFIER_FILENAME", "rf_classifier.pkl")

# Risk/level labels for the classifier's 0/1/2 output (documented in the model card).
_LEVEL_LABELS = {0: "Weak", 1: "Medium", 2: "Strong"}

_lock = threading.Lock()
_reg_model = None
_cls_model = None


class ModelUnavailableError(RuntimeError):
    """Raised when the configured model files are missing/invalid — no fallback."""


def model_files_present() -> bool:
    return (_MODEL_DIR / _REG_FILE).exists() and (_MODEL_DIR / _CLS_FILE).exists()


def _verify_checksum(path: Path, env_key: str) -> None:
    expected = os.environ.get(env_key)
    if not expected:
        return  # checksum verification is optional/configuration-driven
    digest = hashlib.sha256(path.read_bytes()).hexdigest()
    if digest.lower() != expected.lower():
        raise ModelUnavailableError(f"model checksum mismatch for {path.name}")


def load_models() -> None:
    """Load both models from the configured directory. Explicit failure if absent.

    NEVER downloads or replaces a model. Optional integrity check via
    MODEL_REGRESSOR_SHA256 / MODEL_CLASSIFIER_SHA256.
    """
    global _reg_model, _cls_model
    if _reg_model is not None and _cls_model is not None:
        return
    with _lock:
        if _reg_model is not None and _cls_model is not None:
            return
        reg_path = _MODEL_DIR / _REG_FILE
        cls_path = _MODEL_DIR / _CLS_FILE
        if not reg_path.exists() or not cls_path.exists():
            raise ModelUnavailableError(
                f"prediction model files not found in {_MODEL_DIR} "
                f"({_REG_FILE}, {_CLS_FILE}); configure MODEL_DIR"
            )
        _verify_checksum(reg_path, "MODEL_REGRESSOR_SHA256")
        _verify_checksum(cls_path, "MODEL_CLASSIFIER_SHA256")
        import joblib

        _reg_model = joblib.load(reg_path)
        _cls_model = joblib.load(cls_path)


def _preprocess(features: Dict[str, Any]):
    import pandas as pd

    df = pd.get_dummies(pd.DataFrame([features]))
    df_reg = df.reindex(columns=_reg_model.feature_names_in_, fill_value=0)
    df_cls = df.reindex(columns=_cls_model.feature_names_in_, fill_value=0)
    return df_reg, df_cls


def predict_with_meta(features: Dict[str, Any]) -> Dict[str, Any]:
    """Execute the model and return score, level, genuine confidence, and factors.

    Confidence is the classifier's max ``predict_proba`` for the chosen class —
    a real model-produced probability (never invented).
    """
    load_models()
    df_reg, df_cls = _preprocess(features)

    score = float(_reg_model.predict(df_reg)[0])
    level_index = int(_cls_model.predict(df_cls)[0])

    confidence: Optional[float] = None
    if hasattr(_cls_model, "predict_proba"):
        proba = _cls_model.predict_proba(df_cls)[0]
        confidence = float(max(proba))  # genuine probability for the predicted class

    factors = _top_factors(df_cls, top_k=5)

    return {
        "score": round(score, 2),
        "level_index": level_index,
        "level_label": _LEVEL_LABELS.get(level_index, "Unknown"),
        "confidence": None if confidence is None else round(confidence, 4),
        "factors": factors,
        "model_name": MODEL_NAME,
        "model_version": MODEL_VERSION,
        "feature_schema_version": FEATURE_SCHEMA_VERSION,
    }


def _top_factors(df_cls, top_k: int) -> List[Dict[str, Any]]:
    """Top global feature-importance factors (NOT causal, NOT per-student SHAP)."""
    importances = getattr(_cls_model, "feature_importances_", None)
    names = getattr(_cls_model, "feature_names_in_", None)
    if importances is None or names is None:
        return []
    ranked = sorted(zip(list(names), [float(x) for x in importances]), key=lambda t: t[1], reverse=True)
    return [{"feature": n, "importance": round(w, 4), "kind": "global_importance"} for n, w in ranked[:top_k]]
