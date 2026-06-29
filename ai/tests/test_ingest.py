"""
Document ingestion / lifecycle tests (Phase 6 §7) — deterministic, in-memory.

Covers: indexing, checksum idempotency, version-aware re-index replacement,
deletion, cross-tenant retrieval + deletion isolation, identical checksum across
tenants, deleted-content exclusion, metadata filtering, tenant-prefixed chunk
ids, empty content rejection, Arabic + English ingest/retrieve, and a grounded
tutor answer produced through the real ingestion path (no manual store priming
of chunk objects — everything goes through ``ingest_document``).
"""
import pytest

from app.internal.v1.ingest import IngestError, delete_document, document_status, ingest_document
from app.internal.v1.schemas import IngestDocumentRequest
from app.internal.v1.tutor import TutorConfig, run_tutor
from app.internal.v1.schemas import TutorRequest
from app.rag.store import InMemoryTenantVectorStore, simple_paragraph_chunker
from app.rag.tenant import tenant_collection_name

CHUNKER = simple_paragraph_chunker

PHOTO_EN = (
    "Photosynthesis is the process by which green plants convert light energy into chemical energy.\n\n"
    "Chlorophyll in the leaves absorbs sunlight to produce glucose and oxygen."
)
PHOTO_AR = (
    "البناء الضوئي هو عملية تحويل الطاقة الضوئية إلى طاقة كيميائية في النباتات الخضراء.\n\n"
    "يمتص الكلوروفيل ضوء الشمس لإنتاج الجلوكوز والأكسجين."
)


def _req(document_id="doc-1", version=1, content=PHOTO_EN, language="en", **over):
    base = dict(
        correlation_id="c1", document_id=document_id, version=version, content=content,
        language=language, material_type="textbook", grade=8, subject="Science",
    )
    base.update(over)
    return IngestDocumentRequest(**base)


def _ingest(store, **over):
    return ingest_document(_req(**over), "tenant-1", store=store, chunker=CHUNKER)


def test_ingest_indexes_chunks_and_is_retrievable():
    store = InMemoryTenantVectorStore()
    res = _ingest(store)
    assert res.status == "indexed"
    assert res.chunk_count >= 1
    assert res.checksum
    hits = store.retrieve(tenant_id="tenant-1", query="photosynthesis light energy", k=4)
    assert hits and hits[0].document_id == "doc-1"


def test_chunk_ids_are_tenant_prefixed():
    store = InMemoryTenantVectorStore()
    _ingest(store)
    hits = store.retrieve(tenant_id="tenant-1", query="chlorophyll", k=4)
    assert hits[0].chunk_id.startswith(tenant_collection_name("tenant-1"))


def test_idempotent_same_checksum_same_version():
    store = InMemoryTenantVectorStore()
    first = _ingest(store)
    again = _ingest(store)
    assert again.status == "duplicate"
    assert again.removed_chunks == 0
    assert again.chunk_count == first.chunk_count


def test_reindex_replaces_previous_version():
    store = InMemoryTenantVectorStore()
    _ingest(store, version=1)
    new_content = "Mitosis is the division of a cell into two identical daughter cells."
    res = ingest_document(_req(version=2, content=new_content, subject="Science"),
                          "tenant-1", store=store, chunker=CHUNKER)
    assert res.status == "reindexed"
    assert res.removed_chunks >= 1
    # old content gone, new content present
    assert store.retrieve(tenant_id="tenant-1", query="photosynthesis", k=4) == []
    assert store.retrieve(tenant_id="tenant-1", query="mitosis cell division", k=4)


def test_delete_removes_all_chunks_and_excludes_from_retrieval():
    store = InMemoryTenantVectorStore()
    _ingest(store)
    removed = delete_document("tenant-1", "doc-1", store=store)
    assert removed >= 1
    assert store.retrieve(tenant_id="tenant-1", query="photosynthesis", k=4) == []
    assert document_status("tenant-1", "doc-1", store=store) is None


def test_cross_tenant_retrieval_isolation():
    store = InMemoryTenantVectorStore()
    _ingest(store)  # tenant-1
    assert store.retrieve(tenant_id="tenant-2", query="photosynthesis", k=4) == []


def test_cross_tenant_deletion_isolation():
    store = InMemoryTenantVectorStore()
    ingest_document(_req(), "tenant-1", store=store, chunker=CHUNKER)
    ingest_document(_req(), "tenant-2", store=store, chunker=CHUNKER)  # same document_id
    delete_document("tenant-1", "doc-1", store=store)
    # tenant-2's identically-named document is untouched
    assert store.retrieve(tenant_id="tenant-2", query="photosynthesis", k=4)
    assert store.retrieve(tenant_id="tenant-1", query="photosynthesis", k=4) == []


def test_identical_checksum_different_tenants_isolated():
    store = InMemoryTenantVectorStore()
    a = ingest_document(_req(), "tenant-1", store=store, chunker=CHUNKER)
    b = ingest_document(_req(), "tenant-2", store=store, chunker=CHUNKER)
    assert a.checksum == b.checksum  # same content
    assert a.status == "indexed" and b.status == "indexed"  # not treated as duplicate cross-tenant


def test_metadata_filtering_by_grade():
    store = InMemoryTenantVectorStore()
    _ingest(store, grade=8)
    assert store.retrieve(tenant_id="tenant-1", query="photosynthesis", k=4, grade=7) == []
    assert store.retrieve(tenant_id="tenant-1", query="photosynthesis", k=4, grade=8)


def test_empty_content_rejected():
    store = InMemoryTenantVectorStore()
    with pytest.raises(IngestError) as ei:
        ingest_document(_req(content="   "), "tenant-1", store=store, chunker=CHUNKER)
    assert ei.value.category == "empty_content"


def test_arabic_ingest_and_retrieve():
    store = InMemoryTenantVectorStore()
    ingest_document(_req(document_id="doc-ar", content=PHOTO_AR, language="ar"),
                    "tenant-1", store=store, chunker=CHUNKER)
    hits = store.retrieve(tenant_id="tenant-1", query="البناء الضوئي الكلوروفيل", k=4)
    assert hits and hits[0].document_id == "doc-ar"
    assert hits[0].language == "ar"


def test_english_ingest_and_retrieve():
    store = InMemoryTenantVectorStore()
    _ingest(store)
    hits = store.retrieve(tenant_id="tenant-1", query="oxygen glucose", k=4)
    assert hits and hits[0].language == "en"


def test_tutor_grounded_through_real_ingestion_path():
    """End-to-end (deterministic): ingest -> tutor returns grounded answer +
    citations with tenant-owned metadata and no raw file path."""
    store = InMemoryTenantVectorStore()
    ingest_document(_req(title="Science Unit 1"), "tenant-1", store=store, chunker=CHUNKER)

    captured = {}

    def llm(*, system, context, question, model):
        captured["context"] = context
        return "Photosynthesis converts light into chemical energy."

    resp = run_tutor(
        TutorRequest(correlation_id="c1", message="Explain photosynthesis and chlorophyll", language="en", grade=8, subject="Science"),
        "tenant-1", retriever=store, llm=llm, config=TutorConfig(min_score=0.1),
    )
    assert resp.grounded is True
    assert resp.answer
    assert resp.citation_count >= 1
    cit = resp.citations[0]
    assert cit.source_document_id == "doc-1"
    assert cit.title == "Science Unit 1"
    # no raw file path leaks anywhere
    assert "source_path" not in (cit.snippet or "")
    assert ":\\" not in (cit.snippet or "") and "/data/" not in (cit.snippet or "")
