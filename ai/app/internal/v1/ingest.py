"""
Document ingestion / lifecycle service (Phase 6 §7).

Pure, store-agnostic logic shared by the deterministic tests (in-memory store)
and the live path (Chroma store), so the contract proven in tests is the one
that runs in production. Tenant is supplied by the caller from the SIGNED token.
"""
from __future__ import annotations

import hashlib
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Callable, List, Optional

from app.internal.v1.schemas import IngestDocumentRequest
from app.rag.store import ChunkRecord, TenantVectorStore, default_text_chunker
from app.rag.tenant import tenant_chunk_id, tenant_collection_name, validate_tenant_id


class IngestError(RuntimeError):
    """Ingestion failure with a stable, non-sensitive category."""
    def __init__(self, category: str, message: str):
        super().__init__(message)
        self.category = category


@dataclass(frozen=True)
class IngestResult:
    document_id: str
    version: int
    status: str            # "indexed" | "reindexed" | "duplicate"
    chunk_count: int
    removed_chunks: int
    checksum: str
    language: str
    collection: str        # opaque, non-sensitive tenant collection id
    indexed_at: str


def _embedding_model_version() -> str:
    # Single source of truth (§14): the configured embedding model id IS the
    # embedding-model version recorded with indexed documents.
    from app.rag.indexer import embedding_model_name

    return embedding_model_name()


def _checksum(content: str) -> str:
    return hashlib.sha256(content.encode("utf-8")).hexdigest()


def ingest_document(
    req: IngestDocumentRequest,
    tenant_id: str,
    *,
    store: TenantVectorStore,
    chunker: Callable[..., List[str]] = default_text_chunker,
    force: bool = False,
    embedding_model_version: Optional[str] = None,
    clock: Callable[[], datetime] = lambda: datetime.now(timezone.utc),
) -> IngestResult:
    """Idempotent, version-aware, tenant-scoped ingestion of one document."""
    validate_tenant_id(tenant_id)

    content = req.content.strip()
    if not content:
        raise IngestError("empty_content", "document content is empty")

    checksum = _checksum(req.content)
    existing = store.get_document_meta(tenant_id, req.document_id)

    # Idempotency: identical checksum at an equal-or-newer version is a no-op.
    if existing and not force and existing.checksum == checksum and existing.version >= req.version:
        return IngestResult(
            document_id=req.document_id, version=existing.version, status="duplicate",
            chunk_count=existing.chunk_count, removed_chunks=0, checksum=checksum,
            language=req.language, collection=tenant_collection_name(tenant_id),
            indexed_at=clock().isoformat(),
        )

    # Replace/supersede any prior version of this document (re-index safety).
    removed = store.delete_document(tenant_id, req.document_id) if existing else 0

    pieces = [p for p in chunker(content) if p and p.strip()]
    if not pieces:
        raise IngestError("no_chunks", "document produced no chunks")

    emb_ver = embedding_model_version or _embedding_model_version()
    indexed_at = clock().isoformat()
    records: List[ChunkRecord] = []
    for i, text in enumerate(pieces):
        cid = tenant_chunk_id(tenant_id, f"{req.document_id}_v{req.version}_c{i}")
        records.append(ChunkRecord(
            id=cid,
            text=text,
            metadata={
                "tenant_id": tenant_id,
                "document_id": req.document_id,
                "file_id": req.file_id,
                "academic_year": req.academic_year,
                "grade": req.grade,
                "subject": (req.subject or None) and req.subject.lower(),
                "unit": req.unit,
                "lesson": req.lesson,
                "material_type": req.material_type,
                "language": req.language,
                "version": req.version,
                "checksum": checksum,
                "embedding_model_version": emb_ver,
                "indexed_at": indexed_at,
                "active": True,
                "title": req.title,
                "chunk_id": cid,
                "chunk_index": i,
            },
        ))

    try:
        store.upsert_chunks(tenant_id, records)
    except IngestError:
        raise
    except Exception as exc:  # embedding / vector-store failure
        raise IngestError("embedding_failed", "vector store or embedding failure") from exc

    return IngestResult(
        document_id=req.document_id, version=req.version,
        status="reindexed" if existing else "indexed",
        chunk_count=len(records), removed_chunks=removed, checksum=checksum,
        language=req.language, collection=tenant_collection_name(tenant_id),
        indexed_at=indexed_at,
    )


def delete_document(tenant_id: str, document_id: str, *, store: TenantVectorStore) -> int:
    """Delete all chunks of a document within a tenant. Returns removed count."""
    validate_tenant_id(tenant_id)
    return store.delete_document(tenant_id, document_id)


def document_status(tenant_id: str, document_id: str, *, store: TenantVectorStore):
    """Return the indexed status of a document, or None if not indexed."""
    validate_tenant_id(tenant_id)
    return store.get_document_meta(tenant_id, document_id)
