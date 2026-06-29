"""
Quiz draft generation tests (Phase 6 §11) — deterministic, no provider/ML.

Ingest a fixture through the real ingestion path, then drive ``generate_quiz_draft``
with a fake JSON-LLM to prove every strict-validation rule and that drafts are
grounded with tenant-owned source references and never persisted.
"""
import json

import pytest

from app.internal.v1.ingest import ingest_document
from app.internal.v1.quiz import QuizConfig, QuizDraftError, generate_quiz_draft
from app.internal.v1.schemas import IngestDocumentRequest, QuizDraftRequest
from app.rag.store import InMemoryTenantVectorStore, simple_paragraph_chunker

CFG = QuizConfig(provider="groq", model="m", model_version="m-v1", min_score=0.1)
FIXTURE = (
    "Photosynthesis is the process by which plants convert light energy into chemical energy.\n\n"
    "Chlorophyll absorbs sunlight and the plant releases oxygen as a by-product."
)


def _store_with_fixture(tenant="tenant-1", document_id="doc-1"):
    store = InMemoryTenantVectorStore()
    ingest_document(
        IngestDocumentRequest(correlation_id="c", document_id=document_id, version=1,
                              content=FIXTURE, language="en", material_type="textbook",
                              grade=8, subject="Science", title="Bio U1"),
        tenant, store=store, chunker=simple_paragraph_chunker)
    return store


def _q(text="What does chlorophyll absorb?", options=None, correct=0, points=2, qtype="mcq", explanation="from the text"):
    return {"question_type": qtype, "question_text": text,
            "options": options if options is not None else ["Sunlight", "Water", "Soil", "Air"],
            "correct_index": correct, "explanation": explanation, "points": points}


def _llm(questions, title="Photosynthesis Quiz", instructions="Answer all."):
    payload = {"title": title, "instructions": instructions, "questions": questions}
    return lambda **kw: json.dumps(payload)


def _req(num_questions=2, **over):
    base = dict(correlation_id="c1", num_questions=num_questions, language="en",
                grade=8, subject="Science", topic="photosynthesis chlorophyll light oxygen",
                difficulty="core", question_types=["mcq"])
    base.update(over)
    return QuizDraftRequest(**base)


def test_valid_grounded_draft():
    store = _store_with_fixture()
    llm = _llm([_q(text="What does chlorophyll absorb?"), _q(text="What gas is released?", options=["Oxygen", "Helium", "Neon", "Argon"])])
    resp = generate_quiz_draft(_req(num_questions=2), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert resp.grounded is True
    assert resp.draft.question_count == 2
    assert resp.prompt_version == "quiz.v1"
    assert resp.citations and resp.citations[0].source_document_id == "doc-1"
    # source references come only from grounded tenant-owned docs
    assert all(q.source_references == ["doc-1"] for q in resp.draft.questions)


def test_invalid_json_rejected():
    store = _store_with_fixture()
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-1", retriever=store,
                            llm_json=lambda **kw: "not json at all", config=CFG)
    assert ei.value.category == "invalid_json"


def test_unsupported_type_rejected():
    store = _store_with_fixture()
    llm = _llm([_q(qtype="essay")])
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "unsupported_type"


def test_missing_correct_answer_rejected():
    store = _store_with_fixture()
    llm = _llm([_q(correct=9)])  # out of range
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "missing_correct"


def test_duplicate_options_rejected():
    store = _store_with_fixture()
    llm = _llm([_q(options=["Sunlight", "Sunlight", "Soil", "Air"])])
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "duplicate_options"


def test_duplicate_questions_rejected():
    store = _store_with_fixture()
    llm = _llm([_q(text="Same question?"), _q(text="Same question?", options=["Oxygen", "Helium", "Neon", "Argon"])])
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=2), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "duplicate_questions"


def test_invalid_points_rejected():
    store = _store_with_fixture()
    llm = _llm([_q(points=0)])
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "invalid_points"


def test_count_out_of_bounds_rejected():
    store = _store_with_fixture()
    llm = _llm([_q()])  # only 1 question
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=3), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "count_out_of_bounds"


def test_out_of_scope_when_no_grounded_content():
    store = InMemoryTenantVectorStore()  # empty
    llm = _llm([_q()])
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "out_of_scope"


def test_cross_tenant_has_no_grounding():
    store = _store_with_fixture(tenant="tenant-1")
    llm = _llm([_q()])
    # tenant-2 has no documents -> out of scope (cannot generate from another tenant's curriculum)
    with pytest.raises(QuizDraftError) as ei:
        generate_quiz_draft(_req(num_questions=1), "tenant-2", retriever=store, llm_json=llm, config=CFG)
    assert ei.value.category == "out_of_scope"


def test_true_false_supported():
    store = _store_with_fixture()
    llm = _llm([_q(qtype="true_false", options=["True", "False"], correct=0)])
    resp = generate_quiz_draft(_req(num_questions=1, question_types=["true_false"]),
                              "tenant-1", retriever=store, llm_json=llm, config=CFG)
    assert resp.draft.questions[0].question_type == "true_false"
    assert resp.draft.questions[0].options == ["True", "False"]
