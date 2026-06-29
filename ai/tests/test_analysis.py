"""
Conversation / pain-point analysis tests (Phase 6 §13) — deterministic.

Drives run_analysis with a fake JSON-LLM to prove the strict output, confidence
bounds, category/escalation normalization, mandatory human-review, the
non-diagnostic guard, no-fabricated-fallback, and tenant requirement.
"""
import json

import pytest

from app.internal.v1.analysis import AnalysisConfig, AnalysisError, run_analysis
from app.internal.v1.schemas import AnalysisRequest

CFG = AnalysisConfig(provider="groq", model="m", model_version="m-v1")
CONV = [
    {"role": "user", "content": "I keep getting fractions wrong, I don't understand the steps."},
    {"role": "assistant", "content": "Let's review the steps together."},
]


def _llm(payload):
    return lambda **kw: json.dumps(payload)


def _req(**over):
    base = dict(correlation_id="c1", student_ref="stu-1", conversation=CONV, language="en", subject="Math")
    base.update(over)
    return AnalysisRequest(**base)


def test_valid_analysis():
    llm = _llm({"pain_point_category": "concept", "evidence_summary": "struggles with fraction steps",
                "recommendation": "practice step-by-step fractions", "confidence": 0.7,
                "escalation_level": "monitor", "signals": ["fractions wrong"]})
    resp = run_analysis(_req(), "tenant-1", llm_json=llm, config=CFG)
    assert resp.pain_point_category == "concept"
    assert resp.escalation_level == "monitor"
    assert resp.confidence == 0.7
    assert resp.human_review_required is True       # always
    assert resp.prompt_version == "analysis.v1"
    assert resp.subject == "Math"


def test_unknown_category_and_escalation_normalized():
    llm = _llm({"pain_point_category": "banana", "evidence_summary": "x", "recommendation": "y",
                "confidence": 2.0, "escalation_level": "nuke", "signals": []})
    resp = run_analysis(_req(), "tenant-1", llm_json=llm, config=CFG)
    assert resp.pain_point_category == "none"       # unknown -> none
    assert resp.escalation_level == "none"
    assert resp.confidence == 1.0                   # clamped


def test_invalid_json_no_fabricated_fallback():
    with pytest.raises(AnalysisError) as ei:
        run_analysis(_req(), "tenant-1", llm_json=lambda **kw: "not json", config=CFG)
    assert ei.value.category == "invalid_json"       # NOT a silent 50/50 result


def test_non_diagnostic_guard_rejects_clinical_language():
    llm = _llm({"pain_point_category": "engagement", "evidence_summary": "student shows signs of depression and anxiety",
                "recommendation": "seek therapy", "confidence": 0.6, "escalation_level": "escalate", "signals": []})
    with pytest.raises(AnalysisError) as ei:
        run_analysis(_req(), "tenant-1", llm_json=llm, config=CFG)
    assert ei.value.category == "non_diagnostic_violation"


def test_missing_tenant_rejected():
    llm = _llm({"pain_point_category": "none", "evidence_summary": "x", "recommendation": "y",
                "confidence": 0.1, "escalation_level": "none", "signals": []})
    with pytest.raises(ValueError):
        run_analysis(_req(), "", llm_json=llm, config=CFG)


def test_context_passed_to_llm_not_system():
    seen = {}

    def llm(*, system, context, instruction):
        seen["system"] = system
        seen["context"] = context
        return json.dumps({"pain_point_category": "none", "evidence_summary": "x", "recommendation": "y",
                           "confidence": 0.1, "escalation_level": "none", "signals": []})

    run_analysis(_req(), "tenant-1", llm_json=llm, config=CFG)
    assert "fractions" in seen["context"].lower()
    assert "not a clinician" in seen["system"].lower()
