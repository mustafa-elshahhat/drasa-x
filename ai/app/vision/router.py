"""
Internal CV router (Phase 15). Mounted under ``/internal/v1/vision`` and
protected by the SAME service-token dependency as the rest of the internal
surface (scope ``ai:vision``, tenant taken from the signed token — never the
body). The browser cannot reach this surface; only DerasaX-backend can.

There is NO unauthenticated public CV endpoint (a key requirement that fixes the
reference repo's open ``POST /analyze/``).
"""
from __future__ import annotations

from fastapi import APIRouter, Depends, HTTPException, status

from app.observability import telemetry
from app.security.service_auth import ServiceContext, require_service_token
from app.vision import service as vision_service
from app.vision.preprocess import InvalidImageError
from app.vision.schemas import (
    VisionAnalyzeRequest,
    VisionAnalyzeResponse,
    VisionEndSessionRequest,
    VisionEndSessionResponse,
)

vision_router = APIRouter(prefix="/internal/v1/vision", tags=["internal-v1-vision"])


def get_vision_service():
    """Indirection so contract tests can override the service with a fake."""
    return vision_service


@vision_router.post("/analyze", response_model=VisionAnalyzeResponse)
def analyze_frame(
    req: VisionAnalyzeRequest,
    ctx: ServiceContext = Depends(require_service_token),
    svc=Depends(get_vision_service),
) -> VisionAnalyzeResponse:
    ctx.require_scope("ai:vision")
    tenant_id = ctx.require_tenant()
    try:
        resp = svc.analyze(req, tenant_id)
    except InvalidImageError as exc:
        telemetry.record_ai_event("vision", status="failure", tenant_id=tenant_id, error_category="invalid_image")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "invalid_image", "message": str(exc)},
        ) from exc
    except vision_service.VisionUnavailableError as exc:
        telemetry.record_ai_event("vision", status="failure", tenant_id=tenant_id, error_category="model_unavailable")
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail={"code": "model_unavailable", "message": "CV models are not available"},
        ) from exc
    except HTTPException:
        raise
    except Exception as exc:  # inference failure — never fabricate a result
        telemetry.record_ai_event("vision", status="failure", tenant_id=tenant_id, error_category="inference_failed")
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail={"code": "inference_failed", "message": "CV inference failed"},
        ) from exc

    telemetry.record_ai_event(
        "vision",
        status="success",
        tenant_id=tenant_id,
        model_version=resp.model_version,
        retrieval_count=resp.faces_detected,
    )
    return resp


@vision_router.post("/end-session", response_model=VisionEndSessionResponse)
def end_session(
    req: VisionEndSessionRequest,
    ctx: ServiceContext = Depends(require_service_token),
    svc=Depends(get_vision_service),
) -> VisionEndSessionResponse:
    """Clear ephemeral per-track engagement buffers for a finished session."""
    ctx.require_scope("ai:vision")
    tenant_id = ctx.require_tenant()
    cleared = svc.reset_session(tenant_id, req.session_id)
    telemetry.record_ai_event("vision", status="success", tenant_id=tenant_id, error_category=None)
    return VisionEndSessionResponse(session_id=req.session_id, buffers_cleared=cleared)
