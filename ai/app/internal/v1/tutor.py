"""
Backend-mediated tutor service (Phase 6 §9) — the core of the tutor vertical
slice. Pure-Python and fully injectable: the retriever and the LLM call are
ports, so tenant scoping, the citation contract, and no-answer behavior are
deterministically testable without the ML / provider stack.

Safety (§10): retrieved curriculum is passed to the model as clearly labelled
*untrusted* CONTEXT, separate from the system instruction, and the system
instruction forbids treating context as commands or inventing citations.
"""
from __future__ import annotations

import os
import time
from dataclasses import dataclass
from typing import Callable, List, Optional

from app.internal.v1.schemas import (
    MAX_HISTORY_CHARS,
    MAX_HISTORY_TURNS,
    Citation,
    TutorRequest,
    TutorResponse,
)
from app.prompts.registry import default_prompt_id, get_prompt
from app.rag.tenant import RetrievedChunk, Retriever, validate_tenant_id

# Safe, fixed no-answer messages (never present unsupported content as fact).
_NO_ANSWER = {
    "en": "I don't have enough material to answer that. Please ask about your study material.",
    "ar": "لا تتوفر لديّ مادة كافية للإجابة عن ذلك. من فضلك اسأل عن مادتك الدراسية.",
}

# The LLM port: (system, context, question, model) -> answer text.
LlmFn = Callable[..., str]


@dataclass(frozen=True)
class TutorConfig:
    provider: str = "groq"
    model: str = "llama-3.1-8b-instant"
    model_version: str = "llama-3.1-8b-instant"
    min_score: float = 0.20          # normalized-relevance no-answer threshold
    snippet_max_chars: int = 320
    max_context_chars: int = 6000

    @classmethod
    def from_env(cls) -> "TutorConfig":
        model = os.environ.get("AI_TUTOR_MODEL", cls.model)
        return cls(
            provider=os.environ.get("AI_PROVIDER", cls.provider),
            model=model,
            model_version=os.environ.get("AI_TUTOR_MODEL_VERSION", model),
            min_score=float(os.environ.get("RAG_MIN_SCORE", cls.min_score)),
            snippet_max_chars=int(os.environ.get("RAG_SNIPPET_MAX_CHARS", cls.snippet_max_chars)),
            max_context_chars=int(os.environ.get("RAG_MAX_CONTEXT_CHARS", cls.max_context_chars)),
        )


class TutorError(RuntimeError):
    """Raised when the provider returns no usable content (never faked)."""


def _render_history(history, limit_chars: int) -> str:
    turns = history[-MAX_HISTORY_TURNS:]
    lines: List[str] = []
    for t in turns:
        lines.append(f"{t.role.upper()}: {t.content.strip()}")
    text = "\n".join(lines)
    return text[-limit_chars:] if len(text) > limit_chars else text


def _format_context(chunks: List[RetrievedChunk], cfg: TutorConfig) -> str:
    """Build labelled, bounded, path-free context from grounded chunks only."""
    blocks: List[str] = []
    total = 0
    for i, c in enumerate(chunks, start=1):
        snippet = (c.snippet or "").strip()[: cfg.snippet_max_chars]
        label = c.title or c.document_id or "source"
        block = f"[Source {i}: {label}]\n{snippet}"
        if total + len(block) > cfg.max_context_chars:
            break
        blocks.append(block)
        total += len(block)
    return "\n\n---\n\n".join(blocks)


def _build_citations(chunks: List[RetrievedChunk], cfg: TutorConfig) -> List[Citation]:
    """Deduplicate by source document (keep best-scoring chunk); never invent."""
    best: dict[str, RetrievedChunk] = {}
    for c in chunks:
        key = c.document_id or c.chunk_id
        if key not in best or c.score > best[key].score:
            best[key] = c
    ordered = sorted(best.values(), key=lambda c: c.score, reverse=True)
    return [
        Citation(
            source_document_id=c.document_id or c.chunk_id,
            chunk_id=c.chunk_id,
            score=round(c.score, 4),
            title=c.title,
            grade=c.grade,
            subject=c.subject,
            unit=c.unit,
            lesson=c.lesson,
            snippet=(c.snippet or "").strip()[: cfg.snippet_max_chars] or None,
        )
        for c in ordered
    ]


def run_tutor(
    req: TutorRequest,
    tenant_id: str,
    *,
    retriever: Retriever,
    llm: LlmFn,
    config: Optional[TutorConfig] = None,
    clock: Callable[[], float] = time.monotonic,
) -> TutorResponse:
    """Execute one tenant-scoped, grounded tutor query and return the normalized contract."""
    validate_tenant_id(tenant_id)            # tenant always required (§6)
    cfg = config or TutorConfig.from_env()
    started = clock()

    prompt_id = req.prompt_version or default_prompt_id("tutor")
    tpl = get_prompt(prompt_id)              # unknown version -> UnknownPromptError (fail safe)

    chunks = retriever.retrieve(
        tenant_id=tenant_id,
        query=req.message,
        k=req.top_k,
        grade=req.grade,
        subject=req.subject,
        unit=req.unit_id,
        lesson=req.lesson_id,
    )
    retrieval_count = len(chunks)
    grounded_chunks = [c for c in chunks if c.score >= cfg.min_score]
    top_score = max((c.score for c in chunks), default=0.0)
    grounded = len(grounded_chunks) > 0 and top_score >= cfg.min_score

    def _elapsed_ms() -> int:
        return int((clock() - started) * 1000)

    if not grounded:
        reason = "no_relevant_material" if retrieval_count == 0 else "below_relevance_threshold"
        return TutorResponse(
            answer=_NO_ANSWER.get(req.language, _NO_ANSWER["en"]),
            grounded=False,
            no_answer_reason=reason,
            citations=[],
            provider=cfg.provider,
            model=cfg.model,
            model_version=cfg.model_version,
            prompt_version=tpl.id,
            retrieval_count=retrieval_count,
            citation_count=0,
            latency_ms=_elapsed_ms(),
            correlation_id=req.correlation_id,
        )

    context = _format_context(grounded_chunks, cfg)
    history = _render_history(req.history, MAX_HISTORY_CHARS)
    question = f"{history}\n\nQUESTION:\n{req.message}" if history else req.message

    answer = llm(system=tpl.system, context=context, question=question, model=cfg.model)
    if not answer or not str(answer).strip():
        raise TutorError("provider returned empty content")

    citations = _build_citations(grounded_chunks, cfg)
    return TutorResponse(
        answer=str(answer).strip(),
        grounded=True,
        no_answer_reason=None,
        citations=citations,
        provider=cfg.provider,
        model=cfg.model,
        model_version=cfg.model_version,
        prompt_version=tpl.id,
        retrieval_count=retrieval_count,
        citation_count=len(citations),
        latency_ms=_elapsed_ms(),
        correlation_id=req.correlation_id,
    )
