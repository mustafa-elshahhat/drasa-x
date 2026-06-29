"""
Phase 15 — sequence buffer (tenant/session-scoped) unit tests.
Proves NotReady->Ready behaviour, strict no-overlap reset, tenant isolation,
session clearing, and TTL eviction.
"""
from app.vision.state import SequenceBufferStore


def test_collects_until_sequence_length_then_ready():
    store = SequenceBufferStore(sequence_length=16)
    for i in range(15):
        count = store.add_frame("t1", "s1", "trackA", float(i))
        assert count == i + 1
        assert store.is_ready("t1", "s1", "trackA") is False
        assert store.take_sequence("t1", "s1", "trackA") is None
    count = store.add_frame("t1", "s1", "trackA", 99.0)
    assert count == 16
    assert store.is_ready("t1", "s1", "trackA") is True
    seq = store.take_sequence("t1", "s1", "trackA")
    assert seq is not None and len(seq) == 16
    # strict no-overlap: after taking, the buffer is empty again
    assert store.count("t1", "s1", "trackA") == 0
    assert store.take_sequence("t1", "s1", "trackA") is None


def test_tenant_and_session_isolation():
    store = SequenceBufferStore(sequence_length=16)
    for _ in range(16):
        store.add_frame("t1", "s1", "track", 1.0)
    # Same track id, different tenant or session => independent buffer.
    assert store.is_ready("t1", "s1", "track") is True
    assert store.count("t2", "s1", "track") == 0
    assert store.count("t1", "s2", "track") == 0


def test_clear_session_drops_only_that_session():
    store = SequenceBufferStore(sequence_length=16)
    store.add_frame("t1", "s1", "a", 1.0)
    store.add_frame("t1", "s1", "b", 1.0)
    store.add_frame("t1", "s2", "a", 1.0)
    removed = store.clear_session("t1", "s1")
    assert removed == 2
    assert store.count("t1", "s1", "a") == 0
    assert store.count("t1", "s2", "a") == 1


def test_ttl_eviction():
    store = SequenceBufferStore(sequence_length=16, ttl_seconds=100)
    store.add_frame("t1", "s1", "a", 1.0)
    # nothing evicted yet
    assert store.cleanup(now=_now_of(store) + 50) == 0
    # evict after TTL
    assert store.cleanup(now=_now_of(store) + 1000) == 1
    assert store.size() == 0


def _now_of(store):
    import time
    return time.time()
