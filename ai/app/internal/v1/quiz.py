"""
Quiz draft generation (Phase 6 §11) — stateless, RAG-grounded, draft-only.

The AI service generates a schema-valid DRAFT and NOTHING ELSE: it never stores
quizzes, attempts, grades, or results — DerasaX-backend owns the permanent quiz
lifecycle. Pure and injectable (retriever + JSON-LLM ports) so the strict
validation rules are deterministically testable without a provider.
"""
from __future__ import annotations

import json
import os
import re
from dataclasses import dataclass
from datetime import datetime, timezone
from difflib import SequenceMatcher
from typing import Callable, List, Optional

from app.internal.v1.schemas import (
    MAX_QUESTION_POINTS,
    QuizDraft,
    QuizDraftQuestion,
    QuizDraftRequest,
    QuizDraftResponse,
)
from app.internal.v1.tutor import TutorConfig, _build_citations, _format_context
from app.prompts.registry import default_prompt_id, get_prompt
from app.rag.tenant import Retriever, validate_tenant_id

SUPPORTED_TYPES = {"mcq", "true_false"}
NEAR_DUP_RATIO = 0.92

# JSON-LLM port: (system, context, instruction) -> raw JSON string.
JsonLlmFn = Callable[..., str]


class QuizDraftError(RuntimeError):
    """Draft generation/validation failure with a stable category."""
    def __init__(self, category: str, message: str):
        super().__init__(message)
        self.category = category


@dataclass(frozen=True)
class QuizConfig:
    provider: str = "groq"
    model: str = "llama-3.1-8b-instant"
    model_version: str = "llama-3.1-8b-instant"
    min_score: float = 0.20

    @classmethod
    def from_env(cls) -> "QuizConfig":
        model = os.environ.get("AI_QUIZ_MODEL", os.environ.get("AI_TUTOR_MODEL", cls.model))
        return cls(
            provider=os.environ.get("AI_PROVIDER", cls.provider),
            model=model,
            model_version=os.environ.get("AI_QUIZ_MODEL_VERSION", model),
            min_score=float(os.environ.get("RAG_MIN_SCORE", cls.min_score)),
        )


def _parse_json(raw: str) -> dict:
    s = (raw or "").strip()
    if s.startswith("```"):
        s = s.replace("```json", "").replace("```", "").strip()
    if not s.startswith("{"):
        i, j = s.find("{"), s.rfind("}")
        if i != -1 and j != -1 and j > i:
            s = s[i:j + 1]
    try:
        data = json.loads(s)
    except Exception as exc:
        raise QuizDraftError("invalid_json", "provider did not return valid JSON") from exc
    if not isinstance(data, dict) or "questions" not in data:
        raise QuizDraftError("invalid_json", "provider JSON missing 'questions'")
    return data


def _norm(text: str) -> str:
    return re.sub(r"\s+", " ", (text or "").strip().lower())


def _validate_question(raw: dict, allowed_types: set[str]) -> QuizDraftQuestion:
    qtype = str(raw.get("question_type", "")).lower()
    if qtype not in SUPPORTED_TYPES or qtype not in allowed_types:
        raise QuizDraftError("unsupported_type", f"unsupported question_type: {qtype!r}")

    text = str(raw.get("question_text", "")).strip()
    if not text:
        raise QuizDraftError("invalid_question", "empty question_text")

    options = raw.get("options") or []
    if not isinstance(options, list) or not all(isinstance(o, str) for o in options):
        raise QuizDraftError("invalid_options", "options must be a list of strings")
    options = [o.strip() for o in options]

    if qtype == "true_false":
        options = ["True", "False"]
    else:
        if not (2 <= len(options) <= 6):
            raise QuizDraftError("invalid_options", "mcq must have 2-6 options")

    # duplicate options (case-insensitive)
    if len({o.lower() for o in options}) != len(options):
        raise QuizDraftError("duplicate_options", "options must be unique")

    try:
        correct = int(raw.get("correct_index"))
    except (TypeError, ValueError):
        raise QuizDraftError("missing_correct", "correct_index missing or not an int")
    if correct < 0 or correct >= len(options):
        raise QuizDraftError("missing_correct", "correct_index out of range")

    try:
        points = int(raw.get("points", 1))
    except (TypeError, ValueError):
        raise QuizDraftError("invalid_points", "points must be an int")
    if points < 1 or points > MAX_QUESTION_POINTS:
        raise QuizDraftError("invalid_points", f"points must be 1..{MAX_QUESTION_POINTS}")

    explanation = str(raw.get("explanation", "")).strip()

    return QuizDraftQuestion(
        question_type=qtype, question_text=text, options=options,
        correct_index=correct, explanation=explanation, points=points,
        source_references=[],  # filled with grounded tenant doc ids by caller
    )


