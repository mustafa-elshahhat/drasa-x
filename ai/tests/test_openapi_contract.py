"""
Phase 22 / XL-03 — the internal backend<->AI v1 contract is captured as a checked-in
OpenAPI snapshot (docs/contracts/ai-internal-v1.json) and this test fails on accidental
drift.

Scope: the CORE internal surface (``/internal/v1/*``) EXCLUDING the optional vision
sub-surface (``/internal/v1/vision/*``), which depends on optional CV deps and is covered
by its own tests — so the snapshot is deterministic regardless of whether CV extras are
installed.

To regenerate intentionally after a real contract change, run from ``ai/``:

    .venv/Scripts/python -c "import sys; sys.path.insert(0,'tests'); \
      from test_openapi_contract import internal_v1_contract, _canonical, CONTRACT_PATH; \
      import app.api as api; CONTRACT_PATH.parent.mkdir(parents=True, exist_ok=True); \
      CONTRACT_PATH.write_text(_canonical(internal_v1_contract(api.app))+chr(10), encoding='utf-8')"
"""
import importlib
import json
from pathlib import Path

CONTRACT_PATH = Path(__file__).resolve().parents[2] / "docs" / "contracts" / "ai-internal-v1.json"


def internal_v1_contract(app) -> dict:
    """Stable, env-independent slice of the OpenAPI doc: core /internal/v1 paths."""
    spec = app.openapi()
    paths = {
        p: spec["paths"][p]
        for p in sorted(spec.get("paths", {}))
        if p.startswith("/internal/v1/") and not p.startswith("/internal/v1/vision")
    }
    return {"openapi": spec.get("openapi"), "paths": paths}


def _canonical(d: dict) -> str:
    return json.dumps(d, indent=2, sort_keys=True, ensure_ascii=False)


def _current_contract() -> dict:
    import app.api as api
    importlib.reload(api)
    return internal_v1_contract(api.app)


def test_internal_v1_openapi_snapshot_exists():
    assert CONTRACT_PATH.exists(), (
        f"missing AI internal OpenAPI snapshot at {CONTRACT_PATH} — regenerate it "
        "(see this module's docstring)."
    )


def test_internal_v1_openapi_matches_snapshot():
    expected = json.loads(CONTRACT_PATH.read_text(encoding="utf-8"))
    current = _current_contract()
    assert _canonical(current) == _canonical(expected), (
        "Internal AI v1 OpenAPI contract drifted from docs/contracts/ai-internal-v1.json. "
        "If the change is intentional, regenerate the snapshot (see this module's docstring)."
    )


def test_internal_v1_core_routes_present():
    current = _current_contract()
    for route in ("/internal/v1/tutor", "/internal/v1/quiz/draft", "/internal/v1/analysis",
                  "/internal/v1/prediction", "/internal/v1/documents"):
        assert route in current["paths"], f"core internal route missing from contract: {route}"
