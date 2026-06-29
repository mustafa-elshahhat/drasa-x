"""
Tenant vector-store abstraction (Phase 6 §7).

A single ingestion/retrieval contract with two implementations:

  * ``ChromaTenantStore``  — the live backend, per-tenant Chroma collections.
  * ``InMemoryTenantVectorStore`` — a deterministic in-memory double used by the
    ingestion/lifecycle tests, with a lexical relevance score so retrieval is
    reproducible without embeddings.

The SAME ``ingest_document`` logic (see ``app/internal/v1/ingest.py``) runs
against either store, so the ingestion contract proven deterministically in
tests is exactly the one exercised live — Chroma is never hand-populated.
"""
from __future__ import annotations

import re
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Protocol

from app.rag.tenant import (
    ChromaTenantRetriever,
    RetrievedChunk,
    tenant_collection_name,
    validate_tenant_id,
)


@dataclass(frozen=True)
class ChunkRecord:
    """One chunk ready for indexing: a tenant-prefixed id, text, and metadata."""
    id: str
    text: str
    metadata: Dict[str, Any] = field(default_factory=dict)


@dataclass(frozen=True)
class DocumentMeta:
    """Summary of a document's currently indexed state within a tenant."""
    document_id: str
    version: int
    checksum: str
    chunk_count: int


class TenantVectorStore(Protocol):
    """Ingestion + retrieval port used by the document lifecycle and the tutor."""

    def get_document_meta(self, tenant_id: str, document_id: str) -> Optional[DocumentMeta]: ...
    def upsert_chunks(self, tenant_id: str, chunks: List[ChunkRecord]) -> None: ...
    def delete_document(self, tenant_id: str, document_id: str) -> int: ...
    def retrieve(
        self, *, tenant_id: str, query: str, k: int,
        grade: Optional[int] = None, subject: Optional[str] = None,
        unit: Optional[str] = None, lesson: Optional[str] = None,
    ) -> List[RetrievedChunk]: ...


# --- chunking ---------------------------------------------------------------
def default_text_chunker(content: str, chunk_size: int = 1000, chunk_overlap: int = 150) -> List[str]:
    """Split raw text into chunks (langchain splitter; lazy import)."""
    from langchain_text_splitters import RecursiveCharacterTextSplitter

    splitter = RecursiveCharacterTextSplitter(
        chunk_size=chunk_size,
        chunk_overlap=chunk_overlap,
        separators=["\n\n", "\n", ".", "!", "?", "،", " ", ""],
    )
    return [c for c in splitter.split_text(content) if c.strip()]


def simple_paragraph_chunker(content: str, **_: Any) -> List[str]:
    """Dependency-free chunker for tests: split on blank lines, then sentences."""
    parts = [p.strip() for p in re.split(r"\n\s*\n", content) if p.strip()]
    return parts or [content.strip()]


# --- in-memory store (tests) ------------------------------------------------
def _matches(md: Dict[str, Any], tenant_id: str, grade, subject, unit, lesson) -> bool:
    if str(md.get("tenant_id")) != str(tenant_id):
        return False
    if not md.get("active", True):
        return False
    if grade is not None and md.get("grade") != int(grade):
        return False
    if subject and subject != "auto" and str(md.get("subject", "")).lower() != subject.lower():
        return False
    if unit and str(md.get("unit") or "") != str(unit):
        return False
    if lesson and str(md.get("lesson") or "") != str(lesson):
        return False
    return True


def _lexical_score(query: str, text: str) -> float:
    q = {t for t in re.findall(r"\w+", query.lower()) if t}
    d = {t for t in re.findall(r"\w+", text.lower()) if t}
    if not q or not d:
        return 0.0
    return len(q & d) / len(q)


