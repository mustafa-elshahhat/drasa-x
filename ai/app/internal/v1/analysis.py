"""
Conversation / pain-point analysis (Phase 6 §13) — stateless, non-diagnostic.

Pure and injectable (JSON-LLM port) so the contract, the strict output, and the
non-diagnostic / human-review guarantees are deterministically testable. No
persistence here, and NO fabricated fallback: a provider/JSON failure raises an
explicit error (the prototype's silent 50/50/0.5 result is gone).
"""
from __future__ import annotations

import json
import os
import re
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Callable, List, Optional

from app.internal.v1.schemas import AnalysisRequest, AnalysisResponse
from app.prompts.registry import default_prompt_id, get_prompt
from app.rag.tenant import validate_tenant_id

_CATEGORIES = {"concept", "skill", "attendance", "engagement", "assessment", "none"}
_ESCALATIONS = {"none", "monitor", "escalate"}

# Defense-in-depth: reject obviously clinical/diagnostic language in model output.
_CLINICAL_TERMS = re.compile(
    r"\b(depress|anxiet|anxious|adhd|autis|disorder|diagnos|trauma|mental\s+health|"
    r"clinical|psychiatr|psycholog|therapy|medication|suicid|self-harm)\w*",
    re.IGNORECASE,
)

JsonLlmFn = Callable[..., str]


class AnalysisError(RuntimeError):
    def __init__(self, category: str, message: str):
        super().__init__(message)
        self.category = category


@dataclass(frozen=True)
class AnalysisConfig:
    provider: str = "groq"
    model: str = "llama-3.1-8b-instant"
    model_version: str = "llama-3.1-8b-instant"

    @classmethod
    def from_env(cls) -> "AnalysisConfig":
        model = os.environ.get("AI_ANALYSIS_MODEL", os.environ.get("AI_TUTOR_MODEL", cls.model))
        return cls(
            provider=os.environ.get("AI_PROVIDER", cls.provider),
            model=model,
            model_version=os.environ.get("AI_ANALYSIS_MODEL_VERSION", model),
        )


def _render_conversation(turns) -> str:
    return "\n".join(f"{t.role.upper()}: {t.content.strip()}" for t in turns)


def _parse(raw: str) -> dict:
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
        raise AnalysisError("invalid_json", "provider did not return valid JSON") from exc
    if not isinstance(data, dict):
        raise AnalysisError("invalid_json", "provider JSON is not an object")
    return data


def run_analysis(
    req: AnalysisRequest,
    tenant_id: str,
    *,
    llm_json: JsonLlmFn,
    config: Optional[AnalysisConfig] = None,
    clock: Callable[[], datetime] = lambda: datetime.now(timezone.utc),
) -> AnalysisResponse:
    validate_tenant_id(tenant_id)
    cfg = config or AnalysisConfig.from_env()

    prompt_id = req.prompt_version or default_prompt_id("analysis")
    tpl = get_prompt(prompt_id)

    convo = _render_conversation(req.conversation)
    raw = llm_json(system=tpl.system, context=convo, instruction="Analyze the conversation above.")
    data = _parse(raw)

    category = str(data.get("pain_point_category", "none")).lower()
    if category not in _CATEGORIES:
        category = "none"
    escalation = str(data.get("escalation_level", "none")).lower()
    if escalation not in _ESCALATIONS:
        escalation = "none"

    try:
        confidence = float(data.get("confidence", 0.0))
    except (TypeError, ValueError):
        confidence = 0.0
    confidence = max(0.0, min(1.0, confidence))

    evidence = str(data.get("evidence_summary", "")).strip()
    recommendation = str(data.get("recommendation", "")).strip()
    signals = [str(x) for x in (data.get("signals") or []) if str(x).strip()][:10]

    # Non-diagnostic guard: never emit clinical/diagnostic language.
    if _CLINICAL_TERMS.search(f"{evidence}\n{recommendation}\n{' '.join(signals)}"):
        raise AnalysisError("non_diagnostic_violation", "analysis produced clinical/diagnostic language")

    return AnalysisResponse(
        student_ref=req.student_ref,
        pain_point_category=category,
        subject=req.subject,
        unit=req.unit_id,
        lesson=req.lesson_id,
        evidence_summary=evidence or "No clear evidence.",
        recommendation=recommendation or "Continue practice and review.",
        confidence=round(confidence, 4),
        escalation_level=escalation,
        human_review_required=True,   # always — never an automatic action
        signals=signals,
        model=cfg.model,
        model_version=cfg.model_version,
        prompt_version=tpl.id,
        generated_at=clock().isoformat(),
        correlation_id=req.correlation_id,
    )
