"""
Tenant-scoped RAG primitive tests (Phase 6 §6) — deterministic, no ML deps.

Covers: deterministic + isolated collection naming, tenant-prefixed chunk ids,
invalid-tenant rejection, distance->relevance normalization, and the
defense-in-depth cross-tenant re-filter in ChromaTenantRetriever.
"""
import types

import pytest

from app.rag.tenant import (
    ChromaTenantRetriever,
    RetrievedChunk,
    _normalize_distance,
    tenant_chunk_id,
    tenant_collection_name,
    validate_tenant_id,
)


def test_collection_name_is_deterministic_and_opaque():
    a1 = tenant_collection_name("tenant-1")
    a2 = tenant_collection_name("tenant-1")
    assert a1 == a2
    assert a1.startswith("t_")
    # Raw tenant id must not appear in the collection name.
    assert "tenant-1" not in a1


def test_different_tenants_get_different_collections():
    assert tenant_collection_name("tenant-1") != tenant_collection_name("tenant-2")


def test_chunk_ids_are_tenant_prefixed_and_isolated():
    same_base = "Grade_8_Math.pdf_p12_c2"
    id_a = tenant_chunk_id("tenant-1", same_base)
    id_b = tenant_chunk_id("tenant-2", same_base)
    assert id_a != id_b  # identical base id in two tenants never collides
    assert id_a.startswith(tenant_collection_name("tenant-1"))


@pytest.mark.parametrize("bad", [None, "", "   ", "a" * 200, "bad/slash", "semi;colon"])
def test_invalid_tenant_rejected(bad):
    with pytest.raises(ValueError):
        validate_tenant_id(bad)


def test_distance_normalization_monotonic():
    near = _normalize_distance(0.0)
    far = _normalize_distance(5.0)
    assert near == 1.0
    assert 0.0 < far < near


def _fake_doc(content, metadata):
    return types.SimpleNamespace(page_content=content, metadata=metadata)


class _FakeStore:
    def __init__(self, pairs):
        self._pairs = pairs

    def similarity_search_with_score(self, query, k, filter):  # noqa: A002 - mirror Chroma
        return self._pairs[:k]


def test_retriever_filters_out_foreign_tenant_chunks(monkeypatch):
    """Even if the backing store returns a foreign-tenant doc, it is dropped."""
    retr = ChromaTenantRetriever(persist_dir="x", embeddings=object())

    pairs = [
        (_fake_doc("mine", {"tenant_id": "tenant-1", "chunk_id": "c1", "document_id": "d1"}), 0.1),
        (_fake_doc("theirs", {"tenant_id": "tenant-2", "chunk_id": "c2", "document_id": "d2"}), 0.05),
    ]
    monkeypatch.setattr(retr, "_collection", lambda tenant_id: _FakeStore(pairs))

    out = retr.retrieve(tenant_id="tenant-1", query="q", k=5)
    assert [c.document_id for c in out] == ["d1"]
    assert all(isinstance(c, RetrievedChunk) and c.tenant_id == "tenant-1" for c in out)