class InMemoryTenantVectorStore:
    """Deterministic in-memory store keyed by tenant collection name."""

    def __init__(self) -> None:
        self._collections: Dict[str, Dict[str, ChunkRecord]] = {}

    def _col(self, tenant_id: str) -> Dict[str, ChunkRecord]:
        validate_tenant_id(tenant_id)
        return self._collections.setdefault(tenant_collection_name(tenant_id), {})

    def get_document_meta(self, tenant_id: str, document_id: str) -> Optional[DocumentMeta]:
        col = self._col(tenant_id)
        recs = [r for r in col.values() if r.metadata.get("document_id") == document_id and r.metadata.get("active", True)]
        if not recs:
            return None
        version = max(int(r.metadata.get("version", 1)) for r in recs)
        checksum = next((r.metadata.get("checksum", "") for r in recs), "")
        return DocumentMeta(document_id=document_id, version=version, checksum=checksum, chunk_count=len(recs))

    def upsert_chunks(self, tenant_id: str, chunks: List[ChunkRecord]) -> None:
        col = self._col(tenant_id)
        for c in chunks:
            if str(c.metadata.get("tenant_id")) != str(tenant_id):
                raise ValueError("chunk tenant mismatch")
            col[c.id] = c

    def delete_document(self, tenant_id: str, document_id: str) -> int:
        col = self._col(tenant_id)
        ids = [cid for cid, r in col.items() if r.metadata.get("document_id") == document_id]
        for cid in ids:
            col.pop(cid, None)
        return len(ids)

    def retrieve(self, *, tenant_id, query, k, grade=None, subject=None, unit=None, lesson=None) -> List[RetrievedChunk]:
        col = self._col(tenant_id)
        scored: List[tuple[float, ChunkRecord]] = []
        for r in col.values():
            if not _matches(r.metadata, tenant_id, grade, subject, unit, lesson):
                continue
            score = _lexical_score(query, r.text)
            if score <= 0.0:  # no lexical overlap -> not a candidate (deterministic double)
                continue
            scored.append((score, r))
        scored.sort(key=lambda t: t[0], reverse=True)
        out: List[RetrievedChunk] = []
        for score, r in scored[:k]:
            md = r.metadata
            out.append(RetrievedChunk(
                chunk_id=md.get("chunk_id", r.id), document_id=md.get("document_id", ""),
                score=score, snippet=r.text, tenant_id=tenant_id,
                title=md.get("title"), grade=md.get("grade"), subject=md.get("subject"),
                unit=md.get("unit"), lesson=md.get("lesson"), language=md.get("language"), metadata=md,
            ))
        return out


# --- Chroma store (live) ----------------------------------------------------
def _chroma_where(**kv: Any) -> Dict[str, Any]:
    clauses = [{k: v} for k, v in kv.items() if v is not None]
    if not clauses:
        return {}
    return clauses[0] if len(clauses) == 1 else {"$and": clauses}


class ChromaTenantStore(ChromaTenantRetriever):
    """Live per-tenant Chroma store: inherits tenant-scoped retrieval, adds the
    ingestion/delete/lookup operations on the same collections."""

    def get_document_meta(self, tenant_id: str, document_id: str) -> Optional[DocumentMeta]:
        store = self._collection(tenant_id)
        got = store._collection.get(  # chromadb collection
            where=_chroma_where(tenant_id=tenant_id, document_id=document_id),
            include=["metadatas"],
        )
        ids = got.get("ids") or []
        metas = got.get("metadatas") or []
        if not ids:
            return None
        version = max(int((m or {}).get("version", 1)) for m in metas)
        checksum = next(((m or {}).get("checksum", "") for m in metas), "")
        return DocumentMeta(document_id=document_id, version=version, checksum=checksum, chunk_count=len(ids))

    def upsert_chunks(self, tenant_id: str, chunks: List[ChunkRecord]) -> None:
        validate_tenant_id(tenant_id)
        store = self._collection(tenant_id)
        texts = [c.text for c in chunks]
        ids = [c.id for c in chunks]
        # Chroma metadata values must be scalar and non-null.
        metadatas = [
            {k: v for k, v in c.metadata.items() if v is not None and isinstance(v, (str, int, float, bool))}
            for c in chunks
        ]
        store.add_texts(texts=texts, metadatas=metadatas, ids=ids)

    def delete_document(self, tenant_id: str, document_id: str) -> int:
        store = self._collection(tenant_id)
        where = _chroma_where(tenant_id=tenant_id, document_id=document_id)
        got = store._collection.get(where=where, include=[])
        ids = got.get("ids") or []
        if ids:
            store._collection.delete(where=where)
        return len(ids)
