"""
Internal v1 AI router (Phase 6 §4). Mounted at ``/internal/v1`` and protected by
the service-token dependency. The browser cannot reach this surface: it carries
no valid service token, and the backend is the only token issuer.

The retriever and LLM are FastAPI dependencies so contract tests can override
them with deterministic fakes (``app.dependency_overrides``).
"""
from __future__ import annotations

import os
from pathlib import Path

from fastapi import APIRouter, Depends, HTTPException, status

from app.internal.v1.ingest import (
    IngestError,
    delete_document,
    document_status,
    ingest_document,
)
from app.internal.v1.analysis import AnalysisError, run_analysis
from app.internal.v1.prediction import PredictionError, SklearnPredictor, run_prediction
from app.internal.v1.quiz import QuizDraftError, generate_quiz_draft
from app.internal.v1.schemas import (
    AnalysisRequest,
    AnalysisResponse,
    DeleteDocumentResponse,
    DocumentStatusResponse,
    IngestDocumentRequest,
    IngestDocumentResponse,
    PredictionRequest,
    PredictionResponse,
    QuizDraftRequest,
    QuizDraftResponse,
    TutorRequest,
    TutorResponse,
)
from app.internal.v1.tutor import TutorError, run_tutor
from app.observability import telemetry
from app.prompts.registry import UnknownPromptError
from app.security.service_auth import ServiceContext, require_service_token

router = APIRouter(prefix="/internal/v1", tags=["internal-v1"])

_VECTORSTORE_DIR = Path(
    os.environ.get(
        "VECTORSTORE_DIR",
        str(Path(__file__).resolve().parents[3] / "vectorstore"),
    )
)


def _build_chroma_store():
    """Build the live tenant-scoped Chroma store (read+write). Raises 503 if the
    optional ML/vector deps are unavailable."""
    try:
        from app.rag.indexer import get_embeddings
        from app.rag.store import ChromaTenantStore
    except Exception as exc:  # optional ML deps absent
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail={"code": "rag_unavailable", "message": "retrieval backend is not available"},
        ) from exc
    return ChromaTenantStore(persist_dir=str(_VECTORSTORE_DIR), embeddings=get_embeddings())


def get_tutor_retriever():
    """Tenant-scoped retriever for the tutor (override in tests)."""
    return _build_chroma_store()


def get_document_store():
    """Tenant-scoped ingestion store for the document lifecycle (override in tests)."""
    return _build_chroma_store()


def get_tutor_llm():
    """Return the real provider LLM callable (override in tests)."""
    from app.llm.groq_client import chat_completion

    return chat_completion


def get_quiz_retriever():
    """Tenant-scoped retriever for quiz grounding (override in tests)."""
    return _build_chroma_store()


def get_quiz_llm():
    """Return a JSON-producing LLM callable for quiz drafts (override in tests)."""
    from app.llm.groq_client import chat_completion

    def _json_llm(*, system: str, context: str, instruction: str) -> str:
        return chat_completion(system=system, context=context, question=instruction,
                               temperature=0.0, max_tokens=1500)

    return _json_llm


def get_predictor():
    """Return the configured prediction model wrapper (override in tests)."""
    return SklearnPredictor()


def get_analysis_llm():
    """Return a JSON-producing LLM callable for analysis (override in tests)."""
    from app.llm.groq_client import chat_completion

    def _json_llm(*, system: str, context: str, instruction: str) -> str:
        return chat_completion(system=system, context=context, question=instruction,
                               temperature=0.0, max_tokens=900)

    return _json_llm


@router.post("/tutor", response_model=TutorResponse)
def tutor(
    req: TutorRequest,
    ctx: ServiceContext = Depends(require_service_token),
    retriever=Depends(get_tutor_retriever),
    llm=Depends(get_tutor_llm),
) -> TutorResponse:
    # Tenant is taken from the SIGNED token, never the body.
    ctx.require_scope("ai:tutor")
    tenant_id = ctx.require_tenant()

    try:
        resp = run_tutor(req, tenant_id, retriever=retriever, llm=llm)
    except UnknownPromptError as exc:
        telemetry.record_ai_event("tutor", status="failure", tenant_id=tenant_id, error_category="unknown_prompt_version")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "unknown_prompt_version", "message": str(exc)},
        ) from exc
    except ValueError as exc:  # malformed tenant / invalid scope context
        telemetry.record_ai_event("tutor", status="failure", tenant_id=tenant_id, error_category="invalid_request")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "invalid_request", "message": str(exc)},
        ) from exc
    except TutorError as exc:
        # Provider produced no usable content — surface a stable upstream error,
        # never a fabricated success.
        telemetry.record_ai_event("tutor", status="failure", tenant_id=tenant_id, error_category="provider_no_content")
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail={"code": "provider_no_content", "message": "AI provider returned no content"},
        ) from exc
    except HTTPException:
        raise
    except Exception as exc:  # provider/transport failure — do not leak internals
        telemetry.record_ai_event("tutor", status="failure", tenant_id=tenant_id, error_category="provider_error")
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail={"code": "provider_error", "message": "AI provider request failed"},
        ) from exc

    telemetry.record_ai_event(
        "tutor", status="success", tenant_id=tenant_id, duration_ms=resp.latency_ms,
        prompt_version=resp.prompt_version, model_version=resp.model_version,
        retrieval_count=resp.retrieval_count, citation_count=resp.citation_count,
        no_answer=not resp.grounded,
    )
    return resp