def generate_quiz_draft(
    req: QuizDraftRequest,
    tenant_id: str,
    *,
    retriever: Retriever,
    llm_json: JsonLlmFn,
    config: Optional[QuizConfig] = None,
    clock: Callable[[], datetime] = lambda: datetime.now(timezone.utc),
) -> QuizDraftResponse:
    """Generate a grounded, validated, draft-only quiz."""
    validate_tenant_id(tenant_id)
    cfg = config or QuizConfig.from_env()

    prompt_id = req.prompt_version or default_prompt_id("quiz")
    tpl = get_prompt(prompt_id)

    query = req.topic or " ".join(
        str(x) for x in [req.subject, f"grade {req.grade}" if req.grade else None, req.unit_id, req.lesson_id] if x
    ) or (req.subject or "curriculum")

    chunks = retriever.retrieve(
        tenant_id=tenant_id, query=query, k=req.top_k,
        grade=req.grade, subject=req.subject, unit=req.unit_id, lesson=req.lesson_id,
    )
    grounded_chunks = [c for c in chunks if c.score >= cfg.min_score]
    if not grounded_chunks:
        raise QuizDraftError("out_of_scope", "no grounded curriculum content for the requested scope")

    tutor_cfg = TutorConfig(min_score=cfg.min_score)
    context = _format_context(grounded_chunks, tutor_cfg)
    instruction = (
        f"Generate EXACTLY {req.num_questions} {req.difficulty} questions "
        f"of allowed types {sorted(set(req.question_types))} as the JSON object described."
    )

    raw = llm_json(system=tpl.system, context=context, instruction=instruction)
    data = _parse_json(raw)

    allowed = {t for t in req.question_types}
    raw_questions = data.get("questions") or []
    if not isinstance(raw_questions, list) or not raw_questions:
        raise QuizDraftError("invalid_json", "no questions produced")

    questions: List[QuizDraftQuestion] = []
    seen: List[str] = []
    grounded_doc_ids = sorted({c.document_id for c in grounded_chunks if c.document_id})
    for rq in raw_questions:
        if not isinstance(rq, dict):
            raise QuizDraftError("invalid_json", "question is not an object")
        q = _validate_question(rq, allowed)
        norm = _norm(q.question_text)
        for prev in seen:
            if norm == prev or SequenceMatcher(None, norm, prev).ratio() >= NEAR_DUP_RATIO:
                raise QuizDraftError("duplicate_questions", "duplicate or near-duplicate question")
        seen.append(norm)
        # Source references come ONLY from grounded, tenant-authorized documents.
        q = q.model_copy(update={"source_references": grounded_doc_ids})
        questions.append(q)

    if len(questions) != req.num_questions:
        raise QuizDraftError("count_out_of_bounds",
                             f"expected {req.num_questions} questions, got {len(questions)}")

    citations = _build_citations(grounded_chunks, tutor_cfg)
    draft = QuizDraft(
        title=str(data.get("title") or f"{req.subject or 'Quiz'} draft").strip()[:256],
        instructions=str(data.get("instructions") or "Answer all questions.").strip()[:1000],
        grade=req.grade, subject=req.subject, unit=req.unit_id, lesson=req.lesson_id,
        difficulty=req.difficulty, question_count=len(questions), questions=questions,
    )
    return QuizDraftResponse(
        draft=draft, grounded=True, citations=citations,
        provider=cfg.provider, model=cfg.model, model_version=cfg.model_version,
        prompt_version=tpl.id, retrieval_count=len(chunks),
        correlation_id=req.correlation_id, generated_at=clock().isoformat(),
    )
