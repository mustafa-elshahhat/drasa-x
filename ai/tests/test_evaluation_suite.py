"""
AI evaluation suite (Phase 6 §19) — reproducible, deterministic.

These are *evaluation* tests that drive the full ingest → retrieve → operate
pipeline (real tenant ingestion + real in-memory tenant store + faked LLM), as
opposed to the pure-unit port tests. There is no labelled model-quality dataset,
so this suite reports CONTRACT / behavioral coverage — not invented accuracy
percentages (see AI_EVALUATION_PLAN.md / AI_EVALUATION_RESULTS.md).
"""
import json

import pytest

from app.internal.v1.ingest import delete_document, ingest_document
from app.internal.v1.quiz import QuizConfig, generate_quiz_draft
from app.internal.v1.schemas import IngestDocumentRequest, QuizDraftRequest, TutorRequest
from app.internal.v1.tutor import TutorConfig, run_tutor
from app.rag.store import InMemoryTenantVectorStore, simple_paragraph_chunker

TCFG = TutorConfig(provider="groq", model="m", model_version="m-v1", min_score=0.2)
QCFG = QuizConfig(provider="groq", model="m", model_version="m-v1", min_score=0.2)

EN = "Chlorophyll absorbs sunlight. Photosynthesis converts light into chemical energy and releases oxygen."
AR = "الكلوروفيل يمتص ضوء الشمس. البناء الضوئي يحول الضوء إلى طاقة كيميائية ويطلق الأكسجين."


def _store():
    return InMemoryTenantVectorStore()


def _ingest(store, tenant, doc_id, content, lang="en", version=1, **md):
    return ingest_document(
        IngestDocumentRequest(correlation_id="c", document_id=doc_id, version=version,
                              content=content, language=lang, material_type="textbook", **md),
        tenant, store=store, chunker=simple_paragraph_chunker)


def _tutor(store, tenant, message, lang="en", llm=None):
    req = TutorRequest(correlation_id="c", message=message, language=lang, top_k=5)
    return run_tutor(req, tenant, retriever=store,
                     llm=llm or (lambda **kw: "grounded answer"), config=TCFG)


# --- RAG evaluation ---------------------------------------------------------

def test_eval_english_retrieval_grounded():
    store = _store(); _ingest(store, "t1", "d1", EN, grade=8, subject="Science")
    resp = _tutor(store, "t1", "what does chlorophyll absorb?")
    assert resp.grounded is True and resp.citation_count >= 1


def test_eval_arabic_retrieval_grounded():
    store = _store(); _ingest(store, "t1", "d-ar", AR, lang="ar", grade=8, subject="Science")
    resp = _tutor(store, "t1", "ماذا يمتص الكلوروفيل؟", lang="ar")
    assert resp.grounded is True


def test_eval_mixed_language_corpus_retrieves_relevant_language():
    store = _store()
    _ingest(store, "t1", "d-en", EN)
    _ingest(store, "t1", "d-ar", AR, lang="ar")
    # English query retrieves the English doc as the top citation.
    resp = _tutor(store, "t1", "chlorophyll sunlight oxygen")
    assert resp.grounded is True
    assert resp.citations[0].source_document_id == "d-en"


def test_eval_citation_ownership_is_tenant_scoped():
    store = _store(); _ingest(store, "t1", "d1", EN)
    resp = _tutor(store, "t1", "photosynthesis oxygen")
    # Every citation references a document that exists in THIS tenant's collection.
    for cit in resp.citations:
        assert store.get_document_meta("t1", cit.source_document_id) is not None


def test_eval_tenant_isolation_no_cross_read():
    store = _store(); _ingest(store, "t1", "d1", EN)
    resp = _tutor(store, "t2", "chlorophyll")
    assert resp.grounded is False and resp.citations == []


def test_eval_deleted_document_excluded():
    store = _store(); _ingest(store, "t1", "d1", EN)
    assert _tutor(store, "t1", "chlorophyll").grounded is True
    delete_document("t1", "d1", store=store)
    after = _tutor(store, "t1", "chlorophyll", llm=lambda **kw: "should not be used")
    assert after.grounded is False and after.no_answer_reason


def test_eval_reindex_replaces_content():
    store = _store()
    _ingest(store, "t1", "d1", EN, version=1)
    # Re-index v2 with different content (force replacement).
    res = ingest_document(
        IngestDocumentRequest(correlation_id="c", document_id="d1", version=2,
                              content="Mitochondria are the powerhouse; cellular respiration produces ATP.",
                              language="en", material_type="textbook"),
        "t1", store=store, chunker=simple_paragraph_chunker, force=True)
    assert res.status in ("reindexed", "indexed")
    meta = store.get_document_meta("t1", "d1")
    assert meta.version == 2


# --- Tutor evaluation -------------------------------------------------------

def test_eval_unsupported_request_refused_no_fabrication():
    store = _store(); _ingest(store, "t1", "d1", EN)
    # Off-curriculum question with no matching content → safe no-answer.
    resp = _tutor(store, "t1", "who won the world cup in 1998?",
                  llm=lambda **kw: "should not be used")
    assert resp.grounded is False


def test_eval_empty_retrieval_no_answer():
    store = _store()  # nothing ingested
    resp = _tutor(store, "t1", "anything", llm=lambda **kw: "should not be used")
    assert resp.grounded is False and resp.no_answer_reason == "no_relevant_material"


# --- Quiz evaluation --------------------------------------------------------

def _quiz_llm(n=1):
    qs = [{"question_type": "mcq", "question_text": f"Q{i} about photosynthesis?",
           "options": ["Sunlight", "Water", "Soil", "Air"], "correct_index": 0,
           "explanation": "from the text", "points": 2} for i in range(n)]
    return lambda **kw: json.dumps({"title": "Quiz", "instructions": "Answer.", "questions": qs})


def test_eval_quiz_schema_valid_and_grounded():
    store = _store(); _ingest(store, "t1", "d1", EN, grade=8, subject="Science")
    req = QuizDraftRequest(correlation_id="c", num_questions=1, grade=8, subject="Science",
                           topic="chlorophyll sunlight oxygen", question_types=["mcq"])
    resp = generate_quiz_draft(req, "t1", retriever=store, llm_json=_quiz_llm(1), config=QCFG)
    assert resp.grounded is True
    assert resp.draft.question_count == 1
    q = resp.draft.questions[0]
    assert len(q.options) == len(set(q.options))  # unique options
    assert 0 <= q.correct_index < len(q.options)  # valid answer


def test_eval_quiz_no_ai_persistence_side_effects():
    # generate_quiz_draft returns a value object only; it has no store/write port.
    import inspect
    sig = inspect.signature(generate_quiz_draft)
    assert "llm_json" in sig.parameters and "retriever" in sig.parameters
    # No persistence parameter exists on the AI side (backend owns the lifecycle).
    assert not any(p in sig.parameters for p in ("db", "session", "repository", "store_writer"))
