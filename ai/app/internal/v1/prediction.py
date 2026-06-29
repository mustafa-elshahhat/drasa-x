"""
Performance-prediction internal operation (Phase 6 §12) — inference only.

Pure and injectable: the model is accessed through a ``Predictor`` port so the
contract (schema-version checks, output shape, no-fabrication) is deterministically
testable without loading the real 130 MB model. No persistence, no fallback.
"""
from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Callable, Dict, Protocol

from app.internal.v1.schemas import (
    PredictionFactor,
    PredictionRequest,
    PredictionResponse,
)
from app.rag.tenant import validate_tenant_id

_RISK_BAND = {"Weak": "high", "Medium": "medium", "Strong": "low"}
_LIMITATIONS = [
    "Advisory only — requires human review before any action.",
    "Factors are global model feature importances, not per-student causal explanations.",
    "Not a clinical, psychological, or diagnostic assessment.",
    "Confidence is the model's class probability where available; absent otherwise.",
]


class PredictionError(RuntimeError):
    def __init__(self, category: str, message: str):
        super().__init__(message)
        self.category = category


class Predictor(Protocol):
    @property
    def model_version(self) -> str: ...
    @property
    def feature_schema_version(self) -> str: ...
    @property
    def model_name(self) -> str: ...
    def available(self) -> bool: ...
    def predict(self, features: Dict[str, Any]) -> Dict[str, Any]: ...


class SklearnPredictor:
    """Real predictor backed by the configured scikit-learn models."""

    def __init__(self) -> None:
        from app.prediction import service
        self._service = service

    @property
    def model_version(self) -> str:
        return self._service.MODEL_VERSION

    @property
    def feature_schema_version(self) -> str:
        return self._service.FEATURE_SCHEMA_VERSION

    @property
    def model_name(self) -> str:
        return self._service.MODEL_NAME

    def available(self) -> bool:
        return self._service.model_files_present()

    def predict(self, features: Dict[str, Any]) -> Dict[str, Any]:
        from app.prediction.service import ModelUnavailableError, predict_with_meta
        try:
            return predict_with_meta(features)
        except ModelUnavailableError as exc:
            raise PredictionError("model_unavailable", str(exc)) from exc
        except Exception as exc:  # inference failure
            raise PredictionError("inference_failed", "model inference failed") from exc


def run_prediction(
    req: PredictionRequest,
    tenant_id: str,
    *,
    predictor: Predictor,
    clock: Callable[[], datetime] = lambda: datetime.now(timezone.utc),
) -> PredictionResponse:
    validate_tenant_id(tenant_id)

    if req.feature_schema_version != predictor.feature_schema_version:
        raise PredictionError("unsupported_schema_version",
                              f"unsupported feature_schema_version {req.feature_schema_version!r}")
    if req.model_version and req.model_version != predictor.model_version:
        raise PredictionError("unsupported_model_version",
                              f"unsupported model_version {req.model_version!r}")
    if not predictor.available():
        raise PredictionError("model_unavailable", "prediction model is not available")

    meta = predictor.predict(req.features.model_dump())

    level = str(meta.get("level_label", "Unknown"))
    factors = [PredictionFactor(**f) for f in meta.get("factors", []) if isinstance(f, dict)]
    return PredictionResponse(
        student_ref=req.student_ref,
        prediction_type=req.prediction_type,
        score=float(meta["score"]),
        level=level,
        risk_band=_RISK_BAND.get(level, "unknown"),
        confidence=meta.get("confidence"),
        factors=factors,
        model_name=str(meta.get("model_name", predictor.model_name)),
        model_version=str(meta.get("model_version", predictor.model_version)),
        feature_schema_version=str(meta.get("feature_schema_version", predictor.feature_schema_version)),
        data_range_from=req.data_range_from,
        data_range_to=req.data_range_to,
        generated_at=clock().isoformat(),
        limitations=list(_LIMITATIONS),
        correlation_id=req.correlation_id,
    )