# ---------------------------------------------------------------------------
# Quiz draft generation (§11) — stateless, grounded, DRAFT ONLY.
# Scope "ai:quiz"; tenant from the signed token. No persistence here.
# ---------------------------------------------------------------------------
def _quiz_error_status(category: str) -> int:
    provider_categories = {"invalid_json", "provider_error"}
    return status.HTTP_502_BAD_GATEWAY if category in provider_categories else status.HTTP_422_UNPROCESSABLE_ENTITY


@router.post("/quiz/draft", response_model=QuizDraftResponse)
def quiz_draft(
    req: QuizDraftRequest,
    ctx: ServiceContext = Depends(require_service_token),
    retriever=Depends(get_quiz_retriever),
    llm=Depends(get_quiz_llm),
) -> QuizDraftResponse:
    ctx.require_scope("ai:quiz")
    tenant_id = ctx.require_tenant()
    try:
        resp = generate_quiz_draft(req, tenant_id, retriever=retriever, llm_json=llm)
    except QuizDraftError as exc:
        schema_fail = exc.category in {
            "invalid_json", "unsupported_type", "invalid_question", "invalid_options",
            "duplicate_options", "missing_correct", "invalid_points", "duplicate_questions",
            "count_out_of_bounds",
        }
        telemetry.record_ai_event("quiz", status="failure", tenant_id=tenant_id,
                                  error_category=exc.category, schema_validation_failed=schema_fail or None)
        raise HTTPException(
            status_code=_quiz_error_status(exc.category),
            detail={"code": exc.category, "message": str(exc)},
        ) from exc
    except ValueError as exc:
        telemetry.record_ai_event("quiz", status="failure", tenant_id=tenant_id, error_category="invalid_request")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "invalid_request", "message": str(exc)},
        ) from exc
    except HTTPException:
        raise
    except Exception as exc:  # provider/transport failure
        telemetry.record_ai_event("quiz", status="failure", tenant_id=tenant_id, error_category="provider_error")
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail={"code": "provider_error", "message": "AI provider request failed"},
        ) from exc

    telemetry.record_ai_event(
        "quiz", status="success", tenant_id=tenant_id, prompt_version=resp.prompt_version,
        model_version=resp.model_version, retrieval_count=resp.retrieval_count,
        citation_count=len(resp.citations),
    )
    return resp


# ---------------------------------------------------------------------------
# Conversation / pain-point analysis (§13) — stateless, non-diagnostic.
# Scope "ai:analyze"; tenant from token. No persistence here.
# ---------------------------------------------------------------------------
@router.post("/analysis", response_model=AnalysisResponse)
def analysis(
    req: AnalysisRequest,
    ctx: ServiceContext = Depends(require_service_token),
    llm=Depends(get_analysis_llm),
) -> AnalysisResponse:
    ctx.require_scope("ai:analyze")
    tenant_id = ctx.require_tenant()
    try:
        resp = run_analysis(req, tenant_id, llm_json=llm)
    except AnalysisError as exc:
        code = exc.category
        schema_fail = code in {"invalid_json", "non_diagnostic_violation"}
        telemetry.record_ai_event("analysis", status="failure", tenant_id=tenant_id,
                                  error_category=code, schema_validation_failed=schema_fail or None)
        st = status.HTTP_502_BAD_GATEWAY if code in {"invalid_json", "provider_error"} else status.HTTP_422_UNPROCESSABLE_ENTITY
        raise HTTPException(status_code=st, detail={"code": code, "message": str(exc)}) from exc
    except ValueError as exc:
        telemetry.record_ai_event("analysis", status="failure", tenant_id=tenant_id, error_category="invalid_request")
        raise HTTPException(status_code=status.HTTP_400_BAD_REQUEST,
                            detail={"code": "invalid_request", "message": str(exc)}) from exc

    telemetry.record_ai_event(
        "analysis", status="success", tenant_id=tenant_id, prompt_version=resp.prompt_version,
        model_version=resp.model_version, no_answer=(resp.pain_point_category == "none"),
    )
    return resp


# ---------------------------------------------------------------------------
# Performance prediction (§12) — inference only; scope "ai:prediction".
# ---------------------------------------------------------------------------
def _prediction_error_status(category: str) -> int:
    return {
        "model_unavailable": status.HTTP_503_SERVICE_UNAVAILABLE,
        "inference_failed": status.HTTP_502_BAD_GATEWAY,
    }.get(category, status.HTTP_400_BAD_REQUEST)


