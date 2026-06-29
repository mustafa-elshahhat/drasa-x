"""
Embedding model accessor for the tenant-scoped vector store (Phase 6 §6/§14).

The global, non-tenant ``build_or_load_vectorstore`` prototype was REMOVED (§18);
the only persisted collections are the per-tenant Chroma collections created by
``app.rag.store.ChromaTenantStore``. This module now exposes a single
configuration-driven embedding accessor whose model id is recorded as the
``embedding-model version`` on indexed documents and on readiness output.
"""
from __future__ import annotations

import os

# Embedding model is configuration-driven; the id doubles as the embedding-model
# version recorded with indexed documents (§14). Default is the local MiniLM.
DEFAULT_EMBEDDING_MODEL = "sentence-transformers/all-MiniLM-L6-v2"


def embedding_model_name() -> str:
    """Return the configured embedding model id (a.k.a. embedding-model version)."""
    return os.environ.get("EMBEDDING_MODEL", DEFAULT_EMBEDDING_MODEL)


# The embedding model (~90MB) is expensive to load. It is created ONCE per
# process and reused — never reloaded per request — so ingestion/retrieval stay
# fast and reliable. (Previously a new model was built on every request, which
# caused multi-second latency and occasional timeouts under load.)
_embeddings = None


def get_embeddings():
    """Return the process-cached local embedding function for the tenant store."""
    global _embeddings
    if _embeddings is None:
        from langchain_huggingface import HuggingFaceEmbeddings

        _embeddings = HuggingFaceEmbeddings(model_name=embedding_model_name())
    return _embeddings
