"""
Tenant-scoped RAG primitives (Phase 6 §6).

Every RAG operation is scoped to a single tenant. The collection identifier is
**derived from the trusted backend tenant id** (taken from the signed service
token, never from the request body) and is deterministic but non-sensitive: it
is an HMAC/sha256-based opaque token, so the raw tenant id is never used as a
vector-store collection name or leaked in logs.

Chunk ids and metadata are likewise tenant-prefixed so that identical document
ids in different tenants can never collide in a shared backing store.
"""
from __future__ import annotations

import hashlib
import os
import re
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Protocol

# Collection-name prefix; the rest is a deterministic digest of the tenant id.
_COLLECTION_PREFIX = "t_"
_TENANT_ID_RE = re.compile(r"^[A-Za-z0-9._:-]{1,128}$")


def validate_tenant_id(tenant_id: Optional[str]) -> str:
    """Validate and return a tenant id, or raise ``ValueError``.

    Tenant ids come from the signed token; this guards against absent or
    obviously malformed values before they are used to derive a collection.
    """
    if not tenant_id or not _TENANT_ID_RE.match(tenant_id):
        raise ValueError("missing or invalid tenant id")
    return tenant_id


def tenant_collection_name(tenant_id: str) -> str:
    """Deterministic, non-sensitive Chroma collection name for a tenant.

    The raw tenant id is hashed so the collection name leaks nothing and is
    stable across restarts. A per-deployment salt can be supplied via
    ``RAG_COLLECTION_SALT`` (defaults to a fixed local value).
    """
    validate_tenant_id(tenant_id)
    salt = os.environ.get("RAG_COLLECTION_SALT", "derasax-local")
    digest = hashlib.sha256(f"{salt}:{tenant_id}".encode("utf-8")).hexdigest()
    return f"{_COLLECTION_PREFIX}{digest[:16]}"


def tenant_chunk_id(tenant_id: str, base_chunk_id: str) -> str:
    """Tenant-prefixed chunk id so identical doc ids never collide across tenants."""
    return f"{tenant_collection_name(tenant_id)}:{base_chunk_id}"


@dataclass(frozen=True)
class RetrievedChunk:
    """One retrieved, tenant-authorized chunk with normalized relevance.

    ``score`` is normalized to [0, 1] where higher means more relevant, so the
    no-answer threshold is provider/metric independent.
    """
    chunk_id: str
    document_id: str
    score: float
    snippet: str
    tenant_id: str
    title: Optional[str] = None
    grade: Optional[int] = None
    subject: Optional[str] = None
    unit: Optional[str] = None
    lesson: Optional[str] = None
    language: Optional[str] = None
    metadata: Dict[str, Any] = field(default_factory=dict)


class Retriever(Protocol):
    """Injectable retrieval port so the tutor service is testable without ML deps."""

    def retrieve(
        self,
        *,
        tenant_id: str,
        query: str,
        k: int,
        grade: Optional[int] = None,
        subject: Optional[str] = None,
        unit: Optional[str] = None,
        lesson: Optional[str] = None,
    ) -> List[RetrievedChunk]:
        ...


def _normalize_distance(distance: float) -> float:
    """Map a Chroma cosine/L2 distance (>=0, lower=closer) to relevance in (0, 1]."""
    try:
        d = max(0.0, float(distance))
    except (TypeError, ValueError):
        return 0.0
    return 1.0 / (1.0 + d)


class ChromaTenantRetriever:
    """Real retriever backed by a per-tenant Chroma collection.

    Defense in depth: the collection is tenant-derived AND every result is
    re-filtered to require ``metadata.tenant_id == tenant_id`` so a
    misconfigured shared store can never return another tenant's chunk.
    """

    def __init__(self, persist_dir: str, embeddings: Any) -> None:
        self._persist_dir = persist_dir
        self._embeddings = embeddings
        self._cache: Dict[str, Any] = {}

    def _collection(self, tenant_id: str):
        name = tenant_collection_name(tenant_id)
        if name not in self._cache:
            from langchain_chroma import Chroma  # imported lazily (optional dep)

            self._cache[name] = Chroma(
                collection_name=name,
                persist_directory=self._persist_dir,
                embedding_function=self._embeddings,
            )
        return self._cache[name]

    def retrieve(
        self,
        *,
        tenant_id: str,
        query: str,
        k: int,
        grade: Optional[int] = None,
        subject: Optional[str] = None,
        unit: Optional[str] = None,
        lesson: Optional[str] = None,
    ) -> List[RetrievedChunk]:
        validate_tenant_id(tenant_id)
        store = self._collection(tenant_id)

        clauses: List[Dict[str, Any]] = [{"tenant_id": tenant_id}]
        if subject and subject != "auto":
            clauses.append({"subject": subject.lower()})
        if grade is not None:
            clauses.append({"grade": int(grade)})
        if unit:
            clauses.append({"unit": str(unit)})
        if lesson:
            clauses.append({"lesson": str(lesson)})
        where: Dict[str, Any] = clauses[0] if len(clauses) == 1 else {"$and": clauses}

        pairs = store.similarity_search_with_score(query, k=k, filter=where)

        out: List[RetrievedChunk] = []
        for doc, distance in pairs:
            md = dict(doc.metadata or {})
            # Hard re-check: never surface a chunk that is not this tenant's.
            if str(md.get("tenant_id")) != str(tenant_id):
                continue
            out.append(
                RetrievedChunk(
                    chunk_id=str(md.get("chunk_id") or md.get("id") or ""),
                    document_id=str(md.get("document_id") or md.get("source_file") or ""),
                    score=_normalize_distance(distance),
                    snippet=(doc.page_content or "").strip(),
                    tenant_id=tenant_id,
                    title=md.get("title") or md.get("source_file"),
                    grade=md.get("grade"),
                    subject=md.get("subject"),
                    unit=md.get("unit"),
                    lesson=md.get("lesson"),
                    language=md.get("language"),
                    metadata=md,
                )
            )
        return out