@router.post("/prediction", response_model=PredictionResponse)
def prediction(
    req: PredictionRequest,
    ctx: ServiceContext = Depends(require_service_token),
    predictor=Depends(get_predictor),
) -> PredictionResponse:
    ctx.require_scope("ai:prediction")
    tenant_id = ctx.require_tenant()
    try:
        resp = run_prediction(req, tenant_id, predictor=predictor)
    except PredictionError as exc:
        telemetry.record_ai_event("prediction", status="failure", tenant_id=tenant_id, error_category=exc.category)
        raise HTTPException(
            status_code=_prediction_error_status(exc.category),
            detail={"code": exc.category, "message": str(exc)},
        ) from exc
    except ValueError as exc:
        telemetry.record_ai_event("prediction", status="failure", tenant_id=tenant_id, error_category="invalid_request")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "invalid_request", "message": str(exc)},
        ) from exc

    telemetry.record_ai_event(
        "prediction", status="success", tenant_id=tenant_id,
        model_version=resp.model_version,
    )
    return resp


# ---------------------------------------------------------------------------
# Document lifecycle (§7) — ingestion / re-index / delete / status.
# All tenant-scoped from the signed token; scope "ai:ingest".
# ---------------------------------------------------------------------------
def _ingest_error_status(category: str) -> int:
    return status.HTTP_502_BAD_GATEWAY if category == "embedding_failed" else status.HTTP_400_BAD_REQUEST


def _run_ingest(req: IngestDocumentRequest, tenant_id: str, store, *, force: bool) -> IngestDocumentResponse:
    try:
        result = ingest_document(req, tenant_id, store=store, force=force)
    except IngestError as exc:
        telemetry.metrics.inc("ingestion_failures.total")
        telemetry.record_ai_event("ingest", status="failure", tenant_id=tenant_id, error_category=exc.category)
        raise HTTPException(
            status_code=_ingest_error_status(exc.category),
            detail={"code": exc.category, "message": str(exc)},
        ) from exc
    except ValueError as exc:
        telemetry.metrics.inc("ingestion_failures.total")
        telemetry.record_ai_event("ingest", status="failure", tenant_id=tenant_id, error_category="invalid_request")
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "invalid_request", "message": str(exc)},
        ) from exc
    telemetry.record_ai_event("ingest", status="success", tenant_id=tenant_id)
    return IngestDocumentResponse(
        document_id=result.document_id, version=result.version, status=result.status,
        chunk_count=result.chunk_count, removed_chunks=result.removed_chunks,
        checksum=result.checksum, language=result.language, collection=result.collection,
        indexed_at=result.indexed_at, correlation_id=req.correlation_id,
    )


@router.post("/documents", response_model=IngestDocumentResponse)
def ingest(
    req: IngestDocumentRequest,
    ctx: ServiceContext = Depends(require_service_token),
    store=Depends(get_document_store),
) -> IngestDocumentResponse:
    ctx.require_scope("ai:ingest")
    tenant_id = ctx.require_tenant()
    return _run_ingest(req, tenant_id, store, force=False)


@router.post("/documents/{document_id}/reindex", response_model=IngestDocumentResponse)
def reindex(
    document_id: str,
    req: IngestDocumentRequest,
    ctx: ServiceContext = Depends(require_service_token),
    store=Depends(get_document_store),
) -> IngestDocumentResponse:
    ctx.require_scope("ai:ingest")
    tenant_id = ctx.require_tenant()
    if req.document_id != document_id:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail={"code": "invalid_request", "message": "document_id mismatch"},
        )
    return _run_ingest(req, tenant_id, store, force=True)


@router.delete("/documents/{document_id}", response_model=DeleteDocumentResponse)
def delete(
    document_id: str,
    ctx: ServiceContext = Depends(require_service_token),
    store=Depends(get_document_store),
) -> DeleteDocumentResponse:
    ctx.require_scope("ai:ingest")
    tenant_id = ctx.require_tenant()
    removed = delete_document(tenant_id, document_id, store=store)
    return DeleteDocumentResponse(
        document_id=document_id, deleted_chunks=removed,
        status="deleted" if removed > 0 else "not_found",
    )


@router.get("/documents/{document_id}/status", response_model=DocumentStatusResponse)
def status_endpoint(
    document_id: str,
    ctx: ServiceContext = Depends(require_service_token),
    store=Depends(get_document_store),
) -> DocumentStatusResponse:
    ctx.require_scope("ai:ingest")
    tenant_id = ctx.require_tenant()
    meta = document_status(tenant_id, document_id, store=store)
    if meta is None:
        return DocumentStatusResponse(document_id=document_id, indexed=False)
    return DocumentStatusResponse(
        document_id=document_id, indexed=True, version=meta.version, chunk_count=meta.chunk_count,
    )


# ---------------------------------------------------------------------------
# Service-local metrics snapshot (§17). Authenticated (any valid service token);
# returns only non-sensitive aggregate counters/latency — no payloads, no ids.
# Centralized monitoring is Phase 19; this makes the emitted telemetry usable now.
# ---------------------------------------------------------------------------
@router.get("/metrics")
def metrics_snapshot(ctx: ServiceContext = Depends(require_service_token)) -> dict:
    return telemetry.metrics.snapshot()
